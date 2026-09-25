# LeezenPass – Specification

## Problem

- 4,561 bike thefts in Münster in 2025 (about 1 in 7 reported crimes), clearance rate 12.6%.
- Highest per-capita rate in Germany (~1,500 per 100,000 residents).
- Police bike-pass app discontinued; Münster police online registration no longer available.
- Bikes get stolen because they are easy to resell. LeezenPass attacks the resale market.

## Three levers

| Lever | What | Effect |
| --- | --- | --- |
| Deter | Visible QR tag, FEIN coding promoted | Registered bikes are harder to sell |
| Verify | Frame-number check before buying, verified ownership transfer | Buyers learn to check; market for stolen bikes shrinks |
| Recover | Public stolen list, community sightings matched to owners | More bikes returned |

## Features

### MVP (must work live at the pitch)

1. **Bike pass (registry)**
   - Fields: frame number, FEIN code (hashed), brand, model, type, colours, e-bike flag, battery serial, features, purchase date.
   - Photos: side, frame-number close-up, details, receipt.
   - AI prefill: upload photo → vision LLM suggests type, colours, brand, features, frame number candidate. User confirms; never auto-save.
   - PDF export of the pass (for police and insurance).
2. **Check before you buy** (public, no login)
   - Input: frame number or FEIN code.
   - Result: `stolen` (red, show photo + attributes), `verified_transfer` (green, seller has an open transfer), `unknown` (grey, warning-signs checklist). Loose-key hits return `possible_match` with photos to compare.
   - Rate-limited, captcha, hashed lookup logging.
3. **Theft report**
   - Time, approximate location (map pin), location note, lock type, optional police case number.
   - Generates police-ready summary + link to Polizei NRW Internetwache + insurance reminder.
   - Bike appears in public stolen list (photos, attributes, district only).
   - Share card for messengers/social.
   - Owner can mark as recovered.
4. **Ownership transfer**
   - Seller creates 8-char code (unambiguous alphabet), valid 48 h, single use, stored hashed.
   - Buyer claims → owner changes, history kept, certificate PDF with QR to verification page.
   - The seller's personal data stays behind: receipt photos are deleted and the FEIN hash (encodes the seller's address) is cleared on transfer. Reporting a theft cancels open codes.
   - Seller shares a "LeezenPass verifiziert" text for marketplace listings, linking to `/b/{token}` (registered, handover prepared, frame number hint).

### Stretch (pick one)

5. **QR tag + anonymous finder contact**: `/b/{token}` public page; relay message to owner by email; stolen bikes show "inform the police".
6. **Sightings + matching**: anyone reports photo + GPS + time; background scoring against nearby stolen bikes; only owner sees candidates.
7. **Risk map**: hexagons (~250 m) with ≥ 3 reports + OSM bike parking layer.
8. **Partner view (mock)**: ADFC coding events / bike shops.

### Out of scope

Police system integration, scraping marketplaces, payments, native apps, push notifications.

## Data model

| Table | Columns |
| --- | --- |
| users (Identity) | id (Guid), email, display_name, created_at |
| bikes | id, owner_id, public_token, frame_no_raw, frame_no_norm, frame_no_loose, fein_code_hash, brand, model, type, color_primary, color_secondary, is_ebike, battery_serial, features, purchase_date, status (active/stolen/recovered), created_at |
| bike_photos | id, bike_id, kind (side/frame_no/detail/receipt), path, embedding vector(512) nullable |
| bike_ownership_history | id, bike_id, owner_id, from_at, to_at |
| theft_reports | id, bike_id, stolen_at, location geography(Point,4326), district (derived, the only public location), location_note (owner only), lock_type, police_case_no, status (open/recovered/closed) |
| ownership_transfers | id, bike_id, from_user, to_user, code_hash, expires_at, completed_at, verify_token (set on completion; certificate QR). Postgres `xmin` is the concurrency token, so a code can only be claimed once even under a race |
| sightings | id, reporter_id nullable, photo_path, location geography(Point,4326), seen_at, type, color, notes |
| sighting_matches | sighting_id, bike_id, score, owner_decision (pending/mine/not_mine) |
| relay_messages | id, bike_id, sender_contact, body, created_at |
| lookups | id, ip_hash, query_hash, result, created_at |

Indexes: unique `frame_no_norm`; index `frame_no_loose`, `fein_code_hash`; unique `public_token`; GiST on geography columns.

## API

| Method | Route | Purpose | Auth |
| --- | --- | --- | --- |
| POST | `/api/auth/register`, `/api/auth/login?useCookies=true` | Identity API endpoints | Public |
| POST | `/api/auth/logout` | Custom (MapIdentityApi has no logout) | User |
| GET | `/api/auth/manage/info` | Current user | User |
| GET / POST | `/api/bikes` | List own / register (409 if frame number already registered) | Owner |
| GET / PUT / DELETE | `/api/bikes/{id}` | Manage bike | Owner |
| POST | `/api/bikes/{id}/photos` | Upload photo (multipart `file` + `kind`, header `X-LeezenPass: 1`; oriented, EXIF stripped, max 1600 px + 400 px thumb; rate policy `upload`) | Owner |
| GET | `/api/bikes/{id}/photos/{photoId}?size=thumb` | Photo JPEG (full or thumbnail) | Owner |
| DELETE | `/api/bikes/{id}/photos/{photoId}` | Delete photo | Owner |
| POST | `/api/ai/extract` | Photo → suggested attributes (multipart `file`, header `X-LeezenPass: 1`, rate policy `ai`). Image is re-encoded (no EXIF, max 1024 px) and never stored. 503 `errors.aiUnavailable` on timeout/unreachable model → manual entry | User |
| GET | `/api/bikes/{id}/pass.pdf` | Bike pass PDF (side photo, QR code to `/b/{token}`) | Owner |
| POST | `/api/bikes/{id}/theft` | Report stolen (409 if already stolen) | Owner |
| PUT | `/api/bikes/{id}/theft` | Update location note / police case number of the open report | Owner |
| POST | `/api/bikes/{id}/recovered` | Mark recovered (409 if not stolen) | Owner |
| GET | `/api/stolen?type=&color=&district=` | Public stolen list: no owner data, no frame number, district + date only, side/detail photos only. Filter by district, not bbox (a bbox over exact points could be narrowed to the exact spot) | Public |
| GET | `/api/stolen/{token}` | One stolen bike (share card, QR tag page); 404 if not stolen | Public |
| GET | `/api/stolen/{token}/photos/{photoId}?size=thumb` | Photo of a stolen bike (side/detail only, only while stolen) | Public |
| POST | `/api/check` | `{frameNumber \| feinCode, captchaToken}` → `{result: stolen \| verified_transfer \| possible_match \| unknown, bikes}`. Exact match first (a registered clean bike is `unknown`: registration isn't revealed), then the loose key against **stolen bikes only**. `bikes` (public stolen view) only for stolen / possible_match. Every call logged as keyed hashes (IP with daily salt, query) | Public, rate policy `check` per IP (`RateLimits:CheckPerTenMinutes`), captcha |
| GET / POST / DELETE | `/api/bikes/{id}/transfers` | Transfer state (open code, certificate id, previous owners) / create code (plain code only in this response, replaces an open one, 409 if stolen) / cancel open code | Owner |
| POST | `/api/transfers/claim` | Claim code: owner changes, history kept. Unknown/expired/used codes all get the same 400; rate policy `claim` (10 per 10 min per user) | User |
| GET | `/api/transfers/{id}/certificate.pdf` | Certificate PDF | New owner |
| GET | `/api/verify/{token}` | Certificate verification: date, bike attributes, frame number hint ("…5678"), still with this owner, current status. No identities | Public |
| GET | `/b/{token}` (web route) + `GET /api/tags/{token}` | QR tag page data. Until the stretch feature: `/b/{token}` shows the stolen card via `/api/stolen/{token}`, otherwise "registered", whether a handover is prepared (open transfer) and the frame number hint. The "LeezenPass verifiziert" listing text links here | Public |
| POST | `/api/tags/{token}/message` | Relay message | Public, rate-limited |
| POST | `/api/sightings` | Report sighting | Public |
| GET | `/api/bikes/{id}/matches` | Candidate matches | Owner |
| POST | `/api/matches/{sightingId}/{bikeId}/decision` | mine / not_mine | Owner |
| GET | `/api/riskmap?bbox=` | Hexagon GeoJSON | Public |
| GET | `/api/health` | Health check: `{status, database, demoMode, useFakes}` (web uses `demoMode` for the banner) | Public |

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

Strict JSON schema: `{type, color_primary, color_secondary, brand_guess, features[], frame_number_candidate, confidence}`; enum values are exactly the catalog keys (`color_secondary` may be `none`). Provider: any OpenAI-compatible chat API with image input via `response_format: json_schema` (strict). Chosen: **Qwen3-VL-8B in LM Studio, running locally** (works offline, photos never leave the laptop, ~4–10 s per photo on an M3). Timeout 15 s, then manual entry. Requests are sent one at a time (a local model can't handle two images within the timeout). Type/colours/features are only prefilled at confidence ≥ 0.4; brand and frame number are always offered for the user to check. `FakeVisionExtractor` returns canned data.

### Images

SkiaSharp (+ `SkiaSharp.NativeAssets.Linux.NoDependencies` in Docker): decode, apply EXIF orientation, resize to max 1600 px, re-encode JPEG (drops metadata), thumbnail 400 px.

### Seed data

~200 synthetic bikes, ~60 theft reports spread over Münster districts, labelled "Demo-Daten".

## Privacy / abuse

| Risk | Mitigation |
| --- | --- |
| GDPR | Data minimisation, one-click deletion, privacy page, EU hosting |
| Fake theft reports | Login required, receipt upload, "self-reported" vs "police case no." label |
| Vigilantism | Rough areas only, "call 110, don't confront" banner |
| Enumeration | Rate limit, captcha, minimal responses, hashed logging |
| Location leaks | EXIF stripped |
