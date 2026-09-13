import { portalColors } from './portalTheme'

const roleLabels: Record<string, string> = {
  ROLE_ADMIN: 'Administrator',
  ROLE_AGENCY_ADMIN: 'Agency Admin',
  ROLE_AGENCY: 'Agency Staff',
  ROLE_AGENCY_USER: 'Agency User',
  ROLE_IMPORTER: 'Importer',
  ROLE_EXPORTER: 'Exporter',
  ROLE_BROKER: 'Broker',
  ROLE_EVALUATOR: 'Evaluator',
  ROLE_ACCOUNTANT: 'Accountant',
  ROLE_SECRETARY: 'Agency Secretary',
  ROLE_UNDERSECRETARY: 'Agency Undersecretary',
  ROLE_BILLING_AGENT: 'Billing Agent',
  ROLE_ACCREDITATION_OFFICER: 'DA Accreditation Officer',
  ROLE_DA_SECRETARY: 'DA Secretary',
  ROLE_DA_UNDERSECRETARY: 'DA Undersecretary',
  ROLE_MAV_ADMIN: 'MAV Admin',
  ROLE_MAV_EVALUATOR: 'MAV Evaluator',
  ROLE_MAV_SECRETARY: 'MAV Secretary',
  ROLE_MAV_IMPORTER: 'MAV Importer',
  ROLE_WAREHOUSE_STAFF: 'Warehouse Staff',
  ROLE_DRIVER: 'Driver',
  ROLE_OPERATOR: 'Operator',
  ROLE_INSPECTOR: 'Inspector',
  ROLE_DOCTOR: 'Doctor',
}

export function formatRoleLabel(role: string): string {
  if (roleLabels[role]) return roleLabels[role]
  if (role === 'ROLE_USER') return ''
  return role.replace(/^ROLE_/, '').replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase())
}

export function resolveDashboardPath(redirectPath: string | null | undefined, roles: string[]): string {
  if (roles.includes('ROLE_ADMIN')) return '/admin'
  if (roles.some((r) => r.startsWith('ROLE_MAV_'))) return '/mav'
  if (roles.includes('ROLE_WAREHOUSE_STAFF')) return '/warehouse'
  if (roles.includes('ROLE_OPERATOR') && !roles.includes('ROLE_DRIVER')) return '/operator/containers'
  if (roles.includes('ROLE_DOCTOR') && !roles.some((r) =>
    ['ROLE_EVALUATOR', 'ROLE_ACCOUNTANT', 'ROLE_SECRETARY', 'ROLE_UNDERSECRETARY', 'ROLE_BILLING_AGENT', 'ROLE_AGENCY_ADMIN', 'ROLE_AGENCY', 'ROLE_AGENCY_USER'].includes(r),
  )) return '/doctor/containers'
  if (roles.includes('ROLE_DRIVER')) return '/driver'
  if (redirectPath) return redirectPath
  if (roles.includes('ROLE_DA_SECRETARY') || roles.includes('ROLE_DA_UNDERSECRETARY')) return '/da'
  if (
    roles.includes('ROLE_ACCREDITATION_OFFICER') &&
    !roles.some((r) =>
      ['ROLE_EVALUATOR', 'ROLE_ACCOUNTANT', 'ROLE_SECRETARY', 'ROLE_UNDERSECRETARY', 'ROLE_BILLING_AGENT', 'ROLE_AGENCY_ADMIN', 'ROLE_AGENCY', 'ROLE_AGENCY_USER'].includes(r),
    )
  ) {
    return '/accreditation-officer/dashboard'
  }
  if (roles.some((r) => ['ROLE_EVALUATOR', 'ROLE_ACCOUNTANT', 'ROLE_SECRETARY', 'ROLE_UNDERSECRETARY', 'ROLE_BILLING_AGENT', 'ROLE_ACCREDITATION_OFFICER', 'ROLE_AGENCY_ADMIN', 'ROLE_AGENCY', 'ROLE_AGENCY_USER'].includes(r))) return '/agency'
  if (roles.includes('ROLE_INSPECTOR')) return '/inspector'
  return '/client'
}

export function getStatusBadgeStyle(status: string) {
  switch (status) {
    case 'VERIFIED':
    case 'ACTIVE':
      return { bgcolor: portalColors.successSoft, color: portalColors.successText }
    case 'PENDING':
      return { bgcolor: '#fffbeb', color: '#92400e' }
    case 'SUSPENDED':
      return { bgcolor: '#fef2f2', color: '#991b1b' }
    default:
      return { bgcolor: portalColors.bgMuted, color: portalColors.textMuted }
  }
}

export function getRoleBadgeStyle(role: string) {
  if (role === 'ROLE_ADMIN') return { bgcolor: '#fef2f2', color: '#991b1b' }
  if (role === 'ROLE_EVALUATOR') return { bgcolor: '#fffbeb', color: '#92400e' }
  if (role.startsWith('ROLE_MAV_')) return { bgcolor: portalColors.successSoft, color: portalColors.successText }
  return { bgcolor: portalColors.bgMuted, color: portalColors.textDark }
}
