# Ticketing Engine

Production-ready .NET 9 ticketing system with high-concurrency reservation, async payment processing, and real-time seat availability via SignalR.

## Architecture

Clean Architecture with CQRS (MediatR), Outbox Pattern, and Pessimistic Locking.

```
Domain → Application → Infrastructure → API
```

## Quick Start

```bash
# Start all services (API, Postgres, Redis, RabbitMQ, Jaeger, Grafana, Seq)
docker compose up -d

# API
open http://localhost:8080/swagger

# Dashboards
open http://localhost:16686   # Jaeger traces
open http://localhost:3000    # Grafana metrics
open http://localhost:15672   # RabbitMQ management (admin/admin)
open http://localhost:5341    # Seq structured logs
open http://localhost:8080/hangfire  # Hangfire dashboard
```

## Key Technical Features

| Feature | Implementation |
|---|---|
| Race conditions | `FOR UPDATE SKIP LOCKED` (Postgres row-level lock) |
| Idempotency | Redis cache keyed on `IdempotencyKey` (24h TTL) |
| Optimistic concurrency | `Seat.Version` as EF Core concurrency token |
| Guaranteed event delivery | Outbox Pattern — events commit with domain changes |
| Real-time seat map | SignalR + Redis backplane (horizontally scalable) |
| Auto-expiry | Hangfire scheduled job per reservation |
| Observability | OpenTelemetry → Jaeger (traces) + Prometheus + Grafana |
| Structured logs | Serilog → Seq |

## Running Tests

```bash
# Unit tests
dotnet test tests/TicketingEngine.UnitTests

# Architecture tests (Clean Architecture enforcement)
dotnet test tests/TicketingEngine.ArchitectureTests

# Integration tests (requires Docker daemon for Testcontainers)
dotnet test tests/TicketingEngine.IntegrationTests

# All with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Project Structure

```
src/
  TicketingEngine.Domain/         # Entities, Value Objects, Domain Events
  TicketingEngine.Application/    # CQRS handlers, MediatR pipeline, Interfaces
  TicketingEngine.Infrastructure/ # EF Core, Redis, RabbitMQ, Hangfire, OpenTelemetry
  TicketingEngine.API/            # Controllers, SignalR Hub, Middleware
tests/
  TicketingEngine.UnitTests/
  TicketingEngine.IntegrationTests/
  TicketingEngine.ArchitectureTests/
```
