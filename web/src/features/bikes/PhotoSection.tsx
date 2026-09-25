import { useTranslation } from 'react-i18next'
import { fieldErrors, requestErrorKey } from '../../api/client'
import { type Bike, type PhotoKind, photoKinds } from '../../api/types'
import { TrashIcon } from '../../components/icons'
import { PhotoPicker } from '../../components/PhotoPicker'
import { Alert, Spinner } from '../../components/States'
import { useErrorText } from '../../i18n/useErrorText'
import { useDeletePhoto, useUploadPhoto } from './api'

const maxPhotos = 12

export function PhotoSection({ bike }: { bike: Bike }) {
  const { t } = useTranslation()

  return (
    <>
      <h2 className="mt-8 text-lg font-bold">{t('photos.title')}</h2>
      <p className="text-sm text-slate-600">{t('photos.privacyHint')}</p>
      <div className="mt-3 space-y-4">
        {photoKinds.map((kind) => (
          <PhotoKindRow key={kind} bike={bike} kind={kind} />
        ))}
      </div>
    </>
  )
}

function PhotoKindRow({ bike, kind }: { bike: Bike; kind: PhotoKind }) {
  const { t } = useTranslation()
  const errorText = useErrorText()
  const upload = useUploadPhoto(bike.id)
  const remove = useDeletePhoto(bike.id)
  const photos = bike.photos.filter((p) => p.kind === kind)
  const full = bike.photos.length >= maxPhotos
  const inputId = `photo-${kind}`
  const onFile = (file: File) => upload.mutate({ kind, file })

  const uploadError = upload.error
    ? (Object.values(fieldErrors(upload.error))[0] ?? requestErrorKey(upload.error, 'errors.uploadFailed'))
    : null

  return (
    <div className="rounded-xl border border-slate-200 p-3">
      <div className="space-y-2">
        <div>
          <p className="font-semibold">{t(`photoKind.${kind}`)}</p>
          {kind === 'receipt' && <p className="text-xs text-slate-500">{t('photos.receiptPrivate')}</p>}
        </div>
        {upload.isPending ? (
          <span role="status" className="inline-flex min-h-11 items-center gap-2 text-sm text-slate-600">
            <Spinner className="size-4" />
            {t('photos.uploading')}
          </span>
        ) : (
          <PhotoPicker idPrefix={inputId} disabled={full} onFile={onFile} />
        )}
      </div>

      {photos.length > 0 && (
        <ul className="mt-3 grid grid-cols-3 gap-2">
          {photos.map((photo) => (
            <li key={photo.id} className="relative">
              <a href={photo.url} target="_blank" rel="noreferrer">
                <img
                  src={photo.thumbnailUrl}
                  alt={t(`photoKind.${kind}`)}
                  loading="lazy"
                  className="aspect-square w-full rounded-lg bg-slate-100 object-cover"
                />
              </a>
              <button
                type="button"
                aria-label={t('photos.delete')}
                disabled={remove.isPending}
                onClick={() => {
                  if (window.confirm(t('photos.deleteConfirm'))) remove.mutate(photo.id)
                }}
                className="absolute top-1 right-1 flex size-9 items-center justify-center rounded-full bg-white/90 text-red-700 shadow"
              >
                <TrashIcon className="size-4" />
              </button>
            </li>
          ))}
        </ul>
      )}

      {uploadError && (
        <div className="mt-2">
          <Alert>{errorText(uploadError)}</Alert>
        </div>
      )}
      {remove.isError && (
        <div className="mt-2">
          <Alert>{t('errors.deleteFailed')}</Alert>
        </div>
      )}
    </div>
  )
}
