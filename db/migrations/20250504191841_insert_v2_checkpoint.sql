-- migrate:up

INSERT INTO "processing_state" ("id", "last_processed_id")
VALUES (2, 0)
ON CONFLICT (id) DO NOTHING;

-- migrate:down
