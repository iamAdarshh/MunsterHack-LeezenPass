import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router'
import { BackLink } from '../../components/BackLink'
import { ErrorState, LoadingState } from '../../components/States'
import { useBike, useUpdateBike } from './api'
import { BikeForm } from './BikeForm'

export function EditBikePage() {
  const { t } = useTranslation()
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const bike = useBike(id)
  const update = useUpdateBike(id)

  return (
    <section>
      <BackLink to={`/bikes/${id}`} label={t('common.back')} />
      <h1 className="mt-2 text-2xl font-bold">{t('editBike.title')}</h1>
      <div className="mt-6">
        {bike.isPending && <LoadingState />}
        {bike.isError && <ErrorState onRetry={() => void bike.refetch()} />}
        {bike.data && (
          <BikeForm
            bike={bike.data}
            submitLabel={t('editBike.submit')}
            onSubmit={async (input) => {
              await update.mutateAsync(input)
              navigate(`/bikes/${id}`)
            }}
          />
        )}
      </div>
    </section>
  )
}
