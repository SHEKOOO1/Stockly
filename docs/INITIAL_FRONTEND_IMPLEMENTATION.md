# Initial Frontend Implementation — Stockly

| Field | Value |
|---|---|
| Date | 2026-11-24; addenda through 2026-11-27 |
| Status | **IMPLEMENTED — mocked/static frontend skeleton only** |
| Location | `client/` |
| Phase boundary | Early skeleton allowed by `PROJECT_BLUEPRINT.md` §25.2; **Phase 8 production PWA is not complete** |
| Commit | None — all changes are uncommitted working-tree changes |

## 1. Objective and boundary

This slice implements the approved local demonstration frontend from the handoff
decision recorded in `DEVELOPMENT_STATUS.md` §3a and
`NEXT_DEVELOPMENT_STEPS.md` §8. It is a navigable, Arabic-first, interactive
design-and-behaviour skeleton backed only by local demonstration data.

It is **not** a data-connected client:

- no `/api/v1/**` call, DTO contract, typed client, service adapter or API error
  handling is implemented;
- no real login, logout, refresh, token storage, session handling, permission
  check, role evaluation or client-side authorization decision is implemented;
  the shared-terminal flow in §11 is a local format-only demo gate, not auth;
- no tenant, warehouse, price, quantity, cost or approval state from the demo UI
  is treated as authoritative;
- no persistence is claimed — all mutations are React state and reset on reload;
- no service worker, offline write, push subscription or production shared-terminal
  flow is implemented;
- no Docker, Compose, Nginx or hosted CI validation is claimed.

No ADR, phase decision, design count or backend contract was changed. Phase 1
remains **IN PROGRESS**, and the full frontend remains a Phase 8 deliverable.

## 2. Implemented structure

`client/` contains a React 18 + TypeScript + Vite application with its own
`package.json`, ESLint/Prettier/Vitest configuration, local icon, PWA manifest,
typed domain model, i18n modules, layout components, reusable UI components,
34 page components and smoke tests.

### 2.1 Routes

`client/src/routes/AppRoutes.tsx` maps:

| Context | Routes |
|---|---|
| Platform | 8 |
| Organization | 8 |
| Warehouse | 18 |
| Total page routes | **34** |

`/` and unknown paths redirect to `/platform`. `client/src/navigation.ts` carries
matching route metadata, 29 sidebar/command-palette items, breadcrumbs and
scope-derived navigation for the three contexts: platform, organization and
warehouse.

No page imports or renders `PlaceholderPage`. The component remains as an
unused export, and some `placeholderKey` metadata/i18n strings are retained but
have no runtime consumer; they do not produce placeholder screens.

### 2.2 Application shell

Implemented in `client/src/components/layout/`:

- `AppShell`, `Sidebar`, `Topbar`, `Breadcrumbs`;
- `ContextRail` and `ContextSelectors` for local organisation/warehouse preview;
- `CommandPalette` for local route search and navigation;
- `NotificationsMenu`, `ProfileMenu` and `ToastRegion`;
- responsive navigation with a mobile close control (`.sidebar__close`).

### 2.3 Reusable UI

Implemented in `client/src/components/ui/`:

- `DataTable`, `SearchFilterBar`, `StatusBadge`, `Pagination`;
- `Modal`, `ConfirmDialog`, `FormField`, `Tabs`;
- `StatCard`, `ChartCard`, local SVG `Sparkline` and `BarChart`;
- `EmptyState`, `LoadingState`, `ErrorState`, `DemoNotice`;
- `Toast` and `ToastRegion`.

### 2.4 Pages

All 35 routes have built content grouped as follows:

- **Platform (8):** dashboard, organisations, create organisation, organisation
  details, organisation administrators, subscriptions/plans, settings and
  activity.
- **Organization (8):** dashboard, profile, users, roles/permissions,
  warehouses, create warehouse, activity and settings.
- **Warehouse (19):** dashboard, overview, warehouse operations, products,
  product details, categories, units, suppliers, stock overview, incoming,
  outgoing, movements, adjustments, stocktaking, alerts, expiration, reports,
  employees and settings.

Pages contain local tables, cards, charts, filters, detail views and form flows;
they are not generic blank placeholders.

## 3. Local interactions

The following interactions are implemented with local React state only:

- organisation and warehouse preview selection, with dependent warehouse lists
  and toast feedback (`client/src/context/AppContext.tsx`);
- notification read/unread/dismiss actions;
- global command palette with filtered local navigation and Escape handling;
- in-page search and filters, sortable data tables and pagination;
- local create/edit/delete preview flows with modals, validation, confirmation
  dialogs and toasts;
- tabs on detail/profile pages;
- simulated loading, empty and error states via `useDemoLoader`;
- responsive sidebar and mobile navigation.

`showToast` is the feedback path for local actions. No interaction writes to a
server or to browser storage.

## 4. Data and localization

- All demonstration records live in `client/src/data/demoData.ts` and are marked
  `origin: 'demo'`.
- Demo organisation and warehouse names are generic; the first-target customer
  name is not hardcoded in the frontend source.
- Demo data shapes are illustrative only. They are **not** frozen DTO decisions
  and must not be copied into backend contracts.
- UI strings live in `client/src/i18n/{common,platform,organization,warehouse}.ts`
  with typed keys and `{name}` interpolation.
- The initial locale is Arabic (`ar-EG-u-nu-latn`); `index.html` and the PWA
  manifest set `dir="rtl"` and `lang="ar"`. Numeric/SKU/date cells use isolated
  `dir="ltr"` where needed.
- Arabic-only UI is a known limitation of this slice. English/LTR locale support
  from blueprint module 11 and ADR-0020 remains a later frontend task.

## 5. Validation evidence

Frontend gates run on 2026-11-24:

| Command | Result |
|---|---|
| `npm run format:check` | Pass — all files use Prettier style |
| `npm run typecheck` | Pass |
| `npm run lint` | Pass, `--max-warnings 0` |
| `npm test` | Pass — 1 test file, 5 tests |
| `npm run build` | Pass — Vite production build completed |

Browser verification against `http://127.0.0.1:4173` with the production build:

- all 34 routes returned `dir="rtl"`, a non-empty `h1` and `placeholders: 0`;
- scope selector changed `org-1` → `org-2` and produced a toast;
- command palette opened successfully;
- product search reached the empty state;
- a product-page action opened a modal;
- at 390 px viewport width, `scrollWidth` equalled `innerWidth` on the checked
  mobile view;
- no console or page errors were captured.

That browser scan covered the original 34 routes. The later warehouse operations
route is covered by component tests in §13; no browser scan was re-run in that
later pass.

Backend regression was re-run after the frontend work with
`STOCKLY_TEST_SQLSERVER` set: tool restore, clean `dotnet format`, clean
`-warnaserror` build, clean EF model-drift check and the full test run all
passed — **440 passed, 0 failed, 0 skipped**.

## 6. Status by category

| Category | Status |
|---|---|
| Shell, routing, design system, RTL layout | Implemented |
| Built page content for all 35 routes | Implemented |
| Local interactive preview behaviour | Implemented |
| Arabic i18n namespace scaffolding | Implemented |
| Mock data isolation | Implemented |
| Typed API client / DTOs / service adapters | Not started |
| Authentication, sessions, RBAC evaluation | Not started |
| Tenant or warehouse authorization decisions in client | Demo-only visibility filtering; production authorization remains server-side |
| Persistent writes | Deliberately absent |
| Service worker, offline writes, push | Not started |
| English/LTR locale | Not started |
| Shared terminal UI | Demo-only format gate implemented; production flow not started |
| Docker/component/container validation | Not claimed |
| Phase 8 production PWA | Not complete |

## 7. Relevant files

- `client/package.json`
- `client/src/App.tsx`
- `client/src/routes/AppRoutes.tsx`
- `client/src/navigation.ts`
- `client/src/context/AppContext.tsx`
- `client/src/context/DemoSessionContext.tsx`
- `client/src/components/terminal/TerminalGate.tsx`
- `client/src/data/demoData.ts`
- `client/src/i18n/`
- `client/src/components/layout/`
- `client/src/components/ui/`
- `client/src/pages/platform/`
- `client/src/pages/organization/`
- `client/src/pages/warehouse/`
- `client/src/styles/`
- `client/src/App.test.tsx`
- `client/public/manifest.webmanifest`
- `client/public/icon.svg`
- `client/public/icon-maskable.svg`
- `client/public/icon-192.png`
- `client/public/icon-512.png`
- `.gitignore` — hand-written PWA manifest is allowed to be committed

## 8. Category management completion (2026-11-27)

The Categories page is now a complete local demonstration feature. This update
does not change the mocked/static boundary in §1: all mutations stay in React
state and reset on reload.

### 8.1 Implemented behaviour

- `client/src/data/demoCategoryStore.tsx` provides a shared in-memory category
  store (`categories`, `categorySummaries`, `addCategory`, `updateCategory`,
  `archiveCategory`, `restoreCategory`, `getCategoryById`,
  `getCategorySummaries`, `getSelectableCategories`) and is wired through
  `client/src/App.tsx`.
- Categories, Products, Product Details, Overview, Stock and Expiration pages
  now use the shared store instead of independent local category arrays.
- `client/src/lib/category.ts` centralises trim/case-insensitive validation,
  minimum lengths, duplicate name/code checks, self-parent and descendant-cycle
  prevention, defensive tree flattening, display labels and derived product
  counts.
- The Categories page supports local create/edit/details, search, active/archived
  filters, hierarchical table rows, derived active-product counts and
  archive/restore with confirmation and Arabic feedback.
- Archived categories are excluded from parent and product-form selectable
  lists. Existing products retain their `categoryId`; archived category labels
  gain the `(مؤرشف)` suffix.
- Seed `productCount` values in `client/src/data/demoData.ts` remain illustrative.
  The UI derives counts from the shared product store and excludes archived
  products by default.

### 8.2 Verification

| Command | Result |
|---|---|
| `npm test -- --run` | Pass — 7 test files, 42 tests |
| `npm run format:check` | Pass — all files use Prettier style |
| `npm run typecheck` | Pass |
| `npm run lint` | Pass, `--max-warnings 0` |
| `npm run build` | Pass — Vite production build completed; only the existing >500 kB chunk-size warning |

New and rewritten tests cover store CRUD, derived counts, validation, hierarchy
safety, defensive flattening, archive/restore, cross-page category availability
and no-fetch behaviour:

- `client/src/data/demoCategoryStore.test.tsx`
- `client/src/lib/category.test.ts`
- `client/src/pages/warehouse/CategoriesPage.test.tsx`

### 8.3 Boundary

No `/api/v1/**` call, DTO contract, persistence, authentication, RBAC
evaluation, tenant/warehouse authorization decision or service worker was added.
Backend category CRUD, authoritative product counts, hierarchy invariants and
`master.categories.read` / `master.categories.manage` enforcement remain owning
Phase 3 work. The client-side `Category` fields `isActive`, `isDeleted` and
`updatedAt` are demo UI state only, not a frozen backend schema decision. No ADR,
TBD status, phase boundary or design count changed.

## 9. Units of measurement completion (2026-11-27)

The Units page is now a complete local demonstration feature. It follows the same
boundary as §8: all mutations stay in React state and reset on reload.

### 9.1 Implemented behaviour

- `client/src/data/demoUnitStore.tsx` provides a shared in-memory unit store
  (`units`, `unitSummaries`, `addUnit`, `updateUnit`, `archiveUnit`, `restoreUnit`,
  `getUnitById`, `getUnitSummaries`, `getSelectableUnits`, `getUsageCount`) and is
  wired through `client/src/App.tsx`.
- The Units page, product form, Products, Product Details, Stock, Expiration,
  Dashboard, Incoming and Outgoing views now use shared unit data and consistent
  `name (symbol)` labels instead of independent `demoUnits` arrays.
- `client/src/lib/unit.ts` centralises required/duplicate validation,
  case-insensitive symbol normalisation, decimal-range validation, unit-type
  translation keys, display labels, selectability rules and usage-count
  derivation.
- The Units page supports local create/edit/details, search, active/archived
  filters, symbol/type/decimals/lifecycle table columns, derived active-product
  usage counts, and archive/restore with Arabic feedback.
- Archiving is blocked when an active demo product references the unit as its
  base or purchase unit. Existing references are retained; archived unit labels
  gain the `(مؤرشف)` suffix.
- Archived units are excluded from product-form selectable lists unless they are
  already referenced by the product being edited. This preserves cross-warehouse
  seed references in the demo catalogue.
- Seed units in `client/src/data/demoData.ts` now include demo-only lifecycle and
  type fields and five additional realistic units across the seeded warehouses.

### 9.2 Verification

| Command | Result |
|---|---|
| `npm test -- --run` | Pass — 10 test files, 58 tests |
| `npm run format:check` | Pass — all files use Prettier style |
| `npm run typecheck` | Pass |
| `npm run lint` | Pass, `--max-warnings 0` |
| `npm run build` | Pass — Vite production build completed; only the existing >500 kB chunk-size warning |

New and rewritten tests cover unit validation, symbol normalisation, derived
usage counts, archive guards, cross-page shared-store behaviour, product-form
selectability and no-fetch behaviour:

- `client/src/lib/unit.test.ts`
- `client/src/data/demoUnitStore.test.tsx`
- `client/src/pages/warehouse/UnitsPage.test.tsx`

### 9.3 Boundary

No `/api/v1/**` call, DTO contract, persistence, authentication, RBAC
evaluation, tenant/warehouse authorization decision or service worker was added.
Backend unit CRUD, authoritative usage counts, conversion rules and
`master.units.read` / `master.units.manage` enforcement remain owning Phase 3
work. The client-side `Unit` fields `unitType`, `isActive`, `isDeleted` and
`updatedAt` are demo UI state only, not a frozen backend schema decision. Product
selectors intentionally use tenant-wide active units plus the current archived
reference; this is a demo usability rule and not a backend authorization or
master-data scope decision. No ADR, TBD status, phase boundary or design count
changed.

## 10. Supplier management completion (2026-11-27)

The Suppliers page is now a complete local demonstration feature. It follows the
same boundary as §8 and §9: all mutations stay in React state and reset on
reload.

### 10.1 Implemented behaviour

- `client/src/data/demoSupplierStore.tsx` provides a shared in-memory supplier
  store (`suppliers`, `supplierSummaries`, `addSupplier`, `updateSupplier`,
  `archiveSupplier`, `restoreSupplier`, `getSupplierById`,
  `getSupplierSummaries`, `getSelectableSuppliers`, `getUsageCount`) and is wired
  through `client/src/App.tsx`.
- The Suppliers page, product form, Products, Product Details and Incoming views
  now use shared supplier data instead of independent `demoSuppliers` arrays.
- `client/src/lib/supplier.ts` centralises required/duplicate validation, code
  normalisation, phone/email checks, selectability rules, display labels and
  usage-count derivation. Duplicate checks are scoped by `organizationId`.
- The Suppliers page supports local create/edit/details, search, active/archived
  filters, contact and lifecycle table columns, derived active-product usage
  counts, and archive/restore with Arabic feedback.
- Archiving preserves product references. Archived suppliers remain displayable
  with the `(مؤرشف)` suffix but are excluded from new product selectors; the
  supplier already referenced by the product being edited remains selectable.
- Product-form options use `name (code)` labels so renamed suppliers stay
  identifiable. Products, Product Details and Incoming views use the shared
  store for consistent labels.
- Seed suppliers in `client/src/data/demoData.ts` now include realistic contact
  fields and an archived demo supplier.

### 10.2 Verification

| Command | Result |
|---|---|
| `npm test -- --run` | Pass — 13 test files, 71 tests |
| `npm run format:check` | Pass — all files use Prettier style |
| `npm run typecheck` | Pass |
| `npm run lint` | Pass, `--max-warnings 0` |
| `npm run build` | Pass — Vite production build completed; only the existing >500 kB chunk-size warning |

New and updated tests cover supplier validation, organization-scoped duplicates,
derived usage counts, archive/restore, product-form selectability, no-fetch
behaviour, preserved product references and propagation of supplier renames into
product lists and details:

- `client/src/lib/supplier.test.ts`
- `client/src/data/demoSupplierStore.test.tsx`
- `client/src/pages/warehouse/SuppliersPage.test.tsx`
- `client/src/pages/warehouse/ProductsPage.test.tsx`

### 10.3 Boundary

No `/api/v1/**` call, DTO contract, persistence, authentication, RBAC
evaluation, tenant/warehouse authorization decision or service worker was added.
Backend supplier CRUD, authoritative duplicate checks, usage counts and archive
rules, and `purchasing.suppliers.read` / `purchasing.suppliers.manage`
enforcement remain owning Phase 5 work. The client-side `Supplier` fields
`isActive`, `isDeleted` and `updatedAt` are demo UI state only, not a frozen
backend schema decision. Product selectors intentionally use organization-scoped
active suppliers plus the current archived reference; this is a demo usability
rule and not a backend authorization or master-data scope decision. No ADR, TBD
status, phase boundary or design count changed.

## 11. Demo shared-terminal UX (2026-11-27)

`DemoSessionProvider` wraps the app below `AppProvider`. It keeps the selected
demo user and session entirely in memory. `TerminalGate` renders instead of the
shell until a session exists: the operator chooses an active `demoUsers` record
and enters any 6–12 digits. The keypad state is a masked digit count only; there
is no correct PIN, no secret comparison, no token, no storage and no network
call. The UI explicitly labels the flow as local preview, not authentication.

`AppShell`, `AppRoutes`, the command palette and all demo store providers are
mounted only after the gate is satisfied. Switching users or signing out clears
the session, resets `AppContext` state and remounts the demo stores by session
user id, so a previous operator's local preview state is not visible. The
profile menu reads the active demo user and exposes both actions. Page-level
tests pass `App initialDemoSessionUserId="user-1"` only as a test bootstrap;
app-level tests start at the gate and exercise the real preview flow.

Responsive changes add 44–60px touch targets for gate and shell controls, a
single-column mobile user list, larger keypad buttons and explicit invalid PIN,
checking, offline/unavailable, session-ended and switched-user states. PWA
metadata was tightened with stable `id`, explicit `/` start URL/scope, 192px and
512px PNG icons plus a separate maskable SVG. No service worker, offline write
or production PWA capability is claimed.

### 11.1 Verification

| Command | Result |
|---|---|
| `npm test -- --run` | Pass — 14 test files, 81 tests |
| `npm run format:check` | Pass — all files use Prettier style |
| `npm run typecheck` | Pass |
| `npm run lint` | Pass, `--max-warnings 0` |
| `npm run build` | Pass — Vite production build completed; only the existing >500 kB chunk-size warning |

### 11.2 Boundary

No `/api/v1/**` call, DTO contract, persistence, real authentication, RBAC
evaluation, tenant/warehouse authorization decision or service worker was added.
The terminal identity endpoint, enrollment codes, PIN hashing, throttling and
token sessions in `SECURITY_ARCHITECTURE.md`, `WORKFLOWS.md` and
`API_CONVENTIONS.md` remain Phase 2/4 backend work. This UI must not be treated
as evidence that name selection or a demo PIN grants access. No ADR, TBD status,
phase boundary or design count changed.

## 12. Demo inventory operations (2026-11-27)

`DemoInventoryStoreProvider` shares in-memory movement, stock-count and
adjustment state across the Incoming, Outgoing, Adjustments, Stocktaking, Product
Details, Movements, Dashboard, Reports and Alerts pages. `applyInventoryDelta`
updates product stock balances in `demoProductStore`, so receipts, issues,
adjustments and stock counts are visible outside the originating form.

The demo applies local-only rules: required product and warehouse context,
positive integer quantities, no negative balances, archived-product exclusion
from new operation forms, duplicate-request rejection and Arabic feedback for
validation, archived-product and insufficient-stock failures. Stock counts create
a local count session and a signed adjustment for the difference between
expected and counted quantities. Movement history remains readable for archived
products.

### 12.1 Verification

| Command | Result |
|---|---|
| `npm test -- --run` | Pass — 16 test files, 91 tests |
| `npm run format:check` | Pass — all files use Prettier style |
| `npm run typecheck` | Pass |
| `npm run lint` | Pass, `--max-warnings 0` |
| `npm run build` | Pass — Vite production build completed; only the existing >500 kB chunk-size warning |

The React Router future-flag warnings printed by tests are pre-existing test
warnings. New coverage is in `client/src/data/demoInventoryStore.test.tsx` and
`client/src/pages/warehouse/InventoryOperations.test.tsx`.

### 12.2 Boundary

No `/api/v1/**` call, DTO contract, persistence, real authentication, RBAC
evaluation, tenant/warehouse authorization decision, service worker or
offline-write capability was added. Backend posting, FEFO allocation, immutable
ledger rows, count snapshots, idempotency, audit and outbox behaviour remain
Phase 3/5 work. The local `StockBalance` projection, movement shapes and request
ids are demo UI state, not frozen backend contracts. No ADR, TBD status, phase
boundary or design count changed.

## 13. Demo warehouse operations (2026-11-27)

A daily warehouse operations screen was added at `/warehouse/operations`. It
shows Arabic RTL product search/status filters, low-stock indicators, current
balance and unit, quick quantity add/issue with a live balance preview and
optional reason, and an Add Item form with initial balance and minimum threshold.
Quick operations write through `DemoInventoryStoreProvider`, so balances and
movements remain shared with the existing inventory views.

Demo users carry a `roleCode` used only for local route, command, context and
notification visibility. Operators land on the operations screen and do not see
management tabs; low-stock notifications are raised only for the demo
manager/owner audience and are deduplicated, resolved and reopened locally. This
is advisory UI state only. It introduces no permission code, no role, no RBAC
evaluation and no tenant/warehouse authorization decision.

### 13.1 Verification

| Command | Result |
|---|---|
| `npm test -- --run` | Pass — 17 test files, 100 tests |
| `npm run format:check` | Pass — all files use Prettier style |
| `npm run typecheck` | Pass |
| `npm run lint` | Pass, `--max-warnings 0` |
| `npm run build` | Pass — Vite production build completed; only the existing >500 kB chunk-size warning |

New coverage is in `client/src/pages/warehouse/WarehouseOperations.test.tsx` and
`client/src/data/demoInventoryStore.test.tsx`. React Router future-flag warnings
printed by tests are pre-existing warnings. The backend suite was not re-run in
this pass; recorded evidence remains 440 passed / 0 failed / 0 skipped.

### 13.2 Boundary

No `/api/v1/**` call, DTO contract, persistence, real authentication, RBAC
evaluation, tenant/warehouse authorization decision, service worker, offline-write
capability, ADR, TBD answer, phase-boundary change or design-count change was
added. The stock operations remain local in-memory preview state, and the real
server must remain authoritative in later phases.
