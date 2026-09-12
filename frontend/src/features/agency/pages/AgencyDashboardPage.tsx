import { Box } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { useGetAgencyDashboardQuery } from '../api/agencyApi'

const statCards = [
  { key: 'queueCount', label: 'Evaluation Queue' },
  { key: 'myAssignments', label: 'My Assignments' },
  { key: 'pendingInspections', label: 'Pending Inspections' },
  { key: 'openBillings', label: 'Open Billings' },
  { key: 'pendingAccreditation', label: 'Pending Accreditation' },
]

export function AgencyDashboardPage() {
  const { data, isLoading } = useGetAgencyDashboardQuery()
  const stats = data?.data

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
