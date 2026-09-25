import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { z } from 'zod'
import { ApiError, fieldErrors } from '../../api/client'
import { Button } from '../../components/Button'
import { buttonClass } from '../../components/buttonClass'
import { FormField, inputClass } from '../../components/FormField'
import { Alert, LoadingState } from '../../components/States'
import { useErrorText } from '../../i18n/useErrorText'
import { useLogin, useLogout, useMe, useRegisterAccount } from './api'

type Mode = 'login' | 'register'

const loginSchema = z.object({
  // Trim first: pasted or autocompleted addresses often carry a trailing space.
  email: z.string().trim().pipe(z.email('errors.emailInvalid')),
  password: z.string().min(1, 'errors.required'),
})

const registerSchema = z.object({
  // Trim first: pasted or autocompleted addresses often carry a trailing space.
  email: z.string().trim().pipe(z.email('errors.emailInvalid')),
  // Mirrors the Identity options in AuthConfigs.cs.
  password: z
    .string()
    .min(8, 'auth.errors.PasswordTooShort')
    .regex(/[a-z]/, 'auth.errors.PasswordRequiresLower')
    .regex(/[A-Z]/, 'auth.errors.PasswordRequiresUpper')
    .regex(/[0-9]/, 'auth.errors.PasswordRequiresDigit'),
})

type FormValues = z.infer<typeof loginSchema>

/** Only allow in-app paths (no "//evil.example"). */
function safeReturnTo(value: string | null) {
  return value && value.startsWith('/') && !value.startsWith('//') ? value : '/bikes'
}

export function LoginPage() {
  const { t } = useTranslation()
  const me = useMe()

  if (me.isPending) return <LoadingState />
  return (
    <section>
      <h1 className="text-2xl font-bold">{me.data ? t('account.title') : t('login.title')}</h1>
      {me.data ? <AccountPanel email={me.data.email} /> : <AuthForm />}
    </section>
  )
}

function AccountPanel({ email }: { email: string }) {
  const { t } = useTranslation()
  const logout = useLogout()

  return (
    <div className="mt-4 space-y-4">
      <p className="text-slate-600">{t('account.signedInAs', { email })}</p>
      <Link to="/bikes" className={buttonClass('primary', true)}>
        {t('nav.myBikes')}
      </Link>
      <Button variant="secondary" fullWidth loading={logout.isPending} onClick={() => logout.mutate()}>
        {t('account.logout')}
      </Button>
      {logout.isError && <Alert>{t('errors.generic')}</Alert>}
    </div>
  )
}

function AuthForm() {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [mode, setMode] = useState<Mode>('login')
  const [formError, setFormError] = useState<string | null>(null)
  const login = useLogin()
  const register = useRegisterAccount()

  const form = useForm<FormValues>({
    resolver: zodResolver(mode === 'login' ? loginSchema : registerSchema),
    defaultValues: { email: '', password: '' },
  })
  const { errors, isSubmitting } = form.formState

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      await (mode === 'login' ? login : register).mutateAsync(values)
      navigate(safeReturnTo(searchParams.get('returnTo')), { replace: true })
    } catch (error) {
      if (mode === 'login' && error instanceof ApiError && error.status === 401) {
        setFormError('auth.errors.invalidCredentials')
        return
      }
      // Identity returns error codes such as DuplicateUserName or PasswordRequiresDigit.
      const codes = Object.keys(fieldErrors(error))
      setFormError(codes.length > 0 ? `auth.errors.${codes[0]}` : 'errors.generic')
    }
  })

  const switchMode = (next: Mode) => {
    setMode(next)
    setFormError(null)
    form.clearErrors()
  }

  return (
    <div className="mt-4">
      <div role="tablist" className="grid grid-cols-2 rounded-lg bg-slate-100 p-1">
        {(['login', 'register'] as const).map((m) => (
          <button
            key={m}
            type="button"
            role="tab"
            aria-selected={mode === m}
            onClick={() => switchMode(m)}
            className={`min-h-11 rounded-md font-semibold ${mode === m ? 'bg-white shadow-sm' : 'text-slate-600'}`}
          >
            {t(m === 'login' ? 'login.tabLogin' : 'login.tabRegister')}
          </button>
        ))}
      </div>

      <form onSubmit={onSubmit} noValidate className="mt-6 space-y-4">
        <FormField label={t('login.email')} htmlFor="email" error={errors.email?.message}>
          <input
            id="email"
            type="email"
            autoComplete="email"
            inputMode="email"
            className={inputClass}
            aria-invalid={!!errors.email}
            {...form.register('email')}
          />
        </FormField>
        <FormField
          label={t('login.password')}
          htmlFor="password"
          hint={mode === 'register' ? t('login.passwordHint') : undefined}
          error={errors.password?.message}
        >
          <input
            id="password"
            type="password"
            autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            className={inputClass}
            aria-invalid={!!errors.password}
            {...form.register('password')}
          />
        </FormField>

        {formError && <Alert>{errorText(formError)}</Alert>}

        <Button type="submit" fullWidth loading={isSubmitting}>
          {t(mode === 'login' ? 'login.submitLogin' : 'login.submitRegister')}
        </Button>
      </form>
    </div>
  )
}
