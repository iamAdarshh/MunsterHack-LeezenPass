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

/** Owner view. The FEIN code is never sent back, only whether one is stored. */
export interface Bike {
  id: string
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
