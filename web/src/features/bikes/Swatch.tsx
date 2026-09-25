import type { BikeColor } from '../../api/types'
import { colorSwatches } from './colors'

export function Swatch({ color, className = 'size-5' }: { color: BikeColor; className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={`inline-block shrink-0 rounded-full ring-1 ring-slate-300 ${className}`}
      style={{ background: colorSwatches[color] }}
    />
  )
}
