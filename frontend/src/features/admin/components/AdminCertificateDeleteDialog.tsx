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

interface AdminCertificateDeleteDialogProps {
  open: boolean
  templateName: string
  deleting?: boolean
  onClose: () => void
  onConfirm: () => void | Promise<void>
}

export function AdminCertificateDeleteDialog({
  open,
  templateName,
  deleting = false,
  onClose,
  onConfirm,
}: AdminCertificateDeleteDialogProps) {
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
