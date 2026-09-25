import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useLocation, useNavigate, useParams } from 'react-router'
import type { Bike } from '../../api/types'
import { BackLink } from '../../components/BackLink'
import { Button } from '../../components/Button'
import { buttonClass } from '../../components/buttonClass'
import { StatusBadge } from '../../components/StatusBadge'
import { Alert, ErrorState, LoadingState } from '../../components/States'
import { StolenPanel } from '../theft/StolenPanel'
import { useBike, useDeleteBike } from './api'
import { Swatch } from './Swatch'
import { bikeTitle } from './format'
import { PhotoSection } from './PhotoSection'
import { useFormat } from '../../i18n/useFormat'

export function BikeDetailPage() {
  const { t } = useTranslation()
  const { id = '' } = useParams()
  const bike = useBike(id)

  return (
    <section>
      <BackLink to="/bikes" label={t('nav.myBikes')} />
      <div className="mt-2">
        {bike.isPending && <LoadingState />}
        {bike.isError && <ErrorState onRetry={() => void bike.refetch()} />}
        {bike.data && <BikeDetail bike={bike.data} />}
      </div>
    </section>
  )
}

function BikeDetail({ bike }: { bike: Bike }) {
  const { t } = useTranslation()
  const format = useFormat()
  const location = useLocation()
  const justRegistered = (location.state as { justRegistered?: boolean } | null)?.justRegistered === true

  return (
    <>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h1 className="text-2xl font-bold break-words">{bikeTitle(bike, t)}</h1>
          <p className="font-mono text-slate-600">{bike.frameNumber}</p>
        </div>
        <StatusBadge status={bike.status} />
      </div>

      {justRegistered && (
        <p role="status" className="mt-4 rounded-lg bg-emerald-50 p-3 text-sm text-emerald-900">
          {t('bikeDetail.registered')}
        </p>
      )}

      {bike.theft && <StolenPanel bike={bike} theft={bike.theft} />}

      <PhotoSection bike={bike} />

      <h2 className="mt-8 text-lg font-bold">{t('bikeDetail.details')}</h2>
      <dl className="mt-2 divide-y divide-slate-100 rounded-xl border border-slate-200">
        <Row label={t('bikeForm.type')}>{t(`bikeType.${bike.type}`)}</Row>
        <Row label={t('bikeDetail.colors')}>
          <span className="flex flex-wrap items-center gap-2">
            {[bike.colorPrimary, bike.colorSecondary].map(
              (color) =>
                color && (
                  <span key={color} className="inline-flex items-center gap-1">
                    <Swatch color={color} className="size-4" />
                    {t(`bikeColor.${color}`)}
                  </span>
                ),
            )}
          </span>
        </Row>
        <Row label={t('bikeDetail.feinCode')}>{bike.hasFeinCode ? t('bikeDetail.feinStored') : '–'}</Row>
        <Row label={t('bikeForm.isEbike')}>
          {bike.isEbike ? `${t('common.yes')}${bike.batterySerial ? ` · ${bike.batterySerial}` : ''}` : t('common.no')}
        </Row>
        <Row label={t('bikeForm.features')}>
          {bike.features.length > 0 ? bike.features.map((f) => t(`bikeFeature.${f}`)).join(', ') : '–'}
        </Row>
        <Row label={t('bikeForm.purchaseDate')}>
          {bike.purchaseDate ? format.date(bike.purchaseDate) : '–'}
        </Row>
      </dl>

      <div className="mt-8 space-y-3">
        {bike.status !== 'stolen' && (
          <Link to={`/bikes/${bike.id}/theft`} className={buttonClass('danger', true)}>
            {t('bikeDetail.reportStolen')}
          </Link>
        )}
        <a href={`/api/bikes/${bike.id}/pass.pdf`} download className={buttonClass('secondary', true)}>
          {t('bikeDetail.downloadPass')}
        </a>
        <Link to={`/bikes/${bike.id}/edit`} className={buttonClass('secondary', true)}>
          {t('bikeDetail.edit')}
        </Link>
        <DeleteButton bikeId={bike.id} />
      </div>
    </>
  )
}

function Row({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="grid grid-cols-[8rem_1fr] gap-3 px-3 py-2.5 text-sm">
      <dt className="text-slate-500">{label}</dt>
      <dd className="min-w-0 break-words">{children}</dd>
    </div>
  )
}

function DeleteButton({ bikeId }: { bikeId: string }) {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const remove = useDeleteBike(bikeId)

  return (
    <>
      <Button
        variant="danger"
        fullWidth
        loading={remove.isPending}
        onClick={() => {
          if (!window.confirm(t('bikeDetail.deleteConfirm'))) return
          remove.mutate(undefined, { onSuccess: () => navigate('/bikes', { replace: true }) })
        }}
      >
        {t('bikeDetail.delete')}
      </Button>
      {remove.isError && <Alert>{t('errors.deleteFailed')}</Alert>}
    </>
  )
}
