import { Box } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { useGetAgencyDashboardQuery } from '../../agency/api/agencyApi'

export function InspectorDashboardPage() {
  const { data, isLoading } = useGetAgencyDashboardQuery()
  const stats = data?.data

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Overview"
        title="Inspector Dashboard"
        subtitle={stats ? `${stats.agencyName} (${stats.agencyCode})` : 'Inspection operations overview'}
      />
      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(2, 1fr)' },
        }}
      >
        <PortalStatCard
          label="Pending Inspections"
          value={isLoading ? '…' : (stats?.pendingInspections ?? 0)}
        />
        <PortalStatCard
          label="Agency"
          value={isLoading ? '…' : (stats?.agencyName ?? '—')}
        />
      </Box>
    </Box>
  )
}
