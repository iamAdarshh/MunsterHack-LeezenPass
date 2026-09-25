---
name: demo-check
description: Pre-pitch smoke test of the full LeezenPass demo flow, online and offline (fakes), with a go/no-go report.
disable-model-invocation: true
---

Run a pre-pitch smoke test. Do not add features; only fix blockers you find, and ask before any fix larger than ~15 lines.

1. Build and test: `dotnet build`, `dotnet test`, `cd web && npm run build`.
2. Start infra + API + web (see Commands in CLAUDE.md). Reset and load seed data.
3. Walk the three demo flows via the API (curl) and list the UI click path for each:
   - **A. Register with AI prefill**: log in as demo owner → upload photo → `/api/ai/extract` → register bike → download pass PDF.
   - **B. Check before you buy**: `/api/check` with a stolen demo frame number → `stolen` with photo; with a registered-only bike → `unknown` without data; with a mistyped (O↔0) number → `possible_match`.
   - **C. Theft → recovery**: report theft → appears in `/api/stolen` without owner data → (if built) sighting → owner sees match → mark recovered.
   - **D. Transfer**: create code → claim as second demo user → certificate PDF + verify page.
   - **E. Verify ownership**: receipt (`…pass.jpg`) → challenge → possession photo (`…pass.jpg`) → "Per Beleg geprüft"; wrong or expired code fails with a retry prompt.
   - **F. Check privacy**: registered clean bike → `unknown` with the same response shape as a random frame number; stolen bike → `stolen` with trust label; open transfer → `verified_transfer` with trust label.
   - **G. Duplicate protection**: second user registers the stolen demo frame number and a clean registered one → identical 409; stolen bike's owner gets an email in Mailpit.
   - **H. Partner + goodwill**: partner registers a bike → customer claims → "Von Partner bestätigt"; registration +10, verification +20, retry does not double-credit.
4. Repeat A–D with `Features:UseFakes=true` (simulated no-internet).
5. Privacy spot check: grep public endpoint responses for email, FEIN, owner name; check an uploaded file has no EXIF GPS.
6. Report a table: flow · online · offline · issue. End with GO / NO-GO and the top 3 fixes if NO-GO.
