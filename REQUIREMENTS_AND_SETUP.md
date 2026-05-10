# FlashFortune Backend - Requirements & Setup Guide

## ✅ WHAT IS INSTALLED

### Development Environment
- **.NET SDK:** v10.0.203 ✅
- **Node.js:** v24.15.0 ✅
- **npm:** v11.14.0 ✅
- **PowerShell:** 5.1.19041.6456 ✅

### Backend Build Status
- **All Projects Compile Successfully** ✅
  - FlashFortune.Domain
  - FlashFortune.Application
  - FlashFortune.Infrastructure
  - FlashFortune.API
  - Test projects (Domain.Tests, Application.Tests, Infrastructure.Tests)

### NuGet Packages Installed
**FlashFortune.API:**
- Microsoft.AspNetCore.Authentication.JwtBearer (10.0.7)
- Microsoft.AspNetCore.OpenApi (10.0.7)
- Scalar.AspNetCore (2.5.9 → resolved to 2.6.0)
- Serilog.AspNetCore (10.0.0)
- Serilog.Sinks.Console (6.1.1)

**FlashFortune.Application:**
- FluentValidation (12.1.1)
- FluentValidation.DependencyInjectionExtensions (12.1.1)
- MediatR (14.1.0)
- Microsoft.EntityFrameworkCore (10.0.7)

**FlashFortune.Infrastructure:**
- AWSSDK.S3 (4.0.22.1)
- BCrypt.Net-Next (4.1.0)
- Hangfire.AspNetCore (1.8.23)
- Hangfire.Core (1.8.23)
- Hangfire.PostgreSql (1.21.1)
- MailKit (4.16.0)
- Microsoft.EntityFrameworkCore (10.0.7)
- Microsoft.IdentityModel.Tokens (8.17.0)
- Npgsql.EntityFrameworkCore.PostgreSQL (10.0.1)
- StackExchange.Redis (2.12.14)
- System.IdentityModel.Tokens.Jwt (8.17.0)

---

## ❌ WHAT IS MISSING

### 1. **Infrastructure Services (Required to Run)**

| Service | Status | How to Install | Purpose |
|---------|--------|----------------|---------|
| **PostgreSQL 16** | ❌ Missing | Download from postgresql.org or use Windows installer | Primary database for raffles, users, winners |
| **Redis 7+** | ❌ Missing | Download from redis.io or use Windows binaries | In-memory cache for exclusion sets, session data |
| **MinIO/S3-compatible** | ❌ Missing | Download MinIO desktop or use Docker | Object storage for audit files (WORM immutable) |

### 2. **Docker & Containerization**

| Tool | Status | How to Install |
|------|--------|----------------|
| **Docker Desktop** | ❌ Missing | Download from docker.com (includes docker-compose) |
| **docker-compose** | ❌ Missing | Installed with Docker Desktop |

### 3. **Frontend Framework**

| Tool | Status | How to Install | Notes |
|------|--------|----------------|-------|
| **Angular CLI** | ❌ Missing | `npm install -g @angular/cli@latest` | Required to run `ng serve` for frontend |
| **Angular Frontend Project** | ❌ Missing | Should be in `/frontend` or `/src/FlashFortune-UI` | The actual UI code |

### 4. **NuGet Packages (Code Dependencies)**

These packages are referenced in code but not in .csproj files:

| Package | Layer | Required? | Purpose |
|---------|-------|-----------|---------|
| **SignalR** (Microsoft.AspNetCore.SignalR) | API | ✅ YES | Real-time raffle drawing, live winner announcements |
| **AutoMapper** | Application | ✅ YES | DTO mapping for API responses |
| **QuestPDF** or **iTextSharp** | Infrastructure | ✅ YES | PDF report generation for audit |
| **ClosedXML** or **EPPlus** | Infrastructure | ✅ YES | Excel export for winner lists |
| **Dapper** | Infrastructure | ⚠️ Optional | High-performance queries (nice-to-have for 100M coupon lookups) |
| **Serilog.Sinks.File** | API | ⚠️ Optional | File logging (currently only Console) |

### 5. **Configuration Files**

| File | Status | Purpose |
|------|--------|---------|
| **appsettings.Development.json** | ❌ Missing | Local development config overrides |
| **.env file** | ❌ Missing | Docker environment variables (POSTGRES_PASSWORD, MINIO keys) |

### 6. **Implementation Code (Features)**

These are partially/fully missing from the codebase:

#### Domain Layer
- ❌ **CouponRange** value object (start/end coupon representation)
- ❌ **AuditSeed** value object (CSPRNG + file hash)
- ❌ **AuditLog** entity (exclusion events tracking)

#### Application Layer
- ❌ **IEncryptionService** interface (for PII encryption: telefono, doc_identidad)
- ❌ **CSV Validator** (validate headers before processing)
- ❌ **Conversion Factor Service** (calculate coupons from balance using X factor)

#### Infrastructure Layer
- ❌ **FeistelPermutation** implementation (IPermutationAlgorithm)
- ❌ **RedisCacheService** implementation (currently only interface)
- ❌ **S3FileStorageService** implementation (with Object Lock support)
- ❌ **FileIngestionJob** (Hangfire background job for CSV processing)
- ❌ **EncryptionService** implementation (AES-256 for PII)
- ❌ **ReportGenerationService** (PDF & Excel)

#### API Layer
- ⚠️ **SignalR Hub Configuration** (partial - RaffleHub exists but may be incomplete)
- ⚠️ **Program.cs** service registration (likely incomplete with missing services)

---

## 📋 SETUP CHECKLIST

### Phase 1: Local Development Environment
- [ ] Install PostgreSQL 16
  - Create database: `flashfortune`
  - Create user: `ff_user`
  - Update connection string in `appsettings.json`
- [ ] Install Redis 7+
- [ ] Install MinIO (or Docker Desktop for containerized version)

### Phase 2: .NET Backend Setup
- [ ] Update `appsettings.json` with correct values:
  - `ConnectionStrings:Postgres` - Change password from "CHANGE_ME"
  - `Jwt:Secret` - Use a 32+ character random key
  - `Storage` credentials - MinIO access/secret keys
- [ ] Create `appsettings.Development.json` for local overrides
- [ ] Run EF Core migrations:
  ```bash
  dotnet ef migrations add Initial --project src/FlashFortune.Infrastructure --startup-project src/FlashFortune.API
  dotnet ef database update --project src/FlashFortune.Infrastructure --startup-project src/FlashFortune.API
  ```
- [ ] Add missing NuGet packages:
  ```bash
  dotnet add src/FlashFortune.API package Microsoft.AspNetCore.SignalR
  dotnet add src/FlashFortune.Application package AutoMapper
  dotnet add src/FlashFortune.Infrastructure package QuestPDF
  dotnet add src/FlashFortune.Infrastructure package EPPlus
  ```

### Phase 3: Backend Implementation
- [ ] Implement **FeistelPermutation** (IPermutationAlgorithm)
- [ ] Implement **RedisCacheService**
- [ ] Implement **S3FileStorageService**
- [ ] Create **FileIngestionJob** for Hangfire
- [ ] Implement **EncryptionService**
- [ ] Create missing domain value objects (CouponRange, AuditSeed)
- [ ] Implement CSV validation
- [ ] Add Signal configuration to Program.cs

### Phase 4: Frontend Setup
- [ ] Install Angular CLI globally: `npm install -g @angular/cli@latest`
- [ ] Check if frontend project exists at expected location
- [ ] If missing, generate Angular app:
  ```bash
  ng new FlashFortune-UI --routing --style=tailwind
  cd FlashFortune-UI
  npm install
  ```

### Phase 5: Docker Containerization
- [ ] Install Docker Desktop
- [ ] Create `.env` file with secrets:
  ```
  POSTGRES_PASSWORD=your_secure_password
  MINIO_ACCESS_KEY=minioadmin
  MINIO_SECRET_KEY=minioadmin
  JWT_SECRET=your_32_char_secret_key
  ```
- [ ] Test docker-compose: `docker-compose up -d`

---

## 🚀 HOW TO RUN THE PROJECT

### Option A: Local Development (Recommended)

1. **Start Infrastructure Services:**
   ```bash
   # PostgreSQL (must be running)
   # Redis (must be running)
   # MinIO (must be running)
   ```

2. **Run Backend API:**
   ```bash
   dotnet run --project src/FlashFortune.API/FlashFortune.API.csproj
   ```
   - API will be available at `https://localhost:7001` (or similar)
   - OpenAPI docs at `https://localhost:7001/scalar/v1`

3. **Run Frontend (in separate terminal):**
   ```bash
   cd src/FlashFortune-UI  # or wherever the Angular app is
   npm start  # or ng serve
   ```
   - Frontend will be at `http://localhost:4200`

### Option B: Docker Compose (Once Configured)

1. **Create `.env` file** with all secrets
2. **Run:**
   ```bash
   docker-compose up -d
   ```

### Option C: Run Tests

```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/FlashFortune.Domain.Tests/
dotnet test tests/FlashFortune.Application.Tests/
dotnet test tests/FlashFortune.Infrastructure.Tests/
```

---

## 📊 DEPENDENCY TREE

```
FlashFortune.API (Web Layer)
  ├── FlashFortune.Application
  │   ├── FluentValidation
  │   ├── MediatR
  │   └── FlashFortune.Domain
  │       └── (No external dependencies)
  └── FlashFortune.Infrastructure
      ├── EntityFrameworkCore + Npgsql (PostgreSQL)
      ├── StackExchange.Redis (Caching)
      ├── AWSSDK.S3 (Object Storage)
      ├── Hangfire (Background Jobs)
      ├── MailKit (Email)
      ├── BCrypt.Net-Next (Hashing)
      ├── JWT Libraries
      └── FlashFortune.Application
```

---

## 🔧 CRITICAL CONFIGURATION NOTES

1. **JWT Secret** (appsettings.json):
   - Must be at least 32 characters
   - Keep it secret, don't commit it
   - Use environment variables in production

2. **PostgreSQL Connection**:
   - User: `ff_user`
   - Database: `flashfortune`
   - Default port: 5432
   - Update password in config

3. **Redis**:
   - Default: `localhost:6379`
   - Used for winner exclusion caching
   - No password by default (change in production)

4. **MinIO/S3**:
   - Default keys: minioadmin / minioadmin
   - Bucket: `flashfortune`
   - Endpoint: `http://localhost:9000`

5. **CORS**:
   - Currently allows `http://localhost:4200` (Angular frontend)
   - Update for production domains

---

## 📝 QUICK REFERENCE: BUILD STATUS

| Project | Status | Issues |
|---------|--------|--------|
| FlashFortune.Domain | ✅ Builds | No issues |
| FlashFortune.Application | ✅ Builds | No issues |
| FlashFortune.Infrastructure | ✅ Builds | No issues |
| FlashFortune.API | ⚠️ Builds with warnings | Scalar.AspNetCore version mismatch (2.6.0 used instead of 2.5.9 - not critical) |
| All Test Projects | ✅ Build | No issues |

**Overall:** Code compiles cleanly. Main blocker is missing infrastructure services (PostgreSQL, Redis, MinIO) and frontend application.

---

## 🎯 PRIORITY ACTIONS

**Before you can run this project, do these in order:**

1. **Install PostgreSQL 16** ← First priority
2. **Install Redis 7+** ← Second priority
3. **Install MinIO or Docker** ← Third priority
4. **Add missing NuGet packages** ← Can be done during dev
5. **Implement missing services** ← Main development work
6. **Set up Angular frontend** ← Frontend work
7. **Configure appsettings files** ← Config work

