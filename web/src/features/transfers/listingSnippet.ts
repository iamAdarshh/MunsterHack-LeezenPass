import type { TFunction } from 'i18next'
import type { Bike } from '../../api/types'
import { bikeTitle } from '../bikes/format'

/** Last 4 characters of the normalised frame number, like FrameNumber.Hint on the server. */
export function frameHint(frameNumber: string) {
  const normalized = frameNumber.toUpperCase().replace(/[^A-Z0-9]/g, '')
  return normalized.length <= 4 ? normalized : `…${normalized.slice(-4)}`
}

/** "LeezenPass verifiziert" text for marketplace listings. Links to /b/{token}, which shows status + hint. */
export function listingSnippet(bike: Bike, t: TFunction, origin: string) {
  return t('transfer.snippet', {
    title: bikeTitle(bike, t),
    hint: frameHint(bike.frameNumber),
    url: `${origin}/b/${bike.publicToken}`,
  })
}
