import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  TextField,
} from '@mui/material'
import { useEffect, useState } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'

export function isFormTemplateLive(template: { isActive: boolean; hasPublishedVersion: boolean }) {
  return template.isActive && template.hasPublishedVersion
}

export const FORM_DELETE_LIVE_TOOLTIP = 'Cannot delete a live template. Deactivate or unpublish it first.'

interface AdminFormDeleteDialogProps {
  open: boolean
  templateName: string
  submissionCount?: number
  deleting?: boolean
  onClose: () => void
  onConfirm: () => void | Promise<void>
}

export function AdminFormDeleteDialog({
  open,
  templateName,
  submissionCount = 0,
  deleting = false,
  onClose,
  onConfirm,
}: AdminFormDeleteDialogProps) {
  const [confirmed, setConfirmed] = useState(false)
  const [confirmName, setConfirmName] = useState('')

  useEffect(() => {
    if (!open) {
      setConfirmed(false)
      setConfirmName('')
    }
  }, [open])

  const nameMatches = confirmName.trim() === templateName.trim()
  const canDelete = confirmed && nameMatches

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle sx={{ color: '#b91c1c' }}>Delete template</DialogTitle>
      <DialogContent>
        <Alert severity="error" sx={{ mb: 2 }}>
          You are about to permanently delete <strong>{templateName}</strong>. This action cannot be undone.
        </Alert>
        {submissionCount > 0 ? (
          <Alert severity="warning" sx={{ mb: 2 }}>
            This template has {submissionCount} submission{submissionCount === 1 ? '' : 's'}. Deleting it may affect historical data.
          </Alert>
        ) : null}
        <TextField
          label={`Type "${templateName}" to confirm`}
          value={confirmName}
          onChange={(e) => setConfirmName(e.target.value)}
          fullWidth
          autoFocus
          sx={{ mb: 2 }}
          error={confirmName.length > 0 && !nameMatches}
          helperText={confirmName.length > 0 && !nameMatches ? 'Template name does not match.' : ' '}
        />
        <FormControlLabel
          control={<Checkbox checked={confirmed} onChange={(e) => setConfirmed(e.target.checked)} />}
          label="I understand this action is permanent"
          sx={{ alignItems: 'flex-start', color: portalColors.textDark }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={deleting}>
          Cancel
        </Button>
        <Button color="error" variant="contained" disabled={!canDelete || deleting} onClick={() => void onConfirm()}>
          Delete template
        </Button>
      </DialogActions>
    </Dialog>
  )
}
