import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import { Box, Button, Grid, Skeleton, Stack, Typography } from '@mui/material'
import { useMemo } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { DaAttachedAgencyCard, DaParentAgencyCard } from '../components/DaAgencyCards'
import { useGetDaAgenciesQuery } from '../api/daApi'
import { partitionDaAgencies, summarizeAttachedAgencies } from '../utils/daAgencyUtils'

function AgencyCardSkeleton() {
  return (
    <Box sx={{ borderRadius: '0.875rem', border: `1px solid ${portalColors.border}`, bgcolor: portalColors.bgWhite, p: 2.25 }}>
      <Stack direction="row" spacing={1.5} sx={{ mb: 2 }}>
        <Skeleton variant="rounded" width={52} height={52} />
        <Box sx={{ flex: 1 }}>
          <Skeleton width="30%" />
          <Skeleton width="80%" />
        </Box>
      </Stack>
      <Skeleton height={48} />
    </Box>
  )
}

export function DaAgenciesPage() {
  const { data, isLoading } = useGetDaAgenciesQuery()
  const agencies = data?.data ?? []

  const { daAgency, attachedAgencies } = useMemo(() => partitionDaAgencies(agencies), [agencies])
  const networkTotals = useMemo(() => summarizeAttachedAgencies(attachedAgencies), [attachedAgencies])

  const stats = {
    attachedAgencies: attachedAgencies.length,
    activeAgencies: attachedAgencies.filter((agency) => agency.isActive).length,
    totalEntries: networkTotals.totalEntries,
    pendingEntries: networkTotals.pendingEntries,
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Oversight"
        title="Agency Network"
        subtitle="Department of Agriculture at the top level, with attached bureaus and agencies under its oversight."
        actions={
          <Button
            component={RouterLink}
            to="/da/warehouses"
            variant="outlined"
            startIcon={<WarehouseOutlinedIcon />}
            sx={portalOutlinedButtonSx}
          >
            Manage Warehouses
          </Button>
        }
      />

      <PortalStatGrid
        items={[
          { key: 'attachedAgencies', label: 'Attached Agencies' },
          { key: 'activeAgencies', label: 'Active Agencies' },
          { key: 'totalEntries', label: 'Network Entries' },
          { key: 'pendingEntries', label: 'Pending Entries' },
        ]}
        stats={stats}
        isLoading={isLoading}
        columns={{ xs: '1fr 1fr', md: 'repeat(4, 1fr)' }}
      />

      <Box sx={{ mt: 3 }}>
        {isLoading ? (
          <Skeleton variant="rounded" height={220} sx={{ borderRadius: '0.875rem' }} />
        ) : daAgency ? (
          <DaParentAgencyCard
            agency={daAgency}
            attachedCount={attachedAgencies.length}
            networkTotals={networkTotals}
            oversightHref={`/da/agencies/${daAgency.id}`}
          />
        ) : null}
      </Box>

      <Box sx={{ mt: 4 }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 2 }}>
          <Box sx={{ width: 4, height: 24, borderRadius: 999, bgcolor: portalColors.primary }} />
          <Box>
            <Typography sx={{ fontWeight: 700, color: portalColors.textDark }}>Attached Agencies</Typography>
            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
              Bureaus and units operating under the Department of Agriculture.
            </Typography>
          </Box>
        </Stack>

        {isLoading ? (
          <Grid container spacing={2}>
            {Array.from({ length: 6 }).map((_, index) => (
              <Grid key={index} size={{ xs: 12, sm: 6, lg: 4 }}>
                <AgencyCardSkeleton />
              </Grid>
            ))}
          </Grid>
        ) : attachedAgencies.length === 0 ? (
          <Box
            sx={{
              textAlign: 'center',
              py: 6,
              borderRadius: '0.875rem',
              border: `1px dashed ${portalColors.border}`,
              bgcolor: portalColors.bgWhite,
            }}
          >
            <Typography sx={{ fontWeight: 600, mb: 0.5 }}>No attached agencies found</Typography>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
              Attached agencies will appear here once registered under the DA network.
            </Typography>
          </Box>
        ) : (
          <Grid container spacing={2}>
            {attachedAgencies.map((agency) => (
              <Grid key={agency.id} size={{ xs: 12, sm: 6, lg: 4 }}>
                <DaAttachedAgencyCard agency={agency} to={`/da/agencies/${agency.id}`} />
              </Grid>
            ))}
          </Grid>
        )}
      </Box>
    </Box>
  )
}
