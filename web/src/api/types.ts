// API types, mirroring docs/SPEC.md. Keep in sync with the C# response records.

export interface HealthResponse {
  status: 'ok' | 'degraded'
  database: boolean
  demoMode: boolean
  useFakes: boolean
}
