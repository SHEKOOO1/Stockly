# Stockly (ستوكلي)

**Multi-tenant Warehouse Management SaaS.**

Stockly is a production-grade, multi-tenant warehouse management platform for
organizations such as houses, residences, conferences and hospitality
operations. It is **not** built around any single organization — every piece of
business data is isolated by tenant, and warehouse-level authorization is
enforced independently from roles.

> **Current state: Phase 0 — BLUEPRINT v1.0, blocked on owner ratification.**
> The design is complete and internally consistent; **no application code exists
> yet**. This repository currently contains **project memory documentation
> only**. 20 decisions still need an owner answer before Phase 1 may begin — see
> `docs/PROJECT_BLUEPRINT.md` §23.1 and §27.

---

## Repository layout

```text
Stockly/
├── AGENTS.md                    # Operating rules for AI coding agents
├── README.md                    # This file
├── .gitignore                   # Secrets / build output exclusion rules
├── .env.example                 # Non-secret configuration template (safe to commit)
└── docs/                        # THE PERSISTENT PROJECT MEMORY OF STOCKLY
    ├── PROJECT_BLUEPRINT.md     # Master specification
    ├── ARCHITECTURE.md          # Solution / module / deployment architecture
    ├── SECURITY_ARCHITECTURE.md # Threat model and security controls
    ├── DATABASE_DESIGN.md       # Entity catalogue and relational model
    ├── ROLES_PERMISSIONS.md     # Roles and the permission matrix
    ├── BUSINESS_RULES.md        # Invariants and validation rules
    ├── WORKFLOWS.md             # End-to-end operational workflows
    ├── API_CONVENTIONS.md       # REST API contract conventions
    ├── TESTING_STRATEGY.md      # Test pyramid, security tests, CI gates
    ├── DEVELOPMENT_STATUS.md    # CURRENT STATE (read this first)
      ├── DECISIONS.md             # Architecture decision records (ADR-0001–ADR-0044)
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
7. `docs/TESTING_STRATEGY.md` — how correctness is proven.
8. `docs/CHANGELOG.md` — what changed.

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
5. Batches + Expiry + Waste + Transfers + Stocktake + Locations + Barcode/QR
6. Events + Recipes + Food Cost + Forecasting
7. Reports + Notifications + SaaS + Search + Customization
8. Frontend PWA + Shared Terminal UI + Full System Testing + Production

Security is not a phase: a security regression is a build failure at every
phase. See `docs/PROJECT_BLUEPRINT.md` §25.

## Live project status

See [`docs/DEVELOPMENT_STATUS.md`](docs/DEVELOPMENT_STATUS.md).

## License

TBD — not yet decided.
