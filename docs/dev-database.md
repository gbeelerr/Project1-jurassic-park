# Dev Environment Setup - Database

## Overview

This document describes how to set up the development database.

The database will be used to store movies, showtimes, and bookings.

---

## Database Type

PostgreSQL or SQL Server

---

## Setup Steps

Option 1: Local database

- Install PostgreSQL or SQL Server
- Create a new database

Option 2: Simple setup

- Use in-memory database for development

---

## Connection

The API will connect to the database using Entity Framework.

Example connection string:

Server=localhost;Database=JurassicDB;User Id=sa;Password=1234;

---

## Result

The database is ready for development and testing.