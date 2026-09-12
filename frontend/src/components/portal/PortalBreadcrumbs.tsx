import NavigateNextIcon from '@mui/icons-material/NavigateNext'
import { Box, Breadcrumbs, Link, Typography } from '@mui/material'
import { Link as RouterLink, useLocation } from 'react-router-dom'
import { portalColors } from './portalTheme'
import type { PortalNavItem } from './PortalShell'
import { buildPortalBreadcrumbs, buildSimpleBreadcrumbs, type BreadcrumbItem } from './portalBreadcrumbUtils'
import { useBreadcrumbContext } from './BreadcrumbContext'

interface PortalBreadcrumbsProps {
  navItems?: PortalNavItem[]
  brandHref?: string
  simple?: boolean
  homeHref?: string
}

function BreadcrumbTrail({ items }: { items: BreadcrumbItem[] }) {
  if (items.length === 0) {
    return null
  }

  return (
    <Box
      component="nav"
      aria-label="Breadcrumb"
      sx={{
        mb: 3,
        px: { xs: 0.5, sm: 0 },
      }}
    >
      <Breadcrumbs
        separator={<NavigateNextIcon sx={{ fontSize: 16, color: portalColors.textLight }} />}
        sx={{
          '& .MuiBreadcrumbs-ol': { flexWrap: 'wrap' },
          '& .MuiBreadcrumbs-li': { display: 'flex', alignItems: 'center' },
        }}
      >
        {items.map((item, index) => {
          const isLast = index === items.length - 1

          if (isLast || !item.href) {
            return (
              <Typography
                key={`${item.label}-${index}`}
                sx={{
                  fontSize: '0.8125rem',
                  fontWeight: isLast ? 600 : 500,
                  color: isLast ? portalColors.textDark : portalColors.textMuted,
                }}
              >
                {item.label}
              </Typography>
            )
          }

          return (
            <Link
              key={`${item.label}-${index}`}
              component={RouterLink}
              to={item.href}
              underline="hover"
              sx={{
                fontSize: '0.8125rem',
                fontWeight: 500,
                color: portalColors.textMuted,
                '&:hover': { color: portalColors.primary },
              }}
            >
              {item.label}
            </Link>
          )
        })}
      </Breadcrumbs>
    </Box>
  )
}

export function PortalBreadcrumbs({ navItems = [], brandHref = '/', simple = false, homeHref = '/' }: PortalBreadcrumbsProps) {
  const { pathname } = useLocation()
  const breadcrumbContext = useBreadcrumbContext()
  const currentLabel = breadcrumbContext?.currentLabel

  const items = simple
    ? buildSimpleBreadcrumbs(pathname, homeHref)
    : buildPortalBreadcrumbs(pathname, navItems, brandHref, currentLabel)

  return <BreadcrumbTrail items={items} />
}
