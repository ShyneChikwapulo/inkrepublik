# Inkrepublik Tattoo Studio — Website & Booking

A full-stack **ASP.NET Core + Blazor Server** web application for **Inkrepublik Tattoo Studio**, based in Hout Bay, Cape Town.

The project is designed to provide a modern public-facing studio website together with an online booking workflow and administrative functionality.

---

## Local Development

### Prerequisites

Before running the project locally, make sure you have the following installed:

* [.NET 10 SDK](https://dotnet.microsoft.com/download)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)
* [Visual Studio Code](https://code.visualstudio.com/)
* [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)

---

## Getting Started

### 1. Start the Database

The project uses **SQL Server 2022** running inside Docker for local development.

From the root of the repository, run:

```bash
docker compose up -d
```

This starts the SQL Server container in the background.

The local SQL Server instance is configured as:

| Setting  | Value                 |
| -------- | --------------------- |
| Host     | `localhost`           |
| Port     | `1433`                |
| Username | `sa`                  |
| Password | `Inkrepublik!Dev2026` |
| Database | Project database      |

> **Note:** On the first startup, SQL Server may take approximately 20 seconds to become fully ready.

You can check the container status with:

```bash
docker compose ps
```

To view the latest SQL Server logs:

```bash
docker compose logs sqlserver --tail 5
```

---

### 2. Run the Application

Once SQL Server is ready, run the Blazor application:

```bash
dotnet run --project src/Inkrepublik.Web --launch-profile https
```

The application should be available at:

**https://localhost:7112**

> Your browser may display a warning about the local HTTPS development certificate. This is expected when running ASP.NET Core locally.

---

## Stopping the Database

To stop the SQL Server container while keeping its persisted data:

```bash
docker compose down
```

To stop the container **and delete all Docker volumes/data**:

```bash
docker compose down -v
```

> ⚠️ **Warning:** `docker compose down -v` permanently deletes the local database data stored in Docker volumes.

---

# Project Structure

```text
Inkrepublik.slnx
│
├── src/
│   ├── Inkrepublik.Web/
│   │   └── Blazor Server application
│   │       ├── Public website
│   │       └── Admin area
│   │
│   ├── Inkrepublik.Domain/
│   │   └── Domain entities and enums
│   │
│   ├── Inkrepublik.Data/
│   │   └── Entity Framework Core
│   │       ├── DbContext
│   │       └── Database migrations
│   │
│   └── Inkrepublik.Services/
│       └── Application business logic
│           ├── Email services
│           └── Magic-link functionality
│
├── tests/
│   └── Inkrepublik.Tests/
│       └── xUnit tests
│
├── docker-compose.yml
│   └── SQL Server 2022 local development database
│
└── README.md
```

---

# Architecture

The solution is separated into several projects to keep the application maintainable and enforce separation of concerns.

### `Inkrepublik.Web`

The presentation layer and main web application.

Responsibilities include:

* Public-facing studio website
* Blazor Server UI
* Booking interface
* Administrative interface
* Application configuration
* Dependency injection composition

### `Inkrepublik.Domain`

Contains the core domain model.

Responsibilities include:

* Entities
* Enums
* Domain-level concepts
* Business rules that belong to the domain

This project is intentionally kept independent of infrastructure and framework-specific dependencies where possible.

### `Inkrepublik.Data`

Responsible for persistence and database access.

Responsibilities include:

* Entity Framework Core `DbContext`
* Entity configurations
* Database relationships
* Migrations
* SQL Server integration

### `Inkrepublik.Services`

Contains application and business logic that sits between the UI and data layers.

Responsibilities include:

* Business workflows
* Email functionality
* Magic-link functionality
* Application services

### `Inkrepublik.Tests`

Contains automated tests for the application.

The project uses **xUnit** as the testing framework.

---

# Technology Stack

## Backend

* [.NET 10](https://dotnet.microsoft.com/)
* [ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet)
* C#
* Blazor Server
* Interactive Server render mode

## Data

* Entity Framework Core 10
* SQL Server provider
* SQL Server 2022
* Docker

## Testing

* xUnit

## Development Tools

* Visual Studio Code
* C# Dev Kit
* Docker Desktop
* Git / GitHub

---

# Database

The application uses **Microsoft SQL Server 2022** for local development.

SQL Server is provided through Docker Compose so that developers do not need to install SQL Server directly on their machines.

The development database can therefore be started with:

```bash
docker compose up -d
```

and stopped with:

```bash
docker compose down
```

Database migrations will be managed through **Entity Framework Core** as the project progresses.

---

# Development Workflow

A typical local development workflow is:

```text
Clone Repository
       │
       ▼
Start Docker
       │
       ▼
SQL Server 2022
       │
       ▼
Run ASP.NET Core Application
       │
       ▼
Blazor Server
       │
       ▼
Develop / Test
       │
       ▼
Commit Changes
```

---

# Roadmap

The project is being developed incrementally.

### Phase 1 — Solution Scaffolding

* [x] Create solution structure
* [x] Create application projects
* [x] Configure project references

### Phase 2A — SQL Server via Docker Compose

* [x] Configure SQL Server 2022 container
* [x] Configure Docker Compose
* [x] Verify local database connectivity

### Phase 2B — Domain Entities and Enums

* [x] Define core domain entities
* [x] Define domain enums
* [x] Establish initial domain model

### Phase 2C — DbContext + Relationships

* [ ] Create Entity Framework Core `DbContext`
* [ ] Configure entity relationships
* [ ] Configure database constraints
* [ ] Configure indexes where required

### Phase 2D — Initial Migration

* [ ] Create initial EF Core migration
* [ ] Apply migration to local SQL Server database
* [ ] Verify database schema

### Phase 2E — Seed Data + Dependency Injection

* [ ] Create development seed data
* [ ] Configure dependency injection
* [ ] Connect application services
* [ ] Verify end-to-end database access

### Phase 3 — Public Marketing Site

* [x] 3A — Design system
* [x] 3B — Layout shell (header, footer)
* [x] 3C — Home page
* [x] 3D — Artists listing + detail
* [x] 3E — Services page
* [x] 3F — Gallery + Reviews
* [x] 3G — Contact page

### Phase 4 — Booking Request Flow

* [ ] Booking form
* [ ] Client information
* [ ] Tattoo requirements
* [ ] Preferred dates
* [ ] Booking status
* [ ] Email notifications
* [ ] Booking persistence

### Phase 5 — Admin Area

* [ ] Admin authentication
* [ ] Dashboard
* [ ] Booking management
* [ ] Client management
* [ ] Artist management
* [ ] Portfolio/content management

### Phase 6 — Client Magic-Link Polish

* [ ] Secure client magic links
* [ ] Booking status access
* [ ] Client booking details
* [ ] Email-based authentication flow
* [ ] Expiration and security handling

### Phase 7 — Dockerize for Render

* [ ] Create production Dockerfile
* [ ] Configure production environment variables
* [ ] Configure production database connection
* [ ] Test production container locally

### Phase 8 — Deploy to Render

* [ ] Configure Render service
* [ ] Configure production database
* [ ] Configure environment variables
* [ ] Configure deployment
* [ ] Verify production application

### Phase 9 — Studio Handover & Real Content

* [ ] Replace development content
* [ ] Add real studio information
* [ ] Add real artist profiles
* [ ] Add real portfolio content
* [ ] Configure production email
* [ ] Final testing
* [ ] Studio handover


## Future features (post-handover)

- **Facebook Graph API gallery sync** — pull the studio's Facebook Page photos
  periodically and display them in the gallery. Requires:
  - Facebook App + Business Manager setup on the studio's side
  - Long-lived Page Access Token (System User)
  - A background worker (`IHostedService`) to fetch + download images
  - Object storage for downloaded images (Render disk is ephemeral)
  - Manual artist-tagging on synced images
  Architecture: new `SiteGalleryImage` entity, decoupled from `ArtistImage`.


### Email in development

Emails are sent via SMTP using Resend's shared sandbox domain (`onboarding@resend.dev`).
**In this mode, Resend only delivers to the email address you signed up to Resend with**
(currently `chikwapuloshine@gmail.com`). This is a Resend sandbox restriction, not a bug
in the app.

To send to any recipient in development, either:
- Use a Gmail SMTP setup with an app password, or
- Verify a custom domain on Resend (this is what we'll do in Phase 8 with the studio's domain)

The booking flow itself is unaffected — bookings are always saved, and the owner email
always delivers. Only the client confirmation email is subject to this restriction.