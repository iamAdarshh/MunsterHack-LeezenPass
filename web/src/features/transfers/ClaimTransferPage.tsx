import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { z } from 'zod'
import { fieldErrors, requestErrorKey } from '../../api/client'
import { BackLink } from '../../components/BackLink'
import { Button } from '../../components/Button'
import { FormField, inputClass } from '../../components/FormField'
import { Alert } from '../../components/States'
import { useErrorText } from '../../i18n/useErrorText'
import { useClaimTransfer } from './api'

/** Same alphabet as TransferCode on the server (no 0/O, 1/I/L). */
const alphabet = /^[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{8}$/
const clean = (value: string) => value.toUpperCase().replace(/[\s-]/g, '')

const schema = z.object({
  code: z.string().refine((v) => alphabet.test(clean(v)), 'errors.transferCodeInvalid'),
})

export function ClaimTransferPage() {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const navigate = useNavigate()
  const claim = useClaimTransfer()
  const [formError, setFormError] = useState<string | null>(null)
  const form = useForm({ resolver: zodResolver(schema), defaultValues: { code: '' } })
  const { errors, isSubmitting } = form.formState

  const submit = form.handleSubmit(async ({ code }) => {
    setFormError(null)
    try {
      const result = await claim.mutateAsync(clean(code))
      navigate(`/bikes/${result.bikeId}`, { replace: true, state: { claimed: true } })
    } catch (error) {
      setFormError(Object.values(fieldErrors(error))[0] ?? requestErrorKey(error, 'errors.saveFailed'))
    }
  })

  return (
    <section>
      <BackLink to="/bikes" label={t('nav.myBikes')} />
      <h1 className="mt-2 text-2xl font-bold">{t('claim.title')}</h1>
      <p className="mt-1 text-slate-600">{t('claim.body')}</p>

      <form onSubmit={submit} noValidate className="mt-6 space-y-4">
        <FormField label={t('claim.code')} htmlFor="code" hint={t('claim.codeHint')} error={errors.code?.message}>
          <input
            id="code"
            autoCapitalize="characters"
            autoComplete="one-time-code"
            spellCheck={false}
            placeholder="ABCD-EFGH"
            className={`${inputClass} text-center font-mono text-2xl tracking-widest`}
            aria-invalid={!!errors.code}
            {...form.register('code')}
          />
        </FormField>
        {formError && <Alert>{errorText(formError)}</Alert>}
        <Button type="submit" fullWidth loading={isSubmitting}>
          {t('claim.submit')}
        </Button>
      </form>
    </section>
  )
}
