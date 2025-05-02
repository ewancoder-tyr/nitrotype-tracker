package main

import (
	"context"
	slogmulti "github.com/samber/slog-multi"
	"hash/fnv"
	"log/slog"
	"math/rand"
	"os"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/sokkalf/slog-seq"
)

func main() {
	seqApiKey := os.Getenv("SeqApiKey_Fetch")
	seqUri := os.Getenv("SeqUri")
	if seqApiKey == "" || seqUri == "" {
		slog.Error("Failed to get SEQ configuration")
		os.Exit(1)
	}

	_, seqHandler := slogseq.NewLogger(
		seqUri+"/ingest/clef",
		slogseq.WithAPIKey(seqApiKey),
		slogseq.WithBatchSize(50),
		slogseq.WithFlushInterval(10*time.Second))
	defer seqHandler.Close()

	logger := slog.New(
		slogmulti.Fanout(
			seqHandler,
			slog.NewTextHandler(os.Stdout, &slog.HandlerOptions{}),
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

	conn, err := pgx.ConnectConfig(context.Background(), config)
	if err != nil {
		slog.Error("Failed to connect to database", "error", err)
		os.Exit(1)
	}
	defer conn.Close(context.Background())

	err = conn.Ping(context.Background())
	if err != nil {
		slog.Error("Failed to ping the database", "error", err)
		os.Exit(1)
	}

	runLoop(conn)
}

type TeamInfo struct {
	name          string
	lastQueryTime time.Time
	hash          uint32
}

func runLoop(conn *pgx.Conn) {
	// TODO: Implement changing this list.
	teamInfos := []TeamInfo{
		{name: "KECATS", lastQueryTime: time.Time{}, hash: 0},
		{name: "SSH", lastQueryTime: time.Time{}, hash: 0},
	}

	for {
		for i, team := range teamInfos {
			if time.Since(team.lastQueryTime) < 5*time.Minute {
				continue
			}

			json, err := FetchTeamData(team.name)
			if err != nil {
				slog.Error("Failed to fetch data from API", "error", err)
				continue
			}

			// FNV is faster than SHA. We do not need cryptographic security here.
			hasher := fnv.New32a()
			_, _ = hasher.Write(json)
			newHash := hasher.Sum32()

			if newHash == team.hash {
				slog.Info("Hash did not change for the team, skipping saving the data", "team", team.name)
				teamInfos[i].lastQueryTime = time.Now().UTC()
				continue
			}

			err = StoreTeamData(conn, team.name, json)
			if err != nil {
				slog.Error("Failed to store data in the database", "error", err)
				continue
			}

			teamInfos[i].lastQueryTime = time.Now().UTC()
			teamInfos[i].hash = newHash
			slog.Info("Successfully finished processing (getting/saving) team data", "team", team.name)

			time.Sleep(time.Duration(1500+rand.Intn(2500)) * time.Millisecond)
		}

		time.Sleep(time.Duration(5000+rand.Intn(10000)) * time.Millisecond)
	}
}
