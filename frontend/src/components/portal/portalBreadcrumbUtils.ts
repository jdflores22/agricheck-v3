import type { PortalNavItem } from './PortalShell'

export interface BreadcrumbItem {
  label: string
  href?: string
}

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

const SEGMENT_LABELS: Record<string, string> = {
  admin: 'Admin',
  da: 'DA Leadership',
  agency: 'Agency',
  client: 'Client',
  inspector: 'Inspector',
  mav: 'MAV',
  warehouse: 'Warehouse',
  driver: 'Driver',
  profile: 'My Profile',
  notifications: 'Notifications',
  new: 'New',
  edit: 'Edit',
  confirmation: 'Confirmation',
  compliance: 'Compliance',
  inspection: 'Inspection Upload',
  status: 'Status',
  warehouses: 'Warehouses',
  agencies: 'Agencies',
  entries: 'My Entries',
  accreditation: 'Accreditation',
  certificates: 'Certificates',
  containers: 'Containers',
  inspections: 'Inspections',
  bookings: 'Bookings',
  bills: 'Bills & Payments',
  'payment-history': 'Payment History',
  'payment-config': 'Payment Config',
  'pending-cash': 'Pending Cash',
  users: 'Users',
  settings: 'System Settings',
  forms: 'Form Builder',
  commodities: 'Commodities',
  'audit-logs': 'Audit Logs',
  'certificate-templates': 'Certificate Templates',
  applications: 'Applications',
  licenses: 'Licenses',
  periods: 'Periods',
  reports: 'Reports',
  'year-transition': 'Year Transition',
  inventory: 'Inventory',
  receive: 'Receive',
  releases: 'Releases',
  evaluator: 'Evaluator',
  queue: 'Queue',
  assignments: 'Assignments',
  'transport-tags': 'Transport Tags',
  billing: 'Billing',
  verify: 'Verify Certificate',
  login: 'Sign In',
  register: 'Register',
  'forgot-password': 'Forgot Password',
  'reset-password': 'Reset Password',
  'resend-verification': 'Resend Verification',
}

function humanizeSegment(segment: string): string {
  return segment
    .split('-')
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ')
}

export function buildPortalBreadcrumbs(
  pathname: string,
  navItems: PortalNavItem[],
  brandHref: string,
  currentLabel?: string | null,
): BreadcrumbItem[] {
  const navByPath = new Map(navItems.map((item) => [item.path, item.label]))
  const dashboardLabel = navByPath.get(brandHref) ?? 'Dashboard'

  if (pathname === brandHref) {
    return [{ label: dashboardLabel }]
  }

  const items: BreadcrumbItem[] = [{ label: dashboardLabel, href: brandHref }]
  const segments = pathname.split('/').filter(Boolean)
  let path = ''

  for (let index = 0; index < segments.length; index += 1) {
    path += `/${segments[index]}`
    if (path === brandHref) {
      continue
    }

    const isLast = index === segments.length - 1
    const segment = segments[index]
    let label = navByPath.get(path)

    if (!label) {
      if (UUID_REGEX.test(segment)) {
        label = isLast && currentLabel ? currentLabel : 'Details'
      } else {
        label = SEGMENT_LABELS[segment] ?? humanizeSegment(segment)
      }
    }

    if (isLast && currentLabel) {
      label = currentLabel
    }

    items.push({
      label,
      href: isLast ? undefined : path,
    })
  }

  return items
}

export function buildSimpleBreadcrumbs(pathname: string, homeHref = '/'): BreadcrumbItem[] {
  if (pathname === homeHref || pathname === '/') {
    return [{ label: 'Home' }]
  }

  const items: BreadcrumbItem[] = [{ label: 'Home', href: homeHref }]
  const segments = pathname.split('/').filter(Boolean)
  let path = ''

  for (let index = 0; index < segments.length; index += 1) {
    path += `/${segments[index]}`
    const isLast = index === segments.length - 1
    const segment = segments[index]
    const label = SEGMENT_LABELS[segment] ?? humanizeSegment(segment)

    items.push({
      label,
      href: isLast ? undefined : path,
    })
  }

  return items
}
