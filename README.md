# PowerUp - Gym Management System

A gym management system built with .NET and Angular.

## Table of Contents

- [Features](#features)
  - [User Management](#user-management)
  - [Personal Training](#personal-training)
  - [Group Classes](#group-classes)
  - [Membership](#membership)
  - [Admin Dashboard](#admin-dashboard)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Backend Setup](#2-backend-setup)
  - [3. Frontend Setup](#3-frontend-setup)
- [Default Admin Credentials](#default-admin-credentials)
- [API Documentation](#api-documentation)


## Features

### User Management
- **Role-Based Access Control**: Three user roles (Admin, Instructor, Member)
- **User Authentication**: Secure JWT-based authentication
- **Profile Management**: Users can view and manage their profiles
- **Admin Controls**: Admins can manage users, change roles, and view statistics

### Personal Training
- **Session Booking**: Members can book personal training sessions with instructors
- **Calendar View**: Interactive weekly calendar for viewing and booking sessions
- **Instructor Management**: Instructors can view and manage their scheduled sessions

### Group Classes
- **Class Management**: Instructors can create and manage group fitness classes
- **Class Enrollment**: Members can enroll in and unenroll from group classes
- **Capacity Management**: Track enrollment and maximum capacity for each class

### Membership
- **Subscription Management**: Members can subscribe to and cancel memberships
- **Admin Control**: Admins can create, update, and manage subscription plans
- **Active Subscription Tracking**: Monitor active subscriptions and expiration dates

### Admin Dashboard
- **Analytics**: View user statistics, revenue metrics, and subscription data
- **User Management**: Search and manage all users
- **Membership Management**: Create and manage subscription plans
- **Data Visualization**: Charts showing new users and revenue over time

## Tech Stack

- **.NET 9.0** 
- **Angular 20** 
- **Entity Framework Core 9.0**
- **SQL Server** 
- **JWT Authentication**
- **BCrypt.Net**
- **Swagger/OpenAPI**


## Prerequisites

- **.NET 9.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Node.js 20+** - [Download](https://nodejs.org/)
- **SQL Server** - SQL Server 2019 or later

## Getting Started

### 1. Clone the Repository
```
git clone https://github.com/miguelcorreia01/PowerUp.git
```
### 2. Backend Setup

#### Navigate to the API directory
```
cd PowerUpAPI/PowerUp
```

#### Create Environment File
Create a `.env` file in the `PowerUpAPI/PowerUp` directory:

JWT_KEY=your-secret-key-minimum-32-characters-long

JWT_ISSUER=PowerUpAPI

JWT_AUDIENCE=PowerUpUI

#### Run the API
```
dotnet run
``` 
The API will be available at `http://localhost:5255`

### 3. Frontend Setup

#### Navigate to the UI directory
```
cd PowerUpUI
```
#### Install Dependencies
```
npm install
```
#### Run the Frontend
```
ng serve
```
The application will be available at `http://localhost:4200`

## Default Admin Credentials

On first run, a default admin user is automatically created:

- **Email**: `admin@powerup.com`
- **Password**: `Admin123!`

## API Documentation
Once the API is running, Swagger documentation is available at:
```
http://localhost:5255/swagger
```
