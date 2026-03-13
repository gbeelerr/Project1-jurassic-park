# Website Specification
Jurassic Park Movie Booking Website

## Overview

This website allows guests to browse movies and book seats at the Jurassic Park movie theater.

The website will communicate with an API server to get movie data, showtimes, and booking information.

The goal is to provide a simple movie booking system for park guests.

---

## Users

There are two types of users.

Guest  
Admin

Guests can browse movies and book tickets.  
Admins can manage movies and showtimes.

---

## Guest Features

### Browse Movies

Guests can view a list of movies currently playing.

Information shown:

- Movie title
- Description
- Runtime
- Rating
- Poster image

---

### View Showtimes

Guests can select a movie and see available showtimes.

Information displayed:

- Movie title
- Start time
- Theater room
- Ticket price

---

### Select Seats

Guests can select seats from a seat map.

Seat status:

- Available
- Reserved
- Sold

---

### Booking Tickets

Guests can complete a booking.

Guests will enter:

- Name
- Email

After booking, the system will display a confirmation page.

---

## Admin Features

Admins can:

- Add movies
- Edit movies
- Create showtimes
- View bookings

---

## Authentication

Users can create accounts and log in.

The website will use **Basic Authentication**.

The website will communicate with the API using **Bearer Tokens**.

---

## Technology

Language: C#  
Framework: ASP.NET

Frontend:

- HTML
- Bootstrap
- JavaScript

The website will send REST requests to the API.