import { useTranslation } from 'react-i18next'
import { StarIcon } from '../../components/icons'
import { ErrorState, LoadingState } from '../../components/States'
import { useFormat } from '../../i18n/useFormat'
import { useGoodwill } from './api'

/** Goodwill points, badge, progress to the next badge and history (SPEC feature 6). Own account only. */
export function GoodwillCard() {
  const { t } = useTranslation()
  const format = useFormat()
  const goodwill = useGoodwill()

  if (goodwill.isPending) return <LoadingState />
  if (goodwill.isError) return <ErrorState onRetry={() => void goodwill.refetch()} />

  const { total, badge, nextBadge, history } = goodwill.data
  const from = badge?.threshold ?? 0
  const progress = nextBadge ? Math.round(((total - from) / (nextBadge.threshold - from)) * 100) : 100

  return (
    <section className="space-y-3 rounded-xl border border-amber-200 bg-amber-50/60 p-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-slate-600">{t('goodwill.title')}</h2>
          <p className="text-3xl font-bold whitespace-nowrap">{t('goodwill.points', { count: total })}</p>
        </div>
        {badge && (
          <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-amber-400 px-2.5 py-1 text-xs font-bold whitespace-nowrap text-amber-950">
            <StarIcon className="size-4" />
            {t(`goodwill.badge.${badge.key}`)}
          </span>
        )}
      </div>

      {nextBadge && (
        <div className="space-y-1">
          <div
            className="h-2 overflow-hidden rounded-full bg-amber-100"
            role="progressbar"
            aria-valuenow={progress}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label={t('goodwill.next', { count: nextBadge.threshold - total, badge: t(`goodwill.badge.${nextBadge.key}`) })}
          >
            <div className="h-full rounded-full bg-amber-500" style={{ width: `${progress}%` }} />
          </div>
          <p className="text-sm text-slate-700">
            {t('goodwill.next', { count: nextBadge.threshold - total, badge: t(`goodwill.badge.${nextBadge.key}`) })}
          </p>
        </div>
      )}

      {history.length === 0 ? (
        <p className="text-sm text-slate-600">{t('goodwill.empty')}</p>
      ) : (
        <ul className="divide-y divide-amber-100 text-sm">
          {history.map((entry, index) => (
            <li key={index} className="flex items-center justify-between gap-3 py-2">
              <span className={entry.status === 'revoked' ? 'text-slate-400' : undefined}>
                {t(`goodwill.action.${entry.action}`)}
                <span className="block text-xs text-slate-500">
                  {format.date(entry.createdAt)}
                  {entry.status === 'revoked' && ` · ${t('goodwill.revoked')}`}
                </span>
              </span>
              <span className={`font-semibold ${entry.status === 'revoked' ? 'text-slate-400 line-through' : 'text-amber-800'}`}>
                +{entry.points}
              </span>
            </li>
          ))}
        </ul>
      )}
      <p className="text-xs text-slate-500">{t('goodwill.rules')}</p>
    </section>
  )
}
