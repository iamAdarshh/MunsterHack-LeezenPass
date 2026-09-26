import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router'
import type { CheckResponse, TrustLevel } from '../../api/types'
import { buttonClass } from '../../components/buttonClass'
import { TrustBadge } from '../../components/TrustBadge'
import { AlertIcon, CheckIcon, SearchIcon, ShieldIcon } from '../../components/icons'
import { CallPoliceBanner, StolenBikeCard } from '../theft/StolenBikeCard'

const warningSigns = ['price', 'receipt', 'frame', 'place', 'hurry', 'lock', 'details'] as const

/** Colour is always paired with an icon and text (web/CLAUDE.md). */
export function CheckResultView({ response }: { response: CheckResponse }) {
  const { t } = useTranslation()

  switch (response.result) {
    case 'stolen':
      return (
        <div className="space-y-4">
          <Banner tone="red" icon={<AlertIcon className="size-6" />} title={t('check.result.stolen.title')}>
            {t('check.result.stolen.body')}
          </Banner>
          <CallPoliceBanner />
          {response.bikes.map((bike) => (
            <StolenBikeCard key={bike.token} bike={bike} />
          ))}
        </div>
      )
    case 'possible_match':
      return (
        <div className="space-y-4">
          {response.bikes.length > 0 ? (
            <Banner tone="amber" icon={<SearchIcon className="size-6" />} title={t('check.result.possible_match.title')}>
              {t('check.result.possible_match.body')}
            </Banner>
          ) : (
            // Look-alike of a bike with an open handover, not of a stolen one: no photos, neutral wording.
            <Banner tone="amber" icon={<SearchIcon className="size-6" />} title={t('check.result.possible_match.similarTitle')}>
              {t('check.result.possible_match.similarBody')}
            </Banner>
          )}
          {response.bikes.map((bike) => (
            <StolenBikeCard key={bike.token} bike={bike} showTrust={false} />
          ))}
        </div>
      )
    case 'verified_transfer':
      return (
        <div className="space-y-4">
          <Banner tone="green" icon={<ShieldIcon className="size-6" />} title={t('check.result.verified_transfer.title')}>
            {t('check.result.verified_transfer.body')}
          </Banner>
          {response.trustLevel && <TrustLine level={response.trustLevel} />}
          <Link to="/claim" className={buttonClass('secondary', true)}>
            {t('claim.link')}
          </Link>
        </div>
      )
    default:
      return (
        <div className="space-y-4">
          <Banner tone="grey" icon={<SearchIcon className="size-6" />} title={t('check.result.unknown.title')}>
            {t('check.result.unknown.body')}
          </Banner>
          <section className="rounded-xl border border-slate-200 p-4">
            <h3 className="font-bold">{t('check.signs.title')}</h3>
            <ul className="mt-2 space-y-2">
              {warningSigns.map((sign) => (
                <li key={sign} className="flex items-start gap-2 text-sm">
                  <AlertIcon className="mt-0.5 size-4 shrink-0 text-amber-600" />
                  {t(`check.signs.${sign}`)}
                </li>
              ))}
            </ul>
            <p className="mt-3 flex items-start gap-2 text-sm text-slate-600">
              <CheckIcon className="mt-0.5 size-4 shrink-0 text-emerald-700" />
              {t('check.signs.tip')}
            </p>
          </section>
        </div>
      )
  }
}

const tones = {
  red: 'border-red-300 bg-red-50 text-red-900',
  amber: 'border-amber-300 bg-amber-50 text-amber-950',
  green: 'border-emerald-300 bg-emerald-50 text-emerald-950',
  grey: 'border-slate-300 bg-slate-100 text-slate-900',
} as const

function TrustLine({ level }: { level: TrustLevel }) {
  const { t } = useTranslation()
  return (
    <p className="flex flex-wrap items-center gap-2 text-sm text-slate-700">
      {t('trust.checkLabel')} <TrustBadge level={level} />
    </p>
  )
}

function Banner(props: { tone: keyof typeof tones; icon: ReactNode; title: string; children: ReactNode }) {
  return (
    <div role="status" className={`rounded-xl border-2 p-4 ${tones[props.tone]}`}>
      <p className="flex items-center gap-2 text-lg font-bold">
        {props.icon}
        {props.title}
      </p>
      <p className="mt-1 text-sm">{props.children}</p>
    </div>
  )
}
