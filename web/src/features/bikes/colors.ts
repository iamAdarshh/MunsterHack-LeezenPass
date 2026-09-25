import type { BikeColor } from '../../api/types'

/** Swatch colours for the colour picker and bike cards. */
export const colorSwatches: Record<BikeColor, string> = {
  black: '#111827',
  white: '#ffffff',
  grey: '#6b7280',
  silver: '#c0c4cc',
  red: '#dc2626',
  blue: '#2563eb',
  green: '#16a34a',
  yellow: '#facc15',
  orange: '#f97316',
  brown: '#78350f',
  beige: '#e7d8b8',
  pink: '#ec4899',
  purple: '#7c3aed',
  other: 'conic-gradient(#dc2626, #facc15, #16a34a, #2563eb, #dc2626)',
}
