import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { useGetInspectionQuery } from '../api/clientApi'

export function ClientInspectionDetailPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading } = useGetInspectionQuery(uuid, { skip: !uuid })
  const inspection = data?.data

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading inspection…</Typography>
  if (!inspection) return <Alert severity="error">Inspection not found.</Alert>

  return (
    <Box sx={{ maxWidth: 640 }}>
      <PortalPageHeader
        eyebrow="Inspection"
        title={inspection.entryReferenceNo}
        subtitle={`${inspection.agencyName} (${inspection.agencyCode})`}
        actions={
          <Button component={RouterLink} to="/client/inspections" variant="outlined" sx={portalOutlinedButtonSx}>
            Back
          </Button>
        }
      />
      <Chip size="small" label={inspection.status} sx={{ mb: 2, ...portalStatusChipSx(inspection.status) }} />
      <PortalPanel title="Inspection Details">
        <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
          <Typography variant="body2">Inspector: {inspection.inspectorName ?? '—'}</Typography>
          <Typography variant="body2">
            Scheduled: {inspection.scheduledAt ? new Date(inspection.scheduledAt).toLocaleString() : '—'}
          </Typography>
          <Typography variant="body2">
            Completed: {inspection.completedAt ? new Date(inspection.completedAt).toLocaleString() : '—'}
          </Typography>
          <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
            Findings: {inspection.findings ?? 'No findings recorded yet.'}
          </Typography>
          <Button
            size="small"
            component={RouterLink}
            to={`/client/entries/${inspection.entryUuid}`}
            variant="outlined"
            sx={{ alignSelf: 'flex-start', mt: 1, ...portalOutlinedButtonSx }}
          >
            View Entry
          </Button>
        </Stack>
      </PortalPanel>
    </Box>
  )
}
