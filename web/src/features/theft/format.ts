import type { TFunction } from 'i18next'
import type { StolenBike } from '../../api/types'

export function stolenTitle(bike: StolenBike, t: TFunction) {
  return [bike.brand, bike.model].filter(Boolean).join(' ') || t(`bikeType.${bike.type}`)
}
