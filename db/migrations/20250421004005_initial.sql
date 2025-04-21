-- migrate:up

CREATE TABLE IF NOT EXISTS "raw_data" (
    "team" VARCHAR(50),
    "data" VARCHAR,
    "timestamp" timestamp,
    "id" BIGSERIAL PRIMARY KEY
);

CREATE TABLE IF NOT EXISTS "normalized_data" (
    "id" BIGSERIAL PRIMARY KEY,
    "username" VARCHAR(50) NOT NULL,
    "team" VARCHAR(50) NOT NULL,
    "typed" BIGINT NOT NULL,
    "errors" BIGINT NOT NULL,
    "name" VARCHAR(100) NOT NULL,
    "races_played" INT NOT NULL,
    "timestamp" TIMESTAMP NOT NULL,
    "secs" BIGINT NOT NULL
);

CREATE TABLE IF NOT EXISTS "processing_state" (
    "id" INT PRIMARY KEY DEFAULT 1, -- We will only have one row.
    "last_processed_id" BIGINT NOT NULL DEFAULT 0,
    "last_updated" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Insert the initial record if it doesn't exist.
INSERT INTO "processing_state" ("id", "last_processed_id")
VALUES (1, 0)
ON CONFLICT (id) DO NOTHING;

CREATE INDEX IF NOT EXISTS idx_normalized_data_team ON normalized_data(team);
CREATE INDEX IF NOT EXISTS idx_normalized_data_timestamp ON normalized_data(timestamp);
CREATE INDEX IF NOT EXISTS idx_normalized_data_username ON normalized_data(username);
CREATE UNIQUE INDEX IF NOT EXISTS idx_normalized_data_username_timestamp
    ON normalized_data(username, timestamp);

-- migrate:down

-- We do not need migration down on initial migration.
