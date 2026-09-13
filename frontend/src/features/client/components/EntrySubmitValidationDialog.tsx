import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
} from '@mui/material'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import type { EntrySubmitValidationIssue } from '../utils/entrySubmitValidation'

interface EntrySubmitValidationDialogProps {
  open: boolean
  issue: EntrySubmitValidationIssue | null
  onCancel: () => void
  onConfirmEdit: () => void
}

export function EntrySubmitValidationDialog({
  open,
  issue,
  onCancel,
  onConfirmEdit,
}: EntrySubmitValidationDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onCancel}
      maxWidth="sm"
      fullWidth
      aria-describedby="entry-submit-validation-desc"
    >
      <DialogTitle>{issue?.title ?? 'Cannot submit entry'}</DialogTitle>
      <DialogContent>
        <Typography id="entry-submit-validation-desc" sx={{ color: portalColors.textMuted }}>
          {issue?.message ?? 'Complete the missing details before submitting this entry.'}
        </Typography>
        <Typography sx={{ color: portalColors.textMuted, mt: 1.5 }}>
          Go back to edit this entry and add the container details now?
        </Typography>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onCancel} sx={portalOutlinedButtonSx}>
          Cancel
        </Button>
        <Button onClick={onConfirmEdit} variant="contained" sx={portalPrimaryButtonSx}>
          Yes, edit entry
        </Button>
      </DialogActions>
    </Dialog>
  )
}
