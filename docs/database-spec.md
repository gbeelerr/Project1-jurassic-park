# Database Specification
Jurassic Park Movie Booking Database

## Overview

The database stores movie information, showtimes, seats, and bookings.

The API will access the database using **Entity Framework in C#**.

Database type:

PostgreSQL or SQL Server

---

## Movies Table

Stores movie information.

Fields:

id  
title  
description  
runtime  
rating

---

## Auditoriums Table

Stores theater rooms.

Fields:

id  
name  
capacity

Example room names:

Raptor Hall  
T-Rex Theater  
Giganotosaurus Screen

---

## Showtimes Table

Stores movie showtimes.

Fields:

id  
movie_id  
auditorium_id  
start_time  
price

---

## Seats Table

Stores seat information.

Fields:

id  
auditorium_id  
row  
number

Example seats:

A1  
A2  
B1  
B2

---

## Bookings Table

Stores ticket bookings.

Fields:

id  
showtime_id  
customer_name  
customer_email  
created_at

---

## BookingSeats Table

Stores seats for each booking.

Fields:

booking_id  
seat_id