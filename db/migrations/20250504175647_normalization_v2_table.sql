-- migrate:up
CREATE TABLE IF NOT EXISTS normalized_data_v2 (
     id BIGSERIAL PRIMARY KEY,
     username VARCHAR(50) NOT NULL,
     team VARCHAR(50) NOT NULL,
     typed BIGINT NOT NULL,
     errors BIGINT NOT NULL,
     name VARCHAR(100) NOT NULL,
     races_played INT NOT NULL,
     timestamp TIMESTAMP NOT NULL,
     secs BIGINT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_normalized_data_v2_team ON normalized_data_v2(team);
CREATE UNIQUE INDEX IF NOT EXISTS idx_normalized_data_v2_username_timestamp
    ON normalized_data_v2(timestamp, username);

-- migrate:down
-- We do not want to lose the data accidentally, do not write down here.
