import { zodResolver } from '@hookform/resolvers/zod'
import type { ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { fieldErrors, requestErrorKey } from '../../api/client'
import type { Bike, Theft } from '../../api/types'
import { Button } from '../../components/Button'
import { buttonClass } from '../../components/buttonClass'
import { CopyButton } from '../../components/CopyButton'
import { FormField, inputClass } from '../../components/FormField'
import { AlertIcon } from '../../components/icons'
import { ShareButton } from '../../components/ShareButton'
import { Alert } from '../../components/States'
import { useErrorText } from '../../i18n/useErrorText'
import { useMarkRecovered, useUpdateTheft } from './api'
import { internetwacheUrl, policeSummary } from './policeSummary'
import { useFormat } from '../../i18n/useFormat'

/** Everything the owner needs right after a theft: police, insurance, sharing, recovery. */
export function StolenPanel({ bike, theft }: { bike: Bike; theft: Theft }) {
  const { t } = useTranslation()
  const origin = window.location.origin
  const stolenAt = useFormat().dateTime(theft.stolenAt)

  return (
    <div className="mt-6 space-y-4">
      <div role="status" className="rounded-xl border border-red-200 bg-red-50 p-4 text-red-900">
        <p className="flex items-center gap-2 font-bold">
          <AlertIcon className="size-5" />
          {t('stolenPanel.title')}
        </p>
        <p className="mt-1 text-sm">
          {t('stolenPanel.when', { date: stolenAt, district: t(`district.${theft.district}`) })}
        </p>
        <p className="mt-1 text-sm">{t('stolenPanel.publicHint')}</p>
      </div>

      <Step number={1} title={t('stolenPanel.policeTitle')}>
        <p className="text-sm text-slate-600">{t('stolenPanel.policeBody')}</p>
        <CopyButton text={policeSummary(bike, theft, origin)} label={t('stolenPanel.copySummary')} />
        <a href={internetwacheUrl} target="_blank" rel="noreferrer" className={buttonClass('primary', true)}>
          {t('stolenPanel.openInternetwache')}
        </a>
        <details className="text-sm">
          <summary className="min-h-11 cursor-pointer py-2 font-semibold text-brand-700">{t('stolenPanel.showSummary')}</summary>
          <pre className="rounded-lg bg-slate-100 p-3 text-xs whitespace-pre-wrap [overflow-wrap:anywhere]">{policeSummary(bike, theft, origin)}</pre>
        </details>
      </Step>

      <Step number={2} title={t('stolenPanel.caseNoTitle')}>
        <CaseNumberForm bikeId={bike.id} theft={theft} />
      </Step>

      <Step number={3} title={t('stolenPanel.insuranceTitle')}>
        <p className="text-sm text-slate-600">{t('stolenPanel.insuranceBody')}</p>
        <a href={`/api/bikes/${bike.id}/pass.pdf`} download className={buttonClass('secondary', true)}>
          {t('bikeDetail.downloadPass')}
        </a>
      </Step>

      <Step number={4} title={t('stolenPanel.shareTitle')}>
        <p className="text-sm text-slate-600">{t('stolenPanel.shareBody')}</p>
        <ShareButton
          url={`${origin}/stolen/${bike.publicToken}`}
          title={t('share.stolenTitle')}
          text={t('share.stolenText', { district: t(`district.${theft.district}`) })}
          variant="primary"
        />
      </Step>

      <RecoveredButton bikeId={bike.id} />
    </div>
  )
}

function Step({ number, title, children }: { number: number; title: string; children: ReactNode }) {
  return (
    <section className="space-y-3 rounded-xl border border-slate-200 p-4">
      <h3 className="flex items-center gap-2 font-bold">
        <span className="flex size-7 items-center justify-center rounded-full bg-brand-700 text-sm text-white">{number}</span>
        {title}
      </h3>
      {children}
    </section>
  )
}

const caseNoSchema = z.object({ policeCaseNo: z.string().max(40, 'errors.tooLong') })

function CaseNumberForm({ bikeId, theft }: { bikeId: string; theft: Theft }) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const update = useUpdateTheft(bikeId)
  const form = useForm({
    resolver: zodResolver(caseNoSchema),
    defaultValues: { policeCaseNo: theft.policeCaseNo ?? '' },
  })

  const submit = form.handleSubmit((values) =>
    update.mutate(
      { locationNote: theft.locationNote, policeCaseNo: values.policeCaseNo.trim() || null },
      { onSuccess: () => form.reset(values) },
    ),
  )

  return (
    <form onSubmit={submit} noValidate className="space-y-3">
      <FormField
        label={t('reportTheft.policeCaseNo')}
        htmlFor="caseNo"
        hint={t('stolenPanel.caseNoHint')}
        error={form.formState.errors.policeCaseNo?.message}
      >
        <input id="caseNo" autoComplete="off" className={inputClass} {...form.register('policeCaseNo')} />
      </FormField>
      {update.isError && <Alert>{errorText(requestErrorKey(update.error, 'errors.saveFailed'))}</Alert>}
      {update.isSuccess && !form.formState.isDirty && (
        <p role="status" className="text-sm text-emerald-700">{t('stolenPanel.saved')}</p>
      )}
      <Button type="submit" variant="secondary" fullWidth loading={update.isPending}>
        {t('editBike.submit')}
      </Button>
    </form>
  )
}

function RecoveredButton({ bikeId }: { bikeId: string }) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const recovered = useMarkRecovered(bikeId)
  return (
    <>
      <Button
        fullWidth
        loading={recovered.isPending}
        onClick={() => {
          if (window.confirm(t('stolenPanel.recoveredConfirm'))) recovered.mutate()
        }}
      >
        {t('stolenPanel.recovered')}
      </Button>
      {recovered.isError && (
        <Alert>
          {errorText(
            Object.values(fieldErrors(recovered.error))[0] ?? requestErrorKey(recovered.error, 'errors.saveFailed'),
          )}
        </Alert>
      )}
    </>
  )
}
