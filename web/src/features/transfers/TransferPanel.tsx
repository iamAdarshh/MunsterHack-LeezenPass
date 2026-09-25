import { useTranslation } from 'react-i18next'
import { fieldErrors, requestErrorKey } from '../../api/client'
import type { Bike } from '../../api/types'
import { Button } from '../../components/Button'
import { buttonClass } from '../../components/buttonClass'
import { CopyButton } from '../../components/CopyButton'
import { Alert, ErrorState, LoadingState } from '../../components/States'
import { useErrorText } from '../../i18n/useErrorText'
import { useFormat } from '../../i18n/useFormat'
import { useCancelTransfer, useCreateTransfer, useTransferStatus } from './api'
import { listingSnippet } from './listingSnippet'

/** Owner: sell/hand over the bike with a one-time code, marketplace snippet, certificate. */
export function TransferPanel({ bike }: { bike: Bike }) {
  const { t } = useTranslation()
  const format = useFormat()
  const errorText = useErrorText()
  const status = useTransferStatus(bike.id)
  const create = useCreateTransfer(bike.id)
  const cancel = useCancelTransfer(bike.id)

  if (status.isPending) return <LoadingState />
  if (status.isError) return <ErrorState onRetry={() => void status.refetch()} />

  const { openTransfer, certificateTransferId, previousOwners } = status.data
  // The plain code only exists in the create response; after a reload a new one must be made.
  const code = create.data && openTransfer?.id === create.data.transferId ? create.data : null
  const error = create.error ?? cancel.error

  return (
    <section className="mt-8 space-y-3">
      <h2 className="text-lg font-bold">{t('transfer.title')}</h2>

      {certificateTransferId && (
        <a href={`/api/transfers/${certificateTransferId}/certificate.pdf`} download className={buttonClass('secondary', true)}>
          {t('transfer.certificate')}
        </a>
      )}
      {previousOwners > 0 && <p className="text-sm text-slate-600">{t('transfer.previousOwners', { count: previousOwners })}</p>}

      {bike.status === 'stolen' ? (
        <p className="text-sm text-slate-600">{t('errors.transferStolen')}</p>
      ) : (
        <div className="space-y-3 rounded-xl border border-slate-200 p-4">
          {code ? (
            <div className="space-y-2 text-center">
              <p className="text-sm text-slate-600">{t('transfer.codeLabel')}</p>
              <p className="font-mono text-4xl font-bold tracking-widest" aria-live="polite">{code.code}</p>
              <p className="text-sm text-slate-600">{t('transfer.validUntil', { date: format.dateTime(code.expiresAt) })}</p>
              <CopyButton text={code.code} label={t('transfer.copyCode')} />
            </div>
          ) : openTransfer ? (
            <p className="text-sm">{t('transfer.openHidden', { date: format.dateTime(openTransfer.expiresAt) })}</p>
          ) : (
            <p className="text-sm text-slate-600">{t('transfer.intro')}</p>
          )}

          {(code || openTransfer) && (
            <p className="rounded-lg bg-amber-50 p-3 text-sm text-amber-900">{t('transfer.warning')}</p>
          )}

          <Button
            variant={openTransfer ? 'secondary' : 'primary'}
            fullWidth
            loading={create.isPending}
            onClick={() => {
              if (!openTransfer || window.confirm(t('transfer.replaceConfirm'))) create.mutate()
            }}
          >
            {openTransfer ? t('transfer.newCode') : t('transfer.createCode')}
          </Button>

          {openTransfer && (
            <>
              <div className="space-y-2">
                <p className="text-sm font-semibold">{t('transfer.snippetTitle')}</p>
                <pre className="rounded-lg bg-slate-100 p-3 text-xs whitespace-pre-wrap [overflow-wrap:anywhere]">
                  {listingSnippet(bike, t, window.location.origin)}
                </pre>
                <CopyButton text={listingSnippet(bike, t, window.location.origin)} label={t('transfer.copySnippet')} />
              </div>
              <Button variant="danger" fullWidth loading={cancel.isPending} onClick={() => cancel.mutate()}>
                {t('transfer.cancel')}
              </Button>
            </>
          )}

          {error && (
            <Alert>{errorText(Object.values(fieldErrors(error))[0] ?? requestErrorKey(error, 'errors.saveFailed'))}</Alert>
          )}
        </div>
      )}
    </section>
  )
}
