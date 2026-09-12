import { useLocation } from 'react-router-dom'
import { useAppSelector } from '../app/hooks'
import { selectAuthRedirect, selectCurrentUser } from '../features/auth/authSlice'
import { PortalShell } from '../components/portal/PortalShell'
import { getPortalConfig, resolvePortalKey } from '../components/portal/portalNavConfig'
import { usePortalNavBadgeCounts } from '../components/portal/usePortalNavBadgeCounts'

export function AppPortalLayout() {
  const location = useLocation()
  const user = useAppSelector(selectCurrentUser)
  const redirectPath = useAppSelector(selectAuthRedirect)
  const portalKey = resolvePortalKey(location.pathname, user?.roles ?? [], redirectPath)
  const { portalLabel, brandHref, navItems } = getPortalConfig(portalKey, user?.roles ?? [])
  const badgeCounts = usePortalNavBadgeCounts(
    portalKey,
    navItems.map((item) => item.path),
  )

  return (
    <PortalShell
      portalLabel={portalLabel}
      brandHref={brandHref}
      navItems={navItems}
      badgeCounts={badgeCounts}
    />
  )
}
