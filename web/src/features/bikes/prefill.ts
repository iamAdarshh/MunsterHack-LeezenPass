import type { BikeColor, BikeFeature, BikeType, VisionSuggestion } from '../../api/types'

/** Form fields the AI may suggest. */
export interface Prefill {
  frameNumber?: string
  brand?: string
  type?: BikeType
  colorPrimary?: BikeColor
  colorSecondary?: BikeColor | ''
  features?: BikeFeature[]
}

export type ConfidenceLevel = 'high' | 'medium' | 'low'

/** Below this, type/colours/features are not filled in (the model must answer even for non-bikes). */
export const minConfidenceForAttributes = 0.4

export function confidenceLevel(confidence: number): ConfidenceLevel {
  if (confidence >= 0.75) return 'high'
  if (confidence >= minConfidenceForAttributes) return 'medium'
  return 'low'
}

/**
 * Merges the side-view suggestion (attributes) and the frame close-up suggestion (frame number).
 * Brand is always offered (the user compares them with the bike); the rest only
 * when the side view was recognised with enough confidence.
 */
export function toPrefill(side?: VisionSuggestion, frame?: VisionSuggestion): Prefill {
  const prefill: Prefill = {}

  if (side && side.confidence >= minConfidenceForAttributes) {
    if (side.type) prefill.type = side.type
    if (side.colorPrimary) prefill.colorPrimary = side.colorPrimary
    prefill.colorSecondary = side.colorSecondary ?? ''
    if (side.features.length > 0) prefill.features = side.features
  }

  const brand = side?.brandGuess ?? frame?.brandGuess
  if (brand) prefill.brand = brand

  // Only from the close-up: in a whole-bike shot a "readable" stamped number is almost always made up.
  const frameNumber = frame?.frameNumberCandidate
  if (frameNumber) prefill.frameNumber = frameNumber

  return prefill
}
