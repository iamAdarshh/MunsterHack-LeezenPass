import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/client'
import { buttonClass } from '../../components/buttonClass'
import { AlertIcon, ShieldIcon } from '../../components/icons'
import { ErrorState, LoadingState } from '../../components/States'
import { useStolenBike, useTag } from './api'
import { CallPoliceBanner, StolenBikeCard } from './StolenBikeCard'

/**
 * Target of the QR code on the bike pass (/b/:token).
 * stolen -> stolen card; registered -> "registered, no theft reported" (no attributes, no owner data);
 * unknown token -> a warning, so a fake QR sticker can't vouch for a bike.
 */
export function TagPage() {
  const { t } = useTranslation()
  const { token = '' } = useParams()
  const tag = useTag(token)
  const stolen = useStolenBike(token, tag.data?.status === 'stolen')
  const unknown = tag.error instanceof ApiError && tag.error.status === 404

  return (
    <section className="space-y-4">
      {(tag.isPending || (tag.data?.status === 'stolen' && stolen.isPending)) && <LoadingState />}
      {((tag.isError && !unknown) || stolen.isError) && <ErrorState onRetry={() => void tag.refetch()} />}

      {stolen.data && (
        <>
          <StolenBikeCard bike={stolen.data} />
          <CallPoliceBanner />
        </>
      )}

      {tag.data?.status === 'registered' && (
        <div className="space-y-2 rounded-xl border border-emerald-200 bg-emerald-50 p-4 text-emerald-900">
          <p className="flex items-center gap-2 font-bold">
            <ShieldIcon className="size-5" />
            {t('tag.registeredTitle')}
          </p>
          <p className="text-sm">{t('tag.registeredBody')}</p>
          {tag.data.transferOpen && <p className="text-sm font-semibold">{t('tag.transferOpen')}</p>}
          <p className="text-sm">{t('tag.frameHint', { hint: tag.data.frameNumberHint })}</p>
        </div>
      )}

      {unknown && (
        <div className="space-y-2 rounded-xl border border-amber-300 bg-amber-50 p-4 text-amber-950">
          <p className="flex items-center gap-2 font-bold">
            <AlertIcon className="size-5" />
            {t('tag.unknownTitle')}
          </p>
          <p className="text-sm">{t('tag.unknownBody')}</p>
        </div>
      )}

      {(tag.data || unknown) && (
        <Link to="/" className={buttonClass('secondary', true)}>
          {t('nav.check')}
        </Link>
      )}
    </section>
  )
}
