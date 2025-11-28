# Domestic Logistics Tracking Management System – Cambodia (DLTS Cambodia)

A web-based shipment tracking system focused on **domestic logistics in Cambodia**.  
Built as a **final-year project** and portfolio piece to practice real-world backend and frontend development.

Current implementation uses:

- **Backend:** ASP.NET Core 8 Web API + Entity Framework Core + PostgreSQL  
- **Frontend (Demo UI):** HTML + TailwindCSS + Vanilla JavaScript  
- **DB:** PostgreSQL (`dlts_cambodia`)  
- **Version Control / Hosting:** Git + GitHub + GitHub Pages (for demo UI / landing page)

---

## 🎯 Project Goals

- Allow **Admin / Cashier** to create and manage shipments.
- Allow **Drivers** to update shipment status on the road (with optional location).
- Allow **Customers** to track their shipment **without login**, using only a tracking code.
- Practice full-stack development:
  - API design
  - Database design
  - Simple UI
  - Basic clean architecture
- Produce a project that can be shown in **CV / portfolio / interview**.

---

## 👥 Roles & Main Features

### 1. Admin / Cashier

- Create new shipment with:
  - Sender name / phone
  - Receiver name / phone
  - Origin branch
  - Destination branch
  - Optional driver assignment (driver Id)
- Automatically generate **tracking code**:
  - Format: `KH-YYYYMMDD-XXXXXX` (e.g. `KH-20251128-C81CD1`)
- View list of shipments:
  - Filter by status (Pending / InTransit / Delivered / Cancelled)
  - See created date, branches, assigned driver

### 2. Driver

- See assigned shipment (in future UI).
- Update shipment status by adding a new **tracking event**:
  - Status changes over time:
    - `Pending`
    - `InTransit`
    - `Delivered`
    - `Cancelled`
  - Optional location information:
    - `locationText` (e.g. *Phnom Penh Branch*, *Kampong Thom Checkpoint*)
    - `lat` / `lng` (for future map display)

### 3. Customer

- Public **tracking page** (no login).
- Enter tracking code to see:
  - Current status
  - Origin and destination branch
  - Full **event timeline**:
    - When it was created
    - When it departed origin
    - When it arrived destination
    - When it was delivered

---

## 🧱 Tech Stack

**Backend**

- C#
- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8
- Npgsql (PostgreSQL provider for EF Core)
- PostgreSQL

**Frontend (Demo)**

- HTML
- TailwindCSS (via CDN)
- Vanilla JavaScript

**Tools**

- Visual Studio 2022
- pgAdmin 4
- Git + GitHub
- Swagger (OpenAPI) for API testing

---

## 🏗️ Architecture Overview

```text
[Customer / Admin / Driver Browser]
          |
          v
[Demo UI - HTML + Tailwind + JS]  (docs/index.html)
          |
          v
[ASP.NET Core Web API] (LogisticsTracking.Api)
          |
          v
[PostgreSQL Database] (dlts_cambodia)
```

Core tables:

- `Branches`
- `Shipments`
- `TrackingEvents`
- `Users` (for future auth/roles)

---

## 🚀 Getting Started (Local Setup)

### 1. Prerequisites

- [.NET SDK 8.x](https://dotnet.microsoft.com/)
- [PostgreSQL](https://www.postgresql.org/download/) (e.g. 15/16)
- pgAdmin (optional but useful)
- Visual Studio 2022 (or VS Code with C# tools)
- Git

---

### 2. Clone the Repository

```bash
git clone https://github.com/RavenIsEmpty/domestic-logistics-tracking-system.git
cd domestic-logistics-tracking-system
```

---

### 3. Create PostgreSQL Database

In pgAdmin or psql:

```sql
CREATE DATABASE dlts_cambodia;
```

Make sure you know:

- host (e.g. `localhost`)
- port (usually `5432`)
- username (e.g. `postgres`)
- password

---

### 4. Configure Connection String

Open:

`backend/LogisticsTracking.Api/LogisticsTracking.Api/appsettings.json`

Update the `DefaultConnection`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=dlts_cambodia;Username=postgres;Password=your_password_here"
}
```

---

### 5. Apply EF Core Migrations

Open **Package Manager Console** in Visual Studio:

- **Project:** `LogisticsTracking.Api` (set as Default Project)

Run:

```powershell
Update-Database
```

This will:

- Create EF migrations history table
- Create:
  - `Branches`
  - `Users`
  - `Shipments`
  - `TrackingEvents`

---

### 6. Seed Some Branch Data

For testing, manually insert at least two branches in `Branches`:

```sql
INSERT INTO "Branches" ("Name", "Address")
VALUES 
('Phnom Penh Branch', 'Phnom Penh'),
('Siem Reap Branch', 'Siem Reap');
```

Note their IDs (usually 1 and 2) — these will be used as `OriginBranchId` / `DestinationBranchId`.

---

### 7. Run the API

In Visual Studio:

- Set startup project: `LogisticsTracking.Api`
- Press **F5** or **Run**

Swagger should open at something like:

```text
https://localhost:7111/swagger/index.html
```

Your **API Base URL** will be:

```text
https://localhost:7111
```

(Port may be different on your machine.)

---

### 8. Run the Demo UI

Open:

`docs/index.html` (double click or drag into browser)

At the top of the page, set:

```text
API Base URL = https://localhost:7111
```

Then you can:

1. **Create Shipment** (Admin panel)
2. **Track Shipment** (Customer panel)
3. **List Shipments** (Admin)
4. **Add Event / Update Status** (Driver)

---

## 🌐 GitHub Pages / Online Demo

A GitHub Pages instance is used as a simple **presentation / demo** site:

- Landing / explanation page:  
  `https://ravenisempty.github.io/Logistics-Tracking-Management-System/`

- Demo UI (from `/docs` folder) can also be served via GitHub Pages, but it still calls:
  - `https://localhost:7111` → so **only the developer’s machine** (local API) can use it.

For full public use, the API would need to be deployed to a cloud host and the frontend updated to use that URL.

---

## 🔌 API Overview

### `POST /api/Shipments`

Create a shipment.

**Body**

```json
{
  "senderName": "Empty",
  "senderPhone": "014915449",
  "receiverName": "Gech",
  "receiverPhone": "012345678",
  "originBranchId": 1,
  "destinationBranchId": 2,
  "assignedDriverId": null
}
```

**Response**

```json
{
  "id": 1,
  "trackingCode": "KH-20251128-C81CD1",
  "status": 0,
  "senderName": "Empty",
  "receiverName": "Gech",
  "createdAt": "2025-11-28T06:05:34.477737Z"
}
```

---

### `GET /api/Shipments`

List shipments (Admin). Optional status filter:

```http
GET /api/Shipments
GET /api/Shipments?status=0   // Pending
GET /api/Shipments?status=1   // InTransit
GET /api/Shipments?status=2   // Delivered
GET /api/Shipments?status=3   // Cancelled
```

**Response example**

```json
[
  {
    "id": 1,
    "trackingCode": "KH-20251128-C81CD1",
    "status": 1,
    "originBranchName": "Phnom Penh Branch",
    "destinationBranchName": "Siem Reap Branch",
    "assignedDriverId": null,
    "createdAt": "2025-11-28T06:05:34.477737Z"
  }
]
```

---

### `GET /api/Shipments/{trackingCode}`

Get full details for one shipment (Customer view).

```http
GET /api/Shipments/KH-20251128-C81CD1
```

**Response example**

```json
{
  "trackingCode": "KH-20251128-C81CD1",
  "status": 1,
  "senderName": "Empty",
  "senderPhone": "014915449",
  "receiverName": "Gech",
  "receiverPhone": "012345678",
  "originBranchName": "Phnom Penh Branch",
  "destinationBranchName": "Siem Reap Branch",
  "createdAt": "2025-11-28T06:05:34.477737Z",
  "events": [
    {
      "status": "Pending",
      "description": "Shipment created",
      "createdAt": "2025-11-28T06:05:34.485866Z",
      "lat": null,
      "lng": null,
      "locationText": null
    },
    {
      "status": "InTransit",
      "description": "Departed from origin branch",
      "createdAt": "2025-11-28T07:19:31.9391132Z",
      "lat": null,
      "lng": null,
      "locationText": "Phnom Penh Branch"
    }
  ]
}
```

---

### `POST /api/Shipments/{trackingCode}/events`

Add a tracking event and update shipment status (Driver / Admin).

```http
POST /api/Shipments/KH-20251128-C81CD1/events
```

**Body**

```json
{
  "status": 1,
  "description": "Departed from origin branch",
  "lat": null,
  "lng": null,
  "locationText": "Phnom Penh Branch"
}
```

Returns updated `ShipmentDetailsResponse` (same as `GET /api/Shipments/{code}`).

---

## 📈 Roadmap / Future Work

Planned improvements:

- Authentication & Role-based access:
  - Admin vs Driver vs Customer
- Separate UI pages for each role (Admin dashboard, Driver mobile-friendly view, Customer tracking page).
- Driver location using browser Geolocation (lat/lng) and map display.
- Pagination and search (by sender name, receiver phone, date range, branch).
- Unit tests (xUnit / MSTest) for core services:
  - Create shipment
  - Add tracking event
  - Get by tracking code
- Better UI design and responsive layout.
- Deploy API to a cloud provider so the tracking page works for anyone on the internet.

---

## 👤 Author

- **Name:** HUN SOKHENG
- **Program:** Bachelor in IT (CS Programming), expected graduation 2026
- **Focus:** Backend development (.NET), APIs, and practical logistics software.

This project is built as a **final-year thesis project** and as a **portfolio** for future junior developer roles.
