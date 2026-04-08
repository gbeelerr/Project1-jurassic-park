#!/usr/bin/env bash
set -euo pipefail

psql -v ON_ERROR_STOP=1 \
  --username "${POSTGRES_USER}" \
  --dbname postgres \
  --command "CREATE DATABASE jurassic_web;"

psql -v ON_ERROR_STOP=1 \
  --username "${POSTGRES_USER}" \
  --dbname jurassic_api \
  --file /schema/apidb.sql

psql -v ON_ERROR_STOP=1 \
  --username "${POSTGRES_USER}" \
  --dbname jurassic_web \
  --file /schema/webdb.sql
