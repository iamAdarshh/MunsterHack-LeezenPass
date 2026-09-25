import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { CameraIcon, ImageIcon } from './icons'

interface Props {
  idPrefix: string
  disabled?: boolean
  onFile: (file: File) => void
}

/** Camera opens the rear camera directly; gallery allows existing photos (e.g. a receipt). */
export function PhotoPicker({ idPrefix, disabled = false, onFile }: Props) {
  const { t } = useTranslation()
  return (
    <div className="grid grid-cols-2 gap-2">
      <PickerButton id={`${idPrefix}-camera`} capture disabled={disabled} label={t('photos.camera')} onFile={onFile}>
        <CameraIcon className="size-5" />
      </PickerButton>
      <PickerButton id={`${idPrefix}-gallery`} disabled={disabled} label={t('photos.gallery')} onFile={onFile}>
        <ImageIcon className="size-5" />
      </PickerButton>
    </div>
  )
}

interface PickerButtonProps {
  id: string
  label: string
  capture?: boolean
  disabled: boolean
  onFile: (file: File) => void
  children: ReactNode
}

function PickerButton({ id, label, capture, disabled, onFile, children }: PickerButtonProps) {
  return (
    <>
      <label
        htmlFor={id}
        aria-disabled={disabled}
        className="flex min-h-11 cursor-pointer items-center justify-center gap-1.5 rounded-lg border border-slate-300 px-3 text-sm font-semibold hover:bg-slate-50 aria-disabled:pointer-events-none aria-disabled:opacity-50"
      >
        {children}
        {label}
      </label>
      <input
        id={id}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        capture={capture ? 'environment' : undefined}
        className="sr-only"
        disabled={disabled}
        onChange={(e) => {
          const file = e.target.files?.[0]
          e.target.value = ''
          if (file) onFile(file)
        }}
      />
    </>
  )
}
