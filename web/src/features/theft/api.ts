import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../api/client'
import type { Bike, BikeColor, BikeType, District, StolenBike, TheftInput } from '../../api/types'
import { bikesKey } from '../bikes/api'

const stolenKey = ['stolen'] as const

export interface StolenFilters {
  type?: BikeType
  color?: BikeColor
  district?: District
}

export function useStolenList(filters: StolenFilters) {
  const params = new URLSearchParams()
  Object.entries(filters).forEach(([key, value]) => {
    if (value) params.set(key, value)
  })
  const query = params.toString()
  return useQuery({
    queryKey: [...stolenKey, 'list', query],
    queryFn: () => apiFetch<StolenBike[]>(`/api/stolen${query ? `?${query}` : ''}`),
  })
}

export function useStolenBike(token: string, enabled = true) {
  return useQuery({
    queryKey: [...stolenKey, token],
    queryFn: () => apiFetch<StolenBike>(`/api/stolen/${encodeURIComponent(token)}`),
    enabled,
  })
}

/** Status behind a QR tag. 404 = unknown code (possibly a fake sticker). */
export function useTag(token: string) {
  return useQuery({
    queryKey: ['tag', token],
    queryFn: () => apiFetch<{ status: 'registered' | 'stolen' }>(`/api/tags/${encodeURIComponent(token)}`),
  })
}

/** Owner mutations return the updated bike; keep the bike and public caches in sync. */
function useBikeMutation<TInput>(request: (input: TInput) => Promise<Bike>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSuccess: (bike) => {
      queryClient.setQueryData([...bikesKey, bike.id], bike)
      void queryClient.invalidateQueries({ queryKey: bikesKey, exact: true })
      void queryClient.invalidateQueries({ queryKey: stolenKey })
    },
  })
}

export function useReportTheft(bikeId: string) {
  return useBikeMutation((input: TheftInput) =>
    apiFetch<Bike>(`/api/bikes/${bikeId}/theft`, { method: 'POST', json: { ...input } }),
  )
}

export function useUpdateTheft(bikeId: string) {
  return useBikeMutation((input: { locationNote: string | null; policeCaseNo: string | null }) =>
    apiFetch<Bike>(`/api/bikes/${bikeId}/theft`, { method: 'PUT', json: { ...input } }),
  )
}

export function useMarkRecovered(bikeId: string) {
  return useBikeMutation(() => apiFetch<Bike>(`/api/bikes/${bikeId}/recovered`, { method: 'POST' }))
}
