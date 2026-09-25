# Web (React PWA)

Loaded when working in `web/`. Root rules in `/CLAUDE.md` still apply.

## Structure

```
web/src/
  app/              Router, providers (QueryClient, i18n), layout
  api/              Typed fetch client (credentials: 'include'), API types
  features/
    bikes/          MyBikesPage, RegisterBikePage (with AI prefill), BikeDetailPage
    check/          CheckPage (public)
    theft/          ReportTheftPage, StolenListPage (public)
    transfers/      CreateTransferPanel, ClaimTransferPage, VerifyPage
    auth/           LoginPage, RegisterPage
    verification/   VerifyOwnershipWizard (1 receipt → 2 show code → 3 photo with code → result)
    disputes/       OpenDisputePage, MyDisputesPage
    partner/        PartnerRegisterPage (partner role only)
    goodwill/       ProfilePage (points, badge, history, alias + opt-in), LeaderboardPage
    tags/ sightings/ riskmap/   (stretch)
  components/       Shared UI (Button, Card, StatusBadge, PhotoUpload, MapPicker, DemoBanner)
  i18n/             de.json, en.json
```

## Rules

- TypeScript strict, no `any`. API types in `src/api/types.ts`, mirroring SPEC.
- Server state: TanStack Query only (`useQuery`/`useMutation`, invalidate on success). No server data in `useState`.
- Forms: react-hook-form + zod schema per form; show field errors inline.
- All user-facing text via i18next keys; German default, English secondary.
- Fetch with `credentials: 'include'`; 401 → redirect to login; show toasts for other errors.
- Uploads: send header `X-LeezenPass: 1`. Camera capture via `<input type="file" accept="image/*" capture="environment">`.
- Mobile-first Tailwind; test at 375 px width. Big tap targets.
- Check result colours: red = stolen, green = verified transfer, grey = unknown, amber = possible match. Always pair colour with an icon + text.
- Show `DemoBanner` ("Demo-Daten") when the API reports demo mode.
- Map: MapLibre GL or react-leaflet with OSM tiles; include OSM attribution.
- `TrustBadge` component (icon + text + colour) wherever a bike appears: my bikes, bike detail, check result, stolen list.
- Show the possession code big (≥ 48 px) with a countdown; tell the user to photograph code and frame number together.
- Show "+10 Punkte" style toasts only after the API confirms a credit.

## Dev

- Vite dev server proxies `/api` and `/b` to the API so cookies stay same-origin.
- `npm run lint` and `npm run build` must pass before a commit.
