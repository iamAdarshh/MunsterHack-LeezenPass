import type { ReactNode } from 'react'
import { useErrorText } from '../i18n/useErrorText'

interface Props {
  label: string
  htmlFor?: string
  hint?: string
  /** i18n key (or Identity error code) from zod or the API. */
  error?: string
  children: ReactNode
}

export const inputClass =
  'block min-h-12 w-full rounded-lg border border-slate-300 bg-white px-3 text-base focus:border-brand-600 focus:ring-2 focus:ring-brand-600/30 focus:outline-none aria-invalid:border-red-500'

export function FormField({ label, htmlFor, hint, error, children }: Props) {
  const errorText = useErrorText()
  return (
    <div className="space-y-1">
      <label htmlFor={htmlFor} className="block text-sm font-semibold text-slate-800">
        {label}
      </label>
      {children}
      {hint && !error && <p className="text-xs text-slate-500">{hint}</p>}
      {error && <p className="text-sm text-red-700">{errorText(error)}</p>}
    </div>
  )
}
