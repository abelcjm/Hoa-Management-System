# Homeowners Association (HOA) Management System

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-2088FF?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet)
[![Entity Framework Core](https://img.shields.io/badge/EF_Core-8.0-blue?logo=dotnet&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![SQL Server](https://img.shields.io/badge/Database-SQL_Server-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![Bootstrap](https://img.shields.io/badge/UI-Bootstrap_5-7952B3?logo=bootstrap&logoColor=white)](https://getbootstrap.com/)

A web-based **Homeowners Association (HOA) Management System** built with **ASP.NET Core 8 MVC** and **Entity Framework Core**. Engineered to streamline residential community operations, financial transparency, resident records, vehicle gate pass management, and regulatory compliance aligned with **DHSUD** (*Department of Human Settlements and Urban Development*) standards.

---

## Project Overview

Managing modern residential communities requires unified record-keeping, precise financial tracking, and transparent communication. This HOA Management System bridges the gap between HOA board administrators, operational staff, and community residents by offering:

- **Centralized Registry:** Complete property, household, tenant, pet, and vehicle records mapped by Block & Lot.
- **Financial Integrity:** Transparent billing, multi-type receipting (Official Receipts), automated delinquency tracking, and partial/advance payment support.
- **Self-Service Resident Portal:** Enables homeowners to independently inspect billing statements, transaction receipts, registered vehicles, and profile settings.
- **Automated Operations:** Background worker services handle daily database backups and automated email notices for overdue accounts.
- **Government Compliance:** One-click generation of the official **DHSUD Master List** and printable financial audits with embedded visual charts.

---

## Key Features

### 1. Role-Based Portals & Dashboards
The system features strict Role-Based Access Control (**RBAC**) tailored for four operational user roles:

| Role | Access Scope & Responsibilities |
| :--- | :--- |
| **Admin** | Full system governance, executive analytics, user account management, role assignment, audit log monitoring, and database backup controls. |
| **Treasurer** | Financial dashboard, payment verification, financial analytics, revenue distribution, and DHSUD financial statement generation. |
| **Encoder** | Operational dashboard, property & resident encoding, payment acceptance & Official Receipt (OR) generation, vehicle registrations, and certificate issuance. |
| **Homeowner** | Dedicated resident portal to review outstanding dues, payment history, vehicle sticker records, household/tenant lists, and account credentials. |

---

### 2. Property & Resident Census Management
- **Block & Lot Mapping:** Hierarchical subdivision property tracking with status flags (*Occupied*, *Vacant*).
- **Census & Demographics:** Captures primary owners, co-owners, household size, senior citizens count, children count, helpers, and registered voting status.
- **Tenant Tracking:** Tracks active leaseholders per property alongside primary owner records.
- **Pet Registry:** Tracks household pets (dogs, cats) including leash compliance records.
- **Vehicle & Gate Pass Tracking:** Manages resident vehicles, plate numbers, vehicle types, driver info, and yearly HOA sticker issuances.

---

### 3. Billing, Dues & Payment Processing
- **Automated Dues Generation:** Flexible monthly association dues tracking per property.
- **Flexible Payments:** Supports *Full*, *Partial*, and *Advance* payments.
- **Multi-Category Cashiering:** Processes payments for monthly dues, certificates, vehicle stickers, and administrative fees.
- **Official Receipting:** Automatic generation of unique OR numbers, audit-stamped by the cashier/encoder.
- **Aging & Delinquency Engine:** Real-time calculation of delinquency duration (in months) and outstanding balance breakdowns.

---

### 4. Certifications & Clearances
- Generates official HOA certifications (Residency, Clearance, Good Standing).
- Tracks issuance dates, requesting resident, collecting officer, and certification fee records.

---

### 5. Analytics & Regulatory Reporting
- **Executive Analytics:** Visual breakdown of delinquency rates, collection efficiency, occupancy metrics, and revenue distribution using [Chart.js](https://www.chartjs.org/).
- **DHSUD Master List:** One-click printable, print-optimized landscape report structured specifically for Philippine DHSUD regulatory submission.
- **Financial Reports & Insights:** Printable summary reports with automated financial health insights and cumulative projected vs. actual revenue graphs.
- **Excel Export:** High-fidelity spreadsheet exports powered by [ClosedXML](https://github.com/ClosedXML/ClosedXML).

---

### 6. Automated Background Services (`IHostedService`)
- **Automated Backup Worker (`BackupHostedService`):**
  - Executes daily scheduled database backups (`.bak`) at 4:30 PM.
  - Automatically cleans up and purges backup archives older than 30 days.
- **Delinquency Reminder Robot (`AutoReminderService`):**
  - Wakes up daily to inspect accounts with 12+ months of unpaid dues.
  - Automatically dispatches HTML email notifications via SMTP.
  - Incorporates a 30-day cooldown period per property logged via audit trails to prevent spamming.

---

### 7. Security & Audit Logging
- **Password Hashing:** Industry-standard **BCrypt** hashing with automated upgrade for legacy credentials.
- **Session Protection:** Sliding session expiration (10-minute inactivity limit) synchronized with HTTP-only cookies.
- **CSRF Mitigation:** Anti-Forgery tokens (`[ValidateAntiForgeryToken]`) enforced across state-modifying POST requests.
- **Comprehensive Audit Trail:** Logs every critical action (Create, Update, Delete, Auto-Email, Backups) capturing timestamp, user ID, target table, and record identifiers.

---

## Technology Stack

| Layer | Technologies |
| :--- | :--- |
| **Framework** | ASP.NET Core 8.0 (MVC Pattern) |
| **Language** | C# 12 / .NET 8 |
| **ORM / Data Access** | Entity Framework Core 8.0 (Code-First / Reverse Engineered Scaffolding) |
| **Database** | Microsoft SQL Server / LocalDB |
| **Security & Auth** | BCrypt.Net-Next, Cookie & Session Authentication, Role-based Action Filters |
| **Excel Generation** | ClosedXML 0.105.0 |
| **Frontend & UI** | Bootstrap 5, FontAwesome 6, jQuery, jQuery Validation & Unobtrusive Partial |
| **Data Visualization**| Chart.js (HTML5 Canvas) |

---

## Project Structure

```plaintext
Hoa-Management-System/
├── HOA_Management_System/
│   ├── Controllers/             # MVC Controllers (Auth, Properties, Payments, Reports, etc.)
│   │   ├── AccountController.cs
│   │   ├── AnalyticsController.cs
│   │   ├── AuditLogsController.cs
│   │   ├── BackupController.cs
│   │   ├── CertificatesController.cs
│   │   ├── DashboardController.cs
│   │   ├── HomeownerPortalController.cs
│   │   ├── PaymentsController.cs
│   │   ├── PropertiesController.cs
│   │   ├── ReportsController.cs
│   │   ├── SettingsController.cs
│   │   └── UsersController.cs
│   ├── Filters/                 # Custom Action Filters (UserAuthAttribute)
│   ├── Migrations/              # Entity Framework Core Schema Migrations
│   ├── Models/                  # Data Entities & ViewModels
│   │   ├── HoaDbContext.cs      # EF Core DbContext definition
│   │   ├── ViewModels/          # Tailored ViewModels for Dashboards & Reports
│   │   └── *.cs                 # Domain Models (Property, Membership, MonthlyDue, etc.)
│   ├── Services/                # Hosted Background Services & SMTP Email Service
│   │   ├── AutoReminderService.cs
│   │   ├── BackupHostedService.cs
│   │   └── EmailService.cs
│   ├── Views/                   # Razor Views (.cshtml) organized by controller
│   ├── wwwroot/                 # Static assets (Bootstrap, CSS, JS, Images, Icons)
│   ├── appsettings.example.json # Safe configuration template
│   ├── HOA_Management_System.csproj
│   └── Program.cs               # Application entry point & service dependency injection
├── .gitignore                   # Git ignore configurations (sensitive files & build output)
├── HOA_Management_System.sln    # Visual Studio Solution
└── README.md                    # Project documentation
```

---

## Getting Started

### Prerequisites
Before running the project, ensure you have the following installed:
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server) (or SQL Server Express / LocalDB)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (with *ASP.NET and web development* workload) or [VS Code](https://code.visualstudio.com/)

---

### Installation & Setup

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/abelcjm/Hoa-Management-System.git
   cd Hoa-Management-System
   ```

2. **Configure Application Settings:**
   Copy the provided configuration template to create your `appsettings.json`:
   ```bash
   cp HOA_Management_System/appsettings.example.json HOA_Management_System/appsettings.json
   ```
   Edit `HOA_Management_System/appsettings.json` with your database credentials and optional SMTP settings:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HOA_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
     },
     "SmtpSettings": {
       "Server": "smtp.gmail.com",
       "Port": "587",
       "SenderEmail": "your-email@example.com",
       "SenderName": "HOA Management System",
       "Password": "your-app-password"
     },
     "BackupSettings": {
       "FolderPath": "C:\\HOA_Backups"
     }
   }
   ```

3. **Apply Database Migrations:**
   Using the **.NET CLI**:
   ```bash
   cd HOA_Management_System
   dotnet ef database update
   ```
   *Or using Visual Studio Package Manager Console:*
   ```powershell
   Update-Database
   ```

4. **Run the Application:**
   ```bash
   dotnet run --project HOA_Management_System
   ```
   Navigate to `https://localhost:7262` or `http://localhost:5268` in your browser.

---

## Default Roles & Access

| Role | Default Access Path | Default Landing Page |
| :--- | :--- | :--- |
| **Admin** | Full Management | `/Dashboard/Index` |
| **Treasurer** | Financials & Reports | `/Dashboard/Index` |
| **Encoder** | Daily Operations | `/Dashboard/EncoderIndex` |
| **Homeowner** | Resident Portal | `/HomeownerPortal/Index` |

> *Note: Initial administrative accounts can be provisioned through EF seed scripts or by an existing System Administrator via the User Management module.*

---