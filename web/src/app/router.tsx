import { createBrowserRouter } from 'react-router'
import { LoginPage } from '../features/auth/LoginPage'
import { MyBikesPage } from '../features/bikes/MyBikesPage'
import { CheckPage } from '../features/check/CheckPage'
import { StolenListPage } from '../features/theft/StolenListPage'
import { Layout } from './Layout'
import { NotFoundPage } from './NotFoundPage'

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { index: true, element: <CheckPage /> },
      { path: 'bikes', element: <MyBikesPage /> },
      { path: 'stolen', element: <StolenListPage /> },
      { path: 'login', element: <LoginPage /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
