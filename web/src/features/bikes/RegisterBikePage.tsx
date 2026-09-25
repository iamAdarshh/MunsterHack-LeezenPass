import { useQueryClient } from '@tanstack/react-query'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { BackLink } from '../../components/BackLink'
import { AiPrefill, type PrefillSlot, type SlotState } from './AiPrefill'
import { bikesKey, uploadPhoto, useExtract, useRegisterBike } from './api'
import { BikeForm } from './BikeForm'
import { toPrefill } from './prefill'

export function RegisterBikePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const register = useRegisterBike()
  const extract = useExtract()
  // Draft photos + their suggestions: local UI state until the bike is saved.
  const [slots, setSlots] = useState<Partial<Record<PrefillSlot, SlotState>>>({})
  const previews = useRef<string[]>([])

  useEffect(() => () => previews.current.forEach((url) => URL.revokeObjectURL(url)), [])

  // A local model handles one image at a time; parallel requests would both run into the timeout.
  const queue = useRef<Promise<void>>(Promise.resolve())

  const onPhoto = (slot: PrefillSlot, file: File) => {
    const preview = URL.createObjectURL(file)
    previews.current.push(preview)
    setSlots((s) => ({ ...s, [slot]: { file, preview, status: 'pending' } }))
    queue.current = queue.current.then(async () => {
      try {
        const suggestion = await extract.mutateAsync(file)
        setSlots((s) => (s[slot]?.file === file ? { ...s, [slot]: { file, preview, status: 'done', suggestion } } : s))
      } catch {
        // Model offline, too slow or unsure: the form just stays manual.
        setSlots((s) => (s[slot]?.file === file ? { ...s, [slot]: { file, preview, status: 'failed' } } : s))
      }
    })
  }

  const prefill = useMemo(
    () => toPrefill(slots.side?.suggestion, slots.frame_no?.suggestion),
    [slots.side?.suggestion, slots.frame_no?.suggestion],
  )
  const hasSuggestions = Object.keys(prefill).length > 0

  return (
    <section>
      <BackLink to="/bikes" label={t('nav.myBikes')} />
      <h1 className="mt-2 text-2xl font-bold">{t('registerBike.title')}</h1>
      <p className="mt-1 text-slate-600">{t('registerBike.body')}</p>

      <div className="mt-6 space-y-6">
        <AiPrefill slots={slots} onPhoto={onPhoto} />

        {hasSuggestions && (
          <p role="status" className="rounded-lg bg-amber-50 p-3 text-sm text-amber-900">
            {t('aiPrefill.review')}
            {prefill.frameNumber && ` ${t('aiPrefill.reviewFrame')}`}
          </p>
        )}

        <BikeForm
          prefill={prefill}
          submitLabel={t('registerBike.submit')}
          onSubmit={async (input) => {
            const bike = await register.mutateAsync(input)
            // Photos taken for the prefill become the bike's side / frame number photos.
            let photoUploadFailed = false
            for (const [kind, slot] of Object.entries(slots) as [PrefillSlot, SlotState][]) {
              try {
                await uploadPhoto(bike.id, kind, slot.file)
              } catch {
                photoUploadFailed = true
              }
            }
            await queryClient.invalidateQueries({ queryKey: bikesKey })
            navigate(`/bikes/${bike.id}`, { state: { justRegistered: true, photoUploadFailed } })
          }}
        />
      </div>
    </section>
  )
}
