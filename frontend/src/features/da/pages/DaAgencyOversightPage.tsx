import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import AttachMoneyOutlinedIcon from '@mui/icons-material/AttachMoneyOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import InboxOutlinedIcon from '@mui/icons-material/InboxOutlined'
import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined'
import PendingActionsOutlinedIcon from '@mui/icons-material/PendingActionsOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Grid,
  Stack,
  TableCell,
  TableRow,
  Typography,
} from '@mui/material'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { resolveAgencyLogoUrl } from '../../admin/components/adminAgencyUtils'
import { DaAgencyOversightSidebar } from '../components/DaAgencyOversightSidebar'
import {
  AnalyticsSection,
  BillingSplitPanel,
  ChildAgencyComparison,
  EntryStageBars,
  OperationsMetrics,
} from '../components/DaAgencyAnalyticsPanels'
import { useGetDaAgencyOversightQuery } from '../api/daApi'

export function DaAgencyOversightPage() {
  const { id = '' } = useParams()
  const agencyId = Number(id)
  const { data, isLoading, error } = useGetDaAgencyOversightQuery(agencyId, { skip: !agencyId })
  const oversight = data?.data
  useBreadcrumbLabel(oversight?.agency.name)

  if (!agencyId || Number.isNaN(agencyId)) {
    return <Alert severity="error">Invalid agency id.</Alert>
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  if (error || !oversight) {
    return <Alert severity="error">Agency oversight data not found.</Alert>
  }

  const { agency, report, isParentAgency, parentAgency, childAgencies, activeUsers, openBillings, recentEntries } = oversight
  const backHref = parentAgency ? `/da/agencies/${parentAgency.id}` : '/da/agencies'
  const logoUrl = resolveAgencyLogoUrl(agency.logoUrl)
  const pendingEntries = report.submittedEntries + report.underReviewEntries

  return (
    <Grid container spacing={3}>
      <Grid size={{ xs: 12, lg: 4 }} sx={{ order: { xs: 1, lg: 1 } }}>
        <Box sx={{ position: { lg: 'sticky' }, top: { lg: 24 } }}>
          <DaAgencyOversightSidebar
            agency={agency}
            isParentAgency={isParentAgency}
            parentAgency={parentAgency}
            childAgencyCount={childAgencies.length}
            activeUsers={activeUsers}
            openBillings={openBillings}
            pendingEntries={pendingEntries}
            totalEntries={report.totalEntries}
            secretaryLabel={isParentAgency ? 'DA Secretary' : 'Secretary'}
            undersecretaryLabel={isParentAgency ? 'DA Undersecretaries' : 'Undersecretaries'}
          />
        </Box>
      </Grid>

      <Grid size={{ xs: 12, lg: 8 }} sx={{ order: { xs: 0, lg: 2 } }}>
        <PortalPageHeader
          eyebrow={isParentAgency ? 'Department Oversight' : 'Agency Oversight'}
          title={agency.name}
          subtitle={
            isParentAgency
              ? 'Leadership profile and consolidated view — drill into attached bureaus below.'
              : `Operational oversight for this bureau under ${parentAgency?.code ?? 'DA'}.`
          }
          actions={
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
              <Button
                component={RouterLink}
                to="/da/reports"
                variant="outlined"
                endIcon={<OpenInNewOutlinedIcon />}
                sx={portalOutlinedButtonSx}
              >
                Full reports
              </Button>
              <Button
                component={RouterLink}
                to={backHref}
                variant="outlined"
                startIcon={<ArrowBackIcon />}
                sx={portalOutlinedButtonSx}
              >
                {parentAgency ? `Back to ${parentAgency.code}` : 'Back to agencies'}
              </Button>
            </Stack>
          }
        >
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mt: 1.5 }}>
            {logoUrl ? (
              <Box component="img" src={logoUrl} alt={agency.name} sx={{ width: 32, height: 32, borderRadius: '0.5rem', border: `1px solid ${portalColors.border}`, objectFit: 'contain', p: 0.25 }} />
            ) : null}
            <Chip size="small" label={agency.code} sx={{ fontWeight: 700 }} />
            <Chip size="small" label={isParentAgency ? 'Network scope' : 'Single agency'} variant="outlined" sx={{ fontWeight: 600 }} />
          </Stack>
        </PortalPageHeader>

        {isParentAgency && childAgencies.length > 0 ? (
          <Box sx={{ mb: 3 }}>
            <AnalyticsSection
              title="Attached bureaus"
              subtitle="Select an agency to view its scoped oversight"
              icon={<BusinessOutlinedIcon />}
            >
              <ChildAgencyComparison agencies={childAgencies} />
            </AnalyticsSection>
          </Box>
        ) : null}

        <Box sx={{ mb: 3 }}>
          <AnalyticsSection
            title="Entry activity"
            subtitle={isParentAgency ? 'Consolidated pipeline across all attached agencies' : 'Processing stages for this agency'}
            icon={<AssessmentOutlinedIcon />}
          >
            <EntryStageBars report={report} />
          </AnalyticsSection>
        </Box>

        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid size={{ xs: 12, md: 6 }}>
            <AnalyticsSection title="Operations" subtitle="Inspections and accreditation" icon={<PendingActionsOutlinedIcon />}>
              <OperationsMetrics report={report} />
            </AnalyticsSection>
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <AnalyticsSection title="Billing" subtitle="Issued, paid, and outstanding" icon={<AttachMoneyOutlinedIcon />}>
              <BillingSplitPanel report={report} />
            </AnalyticsSection>
          </Grid>
        </Grid>

        {recentEntries.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 6, borderRadius: '0.875rem', border: `1px dashed ${portalColors.border}`, bgcolor: portalColors.bgWhite }}>
            <InboxOutlinedIcon sx={{ fontSize: 44, color: portalColors.textMuted, mb: 1.5 }} />
            <Typography sx={{ fontWeight: 600, mb: 0.5 }}>No recent entries</Typography>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
              Entry activity for this scope will appear here.
            </Typography>
          </Box>
        ) : (
          <PortalTablePanel
            title="Recent entries"
            columns={['Reference', 'Status', 'Submitted by', 'Created']}
            isLoading={false}
            isEmpty={false}
          >
            {recentEntries.map((entry) => (
              <TableRow key={entry.uuid} hover>
                <TableCell sx={{ fontWeight: 600 }}>{entry.referenceNo}</TableCell>
                <TableCell>
                  <Chip size="small" label={entry.status} sx={portalStatusChipSx(entry.status)} />
                </TableCell>
                <TableCell>{entry.submitterName ?? '—'}</TableCell>
                <TableCell>{new Date(entry.createdAt).toLocaleString()}</TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
        )}

        <Box
          sx={{
            mt: 3,
            p: 2,
            borderRadius: '0.75rem',
            bgcolor: portalAnalyticsColors.soft,
            border: `1px solid ${portalAnalyticsColors.softStrong}`,
          }}
        >
          <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
            Need charts, approval rates, and full metric breakdown? Open the{' '}
            <Typography component={RouterLink} to="/da/reports" sx={{ color: portalAnalyticsColors.dark, fontWeight: 600, textDecoration: 'none' }}>
              department reports
            </Typography>{' '}
            page for network-wide analytics.
          </Typography>
        </Box>
      </Grid>
    </Grid>
  )
}
