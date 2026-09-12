import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import type { ReactNode } from 'react'
import { Box, Chip, Grid, LinearProgress, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import type { DaAgencySummary, DaOversightReport } from '../api/daApi'

export function AnalyticsSection({
  title,
  icon,
  subtitle,
  action,
  children,
}: {
  title: string
  icon: ReactNode
  subtitle?: string
  action?: ReactNode
  children: ReactNode
}) {
  return (
    <Box
      sx={{
        borderRadius: '0.875rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
        overflow: 'hidden',
        height: '100%',
      }}
    >
      <Box
        sx={{
          px: 2.5,
          py: 2,
          borderBottom: `1px solid ${portalColors.border}`,
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          gap: 2,
        }}
      >
        <Box>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: subtitle ? 0.5 : 0 }}>
            <Box sx={{ color: portalColors.primary, display: 'flex', '& .MuiSvgIcon-root': { fontSize: 20 } }}>{icon}</Box>
            <Typography sx={{ fontWeight: 700, color: portalColors.textDark }}>{title}</Typography>
          </Stack>
          {subtitle ? (
            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, pl: 3.5 }}>{subtitle}</Typography>
          ) : null}
        </Box>
        {action}
      </Box>
      <Box sx={{ p: 2.5 }}>{children}</Box>
    </Box>
  )
}

export function DaAnalyticsKpiCard({
  label,
  value,
  meta,
  icon,
  accent = portalAnalyticsColors.dark,
}: {
  label: string
  value: string | number
  meta: string
  icon: ReactNode
  accent?: string
}) {
  return (
    <Box
      sx={{
        height: '100%',
        borderRadius: '0.875rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
        p: 2.25,
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <Box sx={{ position: 'absolute', top: 0, left: 0, bottom: 0, width: 4, bgcolor: accent }} />
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start', justifyContent: 'space-between' }}>
        <Box sx={{ minWidth: 0, pl: 0.5 }}>
          <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.1em', textTransform: 'uppercase', color: portalColors.textMuted }}>
            {label}
          </Typography>
          <Typography sx={{ mt: 0.75, fontSize: '1.875rem', fontWeight: 700, lineHeight: 1.1, fontVariantNumeric: 'tabular-nums' }}>
            {value}
          </Typography>
          <Typography sx={{ mt: 1, fontSize: '0.8125rem', color: portalColors.textMuted }}>{meta}</Typography>
        </Box>
        <Box sx={{ color: accent, opacity: 0.35, '& .MuiSvgIcon-root': { fontSize: 28 } }}>{icon}</Box>
      </Stack>
    </Box>
  )
}

function pct(value: number, total: number) {
  if (total <= 0) return 0
  return Math.round((value / total) * 100)
}

export function EntryStageBars({ report }: { report: DaOversightReport }) {
  const total = Math.max(report.totalEntries, 1)
  const stages = [
    { label: 'Submitted', value: report.submittedEntries, color: portalAnalyticsColors.mid },
    { label: 'Under Review', value: report.underReviewEntries, color: portalAnalyticsColors.base },
    { label: 'Approved', value: report.approvedEntries, color: portalAnalyticsColors.dark },
    { label: 'Rejected', value: report.rejectedEntries, color: portalColors.textLight },
  ]

  return (
    <Stack spacing={2}>
      {stages.map((stage) => (
        <Box key={stage.label}>
          <Stack direction="row" sx={{ justifyContent: 'space-between', mb: 0.75 }}>
            <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>{stage.label}</Typography>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, fontVariantNumeric: 'tabular-nums' }}>
              {stage.value.toLocaleString()} · {pct(stage.value, total)}%
            </Typography>
          </Stack>
          <LinearProgress
            variant="determinate"
            value={Math.min(100, (stage.value / total) * 100)}
            sx={{
              height: 10,
              borderRadius: 999,
              bgcolor: portalColors.bgMuted,
              '& .MuiLinearProgress-bar': { borderRadius: 999, bgcolor: stage.color },
            }}
          />
        </Box>
      ))}
    </Stack>
  )
}

export function ApprovalRateRing({ report }: { report: DaOversightReport }) {
  const decided = report.approvedEntries + report.rejectedEntries
  const rate = decided > 0 ? Math.round((report.approvedEntries / decided) * 100) : 0
  const ringBackground =
    decided > 0
      ? `conic-gradient(${portalAnalyticsColors.dark} 0 ${rate}%, ${portalAnalyticsColors.track} ${rate}% 100%)`
      : portalAnalyticsColors.track

  return (
    <Stack spacing={2} sx={{ alignItems: 'center', textAlign: 'center', py: 1 }}>
      <Box
        sx={{
          width: 132,
          height: 132,
          borderRadius: '50%',
          background: ringBackground,
          display: 'grid',
          placeItems: 'center',
          position: 'relative',
        }}
      >
        <Box
          sx={{
            width: 92,
            height: 92,
            borderRadius: '50%',
            bgcolor: portalColors.bgWhite,
            display: 'grid',
            placeItems: 'center',
          }}
        >
          <Typography sx={{ fontSize: '1.5rem', fontWeight: 700, color: portalAnalyticsColors.dark }}>{rate}%</Typography>
        </Box>
      </Box>
      <Box>
        <Typography sx={{ fontWeight: 700 }}>Approval rate</Typography>
        <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
          {report.approvedEntries.toLocaleString()} approved of {decided.toLocaleString()} decided entries
        </Typography>
      </Box>
    </Stack>
  )
}

function BillingStatBox({
  label,
  value,
  accent,
  emphasis = false,
}: {
  label: string
  value: number
  accent: string
  emphasis?: boolean
}) {
  return (
    <Box
      sx={{
        p: 2,
        borderRadius: '0.75rem',
        height: '100%',
        bgcolor: emphasis ? portalAnalyticsColors.soft : portalColors.bgWhite,
        border: `1px solid ${emphasis ? portalAnalyticsColors.softStrong : portalColors.border}`,
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <Box sx={{ position: 'absolute', top: 0, left: 0, right: 0, height: 3, bgcolor: accent }} />
      <Typography
        sx={{
          fontSize: '0.6875rem',
          fontWeight: 700,
          letterSpacing: '0.1em',
          textTransform: 'uppercase',
          color: portalAnalyticsColors.base,
          mt: 0.5,
        }}
      >
        {label}
      </Typography>
      <Typography
        sx={{
          mt: 0.75,
          fontSize: '1.5rem',
          fontWeight: 700,
          fontVariantNumeric: 'tabular-nums',
          color: portalAnalyticsColors.darkest,
          lineHeight: 1.1,
        }}
      >
        {value.toLocaleString()}
      </Typography>
    </Box>
  )
}

export function BillingSplitPanel({ report }: { report: DaOversightReport }) {
  const total = Math.max(report.issuedBillings, 1)
  const paidPct = pct(report.paidBillings, total)
  const openCount = Math.max(report.issuedBillings - report.paidBillings, 0)

  return (
    <Stack spacing={2.5}>
      <Grid container spacing={1.5}>
        <Grid size={{ xs: 12, sm: 4 }}>
          <BillingStatBox label="Issued" value={report.issuedBillings} accent={portalAnalyticsColors.mid} />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <BillingStatBox label="Paid" value={report.paidBillings} accent={portalAnalyticsColors.dark} emphasis />
        </Grid>
        <Grid size={{ xs: 12, sm: 4 }}>
          <BillingStatBox label="Outstanding" value={openCount} accent={portalAnalyticsColors.darkest} emphasis />
        </Grid>
      </Grid>

      <Box
        sx={{
          p: 2,
          borderRadius: '0.75rem',
          bgcolor: portalAnalyticsColors.soft,
          border: `1px solid ${portalAnalyticsColors.softStrong}`,
        }}
      >
        <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
          <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalAnalyticsColors.darkest }}>
            Collection rate
          </Typography>
          <Typography sx={{ fontSize: '1.125rem', fontWeight: 700, color: portalAnalyticsColors.dark, fontVariantNumeric: 'tabular-nums' }}>
            {paidPct}%
          </Typography>
        </Stack>
        <LinearProgress
          variant="determinate"
          value={paidPct}
          sx={{
            height: 8,
            borderRadius: 999,
            bgcolor: portalColors.bgWhite,
            border: `1px solid ${portalAnalyticsColors.softStrong}`,
            '& .MuiLinearProgress-bar': { borderRadius: 999, bgcolor: portalAnalyticsColors.dark },
          }}
        />
        <Typography sx={{ mt: 1, fontSize: '0.8125rem', color: portalColors.textMuted }}>
          {report.paidBillings.toLocaleString()} collected of {report.issuedBillings.toLocaleString()} issued billings
        </Typography>
      </Box>
    </Stack>
  )
}

export function OperationsMetrics({ report }: { report: DaOversightReport }) {
  const items = [
    { label: 'Completed inspections', value: report.completedInspections, color: portalAnalyticsColors.dark },
    { label: 'Pending accreditation', value: report.pendingAccreditation, color: portalAnalyticsColors.base },
    { label: 'Under review', value: report.underReviewEntries, color: portalAnalyticsColors.mid },
  ]

  return (
    <Stack spacing={2}>
      {items.map((item) => (
        <Stack key={item.label} direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
          <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: item.color, flexShrink: 0 }} />
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>{item.label}</Typography>
          </Box>
          <Typography sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>{item.value.toLocaleString()}</Typography>
        </Stack>
      ))}
    </Stack>
  )
}

export function ChildAgencyComparison({ agencies }: { agencies: DaAgencySummary[] }) {
  const sorted = [...agencies].sort((a, b) => b.totalEntries - a.totalEntries)
  const maxEntries = Math.max(...sorted.map((agency) => agency.totalEntries), 1)

  return (
    <Stack spacing={2}>
      {sorted.map((agency) => (
        <Box
          key={agency.id}
          component={RouterLink}
          to={`/da/agencies/${agency.id}`}
          sx={{
            display: 'block',
            textDecoration: 'none',
            color: 'inherit',
            p: 1.5,
            mx: -1.5,
            borderRadius: '0.75rem',
            transition: 'background-color 0.15s ease',
            '&:hover': { bgcolor: portalColors.bgMuted },
          }}
        >
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 1 }}>
            <Chip size="small" label={agency.code} sx={{ fontWeight: 700, minWidth: 52 }} />
            <Typography sx={{ flex: 1, minWidth: 0, fontWeight: 600, fontSize: '0.875rem' }} noWrap>
              {agency.name}
            </Typography>
            <Typography sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>{agency.totalEntries.toLocaleString()}</Typography>
            <ArrowForwardIcon sx={{ fontSize: 16, color: portalColors.textMuted }} />
          </Stack>
          <LinearProgress
            variant="determinate"
            value={Math.min(100, (agency.totalEntries / maxEntries) * 100)}
            sx={{
              height: 8,
              borderRadius: 999,
              bgcolor: portalColors.bgMuted,
              '& .MuiLinearProgress-bar': { borderRadius: 999, bgcolor: portalAnalyticsColors.dark },
            }}
          />
          <Stack direction="row" spacing={2} sx={{ mt: 1 }}>
            <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
              Pending {agency.pendingEntries.toLocaleString()}
            </Typography>
            <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
              Billings {agency.openBillings.toLocaleString()}
            </Typography>
          </Stack>
        </Box>
      ))}
    </Stack>
  )
}

export function MetricGroup({
  title,
  items,
  stats,
}: {
  title: string
  items: readonly { key: string; label: string }[]
  stats: Record<string, number | undefined>
}) {
  return (
    <Box>
      <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.12em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 1.5 }}>
        {title}
      </Typography>
      <Grid container spacing={1.5}>
        {items.map((item) => (
          <Grid key={item.key} size={{ xs: 6, md: 4 }}>
            <Box sx={{ p: 1.75, borderRadius: '0.75rem', bgcolor: portalAnalyticsColors.soft, border: `1px solid ${portalAnalyticsColors.softStrong}`, height: '100%' }}>
              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{item.label}</Typography>
              <Typography sx={{ mt: 0.5, fontSize: '1.25rem', fontWeight: 700, fontVariantNumeric: 'tabular-nums', color: portalAnalyticsColors.darkest }}>
                {(stats[item.key] ?? 0).toLocaleString()}
              </Typography>
            </Box>
          </Grid>
        ))}
      </Grid>
    </Box>
  )
}
