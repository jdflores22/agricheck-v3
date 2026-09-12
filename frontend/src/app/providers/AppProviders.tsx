import { PropsWithChildren, useLayoutEffect } from 'react'
import { Provider } from 'react-redux'
import { RouterProvider } from 'react-router-dom'
import { Box, CircularProgress, CssBaseline, ThemeProvider } from '@mui/material'
import { store } from '../store'
import { router } from '../router'
import { agricheckTheme } from '../../theme/theme'
import { useAppDispatch, useAppSelector } from '../hooks'
import { selectCurrentUser, selectIsAuthenticated, setUser } from '../../features/auth/authSlice'
import { useMeQuery } from '../../features/auth/api/authApi'
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

function AuthBootstrap({ children }: PropsWithChildren) {
  const dispatch = useAppDispatch()
  const isAuthenticated = useAppSelector(selectIsAuthenticated)
  const user = useAppSelector(selectCurrentUser)
  const { data, isLoading, isFetching, isError } = useMeQuery(undefined, { skip: !isAuthenticated })
  const { spinnerColor } = useSystemBranding()

  useLayoutEffect(() => {
    if (data?.success && data.data?.user) {
      dispatch(setUser(data.data.user))
    }
  }, [data, dispatch])

  if (isAuthenticated && !user) {
    if (isLoading || isFetching || (data?.success && data.data?.user)) {
      return (
        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100dvh' }}>
          <CircularProgress size={32} sx={{ color: spinnerColor }} />
        </Box>
      )
    }
    if (isError) {
      return children ?? <RouterProvider router={router} />
    }
  }

  return children ?? <RouterProvider router={router} />
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
