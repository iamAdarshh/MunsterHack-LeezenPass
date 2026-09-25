import { createBrowserRouter } from 'react-router'
import { LoginPage } from '../features/auth/LoginPage'
import { RequireAuth } from '../features/auth/RequireAuth'
import { BikeDetailPage } from '../features/bikes/BikeDetailPage'
import { EditBikePage } from '../features/bikes/EditBikePage'
import { MyBikesPage } from '../features/bikes/MyBikesPage'
import { RegisterBikePage } from '../features/bikes/RegisterBikePage'
import { CheckPage } from '../features/check/CheckPage'
import { StolenListPage } from '../features/theft/StolenListPage'
import { Layout } from './Layout'
import { NotFoundPage } from './NotFoundPage'

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { index: true, element: <CheckPage /> },
      { path: 'stolen', element: <StolenListPage /> },
      { path: 'login', element: <LoginPage /> },
      {
        element: <RequireAuth />,
        children: [
          { path: 'bikes', element: <MyBikesPage /> },
          { path: 'bikes/new', element: <RegisterBikePage /> },
          { path: 'bikes/:id', element: <BikeDetailPage /> },
          { path: 'bikes/:id/edit', element: <EditBikePage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
