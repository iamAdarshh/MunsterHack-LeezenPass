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
| theft_reports | id, bike_id, stolen_at, location geography(Point,4326), location_note, lock_type, police_case_no, status (open/recovered/closed) |
| ownership_transfers | id, bike_id, from_user, to_user, code_hash, expires_at, completed_at |
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
| GET / POST | `/api/bikes` | List own / register | Owner |
| GET / PUT / DELETE | `/api/bikes/{id}` | Manage bike | Owner |
| POST | `/api/bikes/{id}/photos` | Upload photo (EXIF stripped) | Owner |
| POST | `/api/ai/extract` | Photo → suggested attributes | User |
| GET | `/api/bikes/{id}/pass.pdf` | Bike pass PDF | Owner |
| POST | `/api/bikes/{id}/theft` | Report stolen | Owner |
| POST | `/api/bikes/{id}/recovered` | Mark recovered | Owner |
| GET | `/api/stolen?bbox=&type=&color=` | Public stolen list | Public |
| POST | `/api/check` | Frame no. / FEIN → status | Public, rate-limited, captcha |
| POST | `/api/bikes/{id}/transfers` | Create transfer code | Owner |
| POST | `/api/transfers/claim` | Claim code | User |
| GET | `/api/transfers/{id}/certificate.pdf` | Certificate PDF | New owner |
| GET | `/api/verify/{token}` | Certificate verification | Public |
| GET | `/b/{token}` (web route) + `GET /api/tags/{token}` | QR tag page data | Public |
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

Strict JSON schema: `{type, color_primary, color_secondary, brand_guess, features[], frame_number_candidate, confidence}`. Provider decided at the event (use available AI credits). `FakeVisionExtractor` returns canned data.

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
