// API types, mirroring docs/SPEC.md. Keep in sync with the C# response records.
// Enums are snake_case strings on the wire.

export interface HealthResponse {
  status: 'ok' | 'degraded'
  database: boolean
  demoMode: boolean
  useFakes: boolean
}

export interface CurrentUser {
  email: string
  isEmailConfirmed: boolean
}

export const bikeTypes = ['city', 'trekking', 'mountain', 'road', 'gravel', 'cargo', 'folding', 'kids', 'other'] as const
export type BikeType = (typeof bikeTypes)[number]

export type BikeStatus = 'active' | 'stolen' | 'recovered'

export const photoKinds = ['side', 'frame_no', 'detail', 'receipt'] as const
export type PhotoKind = (typeof photoKinds)[number]

/** Mirrors Domain/Bikes/BikeCatalog.cs. */
export const bikeColors = [
  'black', 'white', 'grey', 'silver', 'red', 'blue', 'green', 'yellow',
  'orange', 'brown', 'beige', 'pink', 'purple', 'other',
] as const
export type BikeColor = (typeof bikeColors)[number]

/** Mirrors Domain/Bikes/BikeCatalog.cs. */
export const bikeFeatures = [
  'rack', 'fenders', 'hub_dynamo', 'lights', 'kickstand', 'basket', 'child_seat',
  'frame_lock', 'suspension_fork', 'disc_brakes', 'coaster_brake', 'bell',
] as const
export type BikeFeature = (typeof bikeFeatures)[number]

export interface BikePhoto {
  id: string
  kind: PhotoKind
  url: string
  thumbnailUrl: string
}

export const lockTypes = ['u_bolt', 'chain', 'folding', 'cable', 'frame', 'none', 'other'] as const
export type LockType = (typeof lockTypes)[number]

/** Mirrors Domain/Theft/MuensterDistricts.cs. */
export const districts = [
  'altstadt', 'kreuzviertel', 'mauritz', 'hansaviertel', 'suedviertel', 'geist', 'aaseestadt', 'sentrup',
  'gievenbeck', 'mecklenbeck', 'roxel', 'albachten', 'nienberge', 'kinderhaus', 'coerde', 'sprakel', 'gelmer',
  'handorf', 'gremmendorf', 'angelmodde', 'wolbeck', 'berg_fidel', 'hiltrup', 'amelsbueren', 'outside',
] as const
export type District = (typeof districts)[number]

/** Owner view of the open theft report (exact location included). */
export interface Theft {
  id: string
  stolenAt: string
  latitude: number
  longitude: number
  district: District
  locationNote: string | null
  lockType: LockType
  policeCaseNo: string | null
  createdAt: string
}

export interface TheftInput {
  stolenAt: string
  latitude: number
  longitude: number
  lockType: LockType
  locationNote: string | null
  policeCaseNo: string | null
}

/** Public view: no owner data, no frame number, district and date only. */
export interface StolenBike {
  token: string
  type: BikeType
  brand: string | null
  model: string | null
  colorPrimary: BikeColor | null
  colorSecondary: BikeColor | null
  isEbike: boolean
  features: BikeFeature[]
  stolenOn: string
  district: District
  policeReported: boolean
  photos: BikePhoto[]
}

/** Owner view. The FEIN code is never sent back, only whether one is stored. */
export interface Bike {
  id: string
  /** Unguessable id used in public links (/stolen/…, /b/…). */
  publicToken: string
  frameNumber: string
  brand: string | null
  model: string | null
  type: BikeType
  colorPrimary: BikeColor | null
  colorSecondary: BikeColor | null
  isEbike: boolean
  batterySerial: string | null
  features: BikeFeature[]
  purchaseDate: string | null
  status: BikeStatus
  hasFeinCode: boolean
  createdAt: string
  photos: BikePhoto[]
  /** Open theft report while the bike is stolen. */
  theft: Theft | null
}

export interface BikeInput {
  frameNumber: string
  /** Register: optional. Update: empty/null keeps the stored code. */
  feinCode: string | null
  removeFeinCode?: boolean
  brand: string | null
  model: string | null
  type: BikeType
  colorPrimary: BikeColor
  colorSecondary: BikeColor | null
  isEbike: boolean
  batterySerial: string | null
  features: BikeFeature[]
  purchaseDate: string | null
}

/** POST /api/ai/extract. Suggestions only: the user confirms everything in the form. */
export interface VisionSuggestion {
  type: BikeType | null
  colorPrimary: BikeColor | null
  colorSecondary: BikeColor | null
  brandGuess: string | null
  features: BikeFeature[]
  frameNumberCandidate: string | null
  /** 0..1 */
  confidence: number
}

/** GET /api/bikes/{id}/transfers (owner). */
export interface TransferStatus {
  openTransfer: { id: string; expiresAt: string } | null
  /** Transfer that made the current owner the owner: certificate PDF. */
  certificateTransferId: string | null
  previousOwners: number
}

/** POST /api/bikes/{id}/transfers. The plain code exists only in this response. */
export interface CreatedTransfer {
  transferId: string
  code: string
  expiresAt: string
}

/** GET /api/verify/{token} (public). */
export interface VerifyResult {
  transferredAt: string
  type: BikeType
  brand: string | null
  model: string | null
  colorPrimary: BikeColor | null
  colorSecondary: BikeColor | null
  frameNumberHint: string
  stillWithThisOwner: boolean
  currentStatus: BikeStatus
}

/** GET /api/tags/{token} (public). */
export interface TagStatus {
  status: 'registered' | 'stolen'
  transferOpen: boolean
  frameNumberHint: string
}
