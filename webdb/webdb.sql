-- ============================================================
--  PostgreSQL Database Dump
--  Generated from: webdb.dbml
--  https://dbdiagram.io/d/webdb-69bb3e3bfb2db18e3bb4deb7
--  Web server database
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
 
-- Enable pgcrypto for gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS pgcrypto;
 
-- ============================================================
--  ENUMS
-- ============================================================
 
CREATE TYPE user_role AS ENUM (
  'customer',
  'staff',
  'admin'
);
 
-- ============================================================
--  USERS
-- ============================================================
 
CREATE TABLE users (
  id            UUID          NOT NULL DEFAULT gen_random_uuid(),
  email         VARCHAR(255)  NOT NULL,
  display_name  VARCHAR(100),
  password_hash TEXT          NOT NULL,
  role          user_role     NOT NULL DEFAULT 'customer',
  is_active     BOOLEAN       NOT NULL DEFAULT true,
  created_at    TIMESTAMPTZ   NOT NULL DEFAULT now(),
  updated_at    TIMESTAMPTZ   NOT NULL DEFAULT now(),
 
  CONSTRAINT users_pkey PRIMARY KEY (id),
  CONSTRAINT users_email_unique UNIQUE (email)
);
 
-- ============================================================
--  SESSIONS
-- ============================================================
 
CREATE TABLE sessions (
  id            UUID        NOT NULL DEFAULT gen_random_uuid(),
  user_id       UUID        NOT NULL,
  refresh_token TEXT        NOT NULL,
  ip_address    INET,
  user_agent    TEXT,
  expires_at    TIMESTAMPTZ NOT NULL,
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
 
  CONSTRAINT sessions_pkey PRIMARY KEY (id),
  CONSTRAINT sessions_user_id_fkey FOREIGN KEY (user_id)
    REFERENCES users (id),
  CONSTRAINT sessions_refresh_token_unique UNIQUE (refresh_token)
);
