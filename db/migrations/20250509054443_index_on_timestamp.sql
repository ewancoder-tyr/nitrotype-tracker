-- migrate:up
CREATE INDEX IF NOT EXISTS idx_normalized_data_v2_timestamp ON normalized_data_v2(timestamp);

-- migrate:down
DROP INDEX idx_normalized_data_v2_timestamp;
