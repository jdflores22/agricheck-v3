import { Alert, Box, Button, Chip, Typography } from '@mui/material'
import { useEffect } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { useGetContainerInspectionsQuery, useGetEntryQuery } from '../api/clientApi'
import { EntryContainerInspectionPanel } from '../components/EntryContainerInspectionPanel'
import { getContainerInspectionProgress } from '../utils/entryInspectionUtils'

export function EntryInspectionUploadPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const { data, isLoading } = useGetEntryQuery(uuid, { skip: !uuid })
  const { data: inspectionsData } = useGetContainerInspectionsQuery(uuid, { skip: !uuid })
  const entry = data?.data
  const inspectionProgress = getContainerInspectionProgress(inspectionsData?.data ?? [])

  useBreadcrumbLabel(entry?.referenceNo)

  useEffect(() => {
    if (!entry || entry.status !== 'ForInspection') return
    if (!inspectionProgress.allUploadsComplete) return
    navigate(`/client/entries/${uuid}?view=entry`, { replace: true })
  }, [entry, inspectionProgress.allUploadsComplete, navigate, uuid])

  if (isLoading) {
    return <Typography sx={{ color: 'text.secondary' }}>Loading entry…</Typography>
  }

  if (!entry) {
    return <Alert severity="error">Entry not found.</Alert>
  }

  if (entry.status === 'ForInspection' && inspectionProgress.allUploadsComplete) {
    return <Typography sx={{ color: 'text.secondary' }}>All photos uploaded. Returning to entry details…</Typography>
  }

  if (entry.status !== 'ForInspection') {
    return (
      <Box>
        <Alert severity="info" sx={{ mb: 2 }}>
          This entry is not currently awaiting container inspection uploads.
        </Alert>
        <Button component={RouterLink} to={`/client/entries/${uuid}`} variant="outlined" sx={portalOutlinedButtonSx}>
          Back to entry
        </Button>
      </Box>
    )
  }

  return (
    <Box sx={{ maxWidth: 900 }}>
      <PortalPageHeader
        eyebrow="Container inspection"
        title={entry.referenceNo}
        subtitle={`${entry.entryType} · ${entry.agencyCode} — upload all required photos per container.`}
        actions={
          <Button
            component={RouterLink}
            to={`/client/entries/${uuid}?view=entry`}
            variant="outlined"
            sx={portalOutlinedButtonSx}
          >
            Entry details
          </Button>
        }
      />

      <Chip size="small" label="For Inspection" sx={{ mb: 2, ...portalStatusChipSx('ForInspection') }} />

      <EntryContainerInspectionPanel entryUuid={uuid} layout="tabs" showProgress />
    </Box>
  )
}
