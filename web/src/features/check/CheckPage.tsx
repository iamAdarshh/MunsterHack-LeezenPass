import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { fieldErrors, requestErrorKey } from '../../api/client'
import { Button } from '../../components/Button'
import { FormField, inputClass } from '../../components/FormField'
import { Alert } from '../../components/States'
import { useErrorText } from '../../i18n/useErrorText'
import { useCheck } from './api'
import { turnstileSiteKey } from './captcha'
import { CheckResultView } from './CheckResultView'
import { Turnstile } from './Turnstile'

type Mode = 'frame' | 'fein'

/** Same normalisation as FrameNumber/FeinCode on the server. */
const normalize = (value: string) => value.toUpperCase().replace(/[^A-Z0-9]/g, '')

const schemas = {
  frame: z.object({
    value: z.string().refine((v) => normalize(v).length >= 4 && normalize(v).length <= 32, 'errors.frameNumberInvalid'),
  }),
  fein: z.object({
    value: z.string().refine((v) => normalize(v).length >= 6 && normalize(v).length <= 32, 'errors.feinCodeInvalid'),
  }),
}

/** Public: check a used bike before buying. */
export function CheckPage() {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const [mode, setMode] = useState<Mode>('frame')
  const [captchaToken, setCaptchaToken] = useState<string>()
  const [captchaReset, setCaptchaReset] = useState(0)
  const check = useCheck()
  const form = useForm({ resolver: zodResolver(schemas[mode]), defaultValues: { value: '' } })
  const { errors } = form.formState
  const needsCaptcha = turnstileSiteKey !== ''

  const submit = form.handleSubmit(({ value }) => {
    check.mutate(
      mode === 'frame' ? { frameNumber: value, captchaToken } : { feinCode: value, captchaToken },
      // Turnstile tokens are single use: get a fresh one for the next check.
      { onSettled: () => needsCaptcha && setCaptchaReset((n) => n + 1) },
    )
  })

  const switchMode = (next: Mode) => {
    setMode(next)
    check.reset()
    form.reset({ value: '' })
  }

  const errorKey = check.error
    ? (Object.values(fieldErrors(check.error))[0] ?? requestErrorKey(check.error, 'errors.checkFailed'))
    : null

  return (
    <section className="space-y-5">
      <div>
        <h1 className="text-2xl font-bold">{t('check.title')}</h1>
        <p className="mt-1 text-slate-600">{t('check.body')}</p>
      </div>

      <div role="tablist" className="grid grid-cols-2 rounded-lg bg-slate-100 p-1">
        {(['frame', 'fein'] as const).map((m) => (
          <button
            key={m}
            type="button"
            role="tab"
            aria-selected={mode === m}
            onClick={() => switchMode(m)}
            className={`min-h-11 rounded-md font-semibold ${mode === m ? 'bg-white shadow-sm' : 'text-slate-600'}`}
          >
            {t(m === 'frame' ? 'bikeForm.frameNumber' : 'check.feinCode')}
          </button>
        ))}
      </div>

      <form onSubmit={submit} noValidate className="space-y-4">
        <FormField
          label={t(mode === 'frame' ? 'bikeForm.frameNumber' : 'check.feinCode')}
          htmlFor="checkValue"
          hint={t(mode === 'frame' ? 'check.frameHint' : 'check.feinHint')}
          error={errors.value?.message}
        >
          <input
            id="checkValue"
            autoCapitalize="characters"
            autoComplete="off"
            spellCheck={false}
            className={`${inputClass} font-mono text-lg`}
            aria-invalid={!!errors.value}
            // A result belongs to the number it was checked for: clear it as soon as the input changes.
            {...form.register('value', { onChange: () => check.reset() })}
          />
        </FormField>

        {needsCaptcha && <Turnstile onToken={setCaptchaToken} resetKey={captchaReset} />}

        <Button type="submit" fullWidth loading={check.isPending} disabled={needsCaptcha && !captchaToken}>
          {t('check.submit')}
        </Button>
      </form>

      {errorKey && <Alert>{errorText(errorKey)}</Alert>}
      {check.data && <CheckResultView response={check.data} />}
    </section>
  )
}
