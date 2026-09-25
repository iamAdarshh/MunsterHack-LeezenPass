import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from './Button'

export function CopyButton({ text, label }: { text: string; label: string }) {
  const { t } = useTranslation()
  const [copied, setCopied] = useState(false)

  return (
    <Button
      variant="secondary"
      fullWidth
      onClick={() => {
        navigator.clipboard.writeText(text).then(
          () => {
            setCopied(true)
            window.setTimeout(() => setCopied(false), 2500)
          },
          () => window.prompt(t('share.copyManually'), text),
        )
      }}
    >
      {copied ? t('share.copied') : label}
    </Button>
  )
}
