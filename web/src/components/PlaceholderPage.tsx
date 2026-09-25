import { useTranslation } from 'react-i18next'
import type { TranslationKey } from '../i18n'

interface Props {
  titleKey: TranslationKey
  bodyKey: TranslationKey
}

export function PlaceholderPage({ titleKey, bodyKey }: Props) {
  const { t } = useTranslation()

  return (
    <section>
      <h1 className="text-2xl font-bold">{t(titleKey)}</h1>
      <p className="mt-2 text-slate-600">{t(bodyKey)}</p>
      <p className="mt-6 rounded-lg border border-dashed border-slate-300 p-4 text-sm text-slate-500">
        {t('common.comingSoon')}
      </p>
    </section>
  )
}
