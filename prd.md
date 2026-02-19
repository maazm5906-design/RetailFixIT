# RetailFixIT - Full Stack Technical Assessment Plan

## Context

RetailFixIT is a web-based operations platform coordinating service jobs between customers and ~1,000 vendors. The assessment requires a production-grade SPA + backend demonstrating:
- Event-driven Azure architecture
- AI-assisted dispatch (Gemini in dev, Azure OpenAI in prod)
- Multi-tenant RBAC isolation
- Real-time updates via SignalR
- Clean architecture and engineering reasoning

**Dev constraints:** Azure Cosmos DB Emulator (Docker) + Google Gemini API (free)
**Prod targets:** Azure Cosmos DB, Azure Service Bus, Azure Cache for Redis, Azure SignalR Service, Azure OpenAI, Azure AD/Entra ID

---

## Product Requirements Document (PRD)

### Overview
RetailFixIT modernizes a field service operations workflow. Four roles (Dispatcher, Vendor Manager, Admin, Support Agent) interact with a shared job board, with AI helping dispatchers pick the best vendor for each job.

### Core Modules

| Module | Description |
|---|---|
| Job Dashboard | Paginated, filterable SPA view of all jobs with real-time status |
| Job Detail | Full job info, assignment history, AI recommendations, timeline |
| Assignment Workflow | Dispatcher selects vendor (AI-assisted), confirms, broadcasts update |
| AI Recommendation | Vendor suggestion + job summary generated via LLM |
| Real-time Updates | SignalR push: job created, assigned, AI ready |
| RBAC + Multi-tenancy | Role-gated actions, strict tenant data isolation |
| Audit Logging | Immutable log of every state-changing operation |
| Event Flow | JobCreated → AI requested → AI generated → Assigned → Broadcast |

### Required Entities
Job, Vendor, Assignment, AIRecommendation, AuditLog, Tenant, UserRole

### User Roles & Permissions

| Action | Dispatcher | VendorManager | Admin | SupportAgent |
|---|---|---|---|---|
| Create/Update Job | ✓ | | ✓ | |
| Assign Vendor | ✓ | | ✓ | |
| Request AI Recommendation | ✓ | | ✓ | |
| Manage Vendors | | ✓ | ✓ | |
| View Audit Logs | | | ✓ | |
| View Dashboard | ✓ | ✓ | ✓ | ✓ |
| Cancel Job | | | ✓ | |

---

## Tech Stack Recommendation

### Frontend — `frontend/`

| Technology | Choice | Reason |
|---|---|---|
| Framework | React 18 + TypeScript | Industry standard SPA, strong typing |
| Build Tool | Vite | Fast HMR, ESM-native |
| Server State | TanStack Query (React Query) | Pagination, caching, optimistic updates built-in |
| Client State | Zustand | Lightweight; auth token, notification queue, UI state |
| Routing | React Router v6 | Protected routes, lazy loading |
| Real-time | `@microsoft/signalr` | Works with both in-process hub (dev) and Azure SignalR (prod) |
| Forms | React Hook Form + Zod | Performant forms, schema validation |
| UI | Radix UI primitives + Tailwind CSS | Accessible, unstyled, no vendor lock-in |
| Table | TanStack Table | Headless, powerful; drives the job dashboard |
| Auth (dev) | JWT decode + axios interceptor | Simple, no external dependency |
| Auth (prod) | `@azure/msal-react` | Azure AD / Entra ID integration |
| Notifications | react-hot-toast | Real-time event toasts |
| Icons | lucide-react | Consistent icon set |
| Testing | Vitest + Testing Library + MSW | Fast, co-located tests with API mocking |

### Backend — `backend/`

| Technology | Choice | Reason |
|---|---|---|
| Framework | ASP.NET Core 8 Web API (C#) | Best Azure-native fit; first-class SignalR, Identity, EF Core |
| Architecture | Clean Architecture (Domain / Application / Infrastructure / API) | Strict layer boundaries, testable, assessment-ready |
| ORM | Entity Framework Core 8 | Code-first migrations; global query filters for tenancy |
| DB (dev) | Azure Cosmos DB Emulator (Docker) | Free, same EF Core Cosmos driver; no schema migrations |
| DB (prod) | Azure Cosmos DB | Globally distributed NoSQL; same EF Core driver, swap endpoint |
| CQRS | MediatR + FluentValidation | Commands/Queries with validation pipeline |
| Event Bus (dev) | MassTransit in-memory transport | Zero config, same consumer code |
| Event Bus (prod) | MassTransit + Azure Service Bus | Swap via config, no consumer code change |
| Real-time (dev) | ASP.NET Core SignalR (in-process) | Zero infrastructure |
| Real-time (prod) | Azure SignalR Service | Scales beyond single server |
| Cache (dev) | `IMemoryCache` | Built-in, zero config |
| Cache (prod) | Azure Cache for Redis | Distributed, shared across instances |
| AI (dev) | Google Gemini API via `HttpClient` | Free, same `IAIProvider` interface |
| AI (prod) | Azure OpenAI via `Azure.AI.OpenAI` SDK | Production LLM inference |
| Auth (dev) | JWT Bearer (`JwtBearer`) | Local token issuance for testing |
| Auth (prod) | Azure AD / Entra ID (`Microsoft.Identity.Web`) | Enterprise RBAC |
| Logging | Serilog + structured JSON | Console (dev), App Insights (prod) |
| Resilience | Polly | Retry + circuit breaker on AI calls |
| Mapping | AutoMapper | Entity ↔ DTO mapping profiles |

---

## Folder Structure

```
RetailFixIT/
├── frontend/
│   ├── index.html
│   ├── vite.config.ts
│   ├── tsconfig.json
│   ├── package.json
│   ├── .env                          # Dev: local API URL, jwt auth
│   ├── .env.production               # Prod: Azure URLs, MSAL config
│   └── src/
│       ├── main.tsx
│       ├── App.tsx
│       ├── api/                      # Axios-based typed API calls
│       │   ├── client.ts
│       │   ├── jobs.api.ts
│       │   ├── vendors.api.ts
│       │   ├── assignments.api.ts
│       │   └── recommendations.api.ts
│       ├── types/                    # TS interfaces matching backend DTOs
│       ├── store/                    # Zustand slices (auth, notifications)
│       ├── hooks/                    # useJobs, useSignalR, usePermissions, etc.
│       ├── features/
│       │   ├── auth/                 # LoginPage, AuthGuard
│       │   ├── dashboard/            # DashboardPage, JobTable, JobFilters
│       │   ├── jobs/                 # JobDetailPage, CreateJobModal
│       │   ├── assignments/          # AssignmentPanel, VendorSelector
│       │   ├── recommendations/      # AIRecommendationCard, RecommendationPanel
│       │   └── vendors/              # VendorListPage
│       ├── components/
│       │   ├── layout/               # AppShell, Sidebar, TopBar
│       │   ├── ui/                   # Button, Badge, Table, Modal, Spinner
│       │   └── rbac/                 # PermissionGate, RoleLabel
│       ├── lib/
│       │   ├── signalr.ts            # SignalR connection factory
│       │   ├── queryClient.ts        # TanStack Query config
│       │   └── msalConfig.ts         # Azure AD config (prod)
│       └── router/
│           ├── index.tsx
│           └── ProtectedRoute.tsx
│
└── backend/
    ├── RetailFixIT.sln
    ├── src/
    │   ├── RetailFixIT.API/
    │   │   ├── Controllers/          # Jobs, Vendors, Assignments, AI, Auth, AuditLogs
    │   │   ├── Hubs/JobHub.cs        # SignalR hub
    │   │   ├── Middleware/           # TenantResolution, ExceptionHandling
    │   │   ├── Program.cs            # Composition root (conditional dev/prod DI)
    │   │   ├── appsettings.json
    │   │   ├── appsettings.Development.json
    │   │   └── appsettings.Production.json
    │   ├── RetailFixIT.Application/
    │   │   ├── Common/Interfaces/    # ICacheService, IAuditService, ICurrentUser
    │   │   ├── Jobs/Commands/        # CreateJob, UpdateJob, CancelJob
    │   │   ├── Jobs/Queries/         # GetJobs (paged+filtered), GetJobById
    │   │   ├── Assignments/Commands/ # AssignVendor, UnassignVendor
    │   │   ├── Vendors/Queries/      # GetVendors, GetVendorById
    │   │   └── AI/Commands/          # RequestAIRecommendation
    │   ├── RetailFixIT.Domain/
    │   │   ├── Entities/             # Job, Vendor, Assignment, AIRecommendation, AuditLog, Tenant, UserRole
    │   │   ├── Enums/                # JobStatus, Priority, AssignmentStatus, UserRoleType
    │   │   ├── Events/               # JobCreated, JobAssigned, AIRecommendationRequested, AIRecommendationGenerated
    │   │   └── Interfaces/           # IJobRepository, IVendorRepository, IAssignmentRepository
    │   └── RetailFixIT.Infrastructure/
    │       ├── Persistence/
    │       │   ├── AppDbContext.cs   # EF Core context with global TenantId filters
    │       │   ├── Configurations/   # EF Cosmos fluent config (ToContainer, HasPartitionKey)
    │       │   └── Repositories/     # Job, Vendor, Assignment repositories
    │       ├── Messaging/
    │       │   ├── Consumers/        # JobCreated, AIRequested, AIGenerated, JobAssigned consumers
    │       │   └── MassTransitConfiguration.cs
    │       ├── AI/
    │       │   ├── IAIProvider.cs    # Interface: GenerateRecommendationAsync(JobContext)
    │       │   ├── GeminiAIProvider.cs     # Dev implementation
    │       │   └── AzureOpenAIProvider.cs  # Prod implementation
    │       ├── Caching/
    │       │   ├── MemoryCacheService.cs   # Dev
    │       │   └── RedisCacheService.cs    # Prod
    │       ├── Auth/
    │       │   ├── JwtTokenService.cs      # Dev
    │       │   └── CurrentUserService.cs
    │       └── Realtime/
    │           └── SignalRNotificationService.cs
    └── tests/
        ├── RetailFixIT.UnitTests/
        └── RetailFixIT.IntegrationTests/
```

---

## Database Schema (Key Fields)

| Entity | Key Fields |
|---|---|
| **Tenants** | Id, Name, Slug (unique), IsActive |
| **UserRoles** | Id, TenantId, UserId, Email, Role (Dispatcher/VendorManager/Admin/SupportAgent) |
| **Vendors** | Id, TenantId, Name, ServiceArea, Specializations (JSON), CapacityLimit, CurrentCapacity, Rating |
| **Jobs** | Id (Guid), TenantId, JobNumber, Title, Description, CustomerName, ServiceAddress, ServiceType, Status, Priority, ScheduledAt, AssignedVendorName (denormalized), _etag (Cosmos optimistic concurrency) |
| **Assignments** | Id (Guid), TenantId, JobId, VendorId, VendorName (denormalized), AssignedByUserId, Status (Active/Completed/Revoked), AssignedAt |
| **AIRecommendations** | Id (Guid), TenantId, JobId, Status, PromptSummary, RecommendedVendorIds (JSON), Reasoning, JobSummary, AIProvider, ModelVersion, LatencyMs |
| **AuditLogs** | Id (Guid), TenantId, EntityName, EntityId, Action, ChangedByUserId, OldValues (JSON), NewValues (JSON), OccurredAt |

All entities have a Cosmos container per type with `TenantId` as partition key. Global EF Core `HasQueryFilter` on `TenantId` enforces tenant isolation. VendorName is denormalized on Assignment and AssignedVendorName on Job to avoid cross-container joins (not supported in Cosmos EF Core).

---

## REST API Endpoints

```
# Auth (dev only)
POST   /api/v1/auth/login
GET    /api/v1/auth/me

# Jobs
GET    /api/v1/jobs                    ?page&pageSize&status&priority&search&sortBy
GET    /api/v1/jobs/{id}
POST   /api/v1/jobs
PUT    /api/v1/jobs/{id}
PATCH  /api/v1/jobs/{id}/status
DELETE /api/v1/jobs/{id}/cancel        [Admin only]
GET    /api/v1/jobs/{id}/timeline

# Vendors
GET    /api/v1/vendors                 ?page&isActive&hasCapacity
GET    /api/v1/vendors/{id}
POST   /api/v1/vendors                 [VendorManager, Admin]
PUT    /api/v1/vendors/{id}            [VendorManager, Admin]

# Assignments
GET    /api/v1/jobs/{jobId}/assignments
POST   /api/v1/jobs/{jobId}/assignments          [Dispatcher, Admin]
DELETE /api/v1/jobs/{jobId}/assignments/{id}     [Dispatcher, Admin]

# AI Recommendations
POST   /api/v1/jobs/{jobId}/recommendations
GET    /api/v1/jobs/{jobId}/recommendations
GET    /api/v1/jobs/{jobId}/recommendations/{id}

# Audit
GET    /api/v1/audit-logs              [Admin only]

# SignalR Hub
WS     /hubs/jobs
# Server->Client: JobCreated, JobUpdated, JobAssigned, AIRecommendationReady
```

---

## Dev vs Prod Configuration Strategy

`appsettings.json` defines schema with empty values. `appsettings.Development.json` sets local Cosmos DB emulator + Gemini API + in-memory SignalR/events/cache. `appsettings.Production.json` switches providers — all actual secrets injected by Azure App Configuration / Key Vault at runtime.

`Program.cs` reads a single `"Provider"` key per service and registers the matching implementation:
- `Auth.Provider`: `"Jwt"` → `AddJwtBearer` | `"AzureAd"` → `Microsoft.Identity.Web`
- `AI.Provider`: `"Gemini"` → `GeminiAIProvider` | `"AzureOpenAI"` → `AzureOpenAIProvider`
- `SignalR.Provider`: `"InMemory"` → in-process hub | `"AzureSignalR"` → `AddAzureSignalR()`
- `Messaging.Provider`: `"InMemory"` → MassTransit in-memory | `"AzureServiceBus"` → MassTransit AzureSB
- `Cache.Provider`: `"Memory"` → `IMemoryCache` | `"Redis"` → `StackExchange.Redis`

No consumer or application-layer code changes between dev and prod.

---

## Event Flow (Full Lifecycle)

```
1. POST /api/v1/jobs
   → CreateJobCommandHandler persists Job (Status: New)
   → Publishes JobCreatedEvent via MassTransit

2. JobCreatedConsumer
   → Validates, updates status to InReview
   → Publishes AIRecommendationRequestedEvent

3. AIRecommendationRequestedConsumer
   → Calls IAIProvider.GenerateRecommendationAsync()
     [Dev: Gemini HTTP call | Prod: Azure OpenAI SDK]
   → Polly retry (3x, exponential backoff)
   → Saves AIRecommendation record (Status: Completed or Failed)
   → Publishes AIRecommendationGeneratedEvent

4. AIRecommendationGeneratedConsumer
   → Broadcasts via SignalR: AIRecommendationReady { jobId, recommendationId }
   → Writes AuditLog entry

5. Dispatcher sees toast → opens AI panel → reviews recommendation → clicks Assign

6. POST /api/v1/jobs/{id}/assignments
   → AssignVendorCommandHandler creates Assignment, updates Job status to Assigned
   → Publishes JobAssignedEvent

7. JobAssignedConsumer
   → Broadcasts via SignalR: JobAssigned { jobId, vendorName }
   → Writes AuditLog entry
   → All connected dashboards refresh the row instantly
```

---

## Build Order

1. **Domain** — Entities, enums, events, repository interfaces (no dependencies)
2. **Contracts** — MassTransit event interfaces
3. **Infrastructure/Persistence** — AppDbContext, EF Cosmos configs (ToContainer/HasPartitionKey), global tenant filters, repositories (no migrations)
4. **Infrastructure/Services** — GeminiAIProvider, MemoryCacheService, JwtTokenService, AuditService
5. **Infrastructure/Messaging** — MassTransit consumers wired to in-memory transport
6. **Application** — MediatR commands/queries + handlers + FluentValidation + AutoMapper profiles
7. **API** — Controllers, SignalR hub, middleware, Program.cs (full conditional DI), Swagger
8. **Frontend Foundation** — Vite setup, Tailwind, axios client, React Query, Router, login
9. **Frontend Features** — Dashboard → Job Detail → Assignment → AI Panel → Vendors → Notifications
10. **Real-time Wiring** — `useSignalR` hook, query invalidation on events, toast notifications
11. **End-to-end Event Flow** — Full POST→event→AI→SignalR→dashboard cycle working
12. **Hardening** — Tests, `.http` files, `docker-compose.yml` for Cosmos DB emulator, README

---

## Verification

1. Start Cosmos DB emulator: `docker-compose up -d` (wait ~60s for emulator to initialize)
2. Run backend: `dotnet run` → EnsureCreatedAsync() creates containers automatically → Swagger at `http://localhost:5000/swagger`
3. POST `/api/v1/auth/login` with test credentials → get JWT
4. POST `/api/v1/jobs` → verify job created, AI recommendation triggers via in-memory bus
5. GET `/api/v1/jobs/{id}/recommendations` → verify AI response from Gemini
6. POST `/api/v1/jobs/{id}/assignments` → verify assignment created, audit log written
7. Run frontend: `cd frontend && npm run dev` → login, view dashboard, see real-time update
8. Open two browser tabs → assign job in tab 1 → confirm tab 2 updates via SignalR

---

## Critical Files (Implementation Start Points)

| File | Purpose |
|---|---|
| `backend/src/RetailFixIT.API/Program.cs` | Composition root — all conditional dev/prod DI registration |
| `backend/src/RetailFixIT.Infrastructure/Persistence/AppDbContext.cs` | Global tenant query filters — the security boundary |
| `backend/src/RetailFixIT.Infrastructure/AI/IAIProvider.cs` | Interface decoupling Gemini from Azure OpenAI |
| `backend/src/RetailFixIT.Infrastructure/Messaging/Consumers/` | Full event pipeline |
| `frontend/src/lib/signalr.ts` | SignalR connection factory used by all real-time hooks |
| `frontend/src/features/dashboard/DashboardPage.tsx` | Main entry point — drives React Query + table + filters |
