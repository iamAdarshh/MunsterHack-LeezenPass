import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertIcon } from './icons'

export function Spinner({ className = 'size-6' }: { className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={`inline-block animate-spin rounded-full border-2 border-current border-t-transparent ${className}`}
    />
  )
}

export function LoadingState() {
  const { t } = useTranslation()
  return (
    <div role="status" className="flex items-center justify-center gap-3 py-12 text-slate-500">
      <Spinner />
      {t('common.loading')}
    </div>
  )
}

export function ErrorState({ onRetry }: { onRetry?: () => void }) {
  const { t } = useTranslation()
  return (
    <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
      <p className="flex items-center gap-2 font-semibold">
        <AlertIcon className="size-5" />
        {t('errors.loadFailed')}
      </p>
      {onRetry && (
        <button type="button" onClick={onRetry} className="mt-2 min-h-11 font-semibold underline">
          {t('common.retry')}
        </button>
      )}
    </div>
  )
}

export function Alert({ children }: { children: ReactNode }) {
  return (
    <p role="alert" className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800">
      <AlertIcon className="mt-0.5 size-4 shrink-0" />
      <span>{children}</span>
    </p>
  )
}
