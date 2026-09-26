import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ApiError, fieldErrors, requestErrorKey } from '../../api/client'
import type { Bike, VerificationResult } from '../../api/types'
import { Button } from '../../components/Button'
import { CheckIcon } from '../../components/icons'
import { PhotoPicker } from '../../components/PhotoPicker'
import { Alert, ErrorState, LoadingState, Spinner } from '../../components/States'
import { TrustBadge } from '../../components/TrustBadge'
import { useErrorText } from '../../i18n/useErrorText'
import { useCreateChallenge, useVerificationStatus, useVerifyPossession, useVerifyReceipt } from './api'

/**
 * Owner: raise the trust level to "Per Beleg geprüft" (SPEC feature 5).
 * 1 receipt photo → 2 show a 4-digit code → 3 photo of code + frame number → result.
 */
export function VerifyOwnershipWizard({ bike }: { bike: Bike }) {
  const { t } = useTranslation()
  const status = useVerificationStatus(bike.id)

  if (status.isPending) return <LoadingState />
  if (status.isError) return <ErrorState onRetry={() => void status.refetch()} />

  const { trustLevel, receipt } = status.data

  return (
    <section className="mt-8 space-y-3">
      <h2 className="flex flex-wrap items-center gap-2 text-lg font-bold">
        {t('ownership.title')} <TrustBadge level={trustLevel} />
      </h2>

      {trustLevel !== 'self_declared' ? (
        <p className="flex items-start gap-2 rounded-lg bg-teal-50 p-3 text-sm text-teal-900">
          <CheckIcon className="mt-0.5 size-4 shrink-0" />
          {t(`trust.hint.${trustLevel}`)}
        </p>
      ) : (
        <div className="space-y-4 rounded-xl border border-slate-200 p-4">
          <p className="text-sm text-slate-600">{t('ownership.intro')}</p>
          <ReceiptStep bikeId={bike.id} passed={receipt === 'passed'} />
          {receipt === 'passed' && <PossessionStep bikeId={bike.id} />}
        </div>
      )}
    </section>
  )
}

function ReceiptStep({ bikeId, passed }: { bikeId: string; passed: boolean }) {
  const { t } = useTranslation()
  const verify = useVerifyReceipt(bikeId)

  return (
    <div className="space-y-2">
      <StepTitle number={1} done={passed}>
        {t('ownership.receiptTitle')}
      </StepTitle>
      {!passed && (
        <>
          <p className="text-sm text-slate-600">{t('ownership.receiptHint')}</p>
          <PhotoPicker idPrefix="receipt-check" disabled={verify.isPending} onFile={(file) => verify.mutate(file)} />
          <Progress pending={verify.isPending} />
          <Outcome result={verify.data} error={verify.error} failedKey="ownership.receiptFailed" />
        </>
      )}
    </div>
  )
}

function PossessionStep({ bikeId }: { bikeId: string }) {
  const { t } = useTranslation()
  const challenge = useCreateChallenge(bikeId)
  const verify = useVerifyPossession(bikeId)
  const secondsLeft = useSecondsLeft(challenge.data?.expiresAt)
  const code = challenge.data && secondsLeft > 0 ? challenge.data.code : null

  return (
    <div className="space-y-2">
      <StepTitle number={2} done={false}>
        {t('ownership.possessionTitle')}
      </StepTitle>
      {code ? (
        <>
          <div className="rounded-xl bg-slate-900 p-4 text-center text-white">
            <p className="font-mono text-6xl font-bold tracking-[0.3em]" aria-live="polite">
              {code}
            </p>
            <p className="mt-1 text-sm text-slate-300">
              {t('ownership.codeValidFor', { time: `${Math.floor(secondsLeft / 60)}:${String(secondsLeft % 60).padStart(2, '0')}` })}
            </p>
          </div>
          <p className="text-sm font-semibold">{t('ownership.possessionHint')}</p>
          <PhotoPicker
            idPrefix="possession-check"
            disabled={verify.isPending}
            onFile={(file) => verify.mutate({ file, code })}
          />
          <Progress pending={verify.isPending} />
          <Outcome result={verify.data} error={verify.error} failedKey="ownership.possessionFailed" />
        </>
      ) : (
        <>
          {challenge.data && <p className="text-sm text-slate-600">{t('ownership.codeExpired')}</p>}
          <Button fullWidth loading={challenge.isPending} onClick={() => challenge.mutate()}>
            {t('ownership.showCode')}
          </Button>
          {challenge.error && <ErrorAlert error={challenge.error} />}
        </>
      )}
    </div>
  )
}

function StepTitle({ number, done, children }: { number: number; done: boolean; children: string }) {
  return (
    <p className="flex items-center gap-2 font-semibold">
      <span
        className={`flex size-6 items-center justify-center rounded-full text-xs ${done ? 'bg-teal-600 text-white' : 'bg-slate-200'}`}
      >
        {done ? <CheckIcon className="size-4" /> : number}
      </span>
      {children}
    </p>
  )
}

function Progress({ pending }: { pending: boolean }) {
  const { t } = useTranslation()
  if (!pending) return null
  return (
    <p role="status" className="flex items-center gap-2 text-sm text-slate-600">
      <Spinner className="size-4" />
      {t('ownership.checking')}
    </p>
  )
}

function Outcome(props: {
  result: VerificationResult | undefined
  error: Error | null
  failedKey: 'ownership.receiptFailed' | 'ownership.possessionFailed'
}) {
  const { result, error, failedKey } = props
  const { t } = useTranslation()
  // Model unavailable or too slow: a retry prompt, not a dead end.
  const aiDown = error instanceof ApiError && error.status === 503
  if (error && !aiDown) return <ErrorAlert error={error} />
  if (aiDown) {
    return (
      <p role="status" className="rounded-lg bg-amber-50 p-3 text-sm text-amber-900">
        {t('ownership.aiUnavailable')}
      </p>
    )
  }
  if (!result || result.outcome === 'passed') return null
  return result.outcome === 'retry' ? (
    <p role="status" className="rounded-lg bg-amber-50 p-3 text-sm text-amber-900">
      {t('ownership.retry')}
    </p>
  ) : (
    <Alert>{t(failedKey)}</Alert>
  )
}

function ErrorAlert({ error }: { error: Error }) {
  const errorText = useErrorText()
  return <Alert>{errorText(Object.values(fieldErrors(error))[0] ?? requestErrorKey(error, 'errors.saveFailed'))}</Alert>
}

/** Seconds until `expiresAt`, ticking every second. */
function useSecondsLeft(expiresAt: string | undefined) {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    if (!expiresAt) return
    const timer = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [expiresAt])
  // Capped at the 10-minute validity: `now` may be stale for up to a second right after a new code.
  return expiresAt ? Math.min(600, Math.max(0, Math.floor((new Date(expiresAt).getTime() - now) / 1000))) : 0
}
