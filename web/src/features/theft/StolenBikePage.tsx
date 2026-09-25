import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router'
import { ApiError } from '../../api/client'
import { buttonClass } from '../../components/buttonClass'
import { ShareButton } from '../../components/ShareButton'
import { ErrorState, LoadingState } from '../../components/States'
import { BackLink } from '../../components/BackLink'
import { useStolenBike } from './api'
import { CallPoliceBanner, StolenBikeCard } from './StolenBikeCard'

/** Public share card of one stolen bike (/stolen/:token). */
export function StolenBikePage() {
  const { t } = useTranslation()
  const { token = '' } = useParams()
  const bike = useStolenBike(token)
  const notStolen = bike.error instanceof ApiError && bike.error.status === 404

  return (
    <section className="space-y-4">
      <BackLink to="/stolen" label={t('stolen.title')} />
      {bike.isPending && <LoadingState />}
      {notStolen && <NotStolen />}
      {bike.isError && !notStolen && <ErrorState onRetry={() => void bike.refetch()} />}
      {bike.data && (
        <>
          <StolenBikeCard bike={bike.data} />
          <CallPoliceBanner />
          <ShareButton
            url={window.location.href}
            title={t('share.stolenTitle')}
            text={t('share.stolenText', { district: t(`district.${bike.data.district}`) })}
            variant="primary"
          />
        </>
      )}
    </section>
  )
}

export function NotStolen() {
  const { t } = useTranslation()
  return (
    <div className="space-y-4 rounded-xl border border-slate-200 p-4">
      <p className="font-semibold">{t('stolen.notStolenTitle')}</p>
      <p className="text-sm text-slate-600">{t('stolen.notStolenBody')}</p>
      <Link to="/" className={buttonClass('secondary', true)}>
        {t('nav.check')}
      </Link>
    </div>
  )
}
