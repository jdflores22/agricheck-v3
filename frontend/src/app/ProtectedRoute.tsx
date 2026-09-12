import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAppSelector } from './hooks'
import { selectCurrentUser, selectIsAuthenticated } from '../features/auth/authSlice'
import type { RootState } from './store'

interface ProtectedRouteProps {
  redirectTo?: string
}

export function ProtectedRoute({ redirectTo = '/login' }: ProtectedRouteProps) {
  const isAuthenticated = useAppSelector(selectIsAuthenticated)
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to={redirectTo} replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}

interface RoleProtectedRouteProps {
  roles: string[]
  redirectTo?: string
}

export function RoleProtectedRoute({ roles, redirectTo = '/profile' }: RoleProtectedRouteProps) {
  const user = useAppSelector(selectCurrentUser)

  if (!user) {
    return null
  }

  const hasRole = user.roles.some((role) => roles.includes(role) || role === 'ROLE_ADMIN')

  if (!hasRole) {
    return <Navigate to={redirectTo} replace />
  }

  return <Outlet />
}

interface GuestRouteProps {
  redirectTo?: string
}

export function GuestRoute({ redirectTo = '/client' }: GuestRouteProps) {
  const isAuthenticated = useAppSelector(selectIsAuthenticated)
  const redirectPath = useAppSelector((state: RootState) => state.auth.redirectPath)

  if (isAuthenticated) {
    return <Navigate to={redirectPath ?? redirectTo} replace />
  }

  return <Outlet />
}
