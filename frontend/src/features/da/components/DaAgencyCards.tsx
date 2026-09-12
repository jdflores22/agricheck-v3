import AccountBalanceOutlinedIcon from '@mui/icons-material/AccountBalanceOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import { Avatar, Box, Button, Chip, Grid, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { portalColors } from '../../../components/portal/portalTheme'
import { resolveAgencyLogoUrl } from '../../admin/components/adminAgencyUtils'
import type { DaAgencyLeadershipUser, DaAgencySummary } from '../api/daApi'

function getInitials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('')
}

function Metric({ label, value }: { label: string; value: number }) {
  return (
    <Box sx={{ minWidth: 0 }}>
      <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted }}>
        {label}
      </Typography>
      <Typography sx={{ fontSize: '1.125rem', fontWeight: 700, color: portalColors.textDark, fontVariantNumeric: 'tabular-nums' }}>
        {value.toLocaleString()}
      </Typography>
    </Box>
  )
}

function LeadershipPerson({
  label,
  person,
  variant = 'default',
}: {
  label: string
  person?: DaAgencyLeadershipUser | null
  variant?: 'default' | 'inverse'
}) {
  const muted = variant === 'inverse' ? 'rgba(255,255,255,0.78)' : portalColors.textMuted
  const text = variant === 'inverse' ? '#fff' : portalColors.textDark
  const border = variant === 'inverse' ? 'rgba(255,255,255,0.18)' : portalColors.border
  const bg = variant === 'inverse' ? 'rgba(255,255,255,0.1)' : portalColors.bgMuted

  return (
    <Box
      sx={{
        borderRadius: '0.75rem',
        border: `1px solid ${border}`,
        bgcolor: bg,
        p: 1.5,
        minHeight: 88,
      }}
    >
      <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: muted, mb: 1 }}>
        {label}
      </Typography>
      {person ? (
        <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
          <Avatar sx={{ width: 36, height: 36, bgcolor: variant === 'inverse' ? 'rgba(255,255,255,0.18)' : portalColors.primary, fontSize: '0.8125rem', fontWeight: 700 }}>
            {getInitials(person.fullName)}
          </Avatar>
          <Box sx={{ minWidth: 0 }}>
            <Typography sx={{ fontWeight: 700, color: text, lineHeight: 1.3 }}>{person.fullName}</Typography>
            <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center', color: muted }}>
              <EmailOutlinedIcon sx={{ fontSize: 14 }} />
              <Typography sx={{ fontSize: '0.75rem', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {person.email}
              </Typography>
            </Stack>
          </Box>
        </Stack>
      ) : (
        <Typography sx={{ fontSize: '0.875rem', color: muted, fontStyle: 'italic' }}>Not yet assigned</Typography>
      )}
    </Box>
  )
}

export function LeadershipSection({
  secretary,
  undersecretaries,
  secretaryLabel = 'Secretary',
  undersecretaryLabel = 'Undersecretaries',
  variant = 'default',
}: {
  secretary?: DaAgencyLeadershipUser | null
  undersecretaries: DaAgencyLeadershipUser[]
  secretaryLabel?: string
  undersecretaryLabel?: string
  variant?: 'default' | 'inverse'
}) {
  return (
    <Box>
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1.5 }}>
        <PersonOutlineOutlinedIcon sx={{ fontSize: 18, color: variant === 'inverse' ? '#fff' : portalColors.primary }} />
        <Typography sx={{ fontWeight: 700, color: variant === 'inverse' ? '#fff' : portalColors.textDark }}>
          Leadership
        </Typography>
      </Stack>
      <Grid container spacing={1.5}>
        <Grid size={{ xs: 12, md: 5 }}>
          <LeadershipPerson label={secretaryLabel} person={secretary} variant={variant} />
        </Grid>
        <Grid size={{ xs: 12, md: 7 }}>
          <Box
            sx={{
              borderRadius: '0.75rem',
              border: `1px solid ${variant === 'inverse' ? 'rgba(255,255,255,0.18)' : portalColors.border}`,
              bgcolor: variant === 'inverse' ? 'rgba(255,255,255,0.1)' : portalColors.bgMuted,
              p: 1.5,
              minHeight: 88,
            }}
          >
            <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: variant === 'inverse' ? 'rgba(255,255,255,0.78)' : portalColors.textMuted, mb: 1 }}>
              {undersecretaryLabel}
            </Typography>
            {undersecretaries.length > 0 ? (
              <Stack spacing={1}>
                {undersecretaries.map((person) => (
                  <Stack key={person.userUuid} direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
                    <Avatar sx={{ width: 32, height: 32, bgcolor: variant === 'inverse' ? 'rgba(255,255,255,0.18)' : portalColors.primary, fontSize: '0.75rem', fontWeight: 700 }}>
                      {getInitials(person.fullName)}
                    </Avatar>
                    <Box sx={{ minWidth: 0 }}>
                      <Typography sx={{ fontWeight: 600, color: variant === 'inverse' ? '#fff' : portalColors.textDark, fontSize: '0.875rem' }}>
                        {person.fullName}
                      </Typography>
                      <Typography sx={{ fontSize: '0.75rem', color: variant === 'inverse' ? 'rgba(255,255,255,0.78)' : portalColors.textMuted }}>
                        {person.email}
                      </Typography>
                    </Box>
                  </Stack>
                ))}
              </Stack>
            ) : (
              <Typography sx={{ fontSize: '0.875rem', color: variant === 'inverse' ? 'rgba(255,255,255,0.78)' : portalColors.textMuted, fontStyle: 'italic' }}>
                Not yet assigned
              </Typography>
            )}
          </Box>
        </Grid>
      </Grid>
    </Box>
  )
}

export function DaParentAgencyCard({
  agency,
  attachedCount,
  networkTotals,
  oversightHref,
}: {
  agency: DaAgencySummary
  attachedCount: number
  networkTotals: { totalEntries: number; pendingEntries: number; openBillings: number }
  oversightHref?: string
}) {
  const logoUrl = resolveAgencyLogoUrl(agency.logoUrl)

  return (
    <Box
      sx={{
        borderRadius: '0.875rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
        overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          px: { xs: 2.5, md: 3 },
          py: { xs: 2.5, md: 3 },
          background: `linear-gradient(135deg, ${portalColors.primary} 0%, ${portalColors.primaryDark} 100%)`,
          color: '#fff',
        }}
      >
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2.5} sx={{ alignItems: { md: 'center' } }}>
          <Box
            sx={{
              width: 72,
              height: 72,
              borderRadius: '1rem',
              bgcolor: 'rgba(255,255,255,0.14)',
              border: '1px solid rgba(255,255,255,0.22)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
              overflow: 'hidden',
            }}
          >
            {logoUrl ? (
              <Box component="img" src={logoUrl} alt={agency.name} sx={{ width: '100%', height: '100%', objectFit: 'contain', p: 1 }} />
            ) : (
              <AccountBalanceOutlinedIcon sx={{ fontSize: 34 }} />
            )}
          </Box>

          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', opacity: 0.88, mb: 0.5 }}>
              Top-Level Agency
            </Typography>
            <Typography sx={{ fontSize: { xs: '1.375rem', md: '1.625rem' }, fontWeight: 700, mb: 0.5 }}>
              {agency.name}
            </Typography>
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
              <Chip
                size="small"
                label={agency.code}
                sx={{ bgcolor: 'rgba(255,255,255,0.16)', color: '#fff', fontWeight: 700 }}
              />
              <Chip
                size="small"
                label={agency.isActive ? 'Active' : 'Inactive'}
                sx={{
                  bgcolor: agency.isActive ? 'rgba(255,255,255,0.2)' : 'rgba(0,0,0,0.18)',
                  color: '#fff',
                  fontWeight: 600,
                }}
              />
              <Chip
                size="small"
                label={`${attachedCount} attached agencies`}
                sx={{ bgcolor: 'rgba(255,255,255,0.12)', color: '#fff', fontWeight: 600 }}
              />
            </Stack>
          </Box>
          {oversightHref ? (
            <Button
              component={RouterLink}
              to={oversightHref}
              variant="contained"
              sx={{
                flexShrink: 0,
                bgcolor: '#fff',
                color: portalColors.primary,
                fontWeight: 700,
                textTransform: 'none',
                boxShadow: 'none',
                '&:hover': { bgcolor: portalColors.bgMuted, boxShadow: 'none' },
              }}
            >
              View oversight
            </Button>
          ) : null}
        </Stack>

        <Box sx={{ mt: 2.5, pt: 2.5, borderTop: '1px solid rgba(255,255,255,0.16)' }}>
          <LeadershipSection
            secretary={agency.secretary}
            undersecretaries={agency.undersecretaries ?? []}
            secretaryLabel="DA Secretary"
            undersecretaryLabel="DA Undersecretaries"
            variant="inverse"
          />
        </Box>
      </Box>

      <Box sx={{ px: { xs: 2.5, md: 3 }, py: 2.5 }}>
        <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 2 }}>
          Department-wide oversight across all bureaus and attached agencies under the DA network.
        </Typography>
        <Grid container spacing={2.5}>
          <Grid size={{ xs: 6, md: 3 }}>
            <Metric label="Network entries" value={networkTotals.totalEntries} />
          </Grid>
          <Grid size={{ xs: 6, md: 3 }}>
            <Metric label="Pending entries" value={networkTotals.pendingEntries} />
          </Grid>
          <Grid size={{ xs: 6, md: 3 }}>
            <Metric label="Open billings" value={networkTotals.openBillings} />
          </Grid>
          <Grid size={{ xs: 6, md: 3 }}>
            <Metric label="Attached agencies" value={attachedCount} />
          </Grid>
        </Grid>
      </Box>
    </Box>
  )
}

export function DaAttachedAgencyCard({ agency, to }: { agency: DaAgencySummary; to?: string }) {
  const logoUrl = resolveAgencyLogoUrl(agency.logoUrl)

  const cardSx = {
    height: '100%',
    borderRadius: '0.875rem',
    border: `1px solid ${portalColors.border}`,
    bgcolor: portalColors.bgWhite,
    p: 2.25,
    display: 'flex',
    flexDirection: 'column',
    transition: 'border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease',
    textDecoration: 'none',
    color: 'inherit',
    '&:hover': {
      borderColor: portalColors.primary,
      boxShadow: '0 10px 24px rgba(22, 101, 52, 0.08)',
      transform: 'translateY(-1px)',
    },
  } as const

  const content = (
    <>
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start', mb: 2 }}>
        <Box
          sx={{
            width: 52,
            height: 52,
            borderRadius: '0.75rem',
            border: `1px solid ${portalColors.border}`,
            bgcolor: portalColors.bgMuted,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            overflow: 'hidden',
            flexShrink: 0,
          }}
        >
          {logoUrl ? (
            <Box component="img" src={logoUrl} alt={agency.name} sx={{ width: '100%', height: '100%', objectFit: 'contain', p: 0.75 }} />
          ) : (
            <BusinessOutlinedIcon sx={{ fontSize: 24, color: portalColors.primary }} />
          )}
        </Box>
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 0.75, mb: 0.75 }}>
            <Chip size="small" label={agency.code} sx={{ fontWeight: 700 }} />
            <Chip
              size="small"
              label={agency.isActive ? 'Active' : 'Inactive'}
              sx={{
                bgcolor: agency.isActive ? portalColors.successSoft : portalColors.bgMuted,
                color: agency.isActive ? portalColors.successText : portalColors.textMuted,
                fontWeight: 600,
              }}
            />
          </Stack>
          <Typography sx={{ fontWeight: 700, color: portalColors.textDark, lineHeight: 1.35 }}>
            {agency.name}
          </Typography>
        </Box>
      </Stack>

      <Box sx={{ borderTop: `1px solid ${portalColors.border}`, pt: 2, mb: 2 }}>
        <LeadershipSection
          secretary={agency.secretary}
          undersecretaries={agency.undersecretaries ?? []}
          secretaryLabel="Secretary"
          undersecretaryLabel="Undersecretaries"
        />
      </Box>

      <Grid container spacing={1.5} sx={{ mt: 'auto' }}>
        <Grid size={4}>
          <Metric label="Entries" value={agency.totalEntries} />
        </Grid>
        <Grid size={4}>
          <Metric label="Pending" value={agency.pendingEntries} />
        </Grid>
        <Grid size={4}>
          <Metric label="Billings" value={agency.openBillings} />
        </Grid>
      </Grid>
    </>
  )

  if (to) {
    return (
      <Box component={RouterLink} to={to} sx={cardSx}>
        {content}
      </Box>
    )
  }

  return <Box sx={cardSx}>{content}</Box>
}
