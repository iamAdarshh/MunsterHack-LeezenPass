import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../api/client'
import type { PossessionChallenge, VerificationResult, VerificationStatus } from '../../api/types'
import { bikesKey } from '../bikes/api'

const statusKey = (bikeId: string) => ['verification', bikeId] as const

export function useVerificationStatus(bikeId: string) {
  return useQuery({
    queryKey: statusKey(bikeId),
    queryFn: () => apiFetch<VerificationStatus>(`/api/bikes/${bikeId}/verification`),
  })
}

function useOnResult(bikeId: string) {
  const queryClient = useQueryClient()
  return (result: VerificationResult) => {
    queryClient.setQueryData(statusKey(bikeId), result.status)
    // Trust level and (for a passed receipt) the photo list changed.
    void queryClient.invalidateQueries({ queryKey: bikesKey })
  }
}

export function useVerifyReceipt(bikeId: string) {
  const onResult = useOnResult(bikeId)
  return useMutation({
    mutationFn: (file: File) => {
      const formData = new FormData()
      formData.append('file', file)
      return apiFetch<VerificationResult>(`/api/bikes/${bikeId}/verification/receipt`, { method: 'POST', formData })
    },
    onSuccess: onResult,
  })
}

export function useCreateChallenge(bikeId: string) {
  return useMutation({
    mutationFn: () => apiFetch<PossessionChallenge>(`/api/bikes/${bikeId}/verification/challenge`, { method: 'POST' }),
  })
}

export function useVerifyPossession(bikeId: string) {
  const onResult = useOnResult(bikeId)
  return useMutation({
    mutationFn: ({ file, code }: { file: File; code: string }) => {
      const formData = new FormData()
      formData.append('file', file)
      formData.append('code', code)
      return apiFetch<VerificationResult>(`/api/bikes/${bikeId}/verification/possession`, { method: 'POST', formData })
    },
    onSuccess: onResult,
  })
}
