import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { z } from 'zod'
import { fieldErrors, requestErrorKey } from '../../api/client'
import { lockTypes } from '../../api/types'
import { BackLink } from '../../components/BackLink'
import { Button } from '../../components/Button'
import { FormField, inputClass } from '../../components/FormField'
import { MapPicker } from '../../components/MapPicker'
import { Alert, ErrorState, LoadingState } from '../../components/States'
import { useErrorText } from '../../i18n/useErrorText'
import { useBike } from '../bikes/api'
import { bikeTitle } from '../bikes/format'
import { useReportTheft } from './api'

/** Value for <input type="datetime-local"> in local time. */
function localDateTime(date: Date) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

const schema = z.object({
  stolenAt: z
    .string()
    .min(1, 'errors.required')
    .refine((v) => new Date(v).getTime() <= Date.now() + 5 * 60_000, 'errors.dateInFuture'),
  location: z
    .object({ latitude: z.number(), longitude: z.number() })
    .nullable()
    .refine((v) => v !== null, 'errors.locationRequired'),
  lockType: z.enum(lockTypes),
  locationNote: z.string().max(200, 'errors.tooLong'),
  policeCaseNo: z.string().max(40, 'errors.tooLong'),
})

type FormValues = z.input<typeof schema>
type ValidValues = z.output<typeof schema>

export function ReportTheftPage() {
  const { t } = useTranslation()
  const { id = '' } = useParams()
  const bike = useBike(id)

  return (
    <section>
      <BackLink to={`/bikes/${id}`} label={t('common.back')} />
      <h1 className="mt-2 text-2xl font-bold">{t('reportTheft.title')}</h1>
      {bike.isPending && <LoadingState />}
      {bike.isError && <ErrorState onRetry={() => void bike.refetch()} />}
      {bike.data && (
        <>
          <p className="mt-1 text-slate-600">{bikeTitle(bike.data, t)} · <span className="font-mono">{bike.data.frameNumber}</span></p>
          <p className="mt-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-900">{t('reportTheft.intro')}</p>
          <div className="mt-6">
            {bike.data.status === 'stolen' ? (
              <Alert>{t('errors.alreadyStolen')}</Alert>
            ) : (
              <ReportTheftForm bikeId={id} />
            )}
          </div>
        </>
      )}
    </section>
  )
}

function ReportTheftForm({ bikeId }: { bikeId: string }) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const navigate = useNavigate()
  const report = useReportTheft(bikeId)
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<FormValues, unknown, ValidValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      stolenAt: localDateTime(new Date()),
      location: null,
      lockType: 'u_bolt',
      locationNote: '',
      policeCaseNo: '',
    },
  })
  const { errors, isSubmitting } = form.formState

  const submit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      await report.mutateAsync({
        stolenAt: new Date(values.stolenAt).toISOString(),
        latitude: values.location.latitude,
        longitude: values.location.longitude,
        lockType: values.lockType,
        locationNote: values.locationNote.trim() || null,
        policeCaseNo: values.policeCaseNo.trim() || null,
      })
      navigate(`/bikes/${bikeId}`, { replace: true })
    } catch (error) {
      const fields = fieldErrors(error)
      setFormError(Object.values(fields)[0] ?? requestErrorKey(error, 'errors.saveFailed'))
    }
  })

  return (
    <form onSubmit={submit} noValidate className="space-y-5">
      <FormField label={t('reportTheft.stolenAt')} htmlFor="stolenAt" error={errors.stolenAt?.message}>
        <input
          id="stolenAt"
          type="datetime-local"
          max={localDateTime(new Date())}
          className={inputClass}
          aria-invalid={!!errors.stolenAt}
          {...form.register('stolenAt')}
        />
      </FormField>

      <div className="space-y-1">
        <p className="text-sm font-semibold text-slate-800">{t('reportTheft.location')}</p>
        <p className="text-xs text-slate-500">{t('reportTheft.locationHint')}</p>
        <Controller
          control={form.control}
          name="location"
          render={({ field }) => <MapPicker value={field.value} onChange={field.onChange} />}
        />
        {errors.location && <p className="text-sm text-red-700">{errorText(errors.location.message)}</p>}
      </div>

      <FormField
        label={t('reportTheft.locationNote')}
        htmlFor="locationNote"
        hint={t('reportTheft.locationNoteHint')}
        error={errors.locationNote?.message}
      >
        <input id="locationNote" className={inputClass} {...form.register('locationNote')} />
      </FormField>

      <FormField label={t('reportTheft.lockType')} htmlFor="lockType">
        <select id="lockType" className={inputClass} {...form.register('lockType')}>
          {lockTypes.map((lock) => (
            <option key={lock} value={lock}>
              {t(`lockType.${lock}`)}
            </option>
          ))}
        </select>
      </FormField>

      <FormField
        label={t('reportTheft.policeCaseNo')}
        htmlFor="policeCaseNo"
        hint={t('reportTheft.policeCaseNoHint')}
        error={errors.policeCaseNo?.message}
      >
        <input id="policeCaseNo" autoComplete="off" className={inputClass} {...form.register('policeCaseNo')} />
      </FormField>

      {formError && <Alert>{errorText(formError)}</Alert>}

      <Button type="submit" variant="danger" fullWidth loading={isSubmitting}>
        {t('reportTheft.submit')}
      </Button>
    </form>
  )
}
