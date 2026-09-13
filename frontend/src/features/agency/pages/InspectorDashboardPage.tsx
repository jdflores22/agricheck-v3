import { Box, Button, Stack } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetAgencyDashboardQuery, useGetContainerInspectionQueueQuery } from '../api/agencyApi'

export function InspectorDashboardPage() {
  const { data, isLoading } = useGetAgencyDashboardQuery()
  const { data: myAssignmentsData } = useGetContainerInspectionQueueQuery({ scope: 'mine' })
  const stats = data?.data
  const myActiveCount = myAssignmentsData?.data?.totalCount ?? stats?.myContainerInspectionAssignments ?? 0

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Overview"
        title="Inspector Dashboard"
        subtitle={stats ? `${stats.agencyName} (${stats.agencyCode})` : 'Container inspection operations'}
        actions={
          <Stack direction="row" spacing={1}>
            <Button component={RouterLink} to="inspections" variant="outlined" sx={portalOutlinedButtonSx}>
              View queue
            </Button>
            {myActiveCount > 0 ? (
              <Button component={RouterLink} to="inspections" variant="contained" sx={portalPrimaryButtonSx}>
                My assignments ({myActiveCount})
              </Button>
            ) : null}
          </Stack>
        }
      />

      <Box
        sx={{
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, 1fr)' },
        }}
      >
        <PortalStatCard
          label="Unclaimed containers"
          value={isLoading ? '…' : (stats?.containerInspectionQueue ?? 0)}
        />
        <PortalStatCard
          label="My assignments"
          value={isLoading ? '…' : (stats?.myContainerInspectionAssignments ?? 0)}
        />
        <PortalStatCard
          label="Scheduled inspections"
          value={isLoading ? '…' : (stats?.pendingInspections ?? 0)}
        />
        <PortalStatCard
          label="Agency"
          value={isLoading ? '…' : (stats?.agencyCode ?? '—')}
        />
      </Box>
    </Box>
  )
}
