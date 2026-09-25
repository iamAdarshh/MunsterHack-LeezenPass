import { Navigate, Outlet, useLocation } from 'react-router'
import { ErrorState, LoadingState } from '../../components/States'
import { useMe } from './api'

/** Wraps routes that need a signed-in user; sends everyone else to /login and back afterwards. */
export function RequireAuth() {
  const me = useMe()
  const location = useLocation()

  if (me.isPending) return <LoadingState />
  if (me.isError) return <ErrorState onRetry={() => void me.refetch()} />
  if (!me.data) {
    return <Navigate to={`/login?returnTo=${encodeURIComponent(location.pathname)}`} replace />
  }
  return <Outlet />
}
