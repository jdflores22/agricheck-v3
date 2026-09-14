import ArticleOutlinedIcon from '@mui/icons-material/ArticleOutlined'
import AssignmentOutlinedIcon from '@mui/icons-material/AssignmentOutlined'
import VerifiedUserOutlinedIcon from '@mui/icons-material/VerifiedUserOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined'
import DescriptionOutlinedIcon from '@mui/icons-material/DescriptionOutlined'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined'
import SettingsOutlinedIcon from '@mui/icons-material/SettingsOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import PaymentsOutlinedIcon from '@mui/icons-material/PaymentsOutlined'
import PeopleOutlinedIcon from '@mui/icons-material/PeopleOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import PlaylistAddCheckOutlinedIcon from '@mui/icons-material/PlaylistAddCheckOutlined'
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import ReportOutlinedIcon from '@mui/icons-material/ReportOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import type { PortalNavItem } from './PortalShell'

export type PortalKey =
  | 'admin'
  | 'client'
  | 'agency'
  | 'da'
  | 'accreditation-officer'
  | 'inspector'
  | 'mav'
  | 'warehouse'
  | 'driver'
  | 'operator'
  | 'doctor'

export interface PortalConfig {
  key: PortalKey
  portalLabel: string
  brandHref: string
  navItems: PortalNavItem[]
}

const adminNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/admin', icon: <DashboardOutlinedIcon /> },
  { label: 'Users', path: '/admin/users', icon: <PeopleOutlinedIcon />, section: 'Management' },
  { label: 'Agencies', path: '/admin/agencies', icon: <BusinessOutlinedIcon /> },
  { label: 'Commodities', path: '/admin/commodities', icon: <CategoryOutlinedIcon /> },
  { label: 'Payment Config', path: '/admin/payment-config', icon: <PaymentsOutlinedIcon /> },
  { label: 'Entry Payments', path: '/admin/entry-payments', icon: <ReceiptLongOutlinedIcon /> },
  { label: 'Revenue', path: '/admin/revenue', icon: <ReportOutlinedIcon /> },
  { label: 'Form Builder', path: '/admin/forms', icon: <ArticleOutlinedIcon />, section: 'Templates' },
  { label: 'Certificate Templates', path: '/admin/certificate-templates', icon: <DescriptionOutlinedIcon /> },
  { label: 'Certificates', path: '/admin/certificates', icon: <VerifiedOutlinedIcon /> },
  { label: 'Audit Logs', path: '/admin/audit-logs', icon: <HistoryOutlinedIcon />, section: 'System' },
  { label: 'System Settings', path: '/admin/settings', icon: <SettingsOutlinedIcon />, section: 'System' },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const clientNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/client', icon: <DashboardOutlinedIcon /> },
  { label: 'Accreditation Application', path: '/client/accreditation', icon: <PlaylistAddCheckOutlinedIcon />, section: 'Accreditation' },
  { label: 'My Certificates', path: '/client/certificates', icon: <VerifiedOutlinedIcon /> },
  { label: 'Submit Entry', path: '/client/entries/agencies', icon: <ArticleOutlinedIcon />, section: 'Entries' },
  { label: 'My Entries', path: '/client/entries', icon: <AssignmentOutlinedIcon /> },
  { label: 'My Containers', path: '/client/containers', icon: <Inventory2OutlinedIcon /> },
  { label: 'Inspections', path: '/client/inspections', icon: <PlaylistAddCheckOutlinedIcon /> },
  { label: 'Warehouse Booking', path: '/client/warehouse/bookings', icon: <WarehouseOutlinedIcon />, section: 'Warehouse' },
  { label: 'Bills & Payments', path: '/client/bills', icon: <ReceiptLongOutlinedIcon />, section: 'Payments' },
  { label: 'Payment History', path: '/client/payment-history', icon: <HistoryOutlinedIcon /> },
  { label: 'MAV Import Portal', path: '/mav', icon: <VerifiedUserOutlinedIcon />, section: 'MAV' },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const agencyNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/agency', icon: <DashboardOutlinedIcon /> },
  { label: 'Evaluation Queue', path: '/agency/evaluator/queue', icon: <AssignmentOutlinedIcon /> },
  { label: 'My Assignments', path: '/agency/evaluator/assignments', icon: <FactCheckOutlinedIcon /> },
  { label: 'Transport Tags', path: '/agency/transport-tags', icon: <LocalShippingOutlinedIcon /> },
  { label: 'Accreditation Review', path: '/agency/accreditation', icon: <VerifiedOutlinedIcon /> },
  { label: 'Reports', path: '/agency/reports', icon: <ReportOutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const accreditationOfficerNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/accreditation-officer/dashboard', icon: <DashboardOutlinedIcon /> },
  { label: 'Applications', path: '/accreditation-officer/accreditations', icon: <VerifiedOutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const daNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/da', icon: <DashboardOutlinedIcon /> },
  { label: 'Agencies', path: '/da/agencies', icon: <BusinessOutlinedIcon /> },
  { label: 'Warehouses', path: '/da/warehouses', icon: <WarehouseOutlinedIcon /> },
  { label: 'Reports', path: '/da/reports', icon: <ReportOutlinedIcon /> },
  { label: 'MAV Utilization', path: '/da/reports/mav', icon: <Inventory2OutlinedIcon /> },
  { label: 'Commodity Stock', path: '/da/reports/commodities', icon: <Inventory2OutlinedIcon /> },
  { label: 'Stock Map', path: '/da/reports/stock', icon: <WarehouseOutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const agencyStaffRoleCodes = [
  'ROLE_EVALUATOR',
  'ROLE_ACCOUNTANT',
  'ROLE_SECRETARY',
  'ROLE_UNDERSECRETARY',
  'ROLE_BILLING_AGENT',
  'ROLE_AGENCY_ADMIN',
  'ROLE_AGENCY',
  'ROLE_AGENCY_USER',
  'ROLE_INSPECTOR',
]

function isDaAccreditationOfficerOnly(roles: string[]) {
  return (
    roles.includes('ROLE_ACCREDITATION_OFFICER') &&
    !roles.includes('ROLE_ADMIN') &&
    !roles.some((role) => agencyStaffRoleCodes.includes(role))
  )
}

function isDaLeadershipOnly(roles: string[]) {
  return (
    (roles.includes('ROLE_DA_SECRETARY') || roles.includes('ROLE_DA_UNDERSECRETARY')) &&
    !roles.includes('ROLE_ADMIN') &&
    !roles.some((role) => agencyStaffRoleCodes.includes(role))
  )
}

const agencyOperationalRoleCodes = [
  'ROLE_EVALUATOR',
  'ROLE_ACCOUNTANT',
  'ROLE_SECRETARY',
  'ROLE_UNDERSECRETARY',
  'ROLE_BILLING_AGENT',
  'ROLE_INSPECTOR',
  'ROLE_ACCREDITATION_OFFICER',
]

const agencyAdminNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/agency', icon: <DashboardOutlinedIcon /> },
  { label: 'Payment Config', path: '/agency/payment-config', icon: <PaymentsOutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const billingAgentNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/agency', icon: <DashboardOutlinedIcon /> },
  { label: 'Billing', path: '/agency/billing', icon: <ReceiptLongOutlinedIcon /> },
  { label: 'Revenue Reports', path: '/agency/billing-reports', icon: <ReportOutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const billingAgentRoleCodes = ['ROLE_BILLING_AGENT', 'ROLE_ACCOUNTANT']

function isAgencyAdminOnly(roles: string[]) {
  return (
    roles.includes('ROLE_AGENCY_ADMIN') &&
    !roles.includes('ROLE_ADMIN') &&
    !roles.some((role) => agencyOperationalRoleCodes.includes(role))
  )
}

function isBillingAgentOnly(roles: string[]) {
  return (
    roles.some((role) => billingAgentRoleCodes.includes(role)) &&
    !roles.includes('ROLE_ADMIN') &&
    !roles.includes('ROLE_AGENCY_ADMIN') &&
    !roles.some((role) =>
      agencyOperationalRoleCodes.filter((code) => !billingAgentRoleCodes.includes(code)).includes(role),
    )
  )
}

function hasBillingAccess(roles: string[]) {
  return roles.some((role) => billingAgentRoleCodes.includes(role)) || roles.includes('ROLE_ADMIN')
}

function appendBillingNavItems(items: PortalNavItem[]): PortalNavItem[] {
  const profileIndex = items.findIndex((item) => item.path === '/profile')
  const insertAt = profileIndex >= 0 ? profileIndex : items.length
  const next = [...items]
  next.splice(insertAt, 0, ...billingAgentNavItems.filter((item) => item.path !== '/agency' && item.path !== '/profile'))
  return next
}

function buildAgencyNavItems(roles: string[]): PortalNavItem[] {
  if (isDaAccreditationOfficerOnly(roles)) {
    return accreditationOfficerNavItems
  }

  if (isBillingAgentOnly(roles)) {
    return billingAgentNavItems
  }

  if (isAgencyAdminOnly(roles)) {
    return agencyAdminNavItems
  }

  let items = [...agencyNavItems]

  if (hasBillingAccess(roles)) {
    items = appendBillingNavItems(items)
  }

  if (roles.includes('ROLE_AGENCY_ADMIN') || roles.includes('ROLE_ADMIN')) {
    const profileIndex = items.findIndex((item) => item.path === '/profile')
    items.splice(profileIndex >= 0 ? profileIndex : items.length, 0, {
      label: 'Payment Config',
      path: '/agency/payment-config',
      icon: <PaymentsOutlinedIcon />,
    })
  }

  return items
}

const inspectorNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/inspector', icon: <DashboardOutlinedIcon /> },
  { label: 'Inspections', path: '/inspector/inspections', icon: <PlaylistAddCheckOutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const warehouseNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/warehouse', icon: <DashboardOutlinedIcon /> },
  { label: 'Inventory', path: '/warehouse/inventory', icon: <Inventory2OutlinedIcon /> },
  { label: 'Receive Container', path: '/warehouse/receive', icon: <LocalShippingOutlinedIcon /> },
  { label: 'Release Authorizations', path: '/warehouse/releases', icon: <VerifiedOutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const driverNavItems: PortalNavItem[] = [
  { label: 'Dashboard', path: '/driver', icon: <DashboardOutlinedIcon /> },
  { label: 'My Containers', path: '/driver/containers', icon: <Inventory2OutlinedIcon /> },
  { label: 'Driver Profile', path: '/driver/profile', icon: <LocalShippingOutlinedIcon /> },
  { label: 'Account Settings', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const operatorNavItems: PortalNavItem[] = [
  { label: 'Containers', path: '/operator/containers', icon: <Inventory2OutlinedIcon /> },
  { label: 'Drivers', path: '/operator/drivers', icon: <LocalShippingOutlinedIcon /> },
  { label: 'Vehicles', path: '/operator/vehicles', icon: <LocalShippingOutlinedIcon /> },
  { label: 'Driver Invites', path: '/operator/invites', icon: <LocalShippingOutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const doctorNavItems: PortalNavItem[] = [
  { label: 'Containers', path: '/doctor/containers', icon: <Inventory2OutlinedIcon /> },
  { label: 'My Profile', path: '/profile', icon: <PersonOutlineOutlinedIcon />, section: 'Account' },
]

const mavImporterNav: PortalNavItem[] = [
  { label: 'Dashboard', path: '/mav', icon: <DashboardOutlinedIcon /> },
  { label: 'Applications', path: '/mav/applications', icon: <ArticleOutlinedIcon /> },
  { label: 'Licenses', path: '/mav/licenses', icon: <VerifiedOutlinedIcon /> },
]

const mavStaffNav: PortalNavItem[] = [
  { label: 'Admin Dashboard', path: '/mav/admin', icon: <DashboardOutlinedIcon />, section: 'MAV Admin' },
  { label: 'Application Periods', path: '/mav/admin/periods', icon: <AssignmentOutlinedIcon /> },
  { label: 'Review Applications', path: '/mav/admin/applications', icon: <FactCheckOutlinedIcon /> },
  { label: 'Licenses', path: '/mav/admin/licenses', icon: <VerifiedOutlinedIcon /> },
  { label: 'Import Certificates', path: '/mav/admin/mic', icon: <DescriptionOutlinedIcon /> },
  { label: 'HS Code Library', path: '/mav/admin/hs-library', icon: <CategoryOutlinedIcon /> },
  { label: 'Compliance', path: '/mav/admin/compliance', icon: <PlaylistAddCheckOutlinedIcon /> },
  { label: 'Reports', path: '/mav/admin/reports', icon: <ReportOutlinedIcon /> },
  { label: 'Year Transition', path: '/mav/admin/year-transition', icon: <HistoryOutlinedIcon /> },
]

function hasMavStaffRole(roles: string[]) {
  return roles.some((role) =>
    ['ROLE_MAV_ADMIN', 'ROLE_MAV_EVALUATOR', 'ROLE_MAV_SECRETARY', 'ROLE_ADMIN'].includes(role),
  )
}

function buildMavNavItems(roles: string[]): PortalNavItem[] {
  const items = [...mavImporterNav]
  if (hasMavStaffRole(roles)) {
    items.push(...mavStaffNav)
  }
  items.push({
    label: 'My Profile',
    path: '/profile',
    icon: <PersonOutlineOutlinedIcon />,
    section: 'Account',
  })
  return items
}

const portalConfigs: Record<
  PortalKey,
  Omit<PortalConfig, 'navItems'> & { navItems: PortalNavItem[] | ((roles: string[]) => PortalNavItem[]) }
> = {
  admin: { key: 'admin', portalLabel: 'Admin Portal', brandHref: '/admin', navItems: adminNavItems },
  client: { key: 'client', portalLabel: 'Client Portal', brandHref: '/client', navItems: clientNavItems },
  agency: { key: 'agency', portalLabel: 'Agency Portal', brandHref: '/agency', navItems: buildAgencyNavItems },
  da: { key: 'da', portalLabel: 'DA Leadership Portal', brandHref: '/da', navItems: daNavItems },
  'accreditation-officer': {
    key: 'accreditation-officer',
    portalLabel: 'DA Accreditation Portal',
    brandHref: '/accreditation-officer/dashboard',
    navItems: accreditationOfficerNavItems,
  },
  inspector: { key: 'inspector', portalLabel: 'Inspector Portal', brandHref: '/inspector', navItems: inspectorNavItems },
  mav: { key: 'mav', portalLabel: 'MAV Portal', brandHref: '/mav', navItems: buildMavNavItems },
  warehouse: { key: 'warehouse', portalLabel: 'Warehouse Portal', brandHref: '/warehouse', navItems: warehouseNavItems },
  driver: { key: 'driver', portalLabel: 'Driver Portal', brandHref: '/driver', navItems: driverNavItems },
  operator: { key: 'operator', portalLabel: 'Operator Portal', brandHref: '/operator/containers', navItems: operatorNavItems },
  doctor: { key: 'doctor', portalLabel: 'Doctor Portal', brandHref: '/doctor/containers', navItems: doctorNavItems },
}

function matchesPrefix(pathname: string, prefix: string) {
  return pathname === prefix || pathname.startsWith(`${prefix}/`)
}

export function resolvePortalKey(
  pathname: string,
  roles: string[] = [],
  redirectPath?: string | null,
): PortalKey {
  if (matchesPrefix(pathname, '/admin')) return 'admin'
  if (matchesPrefix(pathname, '/client')) return 'client'
  if (matchesPrefix(pathname, '/da')) return 'da'
  if (matchesPrefix(pathname, '/accreditation-officer')) return 'accreditation-officer'
  if (matchesPrefix(pathname, '/agency')) return 'agency'
  if (matchesPrefix(pathname, '/inspector')) return 'inspector'
  if (matchesPrefix(pathname, '/mav')) return 'mav'
  if (matchesPrefix(pathname, '/warehouse')) return 'warehouse'
  if (matchesPrefix(pathname, '/driver')) return 'driver'
  if (matchesPrefix(pathname, '/operator')) return 'operator'
  if (matchesPrefix(pathname, '/doctor')) return 'doctor'

  if (redirectPath) {
    if (redirectPath.startsWith('/admin')) return 'admin'
    if (redirectPath.startsWith('/client')) return 'client'
    if (redirectPath.startsWith('/da')) return 'da'
    if (redirectPath.startsWith('/accreditation-officer')) return 'accreditation-officer'
    if (redirectPath.startsWith('/agency')) return 'agency'
    if (redirectPath.startsWith('/inspector')) return 'inspector'
    if (redirectPath.startsWith('/mav')) return 'mav'
    if (redirectPath.startsWith('/warehouse')) return 'warehouse'
    if (redirectPath.startsWith('/driver')) return 'driver'
    if (redirectPath.startsWith('/operator')) return 'operator'
    if (redirectPath.startsWith('/doctor')) return 'doctor'
  }

  if (roles.includes('ROLE_ADMIN')) return 'admin'
  if (roles.some((role) => role.startsWith('ROLE_MAV_'))) return 'mav'
  if (roles.includes('ROLE_WAREHOUSE_STAFF')) return 'warehouse'
  if (roles.includes('ROLE_OPERATOR') && !roles.includes('ROLE_DRIVER')) return 'operator'
  if (roles.includes('ROLE_DOCTOR') && !roles.some((role) => agencyStaffRoleCodes.includes(role))) return 'doctor'
  if (roles.includes('ROLE_DRIVER')) return 'driver'
  if (isDaLeadershipOnly(roles)) return 'da'
  if (isDaAccreditationOfficerOnly(roles)) return 'accreditation-officer'
  if (
    roles.some((role) =>
      ['ROLE_EVALUATOR', 'ROLE_ACCOUNTANT', 'ROLE_SECRETARY', 'ROLE_UNDERSECRETARY', 'ROLE_BILLING_AGENT', 'ROLE_AGENCY_ADMIN', 'ROLE_AGENCY', 'ROLE_AGENCY_USER', 'ROLE_ACCREDITATION_OFFICER'].includes(role),
    )
  ) {
    return 'agency'
  }
  if (roles.includes('ROLE_INSPECTOR')) return 'inspector'
  return 'client'
}

export function getPortalConfig(portalKey: PortalKey, roles: string[] = []): PortalConfig {
  const config = portalConfigs[portalKey]
  const navItems = typeof config.navItems === 'function' ? config.navItems(roles) : config.navItems

  return {
    key: config.key,
    portalLabel: config.portalLabel,
    brandHref: config.brandHref,
    navItems,
  }
}
