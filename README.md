<<<<<<< HEAD
# StokSafe
This is a community Saving Money Application ( Social)
=======
# 🏦 StokSafe - Community Banking System

A comprehensive banking-style savings club (stokvel) management system for South African communities.

## 📋 Features

- ✅ **Member Dashboard** - Real-time savings overview
- ✅ **Admin Dashboard** - Full system oversight  
- ✅ **Fine Management** - Automated fines with escalation (30 Rands late meeting, 50 Rands no banking, 30 Rands no proof of payment, 100 Rands service fee)
- ✅ **Loan Management** - 4-week loans with 100% penalty
- ✅ **Daily Banking Reports** - Complete daily financial summary
- ✅ **PDF Export** - Professional report generation
- ✅ **MySQL Database** - Reliable data storage
- ✅ **Role-Based Access** - Admin, Head, Secretary, Treasurer, Member

## 🚀 Technology Stack

| Component | Technology | Version |
|-----------|------------|---------|
| Framework | ASP.NET Core MVC | 8.0 |
| Language | C# | 12.0 |
| Database | MySQL | 8.0 |
| ORM | Entity Framework Core | 8.0 |
| Identity | ASP.NET Core Identity | 8.0 |
| CSS | Bootstrap | 5.3 |
| Charts | Chart.js | 4.4 |

## 📦 Installation

### Prerequisites
- .NET 8.0 SDK
- MySQL Server 8.0+
- Git

### Setup Steps

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/StokSafe.git
cd StokSafe

# 2. Restore packages
dotnet restore

# 3. Update connection string in appsettings.json
# Set your MySQL connection string

# 4. Create database
mysql -u root -p
CREATE DATABASE StokSafe;
EXIT;

# 5. Run migrations
dotnet ef database update

# 6. Run the application
dotnet run
>>>>>>> b5f5122 (InitialCommits)
# StokSafe
