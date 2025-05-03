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
	"github.com/jackc/pgx/v5"
	"github.com/redis/go-redis/v9"

	"github.com/sokkalf/slog-seq"
)

func main() {
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

	runLoop(db, client, rs)
}

func runLoop(db *pgx.Conn, rdb *redis.Client, rs *redsync.Redsync) {
	// TODO: Implement changing this list.
	teamInfos := []string{
		"KECATS", "SSH",
	}

	for {
		for _, team := range teamInfos {
			mutex := rs.NewMutex("tnt_team_lock:"+team, redsync.WithExpiry(4*time.Minute))
			if err := mutex.TryLock(); err != nil {
				slog.Debug("Failed to acquire Redis lock, skipping", "error", err, "team", team)
				continue
			}

			json, err := FetchTeamData(team)
			if err != nil {
				slog.Error("Failed to fetch data from API", "error", err)
				mutex.Unlock()
				continue
			}

			// FNV is faster than SHA. We do not need cryptographic security here.
			hasher := fnv.New32a()
			_, _ = hasher.Write(json)
			newHash := hasher.Sum32()
			newHashStr := fmt.Sprintf("%d", newHash)

			oldHashStr, err := rdb.Get(context.Background(), "tnt_team_data_hash:"+team).Result()
			slog.Error("test")
			slog.Error(oldHashStr)
			if err != nil && err != redis.Nil {
				slog.Error("test")
				os.Exit(1)
				slog.Debug("Did not get hash value from redis, creating a new one", "error", err)
			}

			if newHashStr == oldHashStr {
				slog.Info("Hash did not change for the team, skipping saving the data", "team", team)
				continue
			}

			err = StoreTeamData(db, team, json)
			if err != nil {
				slog.Error("Failed to store data in the database", "error", err)
				mutex.Unlock()
				continue
			}

			err = rdb.Set(context.Background(), "tnt_team_data_hash:"+team, newHashStr, 0).Err()
			if err != nil {
				slog.Error("Failed to store hash in Redis", "error", err)
				mutex.Unlock()
				continue
			}

			slog.Info("Successfully finished processing (getting/saving) team data", "team", team)

			time.Sleep(time.Duration(1500+rand.Intn(2500)) * time.Millisecond)
		}

		time.Sleep(time.Duration(10000+rand.Intn(20000)) * time.Millisecond)
	}
}
