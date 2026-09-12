import AccountTreeOutlinedIcon from '@mui/icons-material/AccountTreeOutlined'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import { Avatar, Box, Chip, Divider, Stack, Typography } from '@mui/material'
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

function SidebarStat({ label, value }: { label: string; value: number | string }) {
  return (
    <Box sx={{ p: 1.5, borderRadius: '0.75rem', bgcolor: portalColors.bgMuted, minWidth: 0 }}>
      <Typography sx={{ fontSize: '0.625rem', fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: portalColors.textMuted }}>
        {label}
      </Typography>
      <Typography sx={{ mt: 0.5, fontSize: '1.125rem', fontWeight: 700, fontVariantNumeric: 'tabular-nums', lineHeight: 1.2 }}>
        {value}
      </Typography>
    </Box>
  )
}

function LeadershipRow({
  label,
  person,
}: {
  label: string
  person?: DaAgencyLeadershipUser | null
}) {
  return (
    <Box sx={{ py: 1.25 }}>
      <Typography sx={{ fontSize: '0.625rem', fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 1 }}>
        {label}
      </Typography>
      {person ? (
        <Stack direction="row" spacing={1.25} sx={{ alignItems: 'flex-start' }}>
          <Avatar sx={{ width: 40, height: 40, bgcolor: portalColors.primary, fontSize: '0.8125rem', fontWeight: 700, flexShrink: 0 }}>
            {getInitials(person.fullName)}
          </Avatar>
          <Box sx={{ minWidth: 0, flex: 1 }}>
            <Typography sx={{ fontWeight: 700, lineHeight: 1.35, fontSize: '0.9375rem' }}>{person.fullName}</Typography>
            <Stack direction="row" spacing={0.5} sx={{ alignItems: 'flex-start', mt: 0.25 }}>
              <EmailOutlinedIcon sx={{ fontSize: 14, color: portalColors.textMuted, mt: '2px', flexShrink: 0 }} />
              <Typography
                component="a"
                href={`mailto:${person.email}`}
                sx={{
                  fontSize: '0.8125rem',
                  color: portalColors.textMuted,
                  textDecoration: 'none',
                  wordBreak: 'break-all',
                  lineHeight: 1.4,
                  '&:hover': { color: portalColors.primary },
                }}
              >
                {person.email}
              </Typography>
            </Stack>
          </Box>
        </Stack>
      ) : (
        <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, fontStyle: 'italic' }}>Not yet assigned</Typography>
      )}
    </Box>
  )
}

export function DaAgencyOversightSidebar({
  agency,
  isParentAgency,
  parentAgency,
  childAgencyCount,
  activeUsers,
  openBillings,
  pendingEntries,
  totalEntries,
  secretaryLabel,
  undersecretaryLabel,
}: {
  agency: DaAgencySummary
  isParentAgency: boolean
  parentAgency?: DaAgencySummary | null
  childAgencyCount: number
  activeUsers: number
  openBillings: number
  pendingEntries: number
  totalEntries: number
  secretaryLabel: string
  undersecretaryLabel: string
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
      <Box sx={{ p: 2.5, bgcolor: isParentAgency ? portalColors.successSoft : portalColors.bgWhite }}>
        <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', mb: 1.5 }}>
          <Box
            sx={{
              width: 56,
              height: 56,
              borderRadius: '0.875rem',
              border: `1px solid ${portalColors.border}`,
              bgcolor: portalColors.bgWhite,
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
              <BusinessOutlinedIcon sx={{ fontSize: 28, color: portalColors.primary }} />
            )}
          </Box>
          <Box sx={{ minWidth: 0 }}>
            <Stack direction="row" spacing={0.75} sx={{ flexWrap: 'wrap', gap: 0.75, mb: 0.5 }}>
              <Chip size="small" label={agency.code} sx={{ fontWeight: 700, height: 22 }} />
              <Chip
                size="small"
                label={agency.isActive ? 'Active' : 'Inactive'}
                sx={{
                  height: 22,
                  bgcolor: agency.isActive ? portalColors.bgWhite : portalColors.bgMuted,
                  color: agency.isActive ? portalColors.successText : portalColors.textMuted,
                  fontWeight: 600,
                }}
              />
            </Stack>
            <Typography sx={{ fontWeight: 700, lineHeight: 1.3, fontSize: '0.9375rem' }}>{agency.name}</Typography>
            <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.25 }}>
              {isParentAgency ? 'Top-level department' : 'Attached bureau / unit'}
            </Typography>
          </Box>
        </Stack>

        {parentAgency ? (
          <Box
            component={RouterLink}
            to={`/da/agencies/${parentAgency.id}`}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              mt: 1.5,
              px: 1.25,
              py: 1,
              borderRadius: '0.625rem',
              border: `1px solid ${portalColors.border}`,
              bgcolor: portalColors.bgWhite,
              textDecoration: 'none',
              color: 'inherit',
              transition: 'border-color 0.15s ease, background-color 0.15s ease',
              '&:hover': { borderColor: portalColors.primary, bgcolor: portalColors.successSoft },
            }}
          >
            <AccountTreeOutlinedIcon sx={{ fontSize: 18, color: portalColors.primary }} />
            <Box sx={{ flex: 1, minWidth: 0 }}>
              <Typography sx={{ fontSize: '0.625rem', fontWeight: 700, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted }}>
                Parent agency
              </Typography>
              <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600 }} noWrap>
                {parentAgency.name}
              </Typography>
            </Box>
            <ArrowForwardIcon sx={{ fontSize: 16, color: portalColors.textMuted }} />
          </Box>
        ) : null}
      </Box>

      <Divider />

      <Box sx={{ p: 2.5 }}>
        <Typography sx={{ fontSize: '0.625rem', fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 1.5 }}>
          At a glance
        </Typography>
        <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 1.25 }}>
          <SidebarStat label="Total entries" value={totalEntries.toLocaleString()} />
          <SidebarStat label="Pending" value={pendingEntries.toLocaleString()} />
          <SidebarStat label="Active users" value={activeUsers.toLocaleString()} />
          <SidebarStat label="Open billings" value={openBillings.toLocaleString()} />
        </Box>
        {isParentAgency ? (
          <Box sx={{ mt: 1.25 }}>
            <SidebarStat label="Attached agencies" value={childAgencyCount.toLocaleString()} />
          </Box>
        ) : null}
      </Box>

      <Divider />

      <Box sx={{ p: 2.5 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
          <PersonOutlineOutlinedIcon sx={{ fontSize: 18, color: portalColors.primary }} />
          <Typography sx={{ fontWeight: 700 }}>Leadership</Typography>
        </Stack>

        <LeadershipRow label={secretaryLabel} person={agency.secretary} />

        {(agency.undersecretaries ?? []).length > 0 ? (
          <>
            <Divider sx={{ my: 0.5 }} />
            <Typography sx={{ fontSize: '0.625rem', fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: portalColors.textMuted, pt: 1, pb: 0.5 }}>
              {undersecretaryLabel}
            </Typography>
            <Stack divider={<Divider flexItem />} spacing={0}>
              {(agency.undersecretaries ?? []).map((person) => (
                <Box key={person.userUuid} sx={{ py: 1.25 }}>
                  <Stack direction="row" spacing={1.25} sx={{ alignItems: 'flex-start' }}>
                    <Avatar sx={{ width: 36, height: 36, bgcolor: portalColors.primary, fontSize: '0.75rem', fontWeight: 700, flexShrink: 0 }}>
                      {getInitials(person.fullName)}
                    </Avatar>
                    <Box sx={{ minWidth: 0, flex: 1 }}>
                      <Typography sx={{ fontWeight: 600, lineHeight: 1.35, fontSize: '0.875rem' }}>{person.fullName}</Typography>
                      <Typography
                        component="a"
                        href={`mailto:${person.email}`}
                        sx={{
                          display: 'block',
                          fontSize: '0.8125rem',
                          color: portalColors.textMuted,
                          textDecoration: 'none',
                          wordBreak: 'break-all',
                          lineHeight: 1.4,
                          mt: 0.25,
                          '&:hover': { color: portalColors.primary },
                        }}
                      >
                        {person.email}
                      </Typography>
                    </Box>
                  </Stack>
                </Box>
              ))}
            </Stack>
          </>
        ) : (
          <>
            <Divider sx={{ my: 1.5 }} />
            <Typography sx={{ fontSize: '0.625rem', fontWeight: 700, letterSpacing: '0.1em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 1 }}>
              {undersecretaryLabel}
            </Typography>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, fontStyle: 'italic' }}>Not yet assigned</Typography>
          </>
        )}
      </Box>
    </Box>
  )
}
