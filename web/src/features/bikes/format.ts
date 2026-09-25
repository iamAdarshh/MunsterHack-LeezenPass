import type { TFunction } from 'i18next'
import type { Bike, BikePhoto } from '../../api/types'

export function bikeTitle(bike: Bike, t: TFunction) {
  const name = [bike.brand, bike.model].filter(Boolean).join(' ')
  return name || t(`bikeType.${bike.type}`)
}

/** Side view first; never the receipt. */
export function coverPhoto(bike: Bike): BikePhoto | undefined {
  return bike.photos.find((p) => p.kind === 'side') ?? bike.photos.find((p) => p.kind !== 'receipt')
}
