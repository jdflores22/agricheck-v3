import { useEffect, useState } from 'react'
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
} from '@mui/material'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import type { ClientContainerInspectionPhoto } from '../../client/api/clientApi'

type ContainerInspectionPhotoReviewControlsProps = {
  photo: ClientContainerInspectionPhoto
  disabled?: boolean
  onSave: (payload: { photoUuid: string; decision: 'Approved' | 'Rejected'; comment?: string }) => Promise<void>
}

export function ContainerInspectionPhotoReviewControls({
  photo,
  disabled,
  onSave,
}: ContainerInspectionPhotoReviewControlsProps) {
  const [status, setStatus] = useState(photo.reviewDecision === 'Pending' ? '' : photo.reviewDecision)
  const [remarks, setRemarks] = useState(photo.reviewComment ?? '')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    setStatus(photo.reviewDecision === 'Pending' ? '' : photo.reviewDecision)
    setRemarks(photo.reviewComment ?? '')
  }, [photo.reviewComment, photo.reviewDecision])

  const remarksRequired = status === 'Rejected'

  const handleSave = async () => {
    if (!status || (status !== 'Approved' && status !== 'Rejected')) {
      setError('Select a review decision before saving.')
      return
    }
    if (remarksRequired && !remarks.trim()) {
      setError('Remarks are required when rejecting a photo.')
      return
    }

    setError('')
    setSaving(true)
    try {
      await onSave({
        photoUuid: photo.uuid,
        decision: status,
        comment: remarks.trim() || undefined,
      })
    } finally {
      setSaving(false)
    }
  }

  return (
    <Box
      sx={{
        borderTop: '1px solid',
        borderColor: 'divider',
        px: 1.5,
        py: 1.5,
        bgcolor: 'background.paper',
      }}
    >
      <Stack spacing={1.5}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
          {photo.reviewDecision !== 'Pending' ? (
            <Chip size="small" label={photo.reviewDecision} sx={portalStatusChipSx(photo.reviewDecision)} />
          ) : (
            <Chip size="small" label="Pending Review" variant="outlined" />
          )}
        </Stack>

        <FormControl size="small" fullWidth disabled={disabled}>
          <InputLabel>Review Decision</InputLabel>
          <Select
            label="Review Decision"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value)
              setError('')
            }}
          >
            <MenuItem value="Approved">Approved</MenuItem>
            <MenuItem value="Rejected">Rejected</MenuItem>
          </Select>
        </FormControl>

        <TextField
          label={remarksRequired ? 'Remarks (required)' : 'Remarks (optional)'}
          value={remarks}
          onChange={(e) => {
            setRemarks(e.target.value)
            setError('')
          }}
          multiline
          minRows={2}
          fullWidth
          disabled={disabled}
          required={remarksRequired}
        />

        {error ? <Alert severity="error">{error}</Alert> : null}

        <Box>
          <Button
            variant="contained"
            size="small"
            sx={portalPrimaryButtonSx}
            disabled={disabled || saving || !status}
            onClick={() => void handleSave()}
          >
            {saving ? 'Saving…' : photo.reviewDecision !== 'Pending' ? 'Update Review' : 'Save Review'}
          </Button>
        </Box>
      </Stack>
    </Box>
  )
}
