CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'movie_status') THEN
        CREATE TYPE movie_status AS ENUM ('coming_soon', 'now_showing', 'ended');
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'screen_type') THEN
        CREATE TYPE screen_type AS ENUM ('standard', 'imax', 'vip');
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'seat_class') THEN
        CREATE TYPE seat_class AS ENUM ('standard', 'premium', 'vip');
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'booking_status') THEN
        CREATE TYPE booking_status AS ENUM ('pending', 'confirmed', 'cancelled');
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'ticket_type') THEN
        CREATE TYPE ticket_type AS ENUM ('adult', 'child', 'senior');
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS movies (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    title VARCHAR(300) NOT NULL,
    description TEXT,
    duration_mins INT,
    rating VARCHAR(10),
    genre TEXT[],
    poster_url TEXT,
    trailer_url TEXT,
    status movie_status NOT NULL DEFAULT 'coming_soon',
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT movies_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS screens (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    screen_type screen_type NOT NULL DEFAULT 'standard',
    is_active BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT screens_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS seats (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    screen_id UUID NOT NULL,
    row_label VARCHAR(5) NOT NULL,
    seat_number INT NOT NULL,
    seat_class seat_class NOT NULL DEFAULT 'standard',
    is_active BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT seats_pkey PRIMARY KEY (id),
    CONSTRAINT seats_screen_id_fkey FOREIGN KEY (screen_id) REFERENCES screens (id),
    CONSTRAINT seats_screen_row_seat_unique UNIQUE (screen_id, row_label, seat_number)
);

CREATE TABLE IF NOT EXISTS showtimes (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    movie_id UUID NOT NULL,
    screen_id UUID NOT NULL,
    starts_at TIMESTAMPTZ NOT NULL,
    ends_at TIMESTAMPTZ NOT NULL,
    base_price NUMERIC(8,2) NOT NULL,
    is_cancelled BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT showtimes_pkey PRIMARY KEY (id),
    CONSTRAINT showtimes_movie_id_fkey FOREIGN KEY (movie_id) REFERENCES movies (id),
    CONSTRAINT showtimes_screen_id_fkey FOREIGN KEY (screen_id) REFERENCES screens (id),
    CONSTRAINT showtimes_screen_starts_unique UNIQUE (screen_id, starts_at)
);

CREATE TABLE IF NOT EXISTS addons (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    price NUMERIC(8,2) NOT NULL,
    image_url TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT addons_pkey PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS bookings (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    showtime_id UUID NOT NULL,
    status booking_status NOT NULL DEFAULT 'pending',
    tickets_cost NUMERIC(10,2) NOT NULL,
    addons_cost NUMERIC(10,2) NOT NULL DEFAULT 0,
    total_cost NUMERIC(10,2) NOT NULL,
    confirmed_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT bookings_pkey PRIMARY KEY (id),
    CONSTRAINT bookings_showtime_id_fkey FOREIGN KEY (showtime_id) REFERENCES showtimes (id)
);

CREATE TABLE IF NOT EXISTS tickets (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    booking_id UUID NOT NULL,
    seat_id UUID NOT NULL,
    ticket_type ticket_type NOT NULL DEFAULT 'adult',
    unit_price NUMERIC(8,2) NOT NULL,
    qr_code TEXT UNIQUE,
    is_scanned BOOLEAN NOT NULL DEFAULT false,
    scanned_at TIMESTAMPTZ,
    CONSTRAINT tickets_pkey PRIMARY KEY (id),
    CONSTRAINT tickets_booking_id_fkey FOREIGN KEY (booking_id) REFERENCES bookings (id),
    CONSTRAINT tickets_seat_id_fkey FOREIGN KEY (seat_id) REFERENCES seats (id),
    CONSTRAINT tickets_booking_seat_unique UNIQUE (booking_id, seat_id)
);

CREATE TABLE IF NOT EXISTS booking_addons (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    booking_id UUID NOT NULL,
    addon_id UUID NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    unit_price NUMERIC(8,2) NOT NULL,
    CONSTRAINT booking_addons_pkey PRIMARY KEY (id),
    CONSTRAINT booking_addons_booking_id_fkey FOREIGN KEY (booking_id) REFERENCES bookings (id),
    CONSTRAINT booking_addons_addon_id_fkey FOREIGN KEY (addon_id) REFERENCES addons (id)
);

CREATE TABLE IF NOT EXISTS seat_holds (
    id UUID NOT NULL DEFAULT gen_random_uuid(),
    showtime_id UUID NOT NULL,
    seat_id UUID NOT NULL,
    user_id UUID NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT seat_holds_pkey PRIMARY KEY (id),
    CONSTRAINT seat_holds_showtime_id_fkey FOREIGN KEY (showtime_id) REFERENCES showtimes (id),
    CONSTRAINT seat_holds_seat_id_fkey FOREIGN KEY (seat_id) REFERENCES seats (id),
    CONSTRAINT seat_holds_showtime_seat_unique UNIQUE (showtime_id, seat_id)
);

INSERT INTO movies (title, description, duration_mins, rating, genre, poster_url, trailer_url, status)
SELECT
    'Jurassic Park',
    'During a preview tour, a theme park suffers a major power breakdown that allows cloned dinosaurs to run loose.',
    127,
    'PG-13',
    ARRAY['Adventure', 'Sci-Fi'],
    'https://upload.wikimedia.org/wikipedia/en/e/e7/Jurassic_Park_poster.jpg',
    '',
    'now_showing'
WHERE NOT EXISTS (SELECT 1 FROM movies WHERE title = 'Jurassic Park');

INSERT INTO movies (title, description, duration_mins, rating, genre, poster_url, trailer_url, status)
SELECT
    'The Lost World: Jurassic Park',
    'A research team travels to Isla Sorna where dinosaurs still live in the wild.',
    129,
    'PG-13',
    ARRAY['Adventure', 'Sci-Fi'],
    'https://upload.wikimedia.org/wikipedia/en/c/cc/The_Lost_World_%E2%80%93_Jurassic_Park_poster.jpg',
    '',
    'now_showing'
WHERE NOT EXISTS (SELECT 1 FROM movies WHERE title = 'The Lost World: Jurassic Park');

INSERT INTO movies (title, description, duration_mins, rating, genre, poster_url, trailer_url, status)
SELECT
    'Jurassic Park III',
    'Dr. Alan Grant joins a mission to Isla Sorna and faces dangerous dinosaurs again.',
    92,
    'PG-13',
    ARRAY['Adventure', 'Sci-Fi'],
    'https://upload.wikimedia.org/wikipedia/en/6/6d/Jurassic_Park_III_poster.jpg',
    '',
    'now_showing'
WHERE NOT EXISTS (SELECT 1 FROM movies WHERE title = 'Jurassic Park III');

INSERT INTO movies (title, description, duration_mins, rating, genre, poster_url, trailer_url, status)
SELECT
    'Jurassic World',
    'A new dinosaur theme park is open, but a genetically modified dinosaur escapes.',
    124,
    'PG-13',
    ARRAY['Adventure', 'Action', 'Sci-Fi'],
    'https://upload.wikimedia.org/wikipedia/en/6/6e/Jurassic_World_poster.jpg',
    '',
    'now_showing'
WHERE NOT EXISTS (SELECT 1 FROM movies WHERE title = 'Jurassic World');

INSERT INTO movies (title, description, duration_mins, rating, genre, poster_url, trailer_url, status)
SELECT
    'Jurassic World: Fallen Kingdom',
    'The team returns to save dinosaurs from a volcanic disaster and a new threat.',
    128,
    'PG-13',
    ARRAY['Adventure', 'Action', 'Sci-Fi'],
    'https://upload.wikimedia.org/wikipedia/en/c/c6/Jurassic_World_Fallen_Kingdom.png',
    '',
    'now_showing'
WHERE NOT EXISTS (SELECT 1 FROM movies WHERE title = 'Jurassic World: Fallen Kingdom');

INSERT INTO movies (title, description, duration_mins, rating, genre, poster_url, trailer_url, status)
SELECT
    'Jurassic World Dominion',
    'Humans and dinosaurs must learn to live together in the modern world.',
    147,
    'PG-13',
    ARRAY['Adventure', 'Action', 'Sci-Fi'],
    'https://upload.wikimedia.org/wikipedia/en/c/ce/JurassicWorldDominion_Poster.jpeg',
    '',
    'now_showing'
WHERE NOT EXISTS (SELECT 1 FROM movies WHERE title = 'Jurassic World Dominion');

-- Backfill poster URLs for databases that were initialized from older or broken seed data.
UPDATE movies
SET poster_url = 'https://upload.wikimedia.org/wikipedia/en/e/e7/Jurassic_Park_poster.jpg'
WHERE title = 'Jurassic Park'
  AND (COALESCE(poster_url, '') = '' OR poster_url LIKE 'https://m.media-amazon.com/%');

UPDATE movies
SET poster_url = 'https://upload.wikimedia.org/wikipedia/en/c/cc/The_Lost_World_%E2%80%93_Jurassic_Park_poster.jpg'
WHERE title = 'The Lost World: Jurassic Park'
  AND (COALESCE(poster_url, '') = '' OR poster_url LIKE 'https://m.media-amazon.com/%');

UPDATE movies
SET poster_url = 'https://upload.wikimedia.org/wikipedia/en/6/6d/Jurassic_Park_III_poster.jpg'
WHERE title = 'Jurassic Park III'
  AND (COALESCE(poster_url, '') = '' OR poster_url LIKE 'https://m.media-amazon.com/%');

UPDATE movies
SET poster_url = 'https://upload.wikimedia.org/wikipedia/en/6/6e/Jurassic_World_poster.jpg'
WHERE title = 'Jurassic World'
  AND (COALESCE(poster_url, '') = '' OR poster_url LIKE 'https://m.media-amazon.com/%');

UPDATE movies
SET poster_url = 'https://upload.wikimedia.org/wikipedia/en/c/c6/Jurassic_World_Fallen_Kingdom.png'
WHERE title = 'Jurassic World: Fallen Kingdom'
  AND (COALESCE(poster_url, '') = '' OR poster_url LIKE 'https://m.media-amazon.com/%');

UPDATE movies
SET poster_url = 'https://upload.wikimedia.org/wikipedia/en/c/ce/JurassicWorldDominion_Poster.jpeg'
WHERE title = 'Jurassic World Dominion'
  AND (COALESCE(poster_url, '') = '' OR poster_url LIKE 'https://m.media-amazon.com/%');

INSERT INTO screens (name, screen_type, is_active)
SELECT 'Raptor Hall', 'standard', true
WHERE NOT EXISTS (SELECT 1 FROM screens WHERE name = 'Raptor Hall');

INSERT INTO screens (name, screen_type, is_active)
SELECT 'T-Rex Theater', 'vip', true
WHERE NOT EXISTS (SELECT 1 FROM screens WHERE name = 'T-Rex Theater');

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic Park' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1),
    (CURRENT_DATE + TIME '14:00') AT TIME ZONE 'UTC',
    (CURRENT_DATE + TIME '16:07') AT TIME ZONE 'UTC',
    13.99
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1)
      AND starts_at = ((CURRENT_DATE + TIME '14:00') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic Park' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1),
    (CURRENT_DATE + TIME '18:00') AT TIME ZONE 'UTC',
    (CURRENT_DATE + TIME '20:07') AT TIME ZONE 'UTC',
    15.99
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1)
      AND starts_at = ((CURRENT_DATE + TIME '18:00') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic World' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1),
    (CURRENT_DATE + TIME '17:15') AT TIME ZONE 'UTC',
    (CURRENT_DATE + TIME '19:19') AT TIME ZONE 'UTC',
    17.49
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1)
      AND starts_at = ((CURRENT_DATE + TIME '17:15') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic World' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1),
    (CURRENT_DATE + TIME '20:30') AT TIME ZONE 'UTC',
    (CURRENT_DATE + TIME '22:34') AT TIME ZONE 'UTC',
    18.99
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1)
      AND starts_at = ((CURRENT_DATE + TIME '20:30') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'The Lost World: Jurassic Park' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1),
    ((CURRENT_DATE + 1) + TIME '13:15') AT TIME ZONE 'UTC',
    ((CURRENT_DATE + 1) + TIME '15:24') AT TIME ZONE 'UTC',
    13.99
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1)
      AND starts_at = (((CURRENT_DATE + 1) + TIME '13:15') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'The Lost World: Jurassic Park' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1),
    ((CURRENT_DATE + 1) + TIME '16:30') AT TIME ZONE 'UTC',
    ((CURRENT_DATE + 1) + TIME '18:39') AT TIME ZONE 'UTC',
    14.99
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1)
      AND starts_at = (((CURRENT_DATE + 1) + TIME '16:30') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic Park III' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1),
    ((CURRENT_DATE + 1) + TIME '17:00') AT TIME ZONE 'UTC',
    ((CURRENT_DATE + 1) + TIME '18:32') AT TIME ZONE 'UTC',
    16.49
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1)
      AND starts_at = (((CURRENT_DATE + 1) + TIME '17:00') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic Park III' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1),
    ((CURRENT_DATE + 1) + TIME '20:15') AT TIME ZONE 'UTC',
    ((CURRENT_DATE + 1) + TIME '21:47') AT TIME ZONE 'UTC',
    17.49
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1)
      AND starts_at = (((CURRENT_DATE + 1) + TIME '20:15') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic World: Fallen Kingdom' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1),
    ((CURRENT_DATE + 2) + TIME '15:15') AT TIME ZONE 'UTC',
    ((CURRENT_DATE + 2) + TIME '17:23') AT TIME ZONE 'UTC',
    15.99
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1)
      AND starts_at = (((CURRENT_DATE + 2) + TIME '15:15') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic World: Fallen Kingdom' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1),
    ((CURRENT_DATE + 2) + TIME '18:45') AT TIME ZONE 'UTC',
    ((CURRENT_DATE + 2) + TIME '20:53') AT TIME ZONE 'UTC',
    16.49
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1)
      AND starts_at = (((CURRENT_DATE + 2) + TIME '18:45') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic World Dominion' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1),
    ((CURRENT_DATE + 2) + TIME '16:15') AT TIME ZONE 'UTC',
    ((CURRENT_DATE + 2) + TIME '18:42') AT TIME ZONE 'UTC',
    16.49
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1)
      AND starts_at = (((CURRENT_DATE + 2) + TIME '16:15') AT TIME ZONE 'UTC')
);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
SELECT
    (SELECT id FROM movies WHERE title = 'Jurassic World Dominion' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1),
    ((CURRENT_DATE + 2) + TIME '19:30') AT TIME ZONE 'UTC',
    ((CURRENT_DATE + 2) + TIME '21:57') AT TIME ZONE 'UTC',
    17.99
WHERE NOT EXISTS (
    SELECT 1
    FROM showtimes
    WHERE screen_id = (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1)
      AND starts_at = (((CURRENT_DATE + 2) + TIME '19:30') AT TIME ZONE 'UTC')
);
