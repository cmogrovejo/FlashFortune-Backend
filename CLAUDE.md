# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run API
dotnet run --project src/FlashFortune.API/FlashFortune.API.csproj

# Run all tests
dotnet test

# Run a single test project
dotnet test tests/FlashFortune.Domain.Tests/
dotnet test tests/FlashFortune.Application.Tests/
dotnet test tests/FlashFortune.Infrastructure.Tests/

# EF Core migrations
dotnet ef migrations add <MigrationName> --project src/FlashFortune.Infrastructure --startup-project src/FlashFortune.API
dotnet ef database update --project src/FlashFortune.Infrastructure --startup-project src/FlashFortune.API

# Start local infrastructure (PostgreSQL, Redis, MinIO)
docker-compose up -d
```

## Architecture

Clean Architecture (.NET 10 / ASP.NET Core) with four layers enforcing strict dependency direction (API → Application → Domain; Infrastructure → Application + Domain):

- **`FlashFortune.Domain`** — Entities, value objects, domain exceptions, and the `IPermutationAlgorithm` interface. No external dependencies. Entities: `User`, `BusinessUnit`, `UserUnitRole`, `Raffle`, `Prize`, `Participant`, `Winner`.

- **`FlashFortune.Application`** — CQRS handlers via MediatR, organized under `Features/{Auth,BusinessUnits,Raffles}/{Commands,Queries}`. Defines application contracts (`IFileStorageService`, `ICacheService`, `IEmailService`, etc.). A `ValidationBehavior` pipeline behavior runs FluentValidation before every command handler. `Result<T>` is the standard success/failure return type.

- **`FlashFortune.Infrastructure`** — Implements application contracts: EF Core + Npgsql (`AppDbContext`), Redis caching (`RedisCacheService`), S3/MinIO storage (`S3FileStorageService`), Hangfire background jobs (`FileIngestionJob`), JWT generation, and BCrypt hashing. The **Feistel permutation engine** (`Engine/FeistelPermutation.cs`) is the core algorithm — it maps virtual coupon numbers to participants deterministically without materializing DB rows, enabling 100M+ coupon raffles.

- **`FlashFortune.API`** — Controllers, SignalR hub (`RaffleHub`), exception middleware, and program bootstrap. CORS is pre-configured for the Angular frontend at `http://localhost:4200`.

## Business Rules & Invariants

These are non-negotiable constraints from the product spec. Changing them requires legal/PM sign-off.

| ID | Rule |
|---|---|
| BR-01 | The uploaded participant file (stored in S3 with Object Lock) is the absolute source of truth. |
| BR-02 | `numero_cuenta` must be unique within a raffle upload; duplicates are flagged and skipped. |
| BR-03 | Exclusion is by `doc_identidad`, not by account — one prize per person per raffle. |
| BR-04 | A JWT session is bound to exactly one `BusinessUnitId`; cross-tenant access is denied at middleware. |
| BR-05 | Coupon numbers must be non-contiguous (Feistel permutation enforces this). |
| BR-06 | `saldo_moneda` must be permanently deleted from the operational DB after the coupon universe is generated (post `confirm-upload`). |

**Finalized raffle immutability:** Once a raffle reaches `Ended`, all `POST`/`PUT` mutations must return `403 Forbidden`. Implement an `IsArchived` / status check at the application layer.

**Account lockout:** Lock a `User` after 5 consecutive failed login attempts. Recovery is via a signed token sent to the registered email.

**Single active operator session:** The backend must prevent two concurrent operator sessions from driving the same live draw to avoid double-draw scenarios.

## Participant File Contract

Pipe (`|`) or comma-delimited CSV. The ingestion job must validate column headers before processing any rows; abort immediately if a required column is missing.

| Column | Type | Notes |
|---|---|---|
| `nombre_completo` | String(200) | |
| `doc_identidad` | String(50) | Exclusion key (BR-03) |
| `telefono` | String(20) | PII — encrypt at rest |
| `numero_cuenta` | String(30) | Uniqueness key (BR-02) |
| `saldo_moneda` | Decimal(18,2) | Purged after coupon generation (BR-06) |

**File ingestion implementation:** Always use `StreamReader` line-by-line. Never `File.ReadAllLines()` — a 100M-row file will exhaust available RAM.

## Key Design Decisions

**Multi-tenancy:** The JWT carries a `unit_id` claim. A tenant middleware validates every request against this claim. All queries scope by `BusinessUnitId`.

**Raffle lifecycle:** `Draft → Ready (locked) → Live → Ended`. The state transitions are enforced in the domain entity. Uploading a participant file triggers a Hangfire job (`FileIngestionJob`) that processes the CSV asynchronously. After ingestion, `confirm-upload` locks the raffle.

**Virtual coupons:** Participants are stored with a `CouponRange` value object (start/end), not one row per coupon. The Feistel cipher maps a drawn virtual coupon number to the correct participant at draw time. This is the scalability cornerstone — do not replace it with materialized rows. Target: winner resolution in <100ms even at 100M coupons.

**Audit integrity:** An `AuditSeed` value object (CSPRNG seed + participant file hash) is committed before the draw begins and stored in S3 with Object Lock (WORM). This seed drives the draw sequence and proves the result was not manipulated post-upload.

**Draw exclusions:** Redis stores the exclusion set of already-drawn winners per raffle (keyed by `doc_identidad`) to avoid duplicates in O(1) lookups. Every skipped coupon must be written to an exclusion log for the auditor — silent skips are not acceptable.

**Real-time:** `RaffleHub` (SignalR) broadcasts `WinnerFound` and `PanicReset` events to clients grouped by `raffleId`. The panic-reset command allows operators to void and re-draw the current prize without restarting the entire raffle.

## RBAC Roles

Five roles defined per `UserUnitRole`. Roles are not mutually exclusive (a user can hold multiple roles within a unit).

| Role | Key Permissions |
|---|---|
| SuperAdmin | Create/manage BusinessUnits (platform-wide) |
| UnitAdmin | Invite staff, assign roles within their unit |
| Configurator | Upload participant files, define prizes and conversion factor X |
| Operator | Execute live draws, use panic-reset |
| Auditor | Read-only; download reports and S3 audit files |

## Configuration

Copy `appsettings.json` values to `appsettings.Development.json` for local overrides. Key sections:

| Section | Purpose |
|---|---|
| `ConnectionStrings:Postgres` | PostgreSQL connection |
| `ConnectionStrings:Redis` | Redis connection |
| `Jwt` | Secret (min 32 chars), Issuer, Audience |
| `Storage` | MinIO/S3 endpoint, credentials, bucket |
| `Cors:AllowedOrigins` | Frontend origin |

## Testing

- **Framework:** xUnit + FluentAssertions
- Tests follow the same layer separation: domain tests have no infrastructure dependencies, application tests mock infrastructure interfaces, infrastructure tests may use real services or integration test containers.
- Run a single test by name: `dotnet test --filter "FullyQualifiedName~TestMethodName"`

## Developer Endpoints (local only)

- `GET /scalar/v1` — Interactive OpenAPI documentation
- `GET /hangfire` — Background job dashboard
