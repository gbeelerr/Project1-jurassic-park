-- ============================================================
--  PostgreSQL Database Dump
--  Generated from: apidb.dbml
--  https://dbdiagram.io/d/apidb-69bb4528fb2db18e3bb5109d
-- ============================================================
 
SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;
 
-- Enable pgcrypto for gen_random_uuid() if not already enabled
CREATE EXTENSION IF NOT EXISTS pgcrypto;
 
-- ============================================================
--  ENUMS
-- ============================================================
 
CREATE TYPE movie_status AS ENUM (
  'coming_soon',
  'now_showing',
  'ended'
);
 
CREATE TYPE screen_type AS ENUM (
  'standard',
  'imax',
  'vip'
);
 
CREATE TYPE seat_class AS ENUM (
  'standard',
  'premium',
  'vip'
);
 
CREATE TYPE booking_status AS ENUM (
  'pending',
  'confirmed',
  'cancelled'
);
 
CREATE TYPE ticket_type AS ENUM (
  'adult',
  'child',
  'senior'
);
 
-- ============================================================
--  MOVIES
-- ============================================================
 
CREATE TABLE movies (
  id            UUID          NOT NULL DEFAULT gen_random_uuid(),
  title         VARCHAR(300)  NOT NULL,
  description   TEXT,
  duration_mins INT,
  rating        VARCHAR(10),
  genre         TEXT[],
  poster_url    TEXT,
  trailer_url   TEXT,
  status        movie_status  NOT NULL DEFAULT 'coming_soon',
  created_at    TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ   NOT NULL DEFAULT now(),
 
  CONSTRAINT movies_pkey PRIMARY KEY (id)
);
 
-- ============================================================
--  SCREENS & SEATS
-- ============================================================
 
CREATE TABLE screens (
  id          UUID          NOT NULL DEFAULT gen_random_uuid(),
  name        VARCHAR(100)  NOT NULL,
  screen_type screen_type   NOT NULL DEFAULT 'standard',
  is_active   BOOLEAN       NOT NULL DEFAULT true,
 
  CONSTRAINT screens_pkey PRIMARY KEY (id)
);
 
CREATE TABLE seats (
  id          UUID        NOT NULL DEFAULT gen_random_uuid(),
  screen_id   UUID        NOT NULL,
  row_label   VARCHAR(5)  NOT NULL,
  seat_number INT         NOT NULL,
  seat_class  seat_class  NOT NULL DEFAULT 'standard',
  is_active   BOOLEAN     NOT NULL DEFAULT true,
 
  CONSTRAINT seats_pkey PRIMARY KEY (id),
  CONSTRAINT seats_screen_id_fkey FOREIGN KEY (screen_id)
    REFERENCES screens (id),
  CONSTRAINT seats_screen_row_seat_unique UNIQUE (screen_id, row_label, seat_number)
);
 
-- ============================================================
--  SHOWTIMES
-- ============================================================
 
CREATE TABLE showtimes (
  id           UUID          NOT NULL DEFAULT gen_random_uuid(),
  movie_id     UUID          NOT NULL,
  screen_id    UUID          NOT NULL,
  starts_at    TIMESTAMPTZ   NOT NULL,
  ends_at      TIMESTAMPTZ   NOT NULL,
  base_price   NUMERIC(8,2)  NOT NULL,
  is_cancelled BOOLEAN       NOT NULL DEFAULT false,
  created_at   TIMESTAMPTZ   NOT NULL DEFAULT now(),
 
  CONSTRAINT showtimes_pkey PRIMARY KEY (id),
  CONSTRAINT showtimes_movie_id_fkey FOREIGN KEY (movie_id)
    REFERENCES movies (id),
  CONSTRAINT showtimes_screen_id_fkey FOREIGN KEY (screen_id)
    REFERENCES screens (id),
  CONSTRAINT showtimes_screen_starts_unique UNIQUE (screen_id, starts_at)
);
 
-- ============================================================
--  ADD-ONS
-- ============================================================
 
CREATE TABLE addons (
  id          UUID          NOT NULL DEFAULT gen_random_uuid(),
  name        VARCHAR(100)  NOT NULL,
  description TEXT,
  price       NUMERIC(8,2)  NOT NULL,
  image_url   TEXT,
  is_active   BOOLEAN       NOT NULL DEFAULT true,
  created_at  TIMESTAMPTZ   NOT NULL DEFAULT now(),
 
  CONSTRAINT addons_pkey PRIMARY KEY (id)
);
 
-- ============================================================
--  BOOKINGS & TICKETS
-- ============================================================
 
CREATE TABLE bookings (
  id            UUID            NOT NULL DEFAULT gen_random_uuid(),
  user_id       UUID            NOT NULL,  -- ref to auth api users.id
  showtime_id   UUID            NOT NULL,
  status        booking_status  NOT NULL DEFAULT 'pending',
  tickets_cost  NUMERIC(10,2)   NOT NULL,
  addons_cost   NUMERIC(10,2)   NOT NULL DEFAULT 0,
  total_cost    NUMERIC(10,2)   NOT NULL,
  confirmed_at  TIMESTAMPTZ,
  cancelled_at  TIMESTAMPTZ,
  created_at    TIMESTAMPTZ     NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ     NOT NULL DEFAULT now(),
 
  CONSTRAINT bookings_pkey PRIMARY KEY (id),
  CONSTRAINT bookings_showtime_id_fkey FOREIGN KEY (showtime_id)
    REFERENCES showtimes (id)
);
 
CREATE TABLE tickets (
  id          UUID          NOT NULL DEFAULT gen_random_uuid(),
  booking_id  UUID          NOT NULL,
  seat_id     UUID          NOT NULL,
  ticket_type ticket_type   NOT NULL DEFAULT 'adult',
  unit_price  NUMERIC(8,2)  NOT NULL,
  qr_code     TEXT          UNIQUE,
  is_scanned  BOOLEAN       NOT NULL DEFAULT false,
  scanned_at  TIMESTAMPTZ,
 
  CONSTRAINT tickets_pkey PRIMARY KEY (id),
  CONSTRAINT tickets_booking_id_fkey FOREIGN KEY (booking_id)
    REFERENCES bookings (id),
  CONSTRAINT tickets_seat_id_fkey FOREIGN KEY (seat_id)
    REFERENCES seats (id),
  CONSTRAINT tickets_booking_seat_unique UNIQUE (booking_id, seat_id)
);
 
CREATE TABLE booking_addons (
  id         UUID          NOT NULL DEFAULT gen_random_uuid(),
  booking_id UUID          NOT NULL,
  addon_id   UUID          NOT NULL,
  quantity   INT           NOT NULL DEFAULT 1,
  unit_price NUMERIC(8,2)  NOT NULL,
 
  CONSTRAINT booking_addons_pkey PRIMARY KEY (id),
  CONSTRAINT booking_addons_booking_id_fkey FOREIGN KEY (booking_id)
    REFERENCES bookings (id),
  CONSTRAINT booking_addons_addon_id_fkey FOREIGN KEY (addon_id)
    REFERENCES addons (id)
);
 
CREATE TABLE seat_holds (
  id          UUID        NOT NULL DEFAULT gen_random_uuid(),
  showtime_id UUID        NOT NULL,
  seat_id     UUID        NOT NULL,
  user_id     UUID        NOT NULL,
  expires_at  TIMESTAMPTZ NOT NULL,
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
 
  CONSTRAINT seat_holds_pkey PRIMARY KEY (id),
  CONSTRAINT seat_holds_showtime_id_fkey FOREIGN KEY (showtime_id)
    REFERENCES showtimes (id),
  CONSTRAINT seat_holds_seat_id_fkey FOREIGN KEY (seat_id)
    REFERENCES seats (id),
  CONSTRAINT seat_holds_showtime_seat_unique UNIQUE (showtime_id, seat_id)
);

INSERT INTO movies
    (title, description, duration_mins, rating, genre, poster_url, trailer_url, status)
VALUES
    (
        'Jurassic Park',
        'During a preview tour, a theme park suffers a major power breakdown that allows cloned dinosaurs to run loose.',
        127,
        'PG-13',
        ARRAY['Adventure', 'Sci-Fi'],
        'https://upload.wikimedia.org/wikipedia/en/e/e7/Jurassic_Park_poster.jpg',
        '',
        'now_showing'
    ),
    (
        'The Lost World: Jurassic Park',
        'A research team travels to Isla Sorna where dinosaurs still live in the wild.',
        129,
        'PG-13',
        ARRAY['Adventure', 'Sci-Fi'],
        'https://upload.wikimedia.org/wikipedia/en/c/cc/The_Lost_World_%E2%80%93_Jurassic_Park_poster.jpg',
        '',
        'now_showing'
    ),
    (
        'Jurassic Park III',
        'Dr. Alan Grant joins a mission to Isla Sorna and faces dangerous dinosaurs again.',
        92,
        'PG-13',
        ARRAY['Adventure', 'Sci-Fi'],
        'https://upload.wikimedia.org/wikipedia/en/6/6d/Jurassic_Park_III_poster.jpg',
        '',
        'now_showing'
    ),
    (
        'Jurassic World',
        'A new dinosaur theme park is open, but a genetically modified dinosaur escapes.',
        124,
        'PG-13',
        ARRAY['Adventure', 'Action', 'Sci-Fi'],
        'https://upload.wikimedia.org/wikipedia/en/6/6e/Jurassic_World_poster.jpg',
        '',
        'now_showing'
    ),
    (
        'Jurassic World: Fallen Kingdom',
        'The team returns to save dinosaurs from a volcanic disaster and a new threat.',
        128,
        'PG-13',
        ARRAY['Adventure', 'Action', 'Sci-Fi'],
        'https://upload.wikimedia.org/wikipedia/en/c/c6/Jurassic_World_Fallen_Kingdom.png',
        '',
        'now_showing'
    ),
    (
        'Jurassic World Dominion',
        'Humans and dinosaurs must learn to live together in the modern world.',
        147,
        'PG-13',
        ARRAY['Adventure', 'Action', 'Sci-Fi'],
        'https://upload.wikimedia.org/wikipedia/en/c/ce/JurassicWorldDominion_Poster.jpeg',
        '',
        'now_showing'
    );

INSERT INTO screens (name, screen_type, is_active)
VALUES
    ('Raptor Hall', 'standard', true),
    ('T-Rex Theater', 'vip', true);

INSERT INTO showtimes (movie_id, screen_id, starts_at, ends_at, base_price)
VALUES
    (
        (SELECT id FROM movies WHERE title = 'Jurassic Park' LIMIT 1),
    (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1),
    '2026-04-05 18:00:00+00',
    '2026-04-05 20:07:00+00',
    15.99
    ),
(
  (SELECT id FROM movies WHERE title = 'Jurassic World' LIMIT 1),
  (SELECT id FROM screens WHERE name = 'T-Rex Theater' LIMIT 1),
  '2026-04-05 20:30:00+00',
  '2026-04-05 22:34:00+00',
  18.99
),
(
  (SELECT id FROM movies WHERE title = 'Jurassic World Dominion' LIMIT 1),
  (SELECT id FROM screens WHERE name = 'Raptor Hall' LIMIT 1),
  '2026-04-06 19:00:00+00',
  '2026-04-06 21:27:00+00',
  16.99
);
