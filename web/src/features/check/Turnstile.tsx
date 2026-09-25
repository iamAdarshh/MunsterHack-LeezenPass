import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { loadTurnstile, turnstileSiteKey } from './captcha'

interface Props {
  onToken: (token: string | undefined) => void
  /** Change to get a fresh token (tokens are single use). */
  resetKey: number
}

export function Turnstile({ onToken, resetKey }: Props) {
  const { t, i18n } = useTranslation()
  const container = useRef<HTMLDivElement>(null)
  const widgetId = useRef<string | null>(null)
  const [failed, setFailed] = useState(false)
  // Latest callback for the widget's callbacks, without re-rendering the widget.
  const onTokenRef = useRef(onToken)
  useEffect(() => {
    onTokenRef.current = onToken
  })

  useEffect(() => {
    let cancelled = false
    loadTurnstile().then(
      (turnstile) => {
        if (cancelled || !container.current) return
        widgetId.current = turnstile.render(container.current, {
          sitekey: turnstileSiteKey,
          language: i18n.resolvedLanguage,
          callback: (token) => onTokenRef.current(token),
          'expired-callback': () => onTokenRef.current(undefined),
          'error-callback': () => onTokenRef.current(undefined),
        })
      },
      () => setFailed(true),
    )
    return () => {
      cancelled = true
      if (widgetId.current) window.turnstile?.remove(widgetId.current)
      widgetId.current = null
    }
  }, [i18n.resolvedLanguage])

  useEffect(() => {
    if (resetKey > 0 && widgetId.current) {
      window.turnstile?.reset(widgetId.current)
      onTokenRef.current(undefined)
    }
  }, [resetKey])

  return (
    <div>
      <div ref={container} className="min-h-16" />
      {failed && <p className="text-sm text-red-700">{t('check.captchaUnavailable')}</p>}
    </div>
  )
}
