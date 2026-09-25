import { useTranslation } from 'react-i18next'
import type { VisionSuggestion } from '../../api/types'
import { AlertIcon, CheckIcon } from '../../components/icons'
import { PhotoPicker } from '../../components/PhotoPicker'
import { Spinner } from '../../components/States'
import { confidenceLevel, minConfidenceForAttributes } from './prefill'

export type PrefillSlot = 'side' | 'frame_no'

export interface SlotState {
  file: File
  preview: string
  status: 'pending' | 'done' | 'failed'
  suggestion?: VisionSuggestion
}

interface Props {
  slots: Partial<Record<PrefillSlot, SlotState>>
  onPhoto: (slot: PrefillSlot, file: File) => void
}

/** Optional first step: photos -> suggestions for the form. Photos are only stored once the bike is saved. */
export function AiPrefill({ slots, onPhoto }: Props) {
  const { t } = useTranslation()
  const slotKeys: PrefillSlot[] = ['side', 'frame_no']

  return (
    <section className="space-y-3 rounded-xl border border-brand-600/30 bg-brand-50 p-4">
      <div>
        <h2 className="font-bold">{t('aiPrefill.title')}</h2>
        <p className="text-sm text-slate-600">{t('aiPrefill.body')}</p>
      </div>
      {slotKeys.map((slot) => (
        <div key={slot} className="space-y-2 rounded-lg bg-white p-3">
          <div className="flex items-center gap-3">
            {slots[slot] ? (
              <img src={slots[slot].preview} alt="" className="size-14 shrink-0 rounded-md object-cover" />
            ) : (
              <div className="size-14 shrink-0 rounded-md border border-dashed border-slate-300" />
            )}
            <div className="min-w-0">
              <p className="font-semibold">{t(`aiPrefill.slot.${slot}`)}</p>
              <SlotStatus slot={slot} state={slots[slot]} />
            </div>
          </div>
          <PhotoPicker
            idPrefix={`prefill-${slot}`}
            disabled={slots[slot]?.status === 'pending'}
            onFile={(file) => onPhoto(slot, file)}
          />
        </div>
      ))}
    </section>
  )
}

function SlotStatus({ slot, state }: { slot: PrefillSlot; state?: SlotState }) {
  const { t } = useTranslation()

  if (!state) return <p className="text-xs text-slate-500">{t(`aiPrefill.hint.${slot}`)}</p>

  if (state.status === 'pending') {
    return (
      <p role="status" className="flex items-center gap-2 text-sm text-slate-600">
        <Spinner className="size-4" />
        {t('aiPrefill.analysing')}
      </p>
    )
  }

  if (state.status === 'failed' || !state.suggestion) {
    return (
      <p role="alert" className="flex items-start gap-1 text-sm text-amber-800">
        <AlertIcon className="mt-0.5 size-4 shrink-0" />
        {t('aiPrefill.failed')}
      </p>
    )
  }

  const s = state.suggestion
  if (slot === 'frame_no') {
    return s.frameNumberCandidate ? (
      <p className="flex items-center gap-1 text-sm text-emerald-800">
        <CheckIcon className="size-4" />
        {t('aiPrefill.frameFound')} <span className="font-mono font-semibold">{s.frameNumberCandidate}</span>
      </p>
    ) : (
      <p className="text-sm text-amber-800">{t('aiPrefill.frameNotFound')}</p>
    )
  }

  const level = confidenceLevel(s.confidence)
  return (
    <p className={`text-sm ${level === 'low' ? 'text-amber-800' : 'text-emerald-800'}`}>
      {s.confidence >= minConfidenceForAttributes ? t('aiPrefill.applied') : t('aiPrefill.notApplied')}
      {' · '}
      {t('aiPrefill.confidence', { level: t(`aiPrefill.level.${level}`) })}
    </p>
  )
}
