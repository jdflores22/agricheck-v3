import { PropsWithChildren, useLayoutEffect } from 'react'
import { Provider } from 'react-redux'
import { RouterProvider } from 'react-router-dom'
import { Box, CircularProgress, CssBaseline, ThemeProvider } from '@mui/material'
import { store } from '../store'
import { router } from '../router'
import { agricheckTheme } from '../../theme/theme'
import { useAppDispatch, useAppSelector } from '../hooks'
import { logout, selectCurrentUser, setUser } from '../../features/auth/authSlice'
import { useMeQuery } from '../../features/auth/api/authApi'
import { hasStoredAuthTokens, hasUsableSession, isTokenExpired } from '../../features/auth/authTokenUtils'
import { useNotificationRealtime } from '../../features/notifications/useNotificationRealtime'
import { SystemBrandingProvider, useSystemBranding } from '../../features/system/SystemBrandingProvider'
import { resolveBrandingAssetUrl, useGetSystemBrandingQuery } from '../../features/system/systemBrandingApi'

function BrandingDocumentSync() {
  const { systemName, spinnerColor } = useSystemBranding()
  const { data } = useGetSystemBrandingQuery()
  const faviconUrl = resolveBrandingAssetUrl(data?.data?.faviconUrl) ?? '/favicon.svg'

  useLayoutEffect(() => {
    document.title = systemName
  }, [systemName])

  useLayoutEffect(() => {
    let link = document.querySelector<HTMLLinkElement>("link[rel='icon']")
    if (!link) {
      link = document.createElement('link')
      link.rel = 'icon'
      document.head.appendChild(link)
    }
    link.type = faviconUrl.endsWith('.svg') ? 'image/svg+xml' : 'image/x-icon'
    link.href = faviconUrl
  }, [faviconUrl])

  useLayoutEffect(() => {
    const themeMeta = document.querySelector<HTMLMetaElement>("meta[name='theme-color']")
    if (themeMeta) {
      themeMeta.content = spinnerColor
    }
  }, [spinnerColor])

  return null
}

function AuthenticatedRealtimeBridge() {
  useNotificationRealtime()
  return null
}

function AuthBootstrap({ children }: PropsWithChildren) {
  const dispatch = useAppDispatch()
  const auth = useAppSelector((state) => state.auth)
  const user = useAppSelector(selectCurrentUser)
  const sessionReady = hasUsableSession(auth)
  const { data, isLoading, isFetching, isError } = useMeQuery(undefined, { skip: !sessionReady })
  const { spinnerColor } = useSystemBranding()

  useLayoutEffect(() => {
    if (auth.accessToken && !auth.refreshToken) {
      dispatch(logout())
      return
    }
    if (auth.refreshToken && isTokenExpired(auth.refreshTokenExpiresAt)) {
      dispatch(logout())
    }
  }, [auth.accessToken, auth.refreshToken, auth.refreshTokenExpiresAt, dispatch])

  useLayoutEffect(() => {
    if (data?.success && data.data?.user) {
      dispatch(setUser(data.data.user))
    }
  }, [data, dispatch])

  useLayoutEffect(() => {
    if (sessionReady && isError) {
      dispatch(logout())
    }
  }, [dispatch, isError, sessionReady])

  if (hasStoredAuthTokens(auth) && !user) {
    if (isLoading || isFetching || (data?.success && data.data?.user)) {
      return (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100dvh' }}>
          <CircularProgress size={32} sx={{ color: spinnerColor }} />
        </Box>
      )
    }
    if (isError) {
      return (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100dvh' }}>
          <CircularProgress size={32} sx={{ color: spinnerColor }} />
        </Box>
      )
    }
  }

  return (
    <>
      {user ? <AuthenticatedRealtimeBridge /> : null}
      {children ?? <RouterProvider router={router} />}
    </>
  )
}

export function AppProviders({ children }: PropsWithChildren) {
  return (
    <Provider store={store}>
      <ThemeProvider theme={agricheckTheme}>
        <CssBaseline />
        <SystemBrandingProvider>
          <BrandingDocumentSync />
          <AuthBootstrap>{children ?? <RouterProvider router={router} />}</AuthBootstrap>
        </SystemBrandingProvider>
      </ThemeProvider>
    </Provider>
  )
}
