import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/client'
import { buttonClass } from '../../components/buttonClass'
import { AlertIcon, CheckIcon } from '../../components/icons'
import { ErrorState, LoadingState } from '../../components/States'
import { useFormat } from '../../i18n/useFormat'
import { Swatch } from '../bikes/Swatch'
import { useVerify } from './api'

/** Public check of a transfer certificate (QR code on the PDF). No names, frame number as hint only. */
export function VerifyPage() {
  const { t } = useTranslation()
  const format = useFormat()
  const { token = '' } = useParams()
  const verify = useVerify(token)
  const notFound = verify.error instanceof ApiError && verify.error.status === 404
  const v = verify.data

  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-bold">{t('verify.title')}</h1>
      {verify.isPending && <LoadingState />}
      {verify.isError && !notFound && <ErrorState onRetry={() => void verify.refetch()} />}

      {notFound && (
        <div role="alert" className="space-y-2 rounded-xl border border-red-200 bg-red-50 p-4 text-red-900">
          <p className="flex items-center gap-2 font-bold">
            <AlertIcon className="size-5" />
            {t('verify.notFoundTitle')}
          </p>
          <p className="text-sm">{t('verify.notFoundBody')}</p>
        </div>
      )}

      {v && (
        <>
          <div className="space-y-3 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-emerald-950">
            <p className="flex items-center gap-2 font-bold">
              <CheckIcon className="size-5" />
              {t('verify.validTitle')}
            </p>
            <p className="text-sm">{t('verify.transferredAt', { date: format.dateTime(v.transferredAt) })}</p>
            <dl className="grid grid-cols-[8rem_1fr] gap-x-3 gap-y-2 text-sm">
              <dt className="text-emerald-800">{t('bikeForm.type')}</dt>
              <dd>{[t(`bikeType.${v.type}`), v.brand, v.model].filter(Boolean).join(' · ')}</dd>
              <dt className="text-emerald-800">{t('bikeDetail.colors')}</dt>
              <dd className="flex flex-wrap gap-2">
                {[v.colorPrimary, v.colorSecondary].map(
                  (c) =>
                    c && (
                      <span key={c} className="inline-flex items-center gap-1">
                        <Swatch color={c} className="size-4" />
                        {t(`bikeColor.${c}`)}
                      </span>
                    ),
                )}
              </dd>
              <dt className="text-emerald-800">{t('bikeForm.frameNumber')}</dt>
              <dd className="font-mono">{t('verify.frameHint', { hint: v.frameNumberHint })}</dd>
            </dl>
          </div>

          {!v.stillWithThisOwner && (
            <p className="rounded-lg bg-amber-50 p-3 text-sm text-amber-900">{t('verify.transferredAgain')}</p>
          )}
          {v.currentStatus === 'stolen' && (
            <p role="alert" className="flex items-start gap-2 rounded-lg bg-red-700 p-3 text-sm font-semibold text-white">
              <AlertIcon className="mt-0.5 size-5 shrink-0" />
              {t('verify.stolenNow')}
            </p>
          )}
        </>
      )}

      {(v || notFound) && (
        <Link to="/" className={buttonClass('secondary', true)}>
          {t('nav.check')}
        </Link>
      )}
    </section>
  )
}
