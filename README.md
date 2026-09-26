# LeezenPass

Digital bike pass for Münster (MÜNSTERHACK 2026). Owners register their bike, buyers check a frame number before
buying a used bike, stolen bikes are listed publicly. Spec: [docs/SPEC.md](docs/SPEC.md).

## Prerequisites

- .NET SDK 10 (`dotnet --version` ≥ 10.0.100)
- Node.js 22 + npm
- Docker (Desktop or compatible)

## First run

```bash
# 1. Infrastructure: PostGIS (5432), MinIO (9000/9001), Mailpit (SMTP 1025, UI 8025)
cp infra/.env.example infra/.env            # optional, defaults work for local dev
docker compose -f infra/docker-compose.yml up -d --wait

# 2. API on http://localhost:5080 (applies migrations on startup)
dotnet tool restore                          # installs dotnet-ef locally
dotnet run --project api/LeezenPass.Api

# 3. Demo data (users, ~200 bikes, 60 theft reports); re-run any time, see seed/README.md
dotnet run --project api/LeezenPass.Api -- seed

# 4. Web on http://localhost:5173 (proxies /api to the API)
cd web && npm install && npm run dev
```

Open http://localhost:5173. On a phone in the same Wi-Fi: `npm run dev -- --host` and open the printed network URL.

Dev config lives in `api/LeezenPass.Api/appsettings.Development.json` (fakes on, demo mode on, dev-only secrets).
Every config key is documented in [infra/.env.example](infra/.env.example).

## Login accounts

All accounts are **local development / demo accounts** with made-up data. They only exist while
`Features:DemoMode=true`; never reuse these passwords anywhere real.

### Seeded demo accounts (every machine, after `-- seed`)

Password for all: **`LeezenDemo2026`**

| Email | Role | What it's for |
| --- | --- | --- |
| `owner@demo.local` | – (owner) | Main demo account: Gazelle (stolen), Kalkhoff (open transfer, code `PASS-2345`), Stevens (clean, for the ownership check). 60 goodwill points |
| `buyer@demo.local` | – (buyer) | Claims the Kalkhoff with `PASS-2345`, or the partner bike. Starts with no bikes and 0 points |
| `partner@demo.local` | **Partner** | Bike shop. Owns a Riese & Müller e-bike (`Von Partner bestätigt`); hand it over with a transfer code and the customer gets +10 points |
| `admin@demo.local` | **Admin** | Role exists; there are no admin screens yet (dispute resolution was cut) |
| `nutzer01@demo.local` … `nutzer40@demo.local` | – | Background users who own the other ~190 bikes |

The seed recreates these accounts every time it runs, so data you change with them resets on the next seed.
Frame numbers and codes for the check demo are in [seed/README.md](seed/README.md).

### Accounts created by hand during development (this machine's database only)

These are not seeded. A fresh database won't have them, and the seed doesn't touch them.

| Email | Password | Role | Notes |
| --- | --- | --- | --- |
| `demo@leezenpass.local` | `Demo1234!` | – | First test account |
| `owner1@leezenpass.local` | `Demo1234` | – | 1 bike |
| `owner2@leezenpass.local` | `Demo1234` | – | 1 bike |
| `owner3@leezenpass.local` | `Demo1234` | – | 1 bike |
| `claude-test@leezenpass.local` | `Test1234` | – | Claude Code test account, no bikes; safe to delete |
| `claude-buyer@leezenpass.local` | `Test1234` | – | Claude Code test account, no bikes; safe to delete |
| `claude-third@leezenpass.local` | `Test1234` | – | Claude Code test account, no bikes; safe to delete |

New accounts: register in the app (Login tab → "Konto erstellen"). Passwords need 8+ characters with upper case, lower case
and a digit. Roles can only be assigned by the seed (or directly in the `user_roles` table).

## AI photo prefill (LM Studio)

The Register page can suggest type, colours, brand and frame number from photos. In development this uses
**Qwen3-VL-8B** running locally in [LM Studio](https://lmstudio.ai) (about 6 GB of RAM, works offline):

```bash
~/.lmstudio/bin/lms get qwen/qwen3-vl-8b                          # once, ~5.8 GB
~/.lmstudio/bin/lms server start                                  # OpenAI-compatible API on :1234
~/.lmstudio/bin/lms load qwen/qwen3-vl-8b --context-length 8192   # ~16 s
~/.lmstudio/bin/lms unload --all                                  # free the RAM again
```

If LM Studio isn't running, the form simply stays manual. To use canned suggestions instead set
`Vision__Provider=Fake`. Load the model before the pitch: the API warms it up at startup, but a cold load takes ~16 s.

## Offline / demo mode

`Features:UseFakes=true` (default in Development) swaps email and captcha for fakes, so the demo works without
Wi-Fi. AI vision follows `Vision:Provider` instead: Development uses the local LM Studio model (works offline too);
set `Vision__Provider=Fake` for canned suggestions without LM Studio. `Features:DemoMode=true` makes the web app show the "Demo-Daten" banner.

## Everyday commands

| What | Command |
| --- | --- |
| Build API | `dotnet build api/LeezenPass.slnx` |
| Test API | `dotnet test --solution api/LeezenPass.slnx` |
| Format check | `dotnet format api/LeezenPass.slnx --verify-no-changes` |
| Add migration | `dotnet ef migrations add <Name> --project api/LeezenPass.Api --output-dir Infrastructure/Data/Migrations` |
| Apply migrations | `dotnet ef database update --project api/LeezenPass.Api` |
| Web lint / build | `cd web && npm run lint && npm run build` |
| API docs (dev) | http://localhost:5080/scalar |
| Mail inbox (dev) | http://localhost:8025 |
| MinIO console | http://localhost:9001 (see `infra/.env.example` for login) |
| Reset demo data | `dotnet run --project api/LeezenPass.Api -- seed` |
| Stop infra | `docker compose -f infra/docker-compose.yml down` |

## Layout

```
api/     ASP.NET Core 10 API (FastEndpoints vertical slices) + xUnit tests
web/     React 19 PWA (Vite, Tailwind v4, TanStack Query, react-i18next)
infra/   docker-compose.yml, .env.example
docs/    SPEC.md
```
