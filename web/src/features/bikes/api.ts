import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '../../api/client'
import type { Bike, BikeInput, BikePhoto, PhotoKind } from '../../api/types'

export const bikesKey = ['bikes'] as const
const bikeKey = (id: string) => [...bikesKey, id] as const

export function useMyBikes() {
  return useQuery({ queryKey: bikesKey, queryFn: () => apiFetch<Bike[]>('/api/bikes') })
}

export function useBike(id: string) {
  return useQuery({ queryKey: bikeKey(id), queryFn: () => apiFetch<Bike>(`/api/bikes/${id}`) })
}

export function useRegisterBike() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: BikeInput) => apiFetch<Bike>('/api/bikes', { method: 'POST', json: { ...input } }),
    onSuccess: (bike) => {
      queryClient.setQueryData(bikeKey(bike.id), bike)
      void queryClient.invalidateQueries({ queryKey: bikesKey, exact: true })
    },
  })
}

export function useUpdateBike(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: BikeInput) => apiFetch<Bike>(`/api/bikes/${id}`, { method: 'PUT', json: { ...input } }),
    onSuccess: (bike) => {
      queryClient.setQueryData(bikeKey(bike.id), bike)
      void queryClient.invalidateQueries({ queryKey: bikesKey, exact: true })
    },
  })
}

export function useDeleteBike(id: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => apiFetch<void>(`/api/bikes/${id}`, { method: 'DELETE' }),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: bikeKey(id) })
      void queryClient.invalidateQueries({ queryKey: bikesKey, exact: true })
    },
  })
}

export function useUploadPhoto(bikeId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ kind, file }: { kind: PhotoKind; file: File }) => {
      const formData = new FormData()
      formData.append('kind', kind)
      formData.append('file', file)
      return apiFetch<BikePhoto>(`/api/bikes/${bikeId}/photos`, { method: 'POST', formData })
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: bikesKey }),
  })
}

export function useDeletePhoto(bikeId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (photoId: string) =>
      apiFetch<void>(`/api/bikes/${bikeId}/photos/${photoId}`, { method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: bikesKey }),
  })
}
