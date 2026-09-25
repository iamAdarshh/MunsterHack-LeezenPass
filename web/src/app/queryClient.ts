import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { ApiError } from '../api/client'
import { meQueryKey } from '../features/auth/api'

/** Session expired: mark as logged out, RequireAuth then redirects to /login. */
function handleUnauthorized(error: Error) {
  if (error instanceof ApiError && error.status === 401) {
    queryClient.setQueryData(meQueryKey, null)
  }
}

export const queryClient: QueryClient = new QueryClient({
  queryCache: new QueryCache({ onError: handleUnauthorized }),
  // Login with a wrong password is also a 401, but the user is logged out then anyway.
  mutationCache: new MutationCache({ onError: handleUnauthorized }),
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Client errors (4xx) won't fix themselves; only retry network/server errors.
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status < 500) && failureCount < 2,
    },
  },
})
