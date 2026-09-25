import { Link } from 'react-router'
import { ChevronLeftIcon } from './icons'

export function BackLink({ to, label }: { to: string; label: string }) {
  return (
    <Link to={to} className="-ml-1 inline-flex min-h-11 items-center gap-1 text-sm font-semibold text-brand-700">
      <ChevronLeftIcon className="size-4" />
      {label}
    </Link>
  )
}
