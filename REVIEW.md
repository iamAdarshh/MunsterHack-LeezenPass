# Code review guide – LeezenPass

Use this when reviewing a diff or PR. This is a 36-hour hackathon project: optimise for a safe, working demo, not for perfection.

## How to report

- Group findings by severity: **Blocker**, **Should fix**, **Nit**.
- Each finding: file:line, what is wrong, a concrete failure scenario, and a suggested fix.
- Maximum ~10 findings. Skip nits entirely if there are blockers.
- If the diff is fine, say so in one line. Don't invent issues.

## Blockers (always flag)

### Privacy and security
- Public endpoint (`/api/check`, `/api/stolen`, `/b/{token}`, `/api/sightings`, `/api/riskmap`) returns or leaks owner data: name, email, address, FEIN code, exact home location.
- FEIN code stored, logged or returned in plain text (must be HMAC hash only).
- Uploaded image stored without EXIF stripping.
- `/api/check` reveals photos/attributes for a bike that is only registered (not stolen), or lacks rate limiting/captcha.
- Risk map returns cells with fewer than 3 reports or raw theft points.
- Authorization missing: a user can read/modify another user's bike, theft report, transfer or matches.
- Transfer code: not single-use, not expiring, stored in plain text, or guessable (short/ambiguous alphabet).
- Secrets, API keys or connection strings committed; `.env` tracked.
- SQL built by string concatenation; raw SQL with user input not parameterised.
- Exception details or stack traces returned to clients.
- `/api/check` distinguishes a registered clean bike from an unknown one (different status, fields, timing), or `possible_match` searches beyond stolen/open-transfer bikes.
- Duplicate-registration responses differ between "stolen" and "registered" (message, status, type, timing).
- Trust level raised without both receipt AND possession checks (or without partner role / eligible transfer).
- Possession challenge reusable, not expiring after 10 min, not bound to bike + user, or stored in plain text.
- Receipt/possession photos reachable by anyone other than the owner.
- Goodwill points credited on submission instead of verified outcome; non-idempotent credit; credit outside the outcome's transaction.
- Leaderboard shows non-opted-in users, emails or real names.
- Partner or admin endpoint without role check.

### Correctness
- Frame-number lookup does not use `FrameNumber` normalisation (exact then loose).
- Async code without `await`, `.Result`/`.Wait()`, missing `CancellationToken` on DB calls in endpoints.
- EF migrations missing for model changes, or migration edits that drop data.
- Geography/SRID mistakes: mixing 4326 and 25832 without `ST_Transform`, distance computed in degrees.
- Breaks the build, tests, or the running demo flow.

### Demo risk
- New hard dependency on a network service without a Fake/offline fallback.
- Preview/RC/beta package added.
- Change touches many slices at once late in the event (after Sat 09:00).
- Seed data missing or broken (empty stolen list, no demo users, no bike per trust level).
- New feature work started after Sat 09:00.

## Should fix

- Business logic inside an endpoint that belongs in a domain type/value object.
- New interface with one implementation and no external dependency (unnecessary abstraction).
- Missing FluentValidation for a request that accepts user input.
- Missing loading/error state in the UI; hard-coded UI strings instead of i18n keys.
- `any` in TypeScript; server state held in `useState` instead of TanStack Query.
- Endpoint added/changed but `docs/SPEC.md` API table not updated.
- N+1 queries on list endpoints (stolen list, matches).
- Point values, caps or badge thresholds outside `GoodwillPolicy`.
- `TrustLevel` assigned directly instead of via domain methods.
- AI verification failure shown as a hard error instead of a retry prompt.

## Don't flag

- Formatting that `dotnet format` / Prettier handles.
- Missing tests for trivial CRUD.
- Naming preferences, minor duplication in two places.
- Missing features that are explicitly out of scope in `docs/SPEC.md`.

## Quick checklist

- [ ] No personal data on public endpoints
- [ ] FEIN hashed, EXIF stripped, transfer codes hashed/expiring
- [ ] Ownership checked on every owner endpoint
- [ ] Validation present, errors mapped to 400/403/404
- [ ] Works offline with fakes
- [ ] Builds, tests pass, lint clean
