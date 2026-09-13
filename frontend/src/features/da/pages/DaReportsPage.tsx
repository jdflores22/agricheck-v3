import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import AttachMoneyOutlinedIcon from '@mui/icons-material/AttachMoneyOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import CheckCircleOutlineOutlinedIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import InsightsOutlinedIcon from '@mui/icons-material/InsightsOutlined'
import PendingActionsOutlinedIcon from '@mui/icons-material/PendingActionsOutlined'
import TimelineOutlinedIcon from '@mui/icons-material/TimelineOutlined'
import type { ReactNode } from 'react'
import { Box, Chip, CircularProgress, Divider, Grid, Stack, Typography } from '@mui/material'
import { useMemo } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import {
  AnalyticsSection,
  ApprovalRateRing,
  BillingSplitPanel,
  ChildAgencyComparison,
  DaAnalyticsKpiCard,
  EntryStageBars,
  MetricGroup,
  OperationsMetrics,
} from '../components/DaAgencyAnalyticsPanels'
import { useGetDaAgenciesQuery, useGetDaDashboardQuery, useGetDaReportQuery } from '../api/daApi'
import { partitionDaAgencies } from '../utils/daAgencyUtils'

const entryMetrics = [
  { key: 'totalEntries', label: 'Total Entries' },
  { key: 'submittedEntries', label: 'Submitted' },
  { key: 'underReviewEntries', label: 'Under Review' },
  { key: 'approvedEntries', label: 'Approved' },
  { key: 'rejectedEntries', label: 'Rejected' },
] as const

const opsMetrics = [
  { key: 'completedInspections', label: 'Completed Inspections' },
  { key: 'pendingAccreditation', label: 'Pending Accreditation' },
] as const

const billingMetrics = [
  { key: 'issuedBillings', label: 'Issued Billings' },
  { key: 'paidBillings', label: 'Paid Billings' },
] as const

function ReportSectionLabel({ children }: { children: ReactNode }) {
  return (
    <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 2 }}>
      <Box sx={{ width: 4, height: 28, borderRadius: 999, bgcolor: portalAnalyticsColors.dark }} />
      <Typography sx={{ fontSize: '1.125rem', fontWeight: 700, color: portalAnalyticsColors.darkest }}>{children}</Typography>
    </Stack>
  )
}

export function DaReportsPage() {
  const { data: reportData, isLoading: reportLoading } = useGetDaReportQuery()
  const { data: dashboardData, isLoading: dashboardLoading } = useGetDaDashboardQuery()
  const { data: agenciesData, isLoading: agenciesLoading } = useGetDaAgenciesQuery()

  const report = reportData?.data
  const dashboard = dashboardData?.data
  const { attachedAgencies } = useMemo(() => partitionDaAgencies(agenciesData?.data ?? []), [agenciesData?.data])

  const isLoading = reportLoading || dashboardLoading || agenciesLoading

  if (isLoading || !report) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  const pendingEntries = report.submittedEntries + report.underReviewEntries
  const decided = report.approvedEntries + report.rejectedEntries
  const approvalRate = decided > 0 ? Math.round((report.approvedEntries / decided) * 100) : 0
  const stats = report as unknown as Record<string, number | undefined>
  const openBillings = dashboard?.openBillings ?? Math.max(report.issuedBillings - report.paidBillings, 0)

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Analytics & Reporting"
        title="Department Oversight Reports"
        subtitle="Full-width analytics report — entry pipeline, billing, operations, and agency comparison across the DA network."
      >
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mt: 1.5 }}>
          <Chip size="small" label="Full report" sx={{ bgcolor: portalAnalyticsColors.soft, color: portalAnalyticsColors.darkest, fontWeight: 700 }} />
          <Chip size="small" label="Network scope" variant="outlined" sx={{ fontWeight: 600 }} />
          <Chip size="small" label={`${attachedAgencies.length} bureaus`} sx={{ fontWeight: 600 }} />
          <Chip
            size="small"
            clickable
            component={RouterLink}
            to="/da/reports/mav"
            label="National MAV utilization"
            sx={{ fontWeight: 700, bgcolor: portalAnalyticsColors.soft, color: portalAnalyticsColors.darkest }}
          />
          <Chip
            size="small"
            clickable
            component={RouterLink}
            to="/da/reports/commodities"
            label="Commodity stock"
            sx={{ fontWeight: 700, bgcolor: portalAnalyticsColors.soft, color: portalAnalyticsColors.darkest }}
          />
          <Chip
            size="small"
            clickable
            component={RouterLink}
            to="/da/reports/stock"
            label="Stock map"
            sx={{ fontWeight: 700 }}
          />
        </Stack>
      </PortalPageHeader>

      <ReportSectionLabel>Summary</ReportSectionLabel>
      <Grid container spacing={2} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Total Entries"
            value={report.totalEntries.toLocaleString()}
            meta={`${pendingEntries.toLocaleString()} in pipeline`}
            icon={<TimelineOutlinedIcon />}
            accent={portalAnalyticsColors.darkest}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Approval Rate"
            value={`${approvalRate}%`}
            meta={`${report.approvedEntries.toLocaleString()} of ${decided.toLocaleString()} decided`}
            icon={<CheckCircleOutlineOutlinedIcon />}
            accent={portalAnalyticsColors.dark}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Active Agencies"
            value={(dashboard?.activeAgencies ?? attachedAgencies.filter((a) => a.isActive).length).toLocaleString()}
            meta={`${dashboard?.totalAgencies ?? attachedAgencies.length + 1} in network`}
            icon={<BusinessOutlinedIcon />}
            accent={portalAnalyticsColors.base}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Open Billings"
            value={openBillings.toLocaleString()}
            meta={`${report.paidBillings.toLocaleString()} collected`}
            icon={<AttachMoneyOutlinedIcon />}
            accent={portalAnalyticsColors.mid}
          />
        </Grid>
      </Grid>

      <Divider sx={{ mb: 4 }} />

      <ReportSectionLabel>Entry analysis</ReportSectionLabel>
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, lg: 7 }}>
          <AnalyticsSection title="Entry pipeline" subtitle="Volume by processing stage" icon={<AssessmentOutlinedIcon />}>
            <EntryStageBars report={report} />
          </AnalyticsSection>
        </Grid>
        <Grid size={{ xs: 12, lg: 5 }}>
          <AnalyticsSection title="Decision outcomes" subtitle="Approved vs rejected" icon={<InsightsOutlinedIcon />}>
            <ApprovalRateRing report={report} />
          </AnalyticsSection>
        </Grid>
      </Grid>

      <Divider sx={{ mb: 4 }} />

      <ReportSectionLabel>Financial & operations</ReportSectionLabel>
      <Grid container spacing={3} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, lg: 6 }}>
          <AnalyticsSection title="Billing performance" subtitle="Issued, paid, and outstanding" icon={<AttachMoneyOutlinedIcon />}>
            <BillingSplitPanel report={report} />
          </AnalyticsSection>
        </Grid>
        <Grid size={{ xs: 12, lg: 6 }}>
          <AnalyticsSection title="Operations" subtitle="Inspections and accreditation workload" icon={<PendingActionsOutlinedIcon />}>
            <OperationsMetrics report={report} />
          </AnalyticsSection>
        </Grid>
      </Grid>

      {attachedAgencies.length > 0 ? (
        <>
          <Divider sx={{ mb: 4 }} />
          <ReportSectionLabel>Agency comparison</ReportSectionLabel>
          <Box sx={{ mb: 4 }}>
            <AnalyticsSection
              title="Entry volume by bureau"
              subtitle="Click any agency to open its scoped oversight view"
              icon={<BusinessOutlinedIcon />}
            >
              <ChildAgencyComparison agencies={attachedAgencies} />
            </AnalyticsSection>
          </Box>
        </>
      ) : null}

      <Divider sx={{ mb: 4 }} />

      <ReportSectionLabel>Detailed breakdown</ReportSectionLabel>
      <AnalyticsSection title="All metrics" subtitle="Complete network statistics" icon={<TimelineOutlinedIcon />}>
        <Stack spacing={3}>
          <MetricGroup title="Entries" items={entryMetrics} stats={stats} />
          <MetricGroup title="Operations" items={opsMetrics} stats={stats} />
          <MetricGroup title="Billing" items={billingMetrics} stats={stats} />
        </Stack>
      </AnalyticsSection>
    </Box>
  )
}
