import { Box } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { useGetMavImporterDashboardQuery } from '../api/mavApi'

const statCards = [
  { key: 'openPeriods', label: 'Open Periods' },
  { key: 'myApplications', label: 'My Applications' },
  { key: 'pendingApplications', label: 'Pending Review' },
  { key: 'activeLicenses', label: 'Active Licenses' },
  { key: 'activeMics', label: 'Active MICs' },
  { key: 'totalAvailableVolume', label: 'Available Volume (MT)' },
]

export function MavDashboardPage() {
  const { data, isLoading } = useGetMavImporterDashboardQuery()
  const stats = data?.data

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Overview"
        title="MAV Dashboard"
        subtitle="Minimum Access Volume applications and licenses."
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
