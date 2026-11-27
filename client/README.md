# Stockly client foundation

This directory contains the React + TypeScript + Vite foundation for Stockly. It is a local,
non-production review skeleton.

## Scope

- Three-level shell: PLATFORM → ORGANIZATION → WAREHOUSE.
- Arabic RTL interface with local i18n modules.
- Isolated demonstration data only. Every demo record carries `origin: 'demo'`.
- Local React state for context selectors, notifications, dialogs and toasts.
- Fully built demonstration pages for every approved route with local interactive previews.
- A demo-only shared-terminal gate: select an active demo user, enter any 6–12 digits, then
  access the shell; signing out or switching users clears the visible demo session.

## Explicit limitations

- No backend calls, real authentication, authorization, persistence or server-state claims.
- The shared-terminal PIN is a local format-only preview: it is never stored, logged, or
  checked against a secret, and it grants no real access.
- The PWA manifest, raster icons, SVG icon and maskable icon are committed, but there is
  **no service worker** and **no offline writes**. The app is online-first by design.
- All mutations are local preview interactions only.

## Commands

```text
npm install
npm run dev
npm run typecheck
npm run lint
npm run format:check
npm test
npm run build
```
