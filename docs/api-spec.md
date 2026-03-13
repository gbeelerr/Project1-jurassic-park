# API Specification
Jurassic Park Movie Booking API

## Overview

This API provides movie and booking data for the Jurassic Park movie booking website.

The API will be built using **C# and ASP.NET Web API**.

All responses will be returned in **JSON format**.

Base URL:

/api

---

## Authentication

API requests require a **Bearer Token**.

Example header:

Authorization: Bearer TOKEN

---

## Endpoints

### Health Check

GET /api/health

Response example:

{
"status": "ok"
}

This endpoint is used to check if the API server is running.

---

### Get Movies

GET /api/movies

Returns a list of movies currently available.

Example response:

[
{
"id": 1,
"title": "Rise of Giganotosaurus",
"runtime": 120
}
]

---

### Get Showtimes

GET /api/showtimes?movieId=1

Returns all showtimes for a specific movie.

---

### Get Seats

GET /api/showtimes/{id}/seats

Returns seat availability for a showtime.

Seat status:

- Available
- Reserved
- Sold

---

### Create Booking

POST /api/bookings

Request example:

{
"showtimeId": 1,
"seats": ["A1", "A2"],
"name": "Yanbo Wang",
"email": "yanwang@chapman.edu"
}

Response example:

{
"bookingId": 101,
"status": "confirmed"
}

---

### Get Booking

GET /api/bookings/{id}

Returns booking details for a specific booking.