import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import type { AgencyEntryEvaluation } from '../api/agencyApi'
import { useEvaluateEntryMavMutation } from '../api/agencyApi'

function mavStatusLabel(status: string) {
  switch (status) {
    case 'Approved':
      return 'Approved'
    case 'Rejected':
      return 'Rejected'
    case 'RevisionRequired':
      return 'Revision Required'
    case 'PendingReview':
      return 'Pending Review'
    default:
      return 'Not Provided'
  }
}

export function EntryMavEvaluationPanel({
  entry,
  canReview,
  onUpdated,
}: {
  entry: AgencyEntryEvaluation
  canReview: boolean
  onUpdated?: () => void
}) {
  const [decision, setDecision] = useState('')
  const [remarks, setRemarks] = useState('')
  const [error, setError] = useState('')
  const [evaluateMav, { isLoading }] = useEvaluateEntryMavMutation()
  const mav = entry.mav

  if (entry.entryType !== 'Import' || !mav) {
    return null
  }

  const isMavTrack = mav.importTrack === 'Mav'

  if (!isMavTrack) {
    return (
      <PortalPanel title="Import track">
        <Stack spacing={1.25} sx={{ p: 2.5 }}>
          <Chip size="small" label="Regular import (out-quota)" sx={portalStatusChipSx('Approved')} />
          <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
            This importer did not claim MAV in-quota coverage. No MAV certificate or MIC review is required, and
            this shipment will not reduce remaining MAV quota.
          </Typography>
        </Stack>
      </PortalPanel>
    )
  }

  const handleSave = async () => {
    if (!decision) {
      setError('Select a review decision.')
      return
    }
    if ((decision === 'Rejected' || decision === 'RevisionRequired') && !remarks.trim()) {
      setError('Remarks are required for rejection or revision.')
      return
    }
    setError('')
    await evaluateMav({ entryUuid: entry.uuid, decision, remarks: remarks.trim() || undefined }).unwrap()
    onUpdated?.()
  }

  return (
    <PortalPanel title="MAV Document Review">
      <Stack spacing={2} sx={{ p: 2.5 }}>
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
          <Chip size="small" label={mavStatusLabel(mav.mavDocumentStatus)} sx={portalStatusChipSx(mav.mavDocumentStatus)} />
          {mav.mavNo && <Chip size="small" variant="outlined" label={`MAV No. ${mav.mavNo}`} />}
          <Chip
            size="small"
            variant="outlined"
            color={mav.totalUtilizedVolume >= (mav.requiredVolume ?? 0) ? 'success' : 'warning'}
            label={`MIC ${mav.totalUtilizedVolume}/${mav.requiredVolume ?? 0} utilized`}
          />
        </Stack>

        {mav.mavCertificateFileName && (
          <Box>
            <Typography variant="caption" sx={{ color: portalColors.textMuted }}>MAV Certificate</Typography>
            <Typography variant="body2" sx={{ fontWeight: 600 }}>{mav.mavCertificateFileName}</Typography>
          </Box>
        )}

        {(mav.micUtilizations?.length ?? 0) > 0 && (
          <Box>
            <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block', mb: 0.5 }}>
              Linked MIC Utilizations
            </Typography>
            {mav.micUtilizations.map((u) => (
              <Typography key={`${u.micUuid}-${u.utilizedAt}`} variant="body2">
                {u.certificateNumber} · {u.volume} {entry.detail?.unit ?? 'kg'} · {u.commodityName}
              </Typography>
            ))}
          </Box>
        )}

        {mav.mavRemarks && (
          <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
            Previous remarks: {mav.mavRemarks}
          </Alert>
        )}

        {canReview && mav.mavCertificateFileName && (
          <Stack spacing={1.5}>
            <FormControl fullWidth size="small">
              <InputLabel>MAV Review Decision</InputLabel>
              <Select label="MAV Review Decision" value={decision} onChange={(e) => setDecision(e.target.value)}>
                <MenuItem value="Approved">Approved</MenuItem>
                <MenuItem value="RevisionRequired">Revision Required</MenuItem>
                <MenuItem value="Rejected">Rejected</MenuItem>
              </Select>
            </FormControl>
            <TextField
              fullWidth
              size="small"
              multiline
              minRows={2}
              label="Remarks"
              value={remarks}
              onChange={(e) => setRemarks(e.target.value)}
            />
            {error && <Alert severity="error">{error}</Alert>}
            <Button variant="contained" sx={portalPrimaryButtonSx} disabled={isLoading} onClick={() => void handleSave()}>
              Save MAV Review
            </Button>
          </Stack>
        )}
      </Stack>
    </PortalPanel>
  )
}
