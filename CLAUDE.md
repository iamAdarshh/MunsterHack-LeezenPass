# LeezenPass

Digital bike pass for Münster, built at MÜNSTERHACK 2026 (25–26 Sep). Owners register their bike, buyers check a frame number before buying a used bike, stolen bikes are reported publicly and matched with community sightings.

Full spec (features, data model, API, algorithms): @docs/SPEC.md
Read the relevant SPEC section before implementing a feature. Backend and frontend details live in `api/CLAUDE.md` and `web/CLAUDE.md`.

## Hackathon rules for you

- Code freeze: **Sat 26 Sep 12:00**. Final pitch 16:00. A working demo beats perfect code.
- Build in this order and don't skip ahead:
  1. Registry (bike CRUD, photos) + public check endpoint
  2. Theft report + public stolen list + pass PDF
  3. **Seed data** (demo users incl. partner, ~200 bikes, ~60 theft reports, demo photos) — the stolen list must never be empty at a demo
  4. AI photo prefill
  5. Ownership transfer + certificate PDF
  6. Trust levels — core scope from SPEC feature 5 (time box 2.5 h)
  7. Goodwill points — core scope from SPEC feature 6 (time box 1 h)
  8. Optional: ONE stretch feature from SPEC 7–12
- Hard stop for new features: **Sat 09:00**. 09:00–12:00 is integration, `/demo-check`, fixes only.
- If a time box runs out, ship what works and move the rest to "Next steps" in the pitch. Cut order: admin dispute resolution → leaderboard → referral → partner UI (keep endpoint) → stretch feature.
- `main` must always build and run. Small commits, one slice at a time.
- Only stable/GA releases. No preview, RC or beta NuGet/npm packages.
- Every external dependency (LLM, email, captcha, storage) needs a fake so the demo still works offline (event Wi-Fi is unreliable).

## Stack

- API: ASP.NET Core 10 (LTS), Ardalis Minimal Clean template (single project, vertical slices, FastEndpoints), FluentValidation
- Data: EF Core 10 + Npgsql + NetTopologySuite, PostgreSQL + PostGIS (pgvector only for stretch)
- Auth: ASP.NET Core Identity API endpoints, cookie auth, same origin. **No IdentityServer, no JWT in the browser.**
- PDFs: QuestPDF (Community licence) + QRCoder. Images: SkiaSharp.
- Web: React 19, Vite, TypeScript (strict), Tailwind v4, React Router, TanStack Query, react-hook-form + zod, react-i18next (de, en), vite-plugin-pwa, MapLibre GL or react-leaflet
- Infra: Docker Compose (postgis, minio, mailpit). API and web run locally in dev.

## Repo layout

```
api/            ASP.NET Core API (Domain/, Features/<Area>/<UseCase>/, Infrastructure/) + tests
web/            React PWA (src/features/<area>/, src/components/, src/api/, src/i18n/)
seed/           Synthetic demo-data generator
infra/          docker-compose.yml, .env.example
docs/           SPEC.md, privacy page text, pitch notes
```

## Commands

All from the repo root. Verified 2026-09-25.

- Infra up: `docker compose -f infra/docker-compose.yml up -d --wait` (PostGIS 5432, MinIO 9000/9001, Mailpit 1025/8025)
- Local tools (dotnet-ef): `dotnet tool restore`
- API run: `dotnet run --project api/LeezenPass.Api` → http://localhost:5080 (migrates DB on startup; Scalar docs at `/scalar`)
- API build: `dotnet build api/LeezenPass.slnx`
- API test: `dotnet test --solution api/LeezenPass.slnx` (xunit.v3 on Microsoft.Testing.Platform, opted in via `global.json`)
- Format check: `dotnet format api/LeezenPass.slnx --verify-no-changes` (migrations are excluded as generated code)
- Migration: `dotnet ef migrations add <Name> --project api/LeezenPass.Api --output-dir Infrastructure/Data/Migrations` · `dotnet ef database update --project api/LeezenPass.Api`
- Web: `cd web && npm run dev` (http://localhost:5173, proxies `/api`) · `npm run build` · `npm run lint` (oxlint)
- AI model (local): `~/.lmstudio/bin/lms server start` · `~/.lmstudio/bin/lms load qwen/qwen3-vl-8b --context-length 8192` (API: `http://localhost:1234/v1`; `Vision__Provider=Fake` for canned data)
- Seed demo data: `dotnet run --project api/LeezenPass.Api -- seed` (DemoMode only; replaces all `*@demo.local` data, keeps other users; logins and check-demo numbers in `seed/README.md`)

## Architecture rules

- Vertical slices: one folder per use case with Endpoint, Request, Response, Validator. A slice owns its logic end to end.
- Domain (`Domain/`) has no references to EF Core, ASP.NET or HTTP. Business rules live in domain types, not endpoints.
- Value objects: `FrameNumber` (normalised + loose key), `FeinCode` (HMAC hash only), `TransferCode`.
- SOLID, pragmatically: interfaces ONLY for external dependencies or real second implementations:
  `IFileStorage`, `IVisionExtractor`, `IPdfRenderer`, `IEmailSender`, `ICaptchaVerifier`, `IClock`.
  Each has a real implementation and a Fake in `Infrastructure/`, selected by config.
- Sighting scoring is a list of `IScoreComponent`s (open/closed: add a component, don't edit others).
- No generic repository. Use `AppDbContext` directly in slices.
- No MediatR / extra layers unless the template already requires them.
- Trust rules live in the domain: `Bike.MarkEvidenceChecked()`, `Bike.MarkPartnerVerified()`, `Bike.InheritTrust(from)`. Endpoints never set `TrustLevel` directly.
- `GoodwillPolicy` (domain) is the only place with point values, caps and badge thresholds. `GoodwillService` (concrete class, no interface) writes the ledger inside the outcome's transaction.
- Roles: `Partner`, `Admin` (Identity roles). Partner/admin endpoints require the role.

## Privacy rules (non-negotiable)

- Public endpoints never return owner personal data (name, email, address, FEIN code).
- FEIN code: store only an HMAC-SHA256 hash with a server-side secret. Never log it, never return it.
- Strip EXIF (especially GPS) from every uploaded image before storing. Apply orientation first.
- `/api/check` returns status only. Photos/attributes only when the bike is reported stolen. Rate-limited + captcha.
- Risk map: only hex cells with ≥ 3 reports.
- Log hashed IPs only. No personal data in logs.
- Seed data is synthetic and labelled "Demo-Daten" in the UI.
- `/api/check` never reveals that a clean bike is registered. Trust labels appear in check results only for `stolen` and `verified_transfer`.
- Duplicate frame-number registration: one identical response whether the existing bike is stolen or not.
- Receipt and possession photos are owner-only.
- Leaderboard: opt-in users only, alias only.

## Code style

- C#: nullable on, file-scoped namespaces, records for DTOs, `CancellationToken` everywhere async, primary constructors where natural. Keep `dotnet format` clean.
- Errors: validation → 400 (FluentValidation), not found → 404, forbidden → 403. Never leak exception details.
- TypeScript: strict, no `any`, server state via TanStack Query, forms via react-hook-form + zod, all UI strings via i18n keys (German first).
- Mobile-first UI; the demo runs on a phone.
- Tests (xUnit): pure logic only (FrameNumber, FeinCode, TransferCode, scoring). No tests for trivial CRUD during the hackathon.

## How to work

- Before coding a slice, state the plan in ≤ 5 bullets (files to touch, endpoint, UI screen). Then implement.
- After changes: build, test, format check, `npm run lint && npm run build`. Fix what you broke.
- Adding or changing an endpoint → update the API table in `docs/SPEC.md`.
- Adding a package → say which and why, stable version only.
- Secrets via `.env` / user-secrets only; document keys in `infra/.env.example`. Never commit secrets.
- Commits: Conventional Commits (`feat(check): ...`). Don't push; the team pushes.
- If a task would take more than ~45 min, stop and propose a smaller cut.

## Definition of done (per slice)

- [ ] Works end to end in the UI on a phone-sized viewport
- [ ] Validation + error states handled; loading states shown
- [ ] Privacy rules above respected
- [ ] Builds, tests pass, lint/format clean
- [ ] Works with fakes (offline mode)
- [ ] SPEC API table up to date
