import { Box } from '@mui/material'
import { useAppSelector } from '../../../app/hooks'
import { selectCurrentUser } from '../../auth/authSlice'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { useGetAgencyDashboardQuery } from '../api/agencyApi'

const evaluatorStatCards = [
  { key: 'queueCount', label: 'Evaluation Queue' },
  { key: 'myAssignments', label: 'My Assignments' },
  { key: 'pendingInspections', label: 'Pending Inspections' },
  { key: 'openBillings', label: 'Open Billings' },
  { key: 'pendingAccreditation', label: 'Pending Accreditation' },
]

const agencyAdminStatCards = [
  { key: 'queueCount', label: 'Evaluation Queue' },
  { key: 'pendingInspections', label: 'Pending Inspections' },
  { key: 'pendingAccreditation', label: 'Pending Accreditation' },
]

const billingAgentStatCards = [
  { key: 'awaitingBilling', label: 'Awaiting Billing' },
  { key: 'openBillings', label: 'Open Billings' },
  { key: 'pendingCashPayments', label: 'Pending Cash (OR)' },
  { key: 'paidBillings', label: 'Paid Billings' },
]

const billingAgentRoleCodes = ['ROLE_BILLING_AGENT', 'ROLE_ACCOUNTANT']

function isAgencyAdminOnly(roles: string[]) {
  const operationalRoles = [
    'ROLE_EVALUATOR',
    'ROLE_ACCOUNTANT',
    'ROLE_SECRETARY',
    'ROLE_UNDERSECRETARY',
    'ROLE_BILLING_AGENT',
    'ROLE_INSPECTOR',
    'ROLE_ACCREDITATION_OFFICER',
  ]
  return (
    roles.includes('ROLE_AGENCY_ADMIN') &&
    !roles.includes('ROLE_ADMIN') &&
    !roles.some((role) => operationalRoles.includes(role))
  )
}

function isBillingAgentOnly(roles: string[]) {
  const otherOperationalRoles = [
    'ROLE_EVALUATOR',
    'ROLE_SECRETARY',
    'ROLE_UNDERSECRETARY',
    'ROLE_INSPECTOR',
    'ROLE_ACCREDITATION_OFFICER',
  ]
  return (
    roles.some((role) => billingAgentRoleCodes.includes(role)) &&
    !roles.includes('ROLE_ADMIN') &&
    !roles.includes('ROLE_AGENCY_ADMIN') &&
    !roles.some((role) => otherOperationalRoles.includes(role))
  )
}

export function AgencyDashboardPage() {
  const user = useAppSelector(selectCurrentUser)
  const roles = user?.roles ?? []
  const { data, isLoading } = useGetAgencyDashboardQuery()
  const stats = data?.data
  const statCards = isBillingAgentOnly(roles)
    ? billingAgentStatCards
    : isAgencyAdminOnly(roles)
      ? agencyAdminStatCards
      : evaluatorStatCards

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Overview"
        title="Agency Dashboard"
        subtitle={stats ? `${stats.agencyName} (${stats.agencyCode})` : 'Agency operations overview'}
      />
      <PortalStatGrid
        items={statCards}
        stats={stats as unknown as Record<string, number | undefined>}
        isLoading={isLoading}
        columns={{ xs: '1fr 1fr', md: 'repeat(3, 1fr)' }}
      />
    </Box>
  )
}
