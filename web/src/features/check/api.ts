import { useMutation } from '@tanstack/react-query'
import { apiFetch } from '../../api/client'
import type { CheckResponse } from '../../api/types'

export interface CheckInput {
  frameNumber?: string
  feinCode?: string
  captchaToken?: string
}

export function useCheck() {
  return useMutation({
    mutationFn: (input: CheckInput) => apiFetch<CheckResponse>('/api/check', { method: 'POST', json: { ...input } }),
  })
}
