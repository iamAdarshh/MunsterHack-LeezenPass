import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import de from './de.json'
import en from './en.json'

export const languages = ['de', 'en'] as const
export type Language = (typeof languages)[number]

export const resources = {
  de: { translation: de },
  en: { translation: en },
} as const

type Leaves<T, Prefix extends string = ''> = {
  [K in keyof T & string]: T[K] extends string ? `${Prefix}${K}` : Leaves<T[K], `${Prefix}${K}.`>
}[keyof T & string]

/** Every valid translation key, e.g. "nav.check". German is the source of truth. */
export type TranslationKey = Leaves<typeof de>

const storageKey = 'leezenpass.lang'

function storedLanguage(): Language {
  try {
    const value = localStorage.getItem(storageKey)
    return value === 'en' ? 'en' : 'de'
  } catch {
    return 'de'
  }
}

export function setLanguage(language: Language) {
  void i18n.changeLanguage(language)
  try {
    localStorage.setItem(storageKey, language)
  } catch {
    // Private mode etc.: language just isn't remembered.
  }
}

i18n.on('languageChanged', (language) => {
  document.documentElement.lang = language
})

void i18n.use(initReactI18next).init({
  resources,
  lng: storedLanguage(),
  fallbackLng: 'de',
  supportedLngs: languages,
  interpolation: { escapeValue: false },
})

export default i18n
