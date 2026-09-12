import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import ReportOutlinedIcon from '@mui/icons-material/ReportOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import { Box, CircularProgress, Grid, Stack, Typography } from '@mui/material'
import { useMemo } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import { AgencyPreviewList, AttentionPanel, NetworkHealthStrip, QuickLinkCard } from '../components/DaDashboardWidgets'
import { useGetDaAgenciesQuery, useGetDaDashboardQuery } from '../api/daApi'
import { partitionDaAgencies } from '../utils/daAgencyUtils'

export function DaDashboardPage() {
  const { data: dashboardData, isLoading: dashboardLoading } = useGetDaDashboardQuery()
  const { data: agenciesData, isLoading: agenciesLoading } = useGetDaAgenciesQuery()

  const dashboard = dashboardData?.data
  const { daAgency, attachedAgencies } = useMemo(
    () => partitionDaAgencies(agenciesData?.data ?? []),
    [agenciesData?.data],
  )

  if (dashboardLoading || agenciesLoading || !dashboard) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Department of Agriculture"
        title="DA Leadership Dashboard"
        subtitle="Your command center — monitor network health, jump to agencies, and spot items that need attention."
      />

      <Box sx={{ mb: 3 }}>
        <NetworkHealthStrip dashboard={dashboard} />
      </Box>

      <Box sx={{ mb: 3 }}>
        <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.12em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 1.5 }}>
          Quick access
        </Typography>
        <Grid container spacing={1.5}>
          <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
            <QuickLinkCard to="/da/agencies" icon={BusinessOutlinedIcon} label="Agencies" description="Browse bureaus and open agency oversight views." />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
            <QuickLinkCard to="/da/reports" icon={ReportOutlinedIcon} label="Reports" description="Full analytics, charts, and detailed network metrics." />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
            <QuickLinkCard to="/da/warehouses" icon={WarehouseOutlinedIcon} label="Warehouses" description="Registry, capacity, and inventory oversight." />
          </Grid>
          <Grid size={{ xs: 12, sm: 6, lg: 3 }}>
            <QuickLinkCard
              to={daAgency ? `/da/agencies/${daAgency.id}` : '/da/agencies'}
              icon={AssessmentOutlinedIcon}
              label="DA oversight"
              description="Consolidated department view with attached agency drill-down."
            />
          </Grid>
        </Grid>
      </Box>

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Box
            sx={{
              borderRadius: '0.875rem',
              border: `1px solid ${portalColors.border}`,
              bgcolor: portalColors.bgWhite,
              p: 2.5,
              height: '100%',
            }}
          >
            <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between', gap: 2, mb: 2 }}>
              <Box>
                <Typography sx={{ fontWeight: 700 }}>Attached agencies</Typography>
                <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                  Sorted by pending workload — open an agency for scoped oversight.
                </Typography>
              </Box>
              <Typography
                component={RouterLink}
                to="/da/agencies"
                sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalAnalyticsColors.dark, textDecoration: 'none' }}
              >
                View all
              </Typography>
            </Stack>
            {attachedAgencies.length > 0 ? (
              <AgencyPreviewList agencies={attachedAgencies} />
            ) : (
              <Typography sx={{ color: portalColors.textMuted, fontStyle: 'italic' }}>No attached agencies found.</Typography>
            )}
          </Box>
        </Grid>

        <Grid size={{ xs: 12, lg: 4 }}>
          <AttentionPanel dashboard={dashboard} />
        </Grid>
      </Grid>
    </Box>
  )
}
