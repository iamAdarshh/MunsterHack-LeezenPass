import { useTranslation } from 'react-i18next'
import { type Language, setLanguage } from '../i18n'

export function LanguageSwitch() {
  const { t, i18n } = useTranslation()
  const next: Language = i18n.resolvedLanguage === 'de' ? 'en' : 'de'

  return (
    <button
      type="button"
      onClick={() => setLanguage(next)}
      aria-label={t('language.switch')}
      className="min-h-11 min-w-11 rounded-md px-2 text-sm font-semibold uppercase hover:bg-brand-800"
    >
      {next}
    </button>
  )
}
