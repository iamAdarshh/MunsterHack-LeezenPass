import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from './Button'

interface Props {
  url: string
  title: string
  text: string
  variant?: 'primary' | 'secondary'
}

/** Native share sheet on phones (WhatsApp, Signal, ...); copies the link elsewhere. */
export function ShareButton({ url, title, text, variant = 'secondary' }: Props) {
  const { t } = useTranslation()
  const [copied, setCopied] = useState(false)

  const share = async () => {
    if (typeof navigator.share === 'function') {
      try {
        await navigator.share({ title, text, url })
        return
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') return
      }
    }
    try {
      await navigator.clipboard.writeText(`${text}\n${url}`)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 2500)
    } catch {
      window.prompt(t('share.copyManually'), url)
    }
  }

  return (
    <Button variant={variant} fullWidth onClick={() => void share()}>
      {copied ? t('share.copied') : t('share.button')}
    </Button>
  )
}
