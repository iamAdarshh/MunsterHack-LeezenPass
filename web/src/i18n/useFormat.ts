import { useTranslation } from 'react-i18next'

/** Dates in the UI language (not the browser's locale). */
export function useFormat() {
  const { i18n } = useTranslation()
  const locale = i18n.resolvedLanguage === 'en' ? 'en-GB' : 'de-DE'

  return {
    /** Date-only value ("2026-09-25"), read as a local date so it never shifts by a day. */
    date: (value: string) => {
      const [year, month, day] = value.slice(0, 10).split('-').map(Number)
      return new Date(year, month - 1, day).toLocaleDateString(locale, { dateStyle: 'medium' })
    },
    /** Instant (ISO with offset), shown in local time. */
    dateTime: (value: string) => new Date(value).toLocaleString(locale, { dateStyle: 'medium', timeStyle: 'short' }),
  }
}
