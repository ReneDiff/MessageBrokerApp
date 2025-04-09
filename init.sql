-- init.sql
CREATE TABLE IF NOT EXISTS messages (
    id SERIAL PRIMARY KEY,
    timestamp TIMESTAMPTZ NOT NULL,
    counter INT NOT NULL
);

-- Valgfrit: Log at tabellen blev oprettet eller allerede eksisterede
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'messages') THEN
        RAISE NOTICE 'Table "messages" already exists.';
    ELSE
        RAISE NOTICE 'Table "messages" created.';
    END IF;
END $$;