# LeezenPass – Specification

## Problem

- 4,561 bike thefts in Münster in 2025 (about 1 in 7 reported crimes), clearance rate 12.6%.
- Highest per-capita rate in Germany (~1,500 per 100,000 residents).
- Police bike-pass app discontinued; Münster police online registration no longer available.
- Bikes get stolen because they are easy to resell. LeezenPass attacks the resale market.

## Three levers

| Lever   | What                                                          | Effect                                                 |
| ------- | ------------------------------------------------------------- | ------------------------------------------------------ |
| Deter   | Visible QR tag, FEIN coding promoted                          | Registered bikes are harder to sell                    |
| Verify  | Frame-number check before buying, verified ownership transfer | Buyers learn to check; market for stolen bikes shrinks |
| Recover | Public stolen list, community sightings matched to owners     | More bikes returned                                    |

## Features

### MVP (must work live at the pitch)

1. **Bike pass (registry)**
   - Fields: frame number, FEIN code (hashed), brand, model, type, colours, e-bike flag, battery serial, features, purchase date.
   - Photos: side, frame-number close-up, details, receipt.
   - AI prefill: upload photo → vision LLM suggests type, colours, brand, features, frame number candidate. User confirms; never auto-save.
   - PDF export of the pass (for police and insurance).
2. **Check before you buy** (public, no login)
   - Input: frame number or FEIN code.
   - Results:
     - `stolen` (red): photo + attributes + trust label of the reporting owner.
     - `verified_transfer` (green): seller has an open transfer; shows trust label (the seller consented to disclosure by creating the transfer).
     - `possible_match` (amber): loose-key hit **among stolen bikes or bikes with an open transfer only**; photos to compare.
     - `unknown` (grey): everything else, **including registered bikes without an open transfer**. Warning-signs checklist.
   - The check never reveals that a clean bike is registered.
   - Rate-limited, captcha, hashed lookup logging.
3. **Theft report**
   - Time, approximate location (map pin), location note, lock type, optional police case number.
   - Police-ready summary + link to Polizei NRW Internetwache + insurance reminder.
   - Bike appears in public stolen list (photos, attributes, district, trust label; no owner data).
   - Share card for messengers/social, ending with "Registriere dein Rad in 60 Sekunden".
   - Owner can mark as recovered.
4. **Ownership transfer**
   - Seller creates 8-char code (unambiguous alphabet), valid 48 h, single use, stored hashed.
   - Buyer claims → owner changes, history kept, certificate PDF with QR to verification page.
   - Transfer keeps the sender's trust level (chain of custody).
   - On a completed transfer: the previous owner's receipt and possession photos are deleted (evidence rows stay, without the photo), the FEIN hash is cleared (the code encodes the previous owner's address; the new owner re-codes), and open possession challenges are cancelled.
   - Reporting a theft cancels all open transfer codes for that bike, so a stolen bike never shows `verified_transfer`.
   - The seller gets a "LeezenPass verifiziert" listing text linking to `/b/{token}`. The `/b/{token}` page belongs to this feature: photos, attributes, trust label, status, and "Verifizierter Verkauf offen" while a transfer is open; no owner data. The token is random (≥ 128 bits) and stays the same across transfers so QR stickers keep working.
5. **Ownership trust levels**
   | Level | Earned by | Label (de) |
   | --- | --- | --- |
   | `SelfDeclared` | Registration with verified email | "Selbst angegeben" |
   | `EvidenceChecked` | Receipt check passed **and** possession check passed | "Per Beleg geprüft" |
   | `ThirdPartyVerified` | Registered by a partner, or transferred from an owner at ≥ `EvidenceChecked` | "Von Partner bestätigt" |
   - Shown to the owner (bike list/detail), on the stolen list, and in check results only for `stolen` / `verified_transfer` (see 2).
   - **Receipt check**: owner uploads receipt photo → `IVisionExtractor.ExtractReceipt` → pass if frame number loose-matches, or brand matches AND model fuzzy-matches AND purchase date ≤ today.
   - **Possession check**: owner requests a challenge → random 4-digit code, valid 10 min, single use → owner photographs frame number with the code visible → `IVisionExtractor.CheckPossession` → pass if code equals the challenge and frame number loose-matches.
   - Both passed → `EvidenceChecked`. Trust never goes down automatically.
   - **Frame number already registered** (exact `frame_no_norm` match), whether the existing bike is stolen or not → **one identical 409 response** ("Diese Rahmennummer ist bereits vergeben. Du kannst eine Klärung beantragen."), same shape and timing. Behind the scenes: security event; if the existing bike is stolen, email its owner. Nothing about the existing bike or owner is revealed.
   - Residual risk: a logged-in user learns that a frame number exists. Mitigation: verified email required, max 5 conflicts per user per day, logged.
   - **Dispute (core)**: "Klärung beantragen" stores claimant + evidence photos with status `open`, notifies the existing owner by email. Resolution is manual (admin endpoint only if time allows).
   - **Partner registration (core)**: one seeded account with role `Partner` can register a single bike for a customer → bike starts `ThirdPartyVerified` under the partner → transfer code handed to the customer (normal transfer flow).
6. **Goodwill points**
   - Points only for **verified outcomes**, never raw submissions. Append-only, idempotent ledger.
     | Action | Points | Credited when | Scope |
     | --- | --- | --- | --- |
     | `register_bike` | 10 | On registration (max 3 bikes per user earn points) | Core |
     | `evidence_verified` | 20 | Bike reaches `EvidenceChecked` | Core |
     | `partner_registration` | 10 | Customer claims a partner-registered bike | Core |
     | `referral_verified` | 15 | Referred user's first bike reaches `EvidenceChecked` | Cut if late |
     | `sighting_confirmed` | 50 | Owner marks your sighting "mine" | Only if Stretch 8 is built |
     | `recovery_contributed` | 100 | Bike recovered via your confirmed sighting/relay | Only if Stretch 7/8 is built |
   - Never: points for reporting your own bike stolen, unconfirmed sightings, check lookups.
   - Revoke `register_bike` if the bike is deleted within 24 h.
   - Badges: 10 "Leezen-Starter:in", 50 "Leezen-Schützer:in", 200 "Leezen-Held:in".
   - Profile (core): total, badge, history.
   - Leaderboard (cut if late): top 10 this month, **opt-in users only, alias only**.

### Stretch (only when 1–6 are demoable; pick one)

7. **Anonymous finder contact**: relay message to the owner from the `/b/{token}` page; stolen bikes show "inform the police".
8. **Sightings + matching**: photo + GPS + time; background scoring against nearby stolen bikes; only the owner sees candidates.
9. **Risk map**: ~250 m hexagons with ≥ 3 reports + OSM bike parking layer.
10. **Partner programme**: self-service partner onboarding, bulk registration at ADFC coding events, bike-shop check-in view, partner dashboard.
11. **District adoption progress** ("Kreuzviertel: 23 % registriert"); needs optional home district on profile.
12. **Found-bike matching**: CSV import of Fundfahrradstation frame numbers (mock) matched against stolen bikes; owner notification.

### Out of scope

Police system integration, scraping marketplaces or Fundbüro portals, payments, native apps, push notifications.

## Data model

| Table                  | Columns                                                                                                                                                                                                                                      |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| users (Identity)       | id (Guid), email, display_name, created_at                                                                                                                                                                                                   |
| bikes                  | id, owner_id, public_token, frame_no_raw, frame_no_norm, frame_no_loose, fein_code_hash, brand, model, type, color_primary, color_secondary, is_ebike, battery_serial, features, purchase_date, status (active/stolen/recovered), created_at |
| bike_photos            | id, bike_id, kind (side/frame_no/detail/receipt), path, embedding vector(512) nullable                                                                                                                                                       |
| bike_ownership_history | id, bike_id, owner_id, from_at, to_at                                                                                                                                                                                                        |
| theft_reports          | id, bike_id, stolen_at, location geography(Point,4326), district (derived, the only public location), location_note (owner only), lock_type, police_case_no, status (open/recovered/closed)                                                  |
| ownership_transfers    | id, bike_id, from_user, to_user, code_hash, expires_at, completed_at, verify_token (set on completion; certificate QR). Postgres `xmin` is the concurrency token, so a code can only be claimed once even under a race                       |
| sightings              | id, reporter_id nullable, photo_path, location geography(Point,4326), seen_at, type, color, notes                                                                                                                                            |
| sighting_matches       | sighting_id, bike_id, score, owner_decision (pending/mine/not_mine)                                                                                                                                                                          |
| relay_messages         | id, bike_id, sender_contact, body, created_at                                                                                                                                                                                                |
| lookups                | id, ip_hash, query_hash, result, created_at                                                                                                                                                                                                  |
| bikes (new columns)    | trust_level (SelfDeclared/EvidenceChecked/ThirdPartyVerified), trust_source (receipt_possession/partner/transfer), trust_verified_at                                                                                                         |
| possession_challenges  | id, bike_id, user_id, code_hash, expires_at, used_at                                                                                                                                                                                         |
| ownership_evidence     | id, bike_id, kind (receipt/possession), photo_id, ai_result jsonb, status (passed/failed), created_at                                                                                                                                        |
| ownership_disputes     | id, frame_no_norm, existing_bike_id, claimant_id, status (open/upheld/rejected), evidence_photo_ids, created_at, resolved_at                                                                                                                 |
| security_events        | id, kind (duplicate_frame_registration, stolen_frame_registration_attempt, …), user_id, bike_id, ip_hash, created_at                                                                                                                         |
| goodwill_events        | id, user_id, action, points, status (credited/revoked), ref_type, ref_id, created_at, revoked_at — unique (user_id, action, ref_type, ref_id)                                                                                                |
| users (new columns)    | alias, show_on_leaderboard (bool, default false), referral_code (unique), referred_by_user_id                                                                                                                                                |

Indexes: unique `frame_no_norm`; index `frame_no_loose`, `fein_code_hash`; unique `public_token`; GiST on geography columns.

## API

| Method              | Route                                                   | Purpose                                                                                                                                                                                                                                                                                                                                                                                                                                                   | Auth                                                                          |
| ------------------- | ------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| POST                | `/api/auth/register`, `/api/auth/login?useCookies=true` | Identity API endpoints                                                                                                                                                                                                                                                                                                                                                                                                                                    | Public                                                                        |
| POST                | `/api/auth/logout`                                      | Custom (MapIdentityApi has no logout)                                                                                                                                                                                                                                                                                                                                                                                                                     | User                                                                          |
| GET                 | `/api/auth/manage/info`                                 | Current user                                                                                                                                                                                                                                                                                                                                                                                                                                              | User                                                                          |
| GET / POST          | `/api/bikes`                                            | List own / register. Existing frame number → one identical 409 (`errors.frameNumberTaken`) whether that bike is stolen or not; stolen → its owner is emailed, security event logged (hashed IP). Max 5 conflicts per user per day (then 429 for every registration)                                                                                                                                                                                                                                                                                                                                                                                              | Owner                                                                         |
| GET / PUT / DELETE  | `/api/bikes/{id}`                                       | Manage bike. PUT with a new frame number: same 409/cap/security event as register; 400 `errors.frameNumberLocked` once trust is above SelfDeclared; changing it on a self-declared bike deletes its verification evidence                                                                                                                                                                                                                                                                                                                                                                                                                                               | Owner                                                                         |
| POST                | `/api/bikes/{id}/photos`                                | Upload photo (multipart `file` + `kind`, header `X-LeezenPass: 1`; oriented, EXIF stripped, max 1600 px + 400 px thumb; rate policy `upload`)                                                                                                                                                                                                                                                                                                             | Owner                                                                         |
| GET                 | `/api/bikes/{id}/photos/{photoId}?size=thumb`           | Photo JPEG (full or thumbnail)                                                                                                                                                                                                                                                                                                                                                                                                                            | Owner                                                                         |
| DELETE              | `/api/bikes/{id}/photos/{photoId}`                      | Delete photo                                                                                                                                                                                                                                                                                                                                                                                                                                              | Owner                                                                         |
| POST                | `/api/ai/extract`                                       | Photo → suggested attributes (multipart `file`, header `X-LeezenPass: 1`, rate policy `ai`). Image is re-encoded (no EXIF, max 1024 px) and never stored. 503 `errors.aiUnavailable` on timeout/unreachable model → manual entry                                                                                                                                                                                                                          | User                                                                          |
| GET                 | `/api/bikes/{id}/pass.pdf`                              | Bike pass PDF (side photo, QR code to `/b/{token}`)                                                                                                                                                                                                                                                                                                                                                                                                       | Owner                                                                         |
| POST                | `/api/bikes/{id}/theft`                                 | Report stolen (409 if already stolen)                                                                                                                                                                                                                                                                                                                                                                                                                     | Owner                                                                         |
| PUT                 | `/api/bikes/{id}/theft`                                 | Update location note / police case number of the open report                                                                                                                                                                                                                                                                                                                                                                                              | Owner                                                                         |
| POST                | `/api/bikes/{id}/recovered`                             | Mark recovered (409 if not stolen)                                                                                                                                                                                                                                                                                                                                                                                                                        | Owner                                                                         |
| GET                 | `/api/stolen?type=&color=&district=`                    | Public stolen list: no owner data, no frame number, district + date only, side/detail photos only, trust level. Filter by district, not bbox (a bbox over exact points could be narrowed to the exact spot)                                                                                                                                                                                                                                                            | Public                                                                        |
| GET                 | `/api/stolen/{token}`                                   | One stolen bike (share card, QR tag page); 404 if not stolen                                                                                                                                                                                                                                                                                                                                                                                              | Public                                                                        |
| GET                 | `/api/stolen/{token}/photos/{photoId}?size=thumb`       | Photo of a stolen bike (side/detail only, only while stolen)                                                                                                                                                                                                                                                                                                                                                                                              | Public                                                                        |
| POST                | `/api/check`                                            | `{frameNumber \| feinCode, captchaToken}` → `{result: stolen \| verified_transfer \| possible_match \| unknown, bikes, trustLevel}`. Exact match first (a registered clean bike is `unknown`: registration isn't revealed), then the loose key against **stolen bikes and bikes with an open transfer only** (see "Check lookup order"). `bikes` (public stolen view, incl. each bike's public trust label) only for stolen / possible_match; top-level `trustLevel` only for stolen / verified_transfer. A look-alike of an open-transfer bike is `possible_match` with no bikes (neutral wording). Every call logged as keyed hashes (IP with daily salt, query) | Public, rate policy `check` per IP (`RateLimits:CheckPerTenMinutes`), captcha |
| GET / POST / DELETE | `/api/bikes/{id}/transfers`                             | Transfer state (open code, certificate id, previous owners) / create code (plain code only in this response, replaces an open one, 409 if stolen) / cancel open code                                                                                                                                                                                                                                                                                      | Owner                                                                         |
| POST                | `/api/transfers/claim`                                  | Claim code: owner changes, history kept. Unknown/expired/used codes all get the same 400; rate policy `claim` (10 per 10 min per user)                                                                                                                                                                                                                                                                                                                    | User                                                                          |
| GET                 | `/api/transfers/{id}/certificate.pdf`                   | Certificate PDF                                                                                                                                                                                                                                                                                                                                                                                                                                           | New owner                                                                     |
| GET                 | `/api/verify/{token}`                                   | Certificate verification: date, bike attributes, frame number hint ("…5678"), still with this owner, current status. No identities                                                                                                                                                                                                                                                                                                                        | Public                                                                        |
| GET                 | `/b/{token}` (web route) + `GET /api/tags/{token}`      | QR tag page data. Until the stretch feature: `/b/{token}` shows the stolen card via `/api/stolen/{token}`, otherwise "registered", whether a handover is prepared (open transfer), the frame number hint and the trust level. The "LeezenPass verifiziert" listing text links here                                                                                                                                                                                         | Public                                                                        |
| POST                | `/api/tags/{token}/message`                             | Relay message                                                                                                                                                                                                                                                                                                                                                                                                                                             | Public, rate-limited                                                          |
| POST                | `/api/sightings`                                        | Report sighting                                                                                                                                                                                                                                                                                                                                                                                                                                           | Public                                                                        |
| GET                 | `/api/bikes/{id}/matches`                               | Candidate matches                                                                                                                                                                                                                                                                                                                                                                                                                                         | Owner                                                                         |
| POST                | `/api/matches/{sightingId}/{bikeId}/decision`           | mine / not_mine                                                                                                                                                                                                                                                                                                                                                                                                                                           | Owner                                                                         |
| GET                 | `/api/riskmap?bbox=`                                    | Hexagon GeoJSON                                                                                                                                                                                                                                                                                                                                                                                                                                           | Public                                                                        |
| GET                 | `/api/health`                                           | Health check: `{status, database, demoMode, useFakes}` (web uses `demoMode` for the banner)                                                                                                                                                                                                                                                                                                                                                               | Public                                                                        |
| POST                | `/api/bikes/{id}/verification/challenge`                | New 4-digit possession code (10 min, single use, stored as hash of code + bike + user). Plain code only in this response. Rate policy `challenge` (5 per hour per user)                                                                                                                                                                                                                                                                                                                                                                                                                      | Owner, rate-limited                                                           |
| POST                | `/api/bikes/{id}/verification/possession`               | Multipart `file` + `code`, header `X-LeezenPass: 1`. Wrong/expired code → 400 `errors.challengeInvalid`. → `{outcome: passed \| failed \| retry, status}`. Photo is only read, never stored. Rate policy `ai`                                                                                                                                                                                                                                                                                                                                                                                                                               | Owner                                                                         |
| POST                | `/api/bikes/{id}/verification/receipt`                  | Multipart `file`, header `X-LeezenPass: 1` → `{outcome, status}`. A passed receipt is kept as the bike's receipt photo (owner-only, deleted on transfer); failed/unclear ones are not stored. Rate policy `ai`                                                                                                                                                                                                                                                                                                                                                                                                                                             | Owner                                                                         |
| GET                 | `/api/bikes/{id}/verification`                          | `{trustLevel, receipt, possession: passed \| failed \| null, challengeExpiresAt}` for the current owner                                                                                                                                                                                                                                                                                                                                                                                                                                | Owner                                                                         |
| POST                | `/api/disputes`                                         | Request clarification for a frame number (evidence photos)                                                                                                                                                                                                                                                                                                                                                                                                | User, rate-limited                                                            |
| GET                 | `/api/me/disputes`                                      | My disputes                                                                                                                                                                                                                                                                                                                                                                                                                                               | User                                                                          |
| POST                | `/api/admin/disputes/{id}/resolve`                      | Uphold / reject (cut if late)                                                                                                                                                                                                                                                                                                                                                                                                                             | Admin                                                                         |
| POST                | `/api/partner/bikes`                                    | Partner registers bike → transfer code for the customer                                                                                                                                                                                                                                                                                                                                                                                                   | Partner                                                                       |
| GET                 | `/api/me/goodwill`                                      | `{total, badge, nextBadge, history[{action, points, status, createdAt}]}`; own ledger only. Credits: `register_bike` on POST `/api/bikes` (first 3 bikes; revoked if deleted within 24 h), `evidence_verified` when a receipt/possession check raises trust (`pointsCredited` in that response), `partner_registration` when claiming a bike a Partner registered. Each credit is written in the outcome's transaction, `ON CONFLICT DO NOTHING` on (user, action, ref)                                                                                                                                                                                                                                                                                                                                                                                                                                     | User                                                                          |
| PUT                 | `/api/me/profile`                                       | Alias, show_on_leaderboard (with the leaderboard; not built yet)                                                                                                                                                                                                                                                                                                                                                                                                                                | User                                                                          |
| GET                 | `/api/goodwill/leaderboard?month=`                      | Top 10 opted-in users (cut if late; not built yet)                                                                                                                                                                                                                                                                                                                                                                                                                       | Public                                                                        |

## Algorithms and details

### Frame-number normalisation

```csharp
static string Norm(string raw) =>
    new(raw.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

static string Loose(string n) => n.Replace('O','0').Replace('I','1').Replace('L','1')
                                  .Replace('S','5').Replace('B','8').Replace('Z','2');
```

Lookup: exact `frame_no_norm` first; else `frame_no_loose` → `possible_match`.

### FEIN code

HMAC-SHA256(normalised code, server secret) → `fein_code_hash`. Lookup by hash only.

### Transfer code

8 chars from `ABCDEFGHJKMNPQRSTUVWXYZ23456789`, cryptographically random, stored as SHA-256 hash, 48 h expiry, single use.

### Sighting score

```
score = 0.35·type + 0.25·color + 0.15·brand
      + 0.15·distanceDecay(km, half-life 3 km)
      + 0.10·timeDecay(days, half-life 14)
decay(x, h) = 0.5^(x / h)
```

Candidates: stolen bikes within 10 km, reported in last 90 days. Score > 0.6 → notify owner. Optional image cosine similarity (pgvector) as extra component.

### Risk map

`ST_HexagonGrid(250, ST_Transform(bbox, 25832))` joined with `ST_Transform(location, 25832)`; `HAVING count(*) >= 3`; return GeoJSON in 4326. Bike parking from Overpass (`amenity=bicycle_parking`), cached to a GeoJSON file at startup.

### AI extraction

Strict JSON schema: `{type, color_primary, color_secondary, brand_guess, features[], frame_number_candidate, confidence}`; enum values are exactly the catalog keys (`color_secondary` may be `none`). Provider: any OpenAI-compatible chat API with image input via `response_format: json_schema` (strict). Chosen: **Qwen3-VL-8B in LM Studio, running locally** (works offline, photos never leave the laptop, ~10–20 s for a new 1024 px photo on an M3, mostly image encoding). Timeout 30 s, then manual entry. Requests are sent one at a time (a local model can't handle two images within the timeout). Type/colours/features are only prefilled at confidence ≥ 0.4; brand and frame number are always offered for the user to check. `FakeVisionExtractor` returns canned data.

### Check lookup order

1. Exact `frame_no_norm` / FEIN hash → if stolen → `stolen`; if open transfer → `verified_transfer`; else → `unknown`.
2. No exact hit → loose key, restricted to stolen bikes and bikes with an open transfer → `possible_match`.
3. Otherwise → `unknown`. Response shape and timing identical for "registered" and "not registered".

### Receipt + possession AI calls

- `ExtractReceipt(photo)` → `{frame_number_candidate, brand, model, purchase_date, shop_name, confidence}`
- `CheckPossession(photo, expectedCode)` → `{code_visible, code_value, frame_number_candidate, confidence}`
- Confidence ≥ 0.6 to pass. Below → "Bitte neues Foto aufnehmen" (retry), not a hard fail.
- Fakes: pass when the uploaded file name contains `pass`, fail otherwise.

### Goodwill

- `GoodwillPolicy` (domain): single source of actions, points, caps, badge thresholds.
- `GoodwillService.CreditAsync(userId, action, refType, refId)` inserts one row in the same transaction as the outcome with `INSERT … ON CONFLICT DO NOTHING` on the unique index (a failed INSERT would abort the outcome's Postgres transaction), returning the points credited (0 for a repeat). `evidence_verified` is keyed by the frame number, so deleting and re-registering a bike can't earn it twice.

### Images

SkiaSharp (+ `SkiaSharp.NativeAssets.Linux.NoDependencies` in Docker): decode, apply EXIF orientation, resize to max 1600 px, re-encode JPEG (drops metadata), thumbnail 400 px.

### Seed data

~200 synthetic bikes, ~60 theft reports spread over Münster districts, labelled "Demo-Daten".
Also: owner, buyer, partner and admin demo users; at least one bike per trust level; one stolen bike and one bike with an open transfer for the check demo; one O/0 look-alike frame number; a few goodwill events for 3 opted-in demo users; sample photos named `*_pass.jpg` / `*_fail.jpg`. Label everything "Demo-Daten".

## Privacy / abuse

| Risk                                          | Mitigation                                                                                 |
| --------------------------------------------- | ------------------------------------------------------------------------------------------ |
| GDPR                                          | Data minimisation, one-click deletion, privacy page, EU hosting                            |
| Fake theft reports                            | Login required, receipt upload, "self-reported" vs "police case no." label                 |
| Vigilantism                                   | Rough areas only, "call 110, don't confront" banner                                        |
| Enumeration                                   | Rate limit, captcha, minimal responses, hashed logging                                     |
| Location leaks                                | EXIF stripped                                                                              |
| Registration reveals registered/stolen status | One identical 409 for all duplicates; verified email; 5 conflicts/day cap; security events |
| Thief registers a stolen bike first           | Trust label visible; clarification requests; owner of a stolen bike notified on attempts   |
| Receipt photos contain name/address           | Owner-only access, never in public responses, EXIF stripped                                |
| Points farming                                | Points only on verified outcomes, caps, idempotent ledger, revocation                      |
| Leaderboard exposure                          | Opt-in, alias only                                                                         |
