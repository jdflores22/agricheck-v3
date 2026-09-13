import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import PieChartOutlineOutlinedIcon from '@mui/icons-material/PieChartOutlineOutlined'
import TimelineOutlinedIcon from '@mui/icons-material/TimelineOutlined'
import { Box, Chip, CircularProgress, Grid, Stack, TableCell, TableRow, TextField, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import { DaAnalyticsKpiCard } from '../components/DaAgencyAnalyticsPanels'
import { useGetDaMavNationalReportQuery } from '../api/daApi'

function formatMt(value: number) {
  return `${value.toLocaleString(undefined, { maximumFractionDigits: 2 })} MT`
}

function SectionLabel({ children }: { children: string }) {
  return (
    <Typography sx={{ mb: 1.5, fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted }}>
      {children}
    </Typography>
  )
}

export function DaMavNationalReportPage() {
  const currentYear = new Date().getFullYear()
  const [mavYear, setMavYear] = useState(currentYear)
  const [yearInput, setYearInput] = useState(String(currentYear))
  const { data, isLoading } = useGetDaMavNationalReportQuery({ mavYear })

  const commitYear = (raw: string) => {
    const next = Number(raw)
    if (Number.isInteger(next) && next >= 1996 && next <= currentYear + 1) {
      setMavYear(next)
      setYearInput(String(next))
      return
    }
    setYearInput(String(mavYear))
  }
  const report = data?.data

  const mavUsed = report?.totalUtilizedVolume ?? 0
  const regular = report?.totalRegularVolume ?? 0
  const combined = report?.totalCombinedVolume ?? mavUsed + regular
  const utilizationRate = useMemo(() => {
    if (!report || report.totalAwardedVolume <= 0) return 0
    return Math.round((report.totalUtilizedVolume / report.totalAwardedVolume) * 100)
  }, [report])
  const mavShare = combined > 0 ? (mavUsed / combined) * 100 : 0
  const regularShare = combined > 0 ? (regular / combined) * 100 : 0

  if (isLoading || !report) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Presidential briefing"
        title="National MAV Utilization"
        subtitle="MAV in-quota utilization stays separate from regular out-quota imports. Combined volume is justified so the Secretary can see the full national picture."
      >
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mt: 1.5, alignItems: 'center' }}>
          <Chip size="small" label={`MAV ${report.mavYear}`} sx={{ bgcolor: portalAnalyticsColors.soft, color: portalAnalyticsColors.darkest, fontWeight: 700 }} />
          <Chip size="small" label={`${report.openPeriods} open period${report.openPeriods === 1 ? '' : 's'}`} variant="outlined" sx={{ fontWeight: 600 }} />
          <Chip size="small" clickable component={RouterLink} to="/da/reports/commodities" label="Commodity stock" sx={{ fontWeight: 700 }} />
          <Chip size="small" clickable component={RouterLink} to="/da/reports/stock" label="Stock map" sx={{ fontWeight: 700 }} />
          <TextField
            label="Year"
            type="number"
            size="small"
            value={yearInput}
            onChange={(e) => setYearInput(e.target.value)}
            onBlur={(e) => commitYear(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') commitYear((e.target as HTMLInputElement).value)
            }}
            sx={{ width: 120 }}
          />
        </Stack>
      </PortalPageHeader>

      <SectionLabel>MAV quota — in-quota only</SectionLabel>
      <Grid container spacing={2} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Awarded volume"
            value={formatMt(report.totalAwardedVolume)}
            meta={`${report.activeLicenses.toLocaleString()} active licenses`}
            icon={<Inventory2OutlinedIcon />}
            accent={portalAnalyticsColors.darkest}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="MAV utilized"
            value={formatMt(mavUsed)}
            meta={`${report.issuedMics.toLocaleString()} MICs issued`}
            icon={<TimelineOutlinedIcon />}
            accent={portalAnalyticsColors.dark}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Remaining MAV"
            value={formatMt(report.totalRemainingVolume)}
            meta="Unused in-quota allocation"
            icon={<PieChartOutlineOutlinedIcon />}
            accent={portalAnalyticsColors.base}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="MAV utilization rate"
            value={`${utilizationRate}%`}
            meta={`${report.approvedApplications.toLocaleString()} of ${report.totalApplications.toLocaleString()} applications approved`}
            icon={<AssessmentOutlinedIcon />}
            accent={portalAnalyticsColors.mid}
          />
        </Grid>
      </Grid>

      <SectionLabel>Regular imports — out-quota</SectionLabel>
      <Grid container spacing={2} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, sm: 6 }}>
          <DaAnalyticsKpiCard
            label="Regular import volume"
            value={formatMt(regular)}
            meta={`${(report.regularImportCount ?? 0).toLocaleString()} entries · does not reduce MAV remaining`}
            icon={<Inventory2OutlinedIcon />}
            accent={portalAnalyticsColors.light}
          />
        </Grid>
      </Grid>

      <SectionLabel>Combined national import</SectionLabel>
      <Box
        sx={{
          mb: 4,
          borderRadius: '0.875rem',
          border: `1px solid ${portalColors.border}`,
          bgcolor: portalColors.bgWhite,
          p: 2.5,
        }}
      >
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ justifyContent: 'space-between', mb: 2 }}>
          <Box>
            <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase', color: portalColors.textMuted }}>
              Total imported MAV products
            </Typography>
            <Typography sx={{ mt: 0.5, fontSize: '1.5rem', fontWeight: 800, color: portalColors.textDark }}>
              {formatMt(combined)}
            </Typography>
          </Box>
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
            <Chip size="small" label={`MAV in-quota ${formatMt(mavUsed)}`} sx={{ bgcolor: portalAnalyticsColors.softStrong, color: portalAnalyticsColors.darkest, fontWeight: 700 }} />
            <Typography sx={{ fontWeight: 700, color: portalColors.textMuted }}>+</Typography>
            <Chip size="small" label={`Regular out-quota ${formatMt(regular)}`} sx={{ bgcolor: portalColors.bgMuted, color: portalColors.textDark, fontWeight: 700 }} />
            <Typography sx={{ fontWeight: 700, color: portalColors.textMuted }}>=</Typography>
            <Chip size="small" label={`Total ${formatMt(combined)}`} sx={{ bgcolor: portalAnalyticsColors.darkest, color: '#fff', fontWeight: 700 }} />
          </Stack>
        </Stack>
        <Box sx={{ height: 14, borderRadius: 999, overflow: 'hidden', bgcolor: portalAnalyticsColors.track, display: 'flex' }}>
          <Box sx={{ width: `${mavShare}%`, bgcolor: portalAnalyticsColors.darkest }} />
          <Box sx={{ width: `${regularShare}%`, bgcolor: portalAnalyticsColors.pale }} />
        </Box>
        <Typography sx={{ mt: 1.25, fontSize: '0.8125rem', color: portalColors.textMuted, lineHeight: 1.55 }}>
          Regular volume is identified separately so the combined total is justified. Only MAV in-quota reduces remaining allocation.
        </Typography>
      </Box>

      <Box sx={{ mb: 3 }}>
        <PortalTablePanel
          title="By agency"
          columns={['Agency', 'Licenses', 'Awarded', 'MAV used', 'Regular', 'Total imported', 'Remaining MAV']}
          isLoading={false}
          isEmpty={report.byAgency.length === 0}
          emptyMessage="No agency MAV licenses or regular imports for this year yet."
        >
          {report.byAgency.map((row) => (
            <TableRow key={row.agencyCode} hover>
              <TableCell>{row.agencyCode}</TableCell>
              <TableCell>{row.activeLicenses}</TableCell>
              <TableCell>{row.awardedVolume.toLocaleString()}</TableCell>
              <TableCell>{row.utilizedVolume.toLocaleString()}</TableCell>
              <TableCell>{(row.regularVolume ?? 0).toLocaleString()}</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>{(row.combinedVolume ?? row.utilizedVolume).toLocaleString()}</TableCell>
              <TableCell>{row.remainingVolume.toLocaleString()}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <PortalTablePanel
        title="By commodity"
        columns={['Agency', 'HS Code', 'Commodity', 'Apps', 'Licenses', 'Awarded', 'MAV used', 'Regular', 'Total imported', 'Remaining MAV']}
        isLoading={false}
        isEmpty={report.byCommodity.length === 0}
        emptyMessage="No commodity utilization yet."
      >
        {report.byCommodity.map((row) => (
          <TableRow key={`${row.agencyCode ?? 'none'}-${row.hsCode || 'no-hs'}-${row.commodityName}`} hover>
            <TableCell>{row.agencyCode ?? '—'}</TableCell>
            <TableCell>{row.hsCode || '—'}</TableCell>
            <TableCell>{row.commodityName}</TableCell>
            <TableCell>{row.applicationCount}</TableCell>
            <TableCell>{row.activeLicenses}</TableCell>
            <TableCell>{row.awardedVolume.toLocaleString()}</TableCell>
            <TableCell>{row.utilizedVolume.toLocaleString()}</TableCell>
            <TableCell>{(row.regularVolume ?? 0).toLocaleString()}</TableCell>
            <TableCell sx={{ fontWeight: 700 }}>{(row.combinedVolume ?? row.utilizedVolume).toLocaleString()}</TableCell>
            <TableCell>{row.remainingVolume.toLocaleString()}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
