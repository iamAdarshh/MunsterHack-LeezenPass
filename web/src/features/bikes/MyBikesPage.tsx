import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { Bike } from '../../api/types'
import { buttonClass } from '../../components/buttonClass'
import { BikeIcon, PlusIcon } from '../../components/icons'
import { StatusBadge } from '../../components/StatusBadge'
import { TrustBadge } from '../../components/TrustBadge'
import { ErrorState, LoadingState } from '../../components/States'
import { useMyBikes } from './api'
import { Swatch } from './Swatch'
import { bikeTitle, coverPhoto } from './format'

export function MyBikesPage() {
  const { t } = useTranslation()
  const bikes = useMyBikes()

  return (
    <section>
      <div className="flex items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">{t('myBikes.title')}</h1>
        {bikes.data && bikes.data.length > 0 && (
          <Link to="/bikes/new" className={buttonClass('primary')}>
            <PlusIcon className="size-5" />
            {t('myBikes.add')}
          </Link>
        )}
      </div>

      <div className="mt-6">
        {bikes.isPending && <LoadingState />}
        {bikes.isError && <ErrorState onRetry={() => void bikes.refetch()} />}
        {bikes.data?.length === 0 && <EmptyState />}
        {bikes.data && bikes.data.length > 0 && (
          <ul className="space-y-3">
            {bikes.data.map((bike) => (
              <li key={bike.id}>
                <BikeCard bike={bike} />
              </li>
            ))}
          </ul>
        )}
      </div>

      {bikes.data && (
        <Link to="/claim" className={`${buttonClass('secondary', true)} mt-6`}>
          {t('claim.link')}
        </Link>
      )}
    </section>
  )
}

function EmptyState() {
  const { t } = useTranslation()
  return (
    <div className="rounded-xl border border-dashed border-slate-300 p-6 text-center">
      <BikeIcon className="mx-auto size-12 text-slate-400" />
      <p className="mt-3 font-semibold">{t('myBikes.emptyTitle')}</p>
      <p className="mt-1 text-sm text-slate-600">{t('myBikes.emptyBody')}</p>
      <Link to="/bikes/new" className={`${buttonClass('primary', true)} mt-5`}>
        <PlusIcon className="size-5" />
        {t('myBikes.add')}
      </Link>
    </div>
  )
}

function BikeCard({ bike }: { bike: Bike }) {
  const { t } = useTranslation()
  const photo = coverPhoto(bike)

  return (
    <Link
      to={`/bikes/${bike.id}`}
      className="flex gap-3 rounded-xl border border-slate-200 bg-white p-3 shadow-sm hover:border-brand-600"
    >
      <div className="flex size-20 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-slate-100">
        {photo ? (
          <img src={photo.thumbnailUrl} alt="" loading="lazy" className="size-full object-cover" />
        ) : (
          <BikeIcon className="size-8 text-slate-400" />
        )}
      </div>
      <div className="min-w-0 flex-1">
        <p className="truncate font-semibold">{bikeTitle(bike, t)}</p>
        <p className="truncate font-mono text-sm text-slate-600">{bike.frameNumber}</p>
        <div className="mt-2 flex flex-wrap items-center gap-2">
          <StatusBadge status={bike.status} />
          <TrustBadge level={bike.trustLevel} />
          {bike.colorPrimary && <Swatch color={bike.colorPrimary} className="size-4" />}
          <span className="text-xs text-slate-500">{t(`bikeType.${bike.type}`)}</span>
        </div>
      </div>
    </Link>
  )
}
