import { Box } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { useGetDriverDashboardQuery } from '../api/opsApi'

const statCards = [
  { key: 'assignedContainers', label: 'Assigned Containers' },
  { key: 'inTransitContainers', label: 'In Transit' },
  { key: 'profileCompletion', label: 'Profile Completion %' },
] as const

export function DriverDashboardPage() {
  const { data, isLoading } = useGetDriverDashboardQuery()
  const stats = data?.data

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Overview"
        title="Driver Dashboard"
        subtitle="Your assigned containers and profile status."
      />
      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, 1fr)' },
        }}
      >
        {statCards.map((card) => (
          <PortalStatCard
            key={card.key}
            label={card.label}
            value={isLoading ? '…' : (stats?.[card.key] ?? 0)}
          />
        ))}
        <PortalStatCard
          label="Face Verified"
          value={isLoading ? '…' : (stats?.faceVerified ? 'Yes' : 'No')}
        />
      </Box>
    </Box>
  )
}
