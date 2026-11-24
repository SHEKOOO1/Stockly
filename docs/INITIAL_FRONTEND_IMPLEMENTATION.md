# Initial Frontend Implementation — Stockly

| Field | Value |
|---|---|
| Date | 2026-11-24 |
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
- no login, logout, refresh, token storage, session handling, permission check,
  role evaluation or client-side authorization decision is implemented;
- no tenant, warehouse, price, quantity, cost or approval state from the demo UI
  is treated as authoritative;
- no persistence is claimed — all mutations are React state and reset on reload;
- no service worker, offline write, push subscription or shared-terminal flow is
  implemented;
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

All 34 routes have built content grouped as follows:

- **Platform (8):** dashboard, organisations, create organisation, organisation
  details, organisation administrators, subscriptions/plans, settings and
  activity.
- **Organization (8):** dashboard, profile, users, roles/permissions,
  warehouses, create warehouse, activity and settings.
- **Warehouse (18):** dashboard, overview, products, product details,
  categories, units, suppliers, stock overview, incoming, outgoing, movements,
  adjustments, stocktaking, alerts, expiration, reports, employees and settings.

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

Backend regression was re-run after the frontend work with
`STOCKLY_TEST_SQLSERVER` set: tool restore, clean `dotnet format`, clean
`-warnaserror` build, clean EF model-drift check and the full test run all
passed — **440 passed, 0 failed, 0 skipped**.

## 6. Status by category

| Category | Status |
|---|---|
| Shell, routing, design system, RTL layout | Implemented |
| Built page content for all 34 routes | Implemented |
| Local interactive preview behaviour | Implemented |
| Arabic i18n namespace scaffolding | Implemented |
| Mock data isolation | Implemented |
| Typed API client / DTOs / service adapters | Not started |
| Authentication, sessions, RBAC evaluation | Not started |
| Tenant or warehouse authorization decisions in client | Deliberately absent |
| Persistent writes | Deliberately absent |
| Service worker, offline writes, push | Not started |
| English/LTR locale | Not started |
| Shared terminal UI | Not started |
| Docker/component/container validation | Not claimed |
| Phase 8 production PWA | Not complete |

## 7. Relevant files

- `client/package.json`
- `client/src/App.tsx`
- `client/src/routes/AppRoutes.tsx`
- `client/src/navigation.ts`
- `client/src/context/AppContext.tsx`
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
- `.gitignore` — hand-written PWA manifest is allowed to be committed
