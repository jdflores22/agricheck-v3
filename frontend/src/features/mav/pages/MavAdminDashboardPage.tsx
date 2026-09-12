import { Box } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { useGetMavAdminDashboardQuery } from '../api/mavApi'

const statCards = [
  { key: 'openPeriods', label: 'Open Periods' },
  { key: 'pendingApplications', label: 'Pending Applications' },
  { key: 'activeLicenses', label: 'Active Licenses' },
  { key: 'activeMics', label: 'Active MICs' },
  { key: 'totalApplications', label: 'Total Applications' },
  { key: 'complianceAlerts', label: 'Compliance Alerts' },
]

export function MavAdminDashboardPage() {
  const { data, isLoading } = useGetMavAdminDashboardQuery()
  const stats = data?.data

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Overview"
        title="MAV Admin Dashboard"
        subtitle="Administrative overview of MAV program activity."
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
