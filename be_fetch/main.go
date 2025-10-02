package main

import (
	"context"
	"fmt"
	slogmulti "github.com/samber/slog-multi"
	"hash/fnv"
	"log/slog"
	"math/rand"
	"os"
	"time"

	"github.com/go-redsync/redsync/v4"
	"github.com/go-redsync/redsync/v4/redis/goredis/v9"
	"github.com/google/uuid"
	"github.com/jackc/pgx/v5"
	"github.com/redis/go-redis/v9"

	"github.com/sokkalf/slog-seq"

    "bufio"
    "strings"
)

func loadSecretsEnv(path string) error {
	file, err := os.Open(path)
	if err != nil {
		return nil // Allow not having a file.
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())

		// Skip empty lines and comments.
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}

		// Split KEY=VALUE.
		parts := strings.SplitN(line, "=", 2)
		if len(parts) != 2 {
			continue // Ignore malformed lines.
		}

		key := strings.TrimSpace(parts[0])
		value := strings.TrimSpace(parts[1])

		// Remove optional surrounding quotes.
		value = strings.Trim(value, `"'`)

		if key != "" {
			if err := os.Setenv(key, value); err != nil {
				return fmt.Errorf("Failed to set env %s: %w", key, err)
			}
		}
	}

	if err := scanner.Err(); err != nil {
		return fmt.Errorf("Error reading secrets file: %w", err)
	}

	return nil
}

func updateHealthCheckFile() {
	filePath := os.Getenv("HEALTHCHECK_FILE")
	if filePath == "" {
		slog.Error("HEALTHCHECK_FILE environment variable is not set")
		return
	}

	for {
		currentTime := time.Now().String()
		err := os.WriteFile(filePath, []byte(currentTime), 0644)
		if err != nil {
			slog.Error("Failed to write to health check file", "error", err)
		}
		time.Sleep(30 * time.Second)
	}
}

func main() {
	if err := loadSecretsEnv("/run/secrets/global-secrets.env"); err != nil {
		fmt.Println("Error:", err)
		os.Exit(1)
	}

	if err := loadSecretsEnv("/run/secrets/secrets.env"); err != nil {
		fmt.Println("Error:", err)
		os.Exit(1)
	}

	podId := uuid.New().String()

	seqApiKey := os.Getenv("SeqApiKey_Fetch")
	seqUri := os.Getenv("SeqUri")
	if seqApiKey == "" || seqUri == "" {
		slog.Error("Failed to get SEQ configuration")
		os.Exit(1)
	}

	redisUrl := os.Getenv("CacheConnectionString")
	if redisUrl == "" {
		slog.Error("Redis connection string is not set")
		os.Exit(1)
	}

	_, seqHandler := slogseq.NewLogger(
		seqUri+"/ingest/clef",
		slogseq.WithAPIKey(seqApiKey),
		slogseq.WithBatchSize(50),
		slogseq.WithFlushInterval(10*time.Second),
		slogseq.WithGlobalAttrs(
			slog.String("pod", podId),
		),
		slogseq.WithHandlerOptions(&slog.HandlerOptions{
			Level:     slog.LevelDebug,
			AddSource: true,
		}))
	defer seqHandler.Close()

	logger := slog.New(
		slogmulti.Fanout(
			seqHandler,
			slog.NewTextHandler(os.Stdout, &slog.HandlerOptions{
				Level:     slog.LevelDebug,
				AddSource: true,
			}).WithAttrs([]slog.Attr{
				slog.String("pod", podId),
			}),
		),
	)

	slog.SetDefault(logger)
	slog.Info("Starting application")

	connectionString := os.Getenv("DATABASE_URL")
	if connectionString == "" {
		slog.Error("Database connection string is not set")
		os.Exit(1)
	}

	config, err := pgx.ParseConfig(connectionString)
	if err != nil {
		slog.Error("Failed to parse connection string", "error", err)
		os.Exit(1)
	}

	db, err := pgx.ConnectConfig(context.Background(), config)
	if err != nil {
		slog.Error("Failed to connect to database", "error", err)
		os.Exit(1)
	}
	defer db.Close(context.Background())

	err = db.Ping(context.Background())
	if err != nil {
		slog.Error("Failed to ping the database", "error", err)
		os.Exit(1)
	}

	client := redis.NewClient(&redis.Options{
		Addr: redisUrl,
		DB:   1,
	})
	defer client.Close()

	err = client.Ping(context.Background()).Err()
	if err != nil {
		slog.Error("Failed to ping Redis", "error", err)
		os.Exit(1)
	}

	pool := goredis.NewPool(client)
	rs := redsync.New(pool)

	go updateHealthCheckFile()
	runLoop(db, client, rs)
}

func runLoop(db *pgx.Conn, rdb *redis.Client, rs *redsync.Redsync) {
	// TODO: Implement changing this list.
	teamInfos := []string{
		"KECATS", "SSH",
	}

	for {
		for _, team := range teamInfos {
			// Make sure this timeout is on the top - to wait between multiple team requests.
			time.Sleep(time.Duration(1500+rand.Intn(3500)) * time.Millisecond)

			mutex := rs.NewMutex("tnt_team_lock:"+team, redsync.WithExpiry(4*time.Minute))
			if err := mutex.TryLock(); err != nil {
				slog.Debug("Could not acquire team lock, skipping", "error", err, "team", team)
				continue
			}

			json, err := FetchTeamData("https://nitrotype.com", team)
			if err != nil {
				slog.Error("Failed to fetch data from API", "error", err)
				// TODO: Unlock, unless 429.
				//mutex.Unlock() // Do not unlock the mutex on failure, it might have been 429.
				continue
			}

			// FNV is faster than SHA. We do not need cryptographic security here.
			hasher := fnv.New32a()
			_, _ = hasher.Write(json)
			newHash := hasher.Sum32()
			newHashStr := fmt.Sprintf("%d", newHash)

			oldHashStr, err := rdb.Get(context.Background(), "tnt_team_data_hash:"+team).Result()
			if err != nil && err != redis.Nil {
				slog.Debug("Did not get hash value from redis, creating a new one", "error", err)
				oldHashStr = "_mock_"
			}

			if newHashStr == oldHashStr {
				slog.Info("Hash did not change for the team, skipping saving the data", "team", team)
				continue
			}

			err = StoreTeamData(db, team, json)
			if err != nil {
				slog.Error("Failed to store data in the database", "error", err)

				time.Sleep(30 * time.Second) // Wait before restarting.
				os.Exit(1)                   // Exit the pod, so that the orchestrator can restart it and reconnect to db.

				//mutex.Unlock()
				continue
			}

			err = rdb.Set(context.Background(), "tnt_team_data_hash:"+team, newHashStr, 0).Err()
			if err != nil {
				slog.Error("Failed to store hash in Redis", "error", err)

				time.Sleep(30 * time.Second) // Wait before restarting.
				os.Exit(1)                   // Exit the pod, so that the orchestrator can restart it and reconnect to db.

				//mutex.Unlock()
				continue
			}

			slog.Info("Successfully finished processing (getting/saving) team data", "team", team)
		}

		time.Sleep(time.Duration(10000+rand.Intn(20000)) * time.Millisecond)
	}
}
