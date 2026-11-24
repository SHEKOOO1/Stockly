# Stockly (ستوكلي)

**Multi-tenant Warehouse Management SaaS.**

Stockly is a production-grade, multi-tenant warehouse management platform for
organizations such as houses, residences, conferences and hospitality
operations. It is **not** built around any single organization — every piece of
business data is isolated by tenant, and warehouse-level authorization is
enforced independently from roles.

> **Current state: blueprint v1.1 RATIFIED. Phase 1 is in progress, not
> complete.** The repository contains the Domain, Application, Infrastructure
> and API projects, two SQL Server migrations, six active test projects, local
> Docker/Nginx reference scaffolding and a CI workflow. **No frontend and no
> business API endpoints exist yet.** Docker validation has not been performed.
>
> Verification on 2026-11-23: clean format, clean `-warnaserror` build, clean EF
> model-drift check and **433 tests passing** against a real local SQL Server
> (322 unit, 51 architecture, 33 integration, 20 security, 3 concurrency,
> 4 migration).
>
> On 2026-09-30 the owner (1) decided the five questions that were blocking the
> Phase 1 foundations — locations, currency, transfers, legal entities, scale
> target; (2) **ratified the blueprint in full**, promoting ADR-0001 – ADR-0044
> to `Accepted` so that all 50 ADRs are `Accepted`; and (3) acknowledged
> SEC-KNW-01 – SEC-KNW-09 as known, accepted V1 limitations — they remain
> registered, not remediated.
>
> Sixteen questions remain open and ratification **did not resolve any of them**:
> each is still gated to a later phase or explicitly deferred. ISS-08 (ADR-0051
> never authored) and ISS-12 (ADR-0052 unratified) remain owner actions. See
> `docs/PROJECT_BLUEPRINT.md` §23.1 and §27.

---

## Repository layout

```text
Stockly/
├── AGENTS.md                    # Operating rules for AI coding agents
├── README.md                    # This file
├── Directory.Build.props        # Shared .NET build settings
├── Stockly.slnx                 # .NET 10 solution
├── Dockerfile                   # Local/reference API image
├── docker-compose.yml           # Local/reference Nginx + API + SQL Server stack
├── infra/nginx/                 # Local/reference TLS reverse proxy configuration
├── .config/dotnet-tools.json    # Pinned EF and coverage-report tools
├── .github/workflows/ci.yml     # Static and database-backed CI gates
├── .gitignore                   # Secrets / build output exclusion rules
├── .env.example                 # Non-secret configuration template (safe to commit)
├── src/                         # Domain, Application, Infrastructure and API projects
├── tests/                       # Unit, architecture, integration, security,
│                                # concurrency, migration and DB fixture projects
├── tools/                       # Database conformance gate
└── docs/                        # THE PERSISTENT PROJECT MEMORY OF STOCKLY
    ├── PROJECT_BLUEPRINT.md     # Master specification
    ├── ARCHITECTURE.md          # Solution / module / deployment architecture
    ├── SECURITY_ARCHITECTURE.md # Threat model and security controls
    ├── DATABASE_DESIGN.md       # Entity catalogue and relational model
    ├── ROLES_PERMISSIONS.md     # Roles and the permission matrix
    ├── BUSINESS_RULES.md        # Invariants and validation rules
    ├── WORKFLOWS.md             # End-to-end operational workflows
    ├── API_CONVENTIONS.md       # REST API contract conventions
    ├── BACKEND_API_CONTRACT_GAPS.md # Implemented-versus-documented API surface
    ├── TESTING_STRATEGY.md      # Test pyramid, security tests, CI gates
    ├── DEVELOPMENT_STATUS.md    # CURRENT STATE (read this first)
    ├── DECISIONS.md             # Architecture decision records (ADR-0001–ADR-0050)
    ├── IMPLEMENTATION_AUDIT.md  # Source-of-truth implementation audit
    ├── IMPLEMENTATION_STATUS.md # Current implementation handoff
    ├── INITIAL_SKELETON.md      # What actually exists in the skeleton
    ├── NEXT_DEVELOPMENT_STEPS.md # Phase-aligned next work
    └── CHANGELOG.md             # Meaningful change history
```

## Documentation is the source of truth

`docs/` is **not optional documentation**. It is Stockly's project memory.
A new developer — human or AI agent — must be able to understand the entire
current state of Stockly by reading these files and inspecting the repository,
**without access to any previous conversation**.

Read in this order:

1. `docs/DEVELOPMENT_STATUS.md` — where are we right now?
2. `docs/PROJECT_BLUEPRINT.md` — what are we building?
3. `docs/ARCHITECTURE.md` + `docs/DECISIONS.md` — how and why.
4. `docs/DATABASE_DESIGN.md` + `docs/BUSINESS_RULES.md` — the data model and invariants.
5. `docs/SECURITY_ARCHITECTURE.md` + `docs/ROLES_PERMISSIONS.md` — the security model.
6. `docs/API_CONVENTIONS.md` + `docs/WORKFLOWS.md` — the external contract.
7. `docs/BACKEND_API_CONTRACT_GAPS.md` — what of that contract is not implemented.
8. `docs/TESTING_STRATEGY.md` — how correctness is proven.
9. `docs/CHANGELOG.md` — what changed.

## Product rules (permanent, non-negotiable)

1. Stockly is multi-tenant. Tenant isolation is mandatory and server-enforced.
2. Warehouse authorization is independent from roles.
3. Devices belong to warehouses, not users.
4. Sessions belong to authenticated users operating on a device.
5. Only authorized administrators create users. Shared terminals never self-register.
6. PINs are authentication credentials and must be securely protected.
7. Inventory is transaction-based, auditable and concurrency-safe.
8. The frontend is never the security boundary.
9. There is no AI assistant / chatbot / copilot inside Stockly.
10. Never claim something passed unless it was actually tested.
11. One tenant is one legal entity, one currency, and warehouse-level stock
    (ADR-0048, ADR-0046, ADR-0045). There is no FX, no legal-entity hierarchy
    and no location-scoped inventory in V1.

## Technology direction

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core Web API (C#, .NET 10 LTS), Entity Framework Core, SQL Server |
| API style | REST + OpenAPI / Swagger |
| Frontend | React + TypeScript + Vite, PWA |
| Architecture | Modular Monolith |
| Infrastructure | Docker, Nginx reverse proxy, HTTPS/TLS, CI/CD |

Implementation is organised as **8 phases** (ADR-0040), not a linear
architecture-then-frontend sequence. Infrastructure belongs to Phase 1:

1. Foundation + Architecture + Database + Security Core
2. Identity + Tenants/Houses + Warehouses + RBAC
3. Products + Units + Inventory Engine
4. Devices + Shared Terminal + Purchasing + Suppliers
5. Batches + Expiry + Waste + Transfers + Stocktake + Barcode/QR
6. Events + Recipes + Food Cost + Forecasting
7. Reports + Notifications + SaaS + Search + Customization
8. Frontend PWA + Shared Terminal UI + Full System Testing + Production

Security is not a phase: a security regression is a build failure at every
phase. See `docs/PROJECT_BLUEPRINT.md` §25.

**Phase 5 does not deliver warehouse locations.** Inventory is warehouse-level
in V1 and V1.1 (ADR-0045); descriptive sub-locations are V1.1 scope and own no
stock; location-level stock is a V2 redesign.

## Live project status

See [`docs/DEVELOPMENT_STATUS.md`](docs/DEVELOPMENT_STATUS.md).

## License

TBD — not yet decided.
