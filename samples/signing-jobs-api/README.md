# Public Signing Jobs API Sample

This directory is a **clean-room portfolio sample** inspired by the engineering patterns used in the Signy case study. It is intentionally small enough to review quickly and does **not** contain production Signy source code, credentials, tenant data, certificates, private URLs, Wacom licence material, or proprietary integrations.

## What this sample demonstrates

- ASP.NET Core minimal APIs
- dependency injection
- tenant-scoped reads and writes
- idempotent job creation using `(tenant, externalReference)`
- explicit signing-job lifecycle state
- concurrency-safe in-memory persistence for the sample
- automated unit tests for tenant isolation and idempotency

## Run locally

Requirements: .NET 8 SDK.

```bash
cd samples/signing-jobs-api/Signy.Demo.Api
dotnet run
```

The sample exposes:

```text
GET  /health
GET  /api/signing-jobs
GET  /api/signing-jobs/{id}
POST /api/signing-jobs
POST /api/signing-jobs/{id}/complete
```

All signing-job endpoints require a demonstration tenant header:

```text
X-Tenant-Id: bank-demo
```

Create a job:

```bash
curl -i http://localhost:5000/api/signing-jobs \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: bank-demo" \
  -d '{
    "externalReference": "ERP-1001",
    "title": "Loan approval",
    "customerName": "Ama Mensah"
  }'
```

Sending the same `externalReference` again for the same tenant returns the existing job instead of creating a duplicate. Another tenant may independently use the same external reference.

## Run tests

```bash
cd samples/signing-jobs-api
dotnet test Signy.Demo.Api.Tests/Signy.Demo.Api.Tests.csproj
```

## Why an in-memory store?

The goal is to make the important behavior reviewable in a few minutes. The private production system uses database-backed persistence and a much larger architecture. This sample deliberately isolates the core reasoning around **tenant boundaries, idempotency, state transitions and testability** without publishing private implementation details.
