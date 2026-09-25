import { useTranslation } from 'react-i18next'
import { NavLink, Outlet } from 'react-router'
import { DemoBanner } from '../components/DemoBanner'
import { AlertIcon, BikeIcon, SearchIcon, UserIcon } from '../components/icons'
import { LanguageSwitch } from '../components/LanguageSwitch'
import { useMe } from '../features/auth/api'

const navItems = [
  { to: '/', labelKey: 'nav.check', Icon: SearchIcon },
  { to: '/bikes', labelKey: 'nav.myBikes', Icon: BikeIcon },
  { to: '/stolen', labelKey: 'nav.stolen', Icon: AlertIcon },
  { to: '/login', labelKey: 'nav.login', Icon: UserIcon },
] as const

export function Layout() {
  const { t } = useTranslation()
  const me = useMe()

  return (
    <div className="mx-auto flex min-h-dvh max-w-screen-sm flex-col bg-white shadow-sm">
      <header className="sticky top-0 z-10 bg-brand-700 text-white">
        <div className="flex h-14 items-center justify-between px-4">
          <NavLink to="/" className="text-lg font-bold tracking-tight">
            {t('app.name')}
          </NavLink>
          <LanguageSwitch />
        </div>
        <DemoBanner />
      </header>

      <main className="flex-1 px-4 pt-6 pb-24">
        <Outlet />
      </main>

      <nav
        aria-label={t('nav.label')}
        className="fixed inset-x-0 bottom-0 z-10 mx-auto max-w-screen-sm border-t border-slate-200 bg-white pb-[env(safe-area-inset-bottom)]"
      >
        <ul className="grid grid-cols-4">
          {navItems.map(({ to, labelKey, Icon }) => (
            <li key={to}>
              <NavLink
                to={to}
                end={to === '/'}
                className={({ isActive }) =>
                  `flex min-h-16 flex-col items-center justify-center gap-1 text-xs font-medium ${
                    isActive ? 'text-brand-700' : 'text-slate-500 hover:text-slate-800'
                  }`
                }
              >
                <Icon className="size-6" />
                {t(to === '/login' && me.data ? 'nav.account' : labelKey)}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </div>
  )
}
