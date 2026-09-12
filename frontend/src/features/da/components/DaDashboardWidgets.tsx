import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import type { ComponentType } from 'react'
import { Box, Chip, Grid, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import { resolveAgencyLogoUrl } from '../../admin/components/adminAgencyUtils'
import type { DaAgencySummary, DaDashboard } from '../api/daApi'

export function QuickLinkCard({
  to,
  icon: Icon,
  label,
  description,
}: {
  to: string
  icon: ComponentType<{ sx?: object }>
  label: string
  description: string
}) {
  return (
    <Box
      component={RouterLink}
      to={to}
      sx={{
        display: 'block',
        height: '100%',
        p: 2,
        borderRadius: '0.875rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
        textDecoration: 'none',
        color: 'inherit',
        transition: 'border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease',
        '&:hover': {
          borderColor: portalAnalyticsColors.softStrong,
          boxShadow: '0 8px 20px rgba(22, 101, 52, 0.08)',
          transform: 'translateY(-1px)',
        },
      }}
    >
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
        <Box
          sx={{
            width: 40,
            height: 40,
            borderRadius: '0.625rem',
            bgcolor: portalAnalyticsColors.soft,
            border: `1px solid ${portalAnalyticsColors.softStrong}`,
            display: 'grid',
            placeItems: 'center',
            color: portalAnalyticsColors.dark,
            flexShrink: 0,
          }}
        >
          <Icon sx={{ fontSize: 20 }} />
        </Box>
        <Box sx={{ minWidth: 0 }}>
          <Typography sx={{ fontWeight: 700, color: portalAnalyticsColors.darkest }}>{label}</Typography>
          <Typography sx={{ mt: 0.25, fontSize: '0.8125rem', color: portalColors.textMuted, lineHeight: 1.45 }}>
            {description}
          </Typography>
        </Box>
      </Stack>
    </Box>
  )
}

export function AttentionPanel({ dashboard }: { dashboard: DaDashboard }) {
  const items = [
    { label: 'Pending entries', value: dashboard.pendingEntries, href: '/da/reports' },
    { label: 'Open billings', value: dashboard.openBillings, href: '/da/reports' },
    { label: 'Pending accreditation', value: dashboard.pendingAccreditation, href: '/da/reports' },
  ]

  return (
    <Box
      sx={{
        borderRadius: '0.875rem',
        border: `1px solid ${portalAnalyticsColors.softStrong}`,
        bgcolor: portalAnalyticsColors.soft,
        p: 2.5,
        height: '100%',
      }}
    >
      <Typography sx={{ fontWeight: 700, color: portalAnalyticsColors.darkest, mb: 0.5 }}>Needs attention</Typography>
      <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mb: 2 }}>
        Operational items that may require DA leadership follow-up.
      </Typography>
      <Stack spacing={1.25}>
        {items.map((item) => (
          <Box
            key={item.label}
            component={RouterLink}
            to={item.href}
            sx={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 2,
              p: 1.5,
              borderRadius: '0.75rem',
              bgcolor: portalColors.bgWhite,
              border: `1px solid ${portalColors.border}`,
              textDecoration: 'none',
              color: 'inherit',
              '&:hover': { borderColor: portalAnalyticsColors.base },
            }}
          >
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>{item.label}</Typography>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Typography sx={{ fontWeight: 700, fontSize: '1.125rem', color: portalAnalyticsColors.darkest }}>
                {item.value.toLocaleString()}
              </Typography>
              <ArrowForwardIcon sx={{ fontSize: 16, color: portalColors.textMuted }} />
            </Stack>
          </Box>
        ))}
      </Stack>
    </Box>
  )
}

export function AgencyPreviewList({ agencies }: { agencies: DaAgencySummary[] }) {
  const sorted = [...agencies].sort((a, b) => b.pendingEntries - a.pendingEntries || b.totalEntries - a.totalEntries)

  return (
    <Stack spacing={1.25}>
      {sorted.map((agency) => {
        const logoUrl = resolveAgencyLogoUrl(agency.logoUrl)
        return (
          <Box
            key={agency.id}
            component={RouterLink}
            to={`/da/agencies/${agency.id}`}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1.5,
              p: 1.5,
              borderRadius: '0.75rem',
              border: `1px solid ${portalColors.border}`,
              bgcolor: portalColors.bgWhite,
              textDecoration: 'none',
              color: 'inherit',
              '&:hover': { borderColor: portalAnalyticsColors.softStrong, bgcolor: portalAnalyticsColors.soft },
            }}
          >
            <Box
              sx={{
                width: 40,
                height: 40,
                borderRadius: '0.625rem',
                border: `1px solid ${portalColors.border}`,
                bgcolor: portalColors.bgMuted,
                display: 'grid',
                placeItems: 'center',
                overflow: 'hidden',
                flexShrink: 0,
              }}
            >
              {logoUrl ? (
                <Box component="img" src={logoUrl} alt={agency.name} sx={{ width: '100%', height: '100%', objectFit: 'contain', p: 0.5 }} />
              ) : (
                <Typography sx={{ fontWeight: 700, fontSize: '0.75rem', color: portalAnalyticsColors.dark }}>{agency.code}</Typography>
              )}
            </Box>
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', mb: 0.25 }}>
                <Chip size="small" label={agency.code} sx={{ height: 20, fontWeight: 700, fontSize: '0.6875rem' }} />
                {agency.pendingEntries > 0 ? (
                  <Chip size="small" label={`${agency.pendingEntries} pending`} sx={{ height: 20, bgcolor: portalAnalyticsColors.softMid, color: portalAnalyticsColors.darkest, fontWeight: 600, fontSize: '0.6875rem' }} />
                ) : null}
              </Stack>
              <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }} noWrap>
                {agency.name}
              </Typography>
            </Box>
            <Box sx={{ textAlign: 'right', flexShrink: 0 }}>
              <Typography sx={{ fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>{agency.totalEntries.toLocaleString()}</Typography>
              <Typography sx={{ fontSize: '0.6875rem', color: portalColors.textMuted }}>entries</Typography>
            </Box>
          </Box>
        )
      })}
    </Stack>
  )
}

export function NetworkHealthStrip({ dashboard }: { dashboard: DaDashboard }) {
  const items = [
    { label: 'Agencies', value: `${dashboard.activeAgencies}/${dashboard.totalAgencies}`, caption: 'active' },
    { label: 'Entries', value: dashboard.totalEntries.toLocaleString(), caption: `${dashboard.pendingEntries} pending` },
    { label: 'Approved', value: dashboard.approvedEntries.toLocaleString(), caption: 'completed reviews' },
    { label: 'Inspections', value: dashboard.completedInspections.toLocaleString(), caption: 'completed' },
  ]

  return (
    <Grid container spacing={1.5}>
      {items.map((item) => (
        <Grid key={item.label} size={{ xs: 6, md: 3 }}>
          <Box
            sx={{
              p: 2,
              borderRadius: '0.875rem',
              bgcolor: portalColors.bgWhite,
              border: `1px solid ${portalColors.border}`,
              height: '100%',
            }}
          >
            <Typography sx={{ fontSize: '0.6875rem', fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: portalColors.textMuted }}>
              {item.label}
            </Typography>
            <Typography sx={{ mt: 0.75, fontSize: '1.5rem', fontWeight: 700, color: portalAnalyticsColors.darkest, lineHeight: 1.1 }}>
              {item.value}
            </Typography>
            <Typography sx={{ mt: 0.5, fontSize: '0.8125rem', color: portalColors.textMuted }}>{item.caption}</Typography>
          </Box>
        </Grid>
      ))}
    </Grid>
  )
}
