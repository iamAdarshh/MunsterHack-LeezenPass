import { useTranslation } from 'react-i18next'
import { useHealth } from '../api/health'

/** Shown when the API runs on synthetic seed data. */
export function DemoBanner() {
  const { t } = useTranslation()
  const { data } = useHealth()

  if (!data?.demoMode) return null

  return (
    <div role="status" className="bg-amber-300 px-4 py-1.5 text-center text-sm font-semibold text-amber-950">
      {t('demo.banner')}
    </div>
  )
}
