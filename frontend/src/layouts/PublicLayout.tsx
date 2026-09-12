import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { Box, Button, Link, Typography } from '@mui/material'
import { Link as RouterLink, Outlet, useLocation } from 'react-router-dom'
import { useAppSelector } from '../app/hooks'
import { selectAuthRedirect, selectCurrentUser, selectIsAuthenticated } from '../features/auth/authSlice'
import { PortalUserMenu } from '../components/portal/PortalUserMenu'
import { resolveDashboardPath } from '../components/portal/portalUtils'
import { BreadcrumbProvider } from '../components/portal/BreadcrumbContext'
import { PortalBreadcrumbs } from '../components/portal/PortalBreadcrumbs'
import { LandingFooter } from '../features/home/components/LandingFooter'
import { useSystemBranding } from '../features/system/SystemBrandingProvider'
import { landingBtnPrimarySx, landingColors, landingContainerSx } from '../features/home/landingTheme'

export function PublicLayout() {
  const location = useLocation()
  const isHome = location.pathname === '/'
  const isVerify = location.pathname === '/verify'
  const isFullWidthPublicPage = isHome || isVerify
  const isAuthenticated = useAppSelector(selectIsAuthenticated)
  const user = useAppSelector(selectCurrentUser)
  const redirectPath = useAppSelector(selectAuthRedirect)
  const dashboardPath = resolveDashboardPath(redirectPath, user?.roles ?? [])
  const { systemName, systemLogoUrl } = useSystemBranding()

  return (
    <Box sx={{ minHeight: '100dvh', bgcolor: landingColors.bgPage, color: landingColors.textDark }}>
      <Box
        component="header"
        sx={{
          position: 'sticky',
          top: 0,
          zIndex: 50,
          borderBottom: `1px solid ${landingColors.border}`,
          bgcolor: 'rgba(255, 255, 255, 0.92)',
          backdropFilter: 'blur(10px)',
        }}
      >
        <Box
          sx={{
            ...landingContainerSx,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 2,
            minHeight: 64,
          }}
        >
          <Link
            component={RouterLink}
            to="/"
            sx={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: 1.5,
              minWidth: 0,
              color: landingColors.textDark,
              textDecoration: 'none',
            }}
          >
            {systemLogoUrl ? (
              <Box component="img" src={systemLogoUrl} alt={systemName} sx={{ height: 36, width: 'auto' }} />
            ) : (
              <Box
                sx={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  width: 36,
                  height: 36,
                  borderRadius: '0.625rem',
                  bgcolor: landingColors.primary,
                  color: '#fff',
                }}
              >
                <CheckCircleIcon sx={{ fontSize: 20 }} />
              </Box>
            )}
            <Typography sx={{ fontWeight: 700, fontSize: '1.125rem', letterSpacing: '-0.02em' }}>
              {systemName}
            </Typography>
          </Link>

          <Box component="nav" aria-label="Primary" sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            {isAuthenticated ? (
              <>
                <Button
                  component={RouterLink}
                  to={dashboardPath}
                  sx={{
                    display: { xs: 'none', sm: 'inline-flex' },
                    fontSize: '0.9375rem',
                    fontWeight: 500,
                    color: landingColors.textMuted,
                    textTransform: 'none',
                    '&:hover': { bgcolor: 'transparent', color: landingColors.primary },
                  }}
                >
                  Dashboard
                </Button>
                <PortalUserMenu />
              </>
            ) : (
              <>
                <Link
                  component={RouterLink}
                  to="/login"
                  sx={{
                    px: 1.5,
                    py: 1,
                    fontSize: '0.9375rem',
                    fontWeight: 500,
                    color: landingColors.textMuted,
                    textDecoration: 'none',
                    '&:hover': { color: landingColors.primary },
                  }}
                >
                  Sign in
                </Link>
                <Button component={RouterLink} to="/register" sx={{ ...landingBtnPrimarySx, minHeight: 40, py: 1 }}>
                  Register
                </Button>
              </>
            )}
          </Box>
        </Box>
      </Box>

      <Box component="main">
        {isFullWidthPublicPage ? (
          <Outlet />
        ) : (
          <Box sx={{ px: { xs: 2, sm: 3, lg: 4 }, py: { xs: 3, sm: 4 } }}>
            <Box sx={{ mx: 'auto', maxWidth: '80rem' }}>
              <BreadcrumbProvider>
                <PortalBreadcrumbs simple homeHref="/" />
                <Outlet />
              </BreadcrumbProvider>
            </Box>
          </Box>
        )}
      </Box>

      <LandingFooter />
    </Box>
  )
}
