import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router'
import { BackLink } from '../../components/BackLink'
import { useRegisterBike } from './api'
import { BikeForm } from './BikeForm'

export function RegisterBikePage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const register = useRegisterBike()

  return (
    <section>
      <BackLink to="/bikes" label={t('nav.myBikes')} />
      <h1 className="mt-2 text-2xl font-bold">{t('registerBike.title')}</h1>
      <p className="mt-1 text-slate-600">{t('registerBike.body')}</p>
      <div className="mt-6">
        <BikeForm
          submitLabel={t('registerBike.submit')}
          onSubmit={async (input) => {
            const bike = await register.mutateAsync(input)
            // Photos come next, on the detail page.
            navigate(`/bikes/${bike.id}`, { state: { justRegistered: true } })
          }}
        />
      </div>
    </section>
  )
}
