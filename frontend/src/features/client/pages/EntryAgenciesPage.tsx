import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import { Alert, Box, Button, Chip, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { useGetDashboardQuery } from '../api/clientApi'

function AgencyEntryCard({
  agency,
  locked,
}: {
  agency: { id: number; code: string; name: string; logoUrl?: string; hasEntryForm: boolean }
  locked?: boolean
}) {
  const noForm = !locked && !agency.hasEntryForm

  const cardBody = (
    <>
      <Box sx={{ width: 52, height: 52, mb: 1.5, display: 'flex', alignItems: 'center', justifyContent: 'center', border: `1px solid ${portalColors.border}`, borderRadius: '0.625rem', bgcolor: '#fafaf9' }}>
        {agency.logoUrl ? (
          <Box component="img" src={agency.logoUrl} alt={agency.name} sx={{ maxWidth: '100%', maxHeight: '100%' }} />
        ) : (
          <BusinessOutlinedIcon sx={{ color: portalColors.primary }} />
        )}
      </Box>
      <Typography sx={{ fontWeight: 600, color: portalColors.textDark }}>{agency.code}</Typography>
      <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.5 }}>{agency.name}</Typography>
      {!locked && agency.hasEntryForm && (
        <Chip size="small" label="Start new entry" sx={{ mt: 1.5, ...getStatusBadgeStyle('Approved') }} />
      )}
      {!locked && noForm && (
        <Chip size="small" label="No entry form yet" sx={{ mt: 1.5, ...getStatusBadgeStyle('Draft') }} />
      )}
      {locked && (
        <Box sx={{ position: 'absolute', inset: 0, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', bgcolor: 'rgba(250,250,249,0.85)', borderRadius: '0.75rem' }}>
          <LockOutlinedIcon sx={{ color: portalColors.textMuted, mb: 0.5 }} />
          <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>Accreditation required</Typography>
        </Box>
      )}
    </>
  )

  const cardSx = {
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    minHeight: 152,
    p: 2,
    border: `1px solid ${portalColors.border}`,
    borderRadius: '0.75rem',
    bgcolor: locked || noForm ? '#fafaf9' : portalColors.bgWhite,
    textAlign: 'center',
    textDecoration: 'none',
    opacity: locked || noForm ? 0.72 : 1,
    pointerEvents: locked || noForm ? 'none' : 'auto',
    transition: 'border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease',
    ...(!locked && !noForm
      ? {
          '&:hover': {
            borderColor: portalColors.primary,
            boxShadow: '0 8px 20px rgba(22, 163, 74, 0.1)',
            transform: 'translateY(-1px)',
          },
        }
      : {}),
  } as const

  if (locked || noForm) {
    return <Box sx={cardSx}>{cardBody}</Box>
  }

  return (
    <Box component={RouterLink} to={`/client/entries/new?agencyId=${agency.id}`} sx={cardSx}>
      {cardBody}
    </Box>
  )
}

export function EntryAgenciesPage() {
  const { data, isLoading } = useGetDashboardQuery()
  const dashboard = data?.data
  const isAccredited = dashboard?.accreditation.isAccredited ?? false
  const agencies = dashboard?.agencies ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Entries"
        title="Submit to DA Agencies"
        subtitle="Choose an agency to start a new import or export entry."
      />

      {!isAccredited && !isLoading && (
        <Alert
          severity="warning"
          sx={{ mb: 3, borderRadius: '0.75rem' }}
          action={
            <Button component={RouterLink} to="/client/accreditation" size="small" sx={{ color: 'inherit' }}>
              Apply
            </Button>
          }
        >
          Accreditation is required before you can submit entries to DA agencies.
        </Alert>
      )}

      <Box sx={{ display: 'grid', gap: 1.5, gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, 1fr)', lg: agencies.length >= 5 ? 'repeat(5, 1fr)' : 'repeat(3, 1fr)' } }}>
        {agencies.map((agency) => (
          <AgencyEntryCard key={agency.id} agency={agency} locked={!isAccredited} />
        ))}
      </Box>
    </Box>
  )
}
