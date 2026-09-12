import { Box } from '@mui/material'
import { Outlet } from 'react-router-dom'
import { AuthBrandPanel } from '../features/auth/components/AuthBrandPanel'
import { authColors } from '../features/auth/components/authTheme'
import { BreadcrumbProvider } from '../components/portal/BreadcrumbContext'
import { PortalBreadcrumbs } from '../components/portal/PortalBreadcrumbs'

export function AuthLayout() {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' },
        minHeight: '100dvh',
        bgcolor: authColors.white,
        fontFamily: '"IBM Plex Sans", system-ui, -apple-system, "Segoe UI", sans-serif',
        color: authColors.textDark,
        height: { md: '100dvh' },
        overflow: { md: 'hidden' },
      }}
    >
      <AuthBrandPanel />

      <Box
        component="main"
        sx={{
          position: 'relative',
          display: 'flex',
          flexDirection: 'column',
          minHeight: { xs: '100vh', md: '100%' },
          bgcolor: authColors.white,
          overflow: { md: 'hidden' },
        }}
      >
        <Box
          sx={{
            flex: 1,
            display: 'flex',
            flexDirection: 'column',
            px: { xs: 2.5, sm: 4 },
            py: { xs: 4, md: 5 },
            overflowY: { md: 'auto' },
          }}
        >
          <BreadcrumbProvider>
            <PortalBreadcrumbs simple homeHref="/" />
            <Box sx={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <Outlet />
            </Box>
          </BreadcrumbProvider>
        </Box>
      </Box>
    </Box>
  )
}
