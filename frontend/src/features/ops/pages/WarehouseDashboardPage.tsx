import { Box } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { useGetWarehouseDashboardQuery } from '../api/opsApi'

const statCards = [
  { key: 'storedContainers', label: 'Stored Containers' },
  { key: 'pendingReleases', label: 'Pending Releases' },
  { key: 'releasedToday', label: 'Released Today' },
  { key: 'activeFacilities', label: 'Active Facilities' },
]

export function WarehouseDashboardPage() {
  const { data, isLoading } = useGetWarehouseDashboardQuery()
  const stats = data?.data

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Overview"
        title="Warehouse Dashboard"
        subtitle="Container storage and release operations."
      />
      <PortalStatGrid
        items={statCards}
        stats={stats as unknown as Record<string, number | undefined>}
        isLoading={isLoading}
        columns={{ xs: '1fr 1fr', md: 'repeat(4, 1fr)' }}
      />
    </Box>
  )
}
