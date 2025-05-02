package main

import (
	"fmt"
	"io"
	"log/slog"
	"net/http"
)

func FetchTeamData(team string) ([]byte, error) {
	slog.Info("Fetching team data from NitroType API", "team", team)

	client := &http.Client{}
	url := fmt.Sprintf("https://nitrotype.com/api/v2/teams/%s", team)

	resp, err := client.Get(url)
	err = isSuccess(resp, err)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		slog.Error("Failed to read response body", "error", err)
		return nil, err
	}

	slog.Info("Successfully fetched team data", "size", len(body), "team", team)
	return body, nil
}

func isSuccess(resp *http.Response, err error) error {
	if err != nil {
		slog.Error("HTTP request failed", "error", err)
		return err
	}

	if resp.StatusCode != 200 {
		err := fmt.Errorf("HTTP request failed with status code: %d", resp.StatusCode)
		slog.Error("HTTP request failed", "error", err)
		return err
	}

	return nil
}
