package main

import (
	"context"
	"github.com/jackc/pgx/v5"
	"log/slog"
)

func StoreTeamData(conn *pgx.Conn, team string, data []byte) error {
	slog.Info("Storing JSON data in database")

	_, err := conn.Exec(context.Background(), "INSERT INTO raw_data (team, data, timestamp) VALUES ($1, $2, NOW() AT TIME ZONE 'utc')", team, string(data))
	if err != nil {
		slog.Error("Failed to insert data", "error", err)
	}
	return err
}
