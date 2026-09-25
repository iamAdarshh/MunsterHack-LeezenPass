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

# 3. Web on http://localhost:5173 (proxies /api to the API)
cd web && npm install && npm run dev
```

Open http://localhost:5173. On a phone in the same Wi-Fi: `npm run dev -- --host` and open the printed network URL.

Dev config lives in `api/LeezenPass.Api/appsettings.Development.json` (fakes on, demo mode on, dev-only secrets).
Every config key is documented in [infra/.env.example](infra/.env.example).

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
| Stop infra | `docker compose -f infra/docker-compose.yml down` |

## Layout

```
api/     ASP.NET Core 10 API (FastEndpoints vertical slices) + xUnit tests
web/     React 19 PWA (Vite, Tailwind v4, TanStack Query, react-i18next)
infra/   docker-compose.yml, .env.example
docs/    SPEC.md
```
