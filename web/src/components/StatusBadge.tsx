import { useTranslation } from 'react-i18next'
import type { BikeStatus } from '../api/types'
import { AlertIcon, CheckIcon, ShieldIcon } from './icons'

const styles: Record<BikeStatus, { className: string; Icon: typeof AlertIcon }> = {
  active: { className: 'bg-emerald-50 text-emerald-800 ring-emerald-200', Icon: ShieldIcon },
  stolen: { className: 'bg-red-50 text-red-800 ring-red-200', Icon: AlertIcon },
  recovered: { className: 'bg-sky-50 text-sky-800 ring-sky-200', Icon: CheckIcon },
}

/** Colour is always paired with an icon and text. */
export function StatusBadge({ status }: { status: BikeStatus }) {
  const { t } = useTranslation()
  const { className, Icon } = styles[status]
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-semibold ring-1 ${className}`}>
      <Icon className="size-3.5" />
      {t(`bikeStatus.${status}`)}
    </span>
  )
}
