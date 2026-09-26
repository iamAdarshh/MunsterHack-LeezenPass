# Demo data (Demo-Daten)

Synthetic data for the pitch. Nothing here belongs to a real person: users, bikes, receipts and photos are generated.
The web app shows the "Demo-Daten" banner in demo mode, and every seeded photo carries a "Demo-Daten" watermark.

```bash
dotnet run --project api/LeezenPass.Api -- seed
```

- Runs only when `Features:DemoMode=true` (Development default). Applies migrations first.
- Replaces everything that belongs to `*@demo.local` users (and bikes they once owned); other users' data stays.
- Same random seed every run, so the data is identical each time. Takes ~5 s.
- **Re-seed before the pitch**: the transfer code is valid for 48 h and single use.

Code: `api/LeezenPass.Api/Infrastructure/Seed/` (`DemoScenario` holds the fixed values below).

## Logins

Password for all: `LeezenDemo2026`

| User | Role | Has |
| --- | --- | --- |
| `owner@demo.local` | – | Gazelle (stolen), Kalkhoff (open transfer), Stevens (clean) |
| `buyer@demo.local` | – | nothing yet: claims the Kalkhoff with the transfer code |
| `partner@demo.local` | Partner | Riese & Müller registered for a customer (`ThirdPartyVerified`) |
| `admin@demo.local` | Admin | – |
| `nutzer01…nutzer40@demo.local` | – | background bikes |

## Check demo (Prüfen)

| Input | Result | Bike |
| --- | --- | --- |
| `GZ-2024-OB77` | `stolen` | owner's Gazelle, stolen 2 days ago near the main station, `EvidenceChecked` |
| `GZ-2024-0877` | `possible_match` | same Gazelle: typo with 0 for O and 8 for B (not registered) |
| `KH-5521-7730` or FEIN `MS-DE 2345 6789` | `verified_transfer` | owner's Kalkhoff e-bike, `EvidenceChecked`, open transfer |
| `ST-8830-1145` or FEIN `MS-DE 4711 0815` | `unknown` | owner's Stevens: registered, but the check never reveals that |
| `XY-0000-1234` | `unknown` | not registered: same response as above |

Transfer code for `buyer@demo.local`: **`PASS-2345`** (Kalkhoff).

## Background data

- 44 users, 200 bikes (about 70 % `SelfDeclared`, 20 % `EvidenceChecked`, 10 % `ThirdPartyVerified`).
- 60 theft reports: 48 open (public stolen list, each with a photo), 12 recovered. Spread over busy districts,
  40 % clustered at hotspots (Hbf, Schloss, Prinzipalmarkt, Hafen, Aasee) so the risk map has cells with ≥ 3 reports.

## Sample photos (`seed/photos/`)

Written by the seed command, for uploading by hand in the demo. They show the Stevens (`ST-8830-1145`).
The fake vision extractor passes a verification when the file name contains `pass`.

| File | Use |
| --- | --- |
| `receipt_pass.jpg` / `receipt_fail.jpg` | Receipt check (bike receipt / grocery receipt) |
| `possession_pass.jpg` / `possession_fail.jpg` | Possession check (right / wrong frame number) |
| `bike_side.jpg` | Side photo for registration with AI prefill |
