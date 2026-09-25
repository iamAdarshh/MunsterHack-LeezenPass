import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../api/client'
import type { CreatedTransfer, TransferStatus, VerifyResult } from '../../api/types'
import { bikesKey } from '../bikes/api'

const statusKey = (bikeId: string) => ['transfers', bikeId] as const

export function useTransferStatus(bikeId: string) {
  return useQuery({
    queryKey: statusKey(bikeId),
    queryFn: () => apiFetch<TransferStatus>(`/api/bikes/${bikeId}/transfers`),
  })
}

export function useCreateTransfer(bikeId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => apiFetch<CreatedTransfer>(`/api/bikes/${bikeId}/transfers`, { method: 'POST' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: statusKey(bikeId) }),
  })
}

export function useCancelTransfer(bikeId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => apiFetch<void>(`/api/bikes/${bikeId}/transfers`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: statusKey(bikeId) }),
  })
}

export function useClaimTransfer() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (code: string) =>
      apiFetch<{ bikeId: string; transferId: string }>('/api/transfers/claim', { method: 'POST', json: { code } }),
    // The bike now belongs to the signed-in user.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: bikesKey }),
  })
}

export function useVerify(token: string) {
  return useQuery({
    queryKey: ['verify', token],
    queryFn: () => apiFetch<VerifyResult>(`/api/verify/${encodeURIComponent(token)}`),
  })
}
