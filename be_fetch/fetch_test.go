package main

import (
	"net/http"
	"net/http/httptest"
	"testing"
)

func TestGetFixedValue(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/api/v2/teams/MYTEAM" {
			t.Errorf("Expected to request '/fixedvalue', got: %s", r.URL.Path)
		}
		/*if r.Header.Get("Accept") != "application/json" {
			t.Errorf("Expected Accept: application/json header, got: %s", r.Header.Get("Accept"))
		}*/
		w.WriteHeader(http.StatusOK)
		w.Write([]byte(`{"test":"value"}`))
	}))
	defer server.Close()

	value, _ := FetchTeamData(server.URL, "MYTEAM")
	if string(value) != `{"test":"value"}` {
		t.Errorf("Expected http value did not match. Got %s", value)
	}
}
