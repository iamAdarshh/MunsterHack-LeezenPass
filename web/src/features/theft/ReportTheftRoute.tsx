import { lazy, Suspense } from 'react'
import { LoadingState } from '../../components/States'

// Leaflet is big; only load it when someone actually reports a theft.
const ReportTheftPage = lazy(() => import('./ReportTheftPage').then((m) => ({ default: m.ReportTheftPage })))

export function ReportTheftRoute() {
  return (
    <Suspense fallback={<LoadingState />}>
      <ReportTheftPage />
    </Suspense>
  )
}
