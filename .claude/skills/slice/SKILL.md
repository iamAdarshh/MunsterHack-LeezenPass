---
name: slice
description: Implement one LeezenPass vertical slice end to end (API endpoint, domain logic, migration, UI screen, i18n) following CLAUDE.md and docs/SPEC.md.
argument-hint: <slice name, e.g. "check" or "theft-report">
disable-model-invocation: true
---

Implement the slice: **$ARGUMENTS**

1. Read `docs/SPEC.md` (feature, data model rows, API rows for this slice) and the relevant `api/CLAUDE.md` / `web/CLAUDE.md`.
2. Post a plan in ≤ 5 bullets: files to create/change, endpoint(s), migration (if any), UI screen(s), what the fake does offline. Then continue without waiting unless something is ambiguous.
3. Backend:
   - Domain logic in `Domain/` (value objects, rules). Unit-test pure logic.
   - Endpoint + request/response records + FluentValidation validator in `Features/<Area>/<UseCase>/`.
   - Ownership checks on owner endpoints (404 if not owner). Privacy rules from CLAUDE.md.
   - Migration if the model changed.
4. Frontend:
   - Page/components in `web/src/features/<area>/`, TanStack Query hooks, react-hook-form + zod, i18n keys in `de.json` and `en.json`.
   - Loading, empty and error states. Test at 375 px width.
5. Verify: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `cd web && npm run lint && npm run build`. Exercise the endpoint with curl against the running API.
6. Update the API table in `docs/SPEC.md` if routes changed.
7. Run the `code-reviewer` subagent on the diff and fix any Blockers.
8. Summarise: what works, how to demo it (click path), known gaps. Propose a Conventional Commit message. Do not push.
