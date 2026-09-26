import { useTranslation } from 'react-i18next'
import type { TrustLevel } from '../api/types'
import { CheckIcon, ShieldIcon, UserIcon } from './icons'

const styles: Record<TrustLevel, { className: string; Icon: typeof UserIcon }> = {
  self_declared: { className: 'bg-slate-100 text-slate-700 ring-slate-300', Icon: UserIcon },
  evidence_checked: { className: 'bg-teal-50 text-teal-800 ring-teal-200', Icon: CheckIcon },
  third_party_verified: { className: 'bg-violet-50 text-violet-800 ring-violet-200', Icon: ShieldIcon },
}

/** How well ownership is proven (SPEC feature 5). Colour is always paired with an icon and text. */
export function TrustBadge({ level }: { level: TrustLevel }) {
  const { t } = useTranslation()
  const { className, Icon } = styles[level]
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold ring-1 ${className}`}
      title={t(`trust.hint.${level}`)}
    >
      <Icon className="size-3.5" />
      {t(`trust.level.${level}`)}
    </span>
  )
}
