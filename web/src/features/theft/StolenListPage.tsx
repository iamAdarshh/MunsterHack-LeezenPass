import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useSearchParams } from 'react-router'
import {
  type BikeColor,
  type BikeType,
  type District,
  type StolenBike,
  bikeColors,
  bikeTypes,
  districts,
} from '../../api/types'
import { inputClass } from '../../components/FormField'
import { BikeIcon } from '../../components/icons'
import { ErrorState, LoadingState } from '../../components/States'
import { Swatch } from '../bikes/Swatch'
import { type StolenFilters, useStolenList } from './api'
import { stolenTitle } from './format'
import { CallPoliceBanner, ReportBadge } from './StolenBikeCard'
import { useFormat } from '../../i18n/useFormat'

function pick<T extends string>(value: string | null, allowed: readonly T[]): T | undefined {
  return allowed.find((a) => a === value)
}

export function StolenListPage() {
  const { t } = useTranslation()
  // Filters live in the URL so a filtered list can be shared.
  const [params, setParams] = useSearchParams()
  const filters: StolenFilters = {
    type: pick<BikeType>(params.get('type'), bikeTypes),
    color: pick<BikeColor>(params.get('color'), bikeColors),
    district: pick<District>(params.get('district'), districts),
  }
  const stolen = useStolenList(filters)

  const setFilter = (key: keyof StolenFilters, value: string) => {
    const next = new URLSearchParams(params)
    if (value) next.set(key, value)
    else next.delete(key)
    setParams(next, { replace: true })
  }

  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-bold">{t('stolen.title')}</h1>
      <CallPoliceBanner />

      <div className="grid grid-cols-3 gap-2">
        <FilterSelect label={t('bikeForm.type')} value={filters.type} onChange={(v) => setFilter('type', v)}>
          {bikeTypes.map((type) => (
            <option key={type} value={type}>{t(`bikeType.${type}`)}</option>
          ))}
        </FilterSelect>
        <FilterSelect label={t('stolen.color')} value={filters.color} onChange={(v) => setFilter('color', v)}>
          {bikeColors.map((color) => (
            <option key={color} value={color}>{t(`bikeColor.${color}`)}</option>
          ))}
        </FilterSelect>
        <FilterSelect label={t('stolen.district')} value={filters.district} onChange={(v) => setFilter('district', v)}>
          {districts.map((district) => (
            <option key={district} value={district}>{t(`district.${district}`)}</option>
          ))}
        </FilterSelect>
      </div>

      {stolen.isPending && <LoadingState />}
      {stolen.isError && <ErrorState onRetry={() => void stolen.refetch()} />}
      {stolen.data?.length === 0 && (
        <p className="rounded-xl border border-dashed border-slate-300 p-6 text-center text-slate-600">
          {t('stolen.empty')}
        </p>
      )}
      {stolen.data && stolen.data.length > 0 && (
        <ul className="space-y-3">
          {stolen.data.map((bike) => (
            <li key={bike.token}>
              <StolenRow bike={bike} />
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}

function FilterSelect(props: {
  label: string
  value: string | undefined
  onChange: (value: string) => void
  children: ReactNode
}) {
  const { t } = useTranslation()
  return (
    <label className="block min-w-0 text-xs font-semibold text-slate-600">
      {props.label}
      <select
        value={props.value ?? ''}
        onChange={(e) => props.onChange(e.target.value)}
        className={`${inputClass} mt-1 px-2 text-sm`}
      >
        <option value="">{t('stolen.all')}</option>
        {props.children}
      </select>
    </label>
  )
}

function StolenRow({ bike }: { bike: StolenBike }) {
  const { t } = useTranslation()
  const format = useFormat()
  const photo = bike.photos[0]

  return (
    <Link
      to={`/stolen/${bike.token}`}
      className="flex gap-3 rounded-xl border border-slate-200 bg-white p-3 shadow-sm hover:border-red-400"
    >
      <div className="flex size-20 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-slate-100">
        {photo ? (
          <img src={photo.thumbnailUrl} alt="" loading="lazy" className="size-full object-cover" />
        ) : (
          <BikeIcon className="size-8 text-slate-400" />
        )}
      </div>
      <div className="min-w-0 flex-1 space-y-1">
        <p className="truncate font-semibold">{stolenTitle(bike, t)}</p>
        <p className="text-sm text-slate-600">
          {t(`district.${bike.district}`)} · {format.date(bike.stolenOn)}
        </p>
        <div className="flex items-center gap-2">
          {bike.colorPrimary && <Swatch color={bike.colorPrimary} className="size-4" />}
          {bike.colorSecondary && <Swatch color={bike.colorSecondary} className="size-4" />}
          <ReportBadge policeReported={bike.policeReported} />
        </div>
      </div>
    </Link>
  )
}
