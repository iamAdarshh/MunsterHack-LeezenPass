import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '../../api/client'
import type { Goodwill } from '../../api/types'

export const goodwillKey = ['goodwill'] as const

export function useGoodwill() {
  return useQuery({ queryKey: goodwillKey, queryFn: () => apiFetch<Goodwill>('/api/me/goodwill') })
}
