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
import type { RequiredDocumentViewModel } from '../../client/components/AccreditationSubmissionSummary'

type AccreditationDocumentReviewControlsProps = {
  document: RequiredDocumentViewModel
  disabled?: boolean
  onSave: (payload: { fileUuid: string; decision: string; comment?: string }) => Promise<void>
}

export function AccreditationDocumentReviewControls({
  document,
  disabled,
  onSave,
}: AccreditationDocumentReviewControlsProps) {
  const [status, setStatus] = useState(document.reviewDecision ?? '')
  const [remarks, setRemarks] = useState(document.reviewComment ?? '')
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    setStatus(document.reviewDecision ?? '')
    setRemarks(document.reviewComment ?? '')
  }, [document.reviewComment, document.reviewDecision])

  const remarksRequired = status === 'RevisionRequired' || status === 'Rejected'

  const handleSave = async () => {
    if (!status) {
      setError('Select a document status before saving.')
      return
    }
    if (remarksRequired && !remarks.trim()) {
      setError('Remarks are required for revision or rejection.')
      return
    }

    setError('')
    setSaving(true)
    try {
      await onSave({
        fileUuid: document.fileUuid,
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
          {document.reviewDecision ? (
            <Chip size="small" label={document.reviewDecision} sx={portalStatusChipSx(document.reviewDecision)} />
          ) : (
            <Chip size="small" label="Pending Review" variant="outlined" />
          )}
        </Stack>

        <FormControl size="small" fullWidth disabled={disabled}>
          <InputLabel>Document Status</InputLabel>
          <Select
            label="Document Status"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value)
              setError('')
            }}
          >
            <MenuItem value="Approved">Approved</MenuItem>
            <MenuItem value="RevisionRequired">Revision Required</MenuItem>
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
            onClick={handleSave}
          >
            {saving ? 'Saving…' : document.reviewDecision ? 'Update Review' : 'Save Review'}
          </Button>
        </Box>
      </Stack>
    </Box>
  )
}
