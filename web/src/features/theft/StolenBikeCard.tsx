import { useTranslation } from 'react-i18next'
import type { StolenBike } from '../../api/types'
import { AlertIcon, CheckIcon } from '../../components/icons'
import { Swatch } from '../bikes/Swatch'
import { stolenTitle } from './format'
import { useFormat } from '../../i18n/useFormat'

/** "Police case no." vs "self-reported", so readers can judge how reliable a report is. */
export function ReportBadge({ policeReported }: { policeReported: boolean }) {
  const { t } = useTranslation()
  return policeReported ? (
    <span className="inline-flex items-center gap-1 rounded-full bg-sky-50 px-2 py-0.5 text-xs font-semibold text-sky-800 ring-1 ring-sky-200">
      <CheckIcon className="size-3.5" />
      {t('stolen.policeReported')}
    </span>
  ) : (
    <span className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-700 ring-1 ring-slate-200">
      {t('stolen.selfReported')}
    </span>
  )
}

export function CallPoliceBanner() {
  const { t } = useTranslation()
  return (
    <p role="note" className="flex items-start gap-2 rounded-lg bg-red-700 p-3 text-sm font-semibold text-white">
      <AlertIcon className="mt-0.5 size-5 shrink-0" />
      {t('stolen.callPolice')}
    </p>
  )
}

/** Full card for one stolen bike: the public share card and the QR tag page. */
export function StolenBikeCard({ bike }: { bike: StolenBike }) {
  const { t } = useTranslation()
  const format = useFormat()
  const colors = [bike.colorPrimary, bike.colorSecondary].filter((c) => c !== null)

  return (
    <article className="overflow-hidden rounded-xl border border-red-200 bg-white shadow-sm">
      <div className="flex items-center gap-2 bg-red-600 px-4 py-2 font-bold text-white">
        <AlertIcon className="size-5" />
        {t('stolen.badge')}
      </div>
      {bike.photos.length > 0 && (
        <div className="flex snap-x snap-mandatory gap-2 overflow-x-auto bg-slate-100 p-2">
          {bike.photos.map((photo) => (
            <img
              key={photo.id}
              src={photo.url}
              alt={t(`photoKind.${photo.kind}`)}
              className="aspect-[4/3] w-11/12 shrink-0 snap-center rounded-lg object-cover"
            />
          ))}
        </div>
      )}
      <div className="space-y-3 p-4">
        <div>
          <h1 className="text-2xl font-bold">{stolenTitle(bike, t)}</h1>
          <p className="text-slate-600">
            {t('stolen.stolenOnIn', {
              date: format.date(bike.stolenOn),
              district: t(`district.${bike.district}`),
            })}
          </p>
        </div>
        <ReportBadge policeReported={bike.policeReported} />
        <dl className="grid grid-cols-[7rem_1fr] gap-x-3 gap-y-2 text-sm">
          <dt className="text-slate-500">{t('bikeForm.type')}</dt>
          <dd>{t(`bikeType.${bike.type}`)}{bike.isEbike ? ` · ${t('bikeForm.isEbike')}` : ''}</dd>
          <dt className="text-slate-500">{t('bikeDetail.colors')}</dt>
          <dd className="flex flex-wrap gap-2">
            {colors.map((color) => (
              <span key={color} className="inline-flex items-center gap-1">
                <Swatch color={color} className="size-4" />
                {t(`bikeColor.${color}`)}
              </span>
            ))}
          </dd>
          <dt className="text-slate-500">{t('bikeForm.features')}</dt>
          <dd>{bike.features.map((f) => t(`bikeFeature.${f}`)).join(', ') || '–'}</dd>
        </dl>
      </div>
    </article>
  )
}
