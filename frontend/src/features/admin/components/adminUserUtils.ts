import type { AdminRole } from '../api/adminApi'

export const adminUserStatuses = ['Active', 'Pending', 'Suspended', 'Deactivated'] as const

const agencyStaffRoleCodes = [
  'ROLE_AGENCY_ADMIN',
  'ROLE_AGENCY',
  'ROLE_AGENCY_USER',
  'ROLE_EVALUATOR',
  'ROLE_ACCOUNTANT',
  'ROLE_SECRETARY',
  'ROLE_UNDERSECRETARY',
  'ROLE_BILLING_AGENT',
  'ROLE_INSPECTOR',
  'ROLE_DOCTOR',
] as const

export function groupAdminRoles(roles: AdminRole[]) {
  const groups: Record<string, AdminRole[]> = {
    System: [],
    DA: [],
    MAV: [],
    Agency: [],
    Operations: [],
    Client: [],
  }

  for (const role of roles) {
    if (role.code === 'ROLE_ADMIN') {
      groups.System.push(role)
    } else if (
      role.code === 'ROLE_ACCREDITATION_OFFICER' ||
      role.code === 'ROLE_DA_SECRETARY' ||
      role.code === 'ROLE_DA_UNDERSECRETARY'
    ) {
      groups.DA.push(role)
    } else if (role.code.startsWith('ROLE_MAV_')) {
      groups.MAV.push(role)
    } else if (role.code.startsWith('ROLE_AGENCY') || agencyStaffRoleCodes.includes(role.code as typeof agencyStaffRoleCodes[number])) {
      groups.Agency.push(role)
    } else if (['ROLE_WAREHOUSE_STAFF', 'ROLE_DRIVER', 'ROLE_OPERATOR'].includes(role.code)) {
      groups.Operations.push(role)
    } else {
      groups.Client.push(role)
    }
  }

  return groups
}

export function roleRequiresAgency(role: AdminRole) {
  return role.requiresAgency
}

export function toggleRoleCode(roleCode: string, current: string[]) {
  return current.includes(roleCode)
    ? current.filter((code) => code !== roleCode)
    : [...current, roleCode]
}

export function toggleAgencyForRole(
  roleCode: string,
  agencyId: number,
  roleAgencies: Record<string, number[]>,
) {
  const current = roleAgencies[roleCode] ?? []
  const next = current.includes(agencyId)
    ? current.filter((id) => id !== agencyId)
    : [...current, agencyId]

  return { ...roleAgencies, [roleCode]: next }
}

export function buildRoleAgenciesPayload(roleCodes: string[], roleAgencies: Record<string, number[]>) {
  const payload: Record<string, number[]> = {}
  for (const roleCode of roleCodes) {
    if ((roleAgencies[roleCode] ?? []).length > 0) {
      payload[roleCode] = roleAgencies[roleCode]
    }
  }
  return payload
}
