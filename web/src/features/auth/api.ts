import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ApiError, apiFetch } from '../../api/client'
import type { CurrentUser } from '../../api/types'

export const meQueryKey = ['me'] as const

export interface Credentials {
  email: string
  password: string
}

/** Signed-in user, or null when logged out. */
export function useMe() {
  return useQuery({
    queryKey: meQueryKey,
    queryFn: async (): Promise<CurrentUser | null> => {
      try {
        return await apiFetch<CurrentUser>('/api/auth/manage/info')
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) return null
        throw error
      }
    },
    staleTime: 5 * 60_000,
  })
}

async function login(credentials: Credentials) {
  await apiFetch<void>('/api/auth/login?useCookies=true', { method: 'POST', json: { ...credentials } })
}

export function useLogin() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: login,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: meQueryKey }),
  })
}

export function useRegisterAccount() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (credentials: Credentials) => {
      await apiFetch<void>('/api/auth/register', { method: 'POST', json: { ...credentials } })
      await login(credentials)
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: meQueryKey }),
  })
}

export function useLogout() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => apiFetch<void>('/api/auth/logout', { method: 'POST' }),
    onSuccess: () => {
      queryClient.setQueryData(meQueryKey, null)
      // Drop everything that belonged to the previous user.
      queryClient.removeQueries({ predicate: (q) => q.queryKey[0] !== 'me' && q.queryKey[0] !== 'health' })
    },
  })
}
