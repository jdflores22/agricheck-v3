import MenuIcon from '@mui/icons-material/Menu'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import {
  AppBar,
  Box,
  Drawer,
  IconButton,
  Toolbar,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'
import { useState } from 'react'
import { Link as RouterLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { portalColors, portalNavItemSx } from './portalTheme'
import { PortalUserMenu } from './PortalUserMenu'
import { NotificationBell } from './NotificationBell'
import { useSystemBranding } from '../../features/system/SystemBrandingProvider'
import { BreadcrumbProvider } from './BreadcrumbContext'
import { PortalBreadcrumbs } from './PortalBreadcrumbs'

export interface PortalNavItem {
  label: string
  path: string
  icon: ReactNode
  section?: string
}

interface PortalShellProps {
  portalLabel: string
  brandHref: string
  navItems: PortalNavItem[]
  badgeCounts?: Record<string, number>
}

const drawerWidth = 256

function isNavActive(pathname: string, path: string) {
  if (path === '/profile') {
    return pathname === '/profile'
  }
  if (path === '/mav') {
    return pathname === '/mav' || (pathname.startsWith('/mav/') && !pathname.startsWith('/mav/admin'))
  }
  const exactPaths = ['/admin', '/client', '/agency', '/da', '/inspector', '/warehouse', '/driver', '/mav/admin']
  if (exactPaths.includes(path)) {
    return pathname === path
  }
  return pathname === path || pathname.startsWith(`${path}/`)
}

function getActiveNavPath(pathname: string, navItems: PortalNavItem[]) {
  const matches = navItems
    .filter((item) => isNavActive(pathname, item.path))
    .sort((a, b) => b.path.length - a.path.length)

  return matches[0]?.path ?? null
}

function NavBadge({ count, active }: { count: number; active: boolean }) {
  return (
    <Box
      component="span"
      aria-label={`${count} pending`}
      sx={{
        ml: 'auto',
        minWidth: 20,
        height: 20,
        px: 0.75,
        borderRadius: 999,
        bgcolor: active ? 'rgba(255,255,255,0.22)' : portalColors.primary,
        color: '#fff',
        fontSize: '0.6875rem',
        fontWeight: 700,
        lineHeight: 1,
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
        flexShrink: 0,
      }}
    >
      {count > 99 ? '99+' : count}
    </Box>
  )
}

function NavList({
  items,
  pathname,
  badgeCounts,
  onNavigate,
}: {
  items: PortalNavItem[]
  pathname: string
  badgeCounts?: Record<string, number>
  onNavigate: (path: string) => void
}) {
  let lastSection: string | undefined
  const activePath = getActiveNavPath(pathname, items)

  return (
    <Box component="nav" sx={{ py: 2 }}>
      {items.map((item) => {
        const showSection = item.section && item.section !== lastSection
        if (item.section) lastSection = item.section
        const isActive = item.path === activePath
        const badgeCount = badgeCounts?.[item.path]

        return (
          <Box key={item.path}>
            {showSection && (
              <Typography
                sx={{
                  px: 3,
                  pt: 2,
                  pb: 1,
                  fontSize: '0.6875rem',
                  fontWeight: 600,
                  letterSpacing: '0.14em',
                  textTransform: 'uppercase',
                  color: portalColors.textLight,
                }}
              >
                {item.section}
              </Typography>
            )}
            <Box sx={{ px: 1.5, pb: 0.5 }}>
              <Box
                component="button"
                type="button"
                className={isActive ? 'active' : undefined}
                onClick={() => onNavigate(item.path)}
                sx={{
                  ...portalNavItemSx,
                  width: '100%',
                  border: 'none',
                  bgcolor: 'transparent',
                  cursor: 'pointer',
                  textAlign: 'left',
                }}
              >
                {item.icon}
                <Box component="span" sx={{ flex: 1, minWidth: 0 }}>
                  {item.label}
                </Box>
                {badgeCount ? <NavBadge count={badgeCount} active={isActive} /> : null}
              </Box>
            </Box>
          </Box>
        )
      })}
    </Box>
  )
}

export function PortalShell({ portalLabel, brandHref, navItems, badgeCounts }: PortalShellProps) {
  const location = useLocation()
  const navigate = useNavigate()
  const [mobileOpen, setMobileOpen] = useState(false)
  const { systemName, systemLogoUrl } = useSystemBranding()

  const handleNavigate = (path: string) => {
    navigate(path)
    setMobileOpen(false)
  }

  const sidebarHeader = (
    <Box sx={{ px: 2.5, py: 2, borderBottom: `1px solid ${portalColors.border}` }}>
      <Typography
        sx={{
          fontSize: '0.6875rem',
          fontWeight: 600,
          letterSpacing: '0.14em',
          textTransform: 'uppercase',
          color: portalColors.textLight,
        }}
      >
        Menu
      </Typography>
      <Typography sx={{ mt: 0.5, fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
        {portalLabel}
      </Typography>
    </Box>
  )

  const drawerContent = (
    <Box sx={{ height: '100%', bgcolor: portalColors.bgWhite }}>
      {sidebarHeader}
      <NavList
        items={navItems}
        pathname={location.pathname}
        badgeCounts={badgeCounts}
        onNavigate={handleNavigate}
      />
    </Box>
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100dvh', bgcolor: portalColors.bgPage }}>
      <AppBar
        position="fixed"
        elevation={0}
        sx={{
          zIndex: (theme) => theme.zIndex.drawer + 1,
          bgcolor: portalColors.primary,
          borderBottom: '1px solid rgba(255,255,255,0.08)',
        }}
      >
        <Toolbar sx={{ minHeight: 64, px: { xs: 2, sm: 3 } }}>
          <IconButton
            color="inherit"
            edge="start"
            onClick={() => setMobileOpen(true)}
            sx={{ mr: 1, display: { md: 'none' } }}
            aria-label="Open navigation menu"
          >
            <MenuIcon />
          </IconButton>

          <Box
            component={RouterLink}
            to={brandHref}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              flexGrow: 1,
              minWidth: 0,
              color: '#fff',
              textDecoration: 'none',
            }}
          >
            {systemLogoUrl ? (
              <Box
                component="img"
                src={systemLogoUrl}
                alt={systemName}
                sx={{ height: { xs: 36, sm: 40 }, width: 'auto', flexShrink: 0 }}
              />
            ) : (
              <CheckCircleIcon sx={{ fontSize: 28, flexShrink: 0 }} />
            )}
            <Typography
              sx={{
                fontWeight: 700,
                fontSize: { xs: '1rem', sm: '1.125rem' },
                whiteSpace: 'nowrap',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
              }}
            >
              {systemName}
            </Typography>
            <Typography
              component="span"
              sx={{
                display: { xs: 'none', sm: 'inline' },
                opacity: 0.75,
                fontSize: '0.875rem',
                fontWeight: 500,
              }}
            >
              · {portalLabel}
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            <NotificationBell />
            <PortalUserMenu />
          </Box>
        </Toolbar>
      </AppBar>

      <Box component="nav" sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 } }}>
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: 'block', md: 'none' },
            '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box' },
          }}
        >
          {drawerContent}
        </Drawer>
        <Drawer
          variant="permanent"
          open
          sx={{
            display: { xs: 'none', md: 'block' },
            '& .MuiDrawer-paper': {
              width: drawerWidth,
              boxSizing: 'border-box',
              borderRight: `1px solid ${portalColors.border}`,
              mt: 8,
              height: 'calc(100% - 64px)',
            },
          }}
        >
          {drawerContent}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { md: `calc(100% - ${drawerWidth}px)` },
          mt: 8,
          minHeight: 'calc(100dvh - 64px)',
        }}
      >
        <Box sx={{ px: { xs: 2, sm: 3, lg: 4 }, py: { xs: 3, sm: 4 } }}>
          <Box sx={{ mx: 'auto', maxWidth: '80rem' }}>
            <BreadcrumbProvider>
              <PortalBreadcrumbs navItems={navItems} brandHref={brandHref} />
              <Outlet />
            </BreadcrumbProvider>
          </Box>
        </Box>
      </Box>
    </Box>
  )
}
