import { useTranslation } from 'react-i18next'
import type { TranslationKey } from '.'

/** Translates an error key from zod/the API ("errors.frameNumberInvalid"); unknown keys become a generic message. */
export function useErrorText() {
  const { t, i18n } = useTranslation()
  return (key: string | undefined) =>
    key && i18n.exists(key) ? t(key as TranslationKey) : t('errors.generic')
}
