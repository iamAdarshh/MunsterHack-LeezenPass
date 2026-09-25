import { zodResolver } from '@hookform/resolvers/zod'
import { type ReactNode, useEffect, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { fieldErrors, requestErrorKey } from '../../api/client'
import { type Bike, type BikeInput, bikeColors, bikeFeatures, bikeTypes } from '../../api/types'
import { Button } from '../../components/Button'
import { FormField, inputClass } from '../../components/FormField'
import { Alert } from '../../components/States'
import { useErrorText } from '../../i18n/useErrorText'
import type { Prefill } from './prefill'
import { Swatch } from './Swatch'

/** Same rule as FrameNumber.Normalize on the server. */
const normalize = (value: string) => value.toUpperCase().replace(/[^A-Z0-9]/g, '')
/** Local date as YYYY-MM-DD (toISOString would give the UTC date, wrong after midnight in Münster). */
const today = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

const schema = z.object({
  frameNumber: z.string().refine((v) => normalize(v).length >= 4 && normalize(v).length <= 32, 'errors.frameNumberInvalid'),
  feinCode: z
    .string()
    .refine((v) => v.trim() === '' || (normalize(v).length >= 6 && normalize(v).length <= 32), 'errors.feinCodeInvalid'),
  removeFeinCode: z.boolean(),
  brand: z.string().max(60, 'errors.tooLong'),
  model: z.string().max(60, 'errors.tooLong'),
  type: z.enum(bikeTypes),
  colorPrimary: z.union([z.enum(bikeColors), z.literal('')]).refine((v) => v !== '', 'errors.required'),
  colorSecondary: z.union([z.enum(bikeColors), z.literal('')]),
  isEbike: z.boolean(),
  batterySerial: z.string().max(64, 'errors.tooLong'),
  features: z.array(z.enum(bikeFeatures)),
  purchaseDate: z.string().refine((v) => v === '' || v <= today(), 'errors.dateInFuture'),
})

/** Raw field values (colour may still be empty). */
type FormValues = z.input<typeof schema>
/** After validation (primary colour is set). */
type ValidValues = z.output<typeof schema>

function toDefaults(bike?: Bike): FormValues {
  return {
    frameNumber: bike?.frameNumber ?? '',
    feinCode: '',
    removeFeinCode: false,
    brand: bike?.brand ?? '',
    model: bike?.model ?? '',
    type: bike?.type ?? 'city',
    colorPrimary: bike?.colorPrimary ?? '',
    colorSecondary: bike?.colorSecondary ?? '',
    isEbike: bike?.isEbike ?? false,
    batterySerial: bike?.batterySerial ?? '',
    features: bike?.features ?? [],
    purchaseDate: bike?.purchaseDate ?? '',
  }
}

const orNull = (value: string) => (value.trim() === '' ? null : value.trim())

function toInput(values: ValidValues): BikeInput {
  return {
    frameNumber: values.frameNumber.trim(),
    feinCode: orNull(values.feinCode),
    removeFeinCode: values.removeFeinCode,
    brand: orNull(values.brand),
    model: orNull(values.model),
    type: values.type,
    colorPrimary: values.colorPrimary,
    colorSecondary: values.colorSecondary || null,
    isEbike: values.isEbike,
    batterySerial: values.isEbike ? orNull(values.batterySerial) : null,
    features: values.features,
    purchaseDate: values.purchaseDate || null,
  }
}

interface Props {
  /** Existing bike when editing. */
  bike?: Bike
  /** AI suggestions. Only fill fields the user hasn't touched, never overwrite their input. */
  prefill?: Prefill
  submitLabel: string
  onSubmit: (input: BikeInput) => Promise<unknown>
}

export function BikeForm({ bike, prefill, submitLabel, onSubmit }: Props) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const [formError, setFormError] = useState<string | null>(null)
  const form = useForm<FormValues, unknown, ValidValues>({
    resolver: zodResolver(schema),
    defaultValues: toDefaults(bike),
  })
  const { errors, isSubmitting } = form.formState
  useEffect(() => {
    if (!prefill) return
    const untouched = (name: keyof Prefill) => !form.getFieldState(name).isDirty
    const options = { shouldValidate: form.formState.isSubmitted }
    const { frameNumber, brand, type, colorPrimary, colorSecondary, features } = prefill
    if (frameNumber !== undefined && untouched('frameNumber')) form.setValue('frameNumber', frameNumber, options)
    if (brand !== undefined && untouched('brand')) form.setValue('brand', brand, options)
    if (type !== undefined && untouched('type')) form.setValue('type', type, options)
    if (colorPrimary !== undefined && untouched('colorPrimary')) form.setValue('colorPrimary', colorPrimary, options)
    if (colorSecondary !== undefined && untouched('colorSecondary')) form.setValue('colorSecondary', colorSecondary, options)
    if (features !== undefined && untouched('features')) form.setValue('features', features, options)
  }, [prefill, form])

  const isEbike = useWatch({ control: form.control, name: 'isEbike' })
  const removeFeinCode = useWatch({ control: form.control, name: 'removeFeinCode' })

  const submit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      await onSubmit(toInput(values))
    } catch (error) {
      // Server-side validation / conflicts come back as i18n keys per field.
      const fields = fieldErrors(error)
      const known = Object.entries(fields).filter(([name]) => name in values)
      known.forEach(([name, reason]) => form.setError(name as keyof FormValues, { message: reason }))
      if (known.length === 0) setFormError(requestErrorKey(error, 'errors.saveFailed'))
    }
  })

  return (
    <form onSubmit={submit} noValidate className="space-y-5">
      <FormField
        label={t('bikeForm.frameNumber')}
        htmlFor="frameNumber"
        hint={t('bikeForm.frameNumberHint')}
        error={errors.frameNumber?.message}
      >
        <input
          id="frameNumber"
          autoCapitalize="characters"
          autoComplete="off"
          spellCheck={false}
          className={`${inputClass} font-mono`}
          aria-invalid={!!errors.frameNumber}
          {...form.register('frameNumber')}
        />
      </FormField>

      <FormField
        label={t('bikeForm.feinCode')}
        htmlFor="feinCode"
        hint={bike?.hasFeinCode ? t('bikeForm.feinCodeStoredHint') : t('bikeForm.feinCodeHint')}
        error={errors.feinCode?.message}
      >
        <input
          id="feinCode"
          autoCapitalize="characters"
          autoComplete="off"
          spellCheck={false}
          disabled={removeFeinCode}
          placeholder={bike?.hasFeinCode ? t('bikeForm.feinCodeStoredPlaceholder') : undefined}
          className={`${inputClass} font-mono disabled:bg-slate-100`}
          aria-invalid={!!errors.feinCode}
          {...form.register('feinCode')}
        />
      </FormField>
      {bike?.hasFeinCode && (
        <label className="flex min-h-11 items-center gap-3 text-sm">
          <input type="checkbox" className="size-5 accent-brand-700" {...form.register('removeFeinCode')} />
          {t('bikeForm.removeFeinCode')}
        </label>
      )}

      <div className="grid grid-cols-2 gap-3">
        <FormField label={t('bikeForm.brand')} htmlFor="brand" error={errors.brand?.message}>
          <input id="brand" className={inputClass} aria-invalid={!!errors.brand} {...form.register('brand')} />
        </FormField>
        <FormField label={t('bikeForm.model')} htmlFor="model" error={errors.model?.message}>
          <input id="model" className={inputClass} aria-invalid={!!errors.model} {...form.register('model')} />
        </FormField>
      </div>

      <FormField label={t('bikeForm.type')} htmlFor="type">
        <select id="type" className={inputClass} {...form.register('type')}>
          {bikeTypes.map((type) => (
            <option key={type} value={type}>
              {t(`bikeType.${type}`)}
            </option>
          ))}
        </select>
      </FormField>

      <Controller
        control={form.control}
        name="colorPrimary"
        render={({ field, fieldState }) => (
          <ColorPicker
            legend={t('bikeForm.colorPrimary')}
            value={field.value}
            onChange={field.onChange}
            error={fieldState.error?.message}
          />
        )}
      />
      <Controller
        control={form.control}
        name="colorSecondary"
        render={({ field }) => (
          <ColorPicker legend={t('bikeForm.colorSecondary')} value={field.value} onChange={field.onChange} optional />
        )}
      />

      <fieldset>
        <legend className="text-sm font-semibold text-slate-800">{t('bikeForm.features')}</legend>
        <div className="mt-2 flex flex-wrap gap-2">
          {bikeFeatures.map((feature) => (
            <label
              key={feature}
              className="flex min-h-11 cursor-pointer items-center gap-2 rounded-full border border-slate-300 px-3 text-sm has-checked:border-brand-700 has-checked:bg-brand-50 has-checked:text-brand-800"
            >
              <input type="checkbox" value={feature} className="sr-only" {...form.register('features')} />
              {t(`bikeFeature.${feature}`)}
            </label>
          ))}
        </div>
      </fieldset>

      <label className="flex min-h-11 items-center gap-3 font-semibold">
        <input type="checkbox" className="size-5 accent-brand-700" {...form.register('isEbike')} />
        {t('bikeForm.isEbike')}
      </label>
      {isEbike && (
        <FormField label={t('bikeForm.batterySerial')} htmlFor="batterySerial" error={errors.batterySerial?.message}>
          <input
            id="batterySerial"
            autoComplete="off"
            className={`${inputClass} font-mono`}
            {...form.register('batterySerial')}
          />
        </FormField>
      )}

      <FormField label={t('bikeForm.purchaseDate')} htmlFor="purchaseDate" error={errors.purchaseDate?.message}>
        <input
          id="purchaseDate"
          type="date"
          max={today()}
          className={inputClass}
          aria-invalid={!!errors.purchaseDate}
          {...form.register('purchaseDate')}
        />
      </FormField>

      {formError && <Alert>{errorText(formError)}</Alert>}

      <Button type="submit" fullWidth loading={isSubmitting}>
        {submitLabel}
      </Button>
    </form>
  )
}

interface ColorPickerProps {
  legend: string
  value: FormValues['colorPrimary']
  onChange: (value: FormValues['colorPrimary']) => void
  error?: string
  optional?: boolean
}

function ColorPicker({ legend, value, onChange, error, optional }: ColorPickerProps) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  return (
    <fieldset>
      <legend className="text-sm font-semibold text-slate-800">{legend}</legend>
      <div className="mt-2 grid grid-cols-3 gap-2 sm:grid-cols-4">
        {optional && (
          <ColorOption selected={value === ''} onSelect={() => onChange('')} label={t('bikeForm.noColor')} />
        )}
        {bikeColors.map((color) => (
          <ColorOption
            key={color}
            selected={value === color}
            onSelect={() => onChange(color)}
            label={t(`bikeColor.${color}`)}
            swatch={<Swatch color={color} />}
          />
        ))}
      </div>
      {error && <p className="mt-1 text-sm text-red-700">{errorText(error)}</p>}
    </fieldset>
  )
}

function ColorOption(props: { selected: boolean; onSelect: () => void; label: string; swatch?: ReactNode }) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={props.selected}
      onClick={props.onSelect}
      className={`flex min-h-11 items-center gap-2 rounded-lg border px-2 text-left text-sm ${
        props.selected ? 'border-brand-700 bg-brand-50 font-semibold text-brand-800' : 'border-slate-300'
      }`}
    >
      {props.swatch}
      {props.label}
    </button>
  )
}
