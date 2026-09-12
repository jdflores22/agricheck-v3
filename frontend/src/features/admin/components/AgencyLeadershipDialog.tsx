import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  FormControlLabel,
  FormGroup,
  InputLabel,
  MenuItem,
  Select,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useAssignAgencySecretaryMutation,
  useAssignAgencyUndersecretariesMutation,
  useGetAdminUsersQuery,
  type AdminAgencyLeadershipUser,
  type AdminUserListItem,
} from '../api/adminApi'

type LeadershipMode = 'secretary' | 'undersecretary'

interface AgencyLeadershipDialogProps {
  open: boolean
  mode: LeadershipMode
  agencyId: number
  currentSecretary?: AdminAgencyLeadershipUser | null
  currentUndersecretaries: AdminAgencyLeadershipUser[]
  onClose: () => void
}

function formatUserOption(user: AdminUserListItem) {
  return `${user.fullName} (${user.email})`
}

export function AgencyLeadershipDialog({
  open,
  mode,
  agencyId,
  currentSecretary,
  currentUndersecretaries,
  onClose,
}: AgencyLeadershipDialogProps) {
  const { data, isLoading: loadingUsers } = useGetAdminUsersQuery({ page: 1, pageSize: 200 }, { skip: !open })
  const [assignSecretary, { isLoading: savingSecretary, error: secretaryError }] = useAssignAgencySecretaryMutation()
  const [assignUndersecretaries, { isLoading: savingUndersecretaries, error: undersecretaryError }] =
    useAssignAgencyUndersecretariesMutation()

  const users = useMemo(
    () => (data?.data.items ?? []).filter((user) => user.status === 'Active'),
    [data?.data.items],
  )

  const [selectedSecretaryUuid, setSelectedSecretaryUuid] = useState('')
  const [selectedUndersecretaryUuids, setSelectedUndersecretaryUuids] = useState<string[]>([])

  useEffect(() => {
    if (!open) return
    setSelectedSecretaryUuid(currentSecretary?.userUuid ?? '')
    setSelectedUndersecretaryUuids(currentUndersecretaries.map((user) => user.userUuid))
  }, [open, currentSecretary?.userUuid, currentUndersecretaries])

  const saving = savingSecretary || savingUndersecretaries
  const mutationError = secretaryError ?? undersecretaryError

  const toggleUndersecretary = (userUuid: string) => {
    setSelectedUndersecretaryUuids((current) =>
      current.includes(userUuid) ? current.filter((id) => id !== userUuid) : [...current, userUuid],
    )
  }

  const handleSave = async () => {
    if (mode === 'secretary') {
      await assignSecretary({
        id: agencyId,
        userUuid: selectedSecretaryUuid || null,
      }).unwrap()
    } else {
      await assignUndersecretaries({
        id: agencyId,
        userUuids: selectedUndersecretaryUuids,
      }).unwrap()
    }
    onClose()
  }

  return (
    <Dialog open={open} onClose={() => !saving && onClose()} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ bgcolor: portalColors.primary, color: '#fff' }}>
        {mode === 'secretary' ? 'Assign Agency Secretary' : 'Assign Agency Undersecretaries'}
      </DialogTitle>
      <DialogContent sx={{ pt: '20px !important' }}>
        {mutationError ? (
          <Alert severity="error" sx={{ mb: 2 }}>
            Unable to save leadership assignment. Please try again.
          </Alert>
        ) : null}

        {loadingUsers ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={28} sx={{ color: portalColors.primary }} />
          </Box>
        ) : mode === 'secretary' ? (
          <Box>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 2 }}>
              Each agency may have only one secretary. Selecting a new user will replace the current assignment.
            </Typography>
            <FormControl fullWidth>
              <InputLabel id="agency-secretary-label">Agency Secretary</InputLabel>
              <Select
                labelId="agency-secretary-label"
                label="Agency Secretary"
                value={selectedSecretaryUuid}
                onChange={(event) => setSelectedSecretaryUuid(event.target.value)}
              >
                <MenuItem value="">
                  <em>None — remove assignment</em>
                </MenuItem>
                {users.map((user) => (
                  <MenuItem key={user.uuid} value={user.uuid}>
                    {formatUserOption(user)}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Box>
        ) : (
          <Box>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 2 }}>
              You may assign multiple undersecretaries to this agency.
            </Typography>
            <FormGroup sx={{ maxHeight: 320, overflowY: 'auto', border: `1px solid ${portalColors.border}`, borderRadius: '0.75rem', p: 1.5 }}>
              {users.length === 0 ? (
                <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, p: 1 }}>
                  No active users available.
                </Typography>
              ) : (
                users.map((user) => (
                  <FormControlLabel
                    key={user.uuid}
                    control={
                      <Checkbox
                        checked={selectedUndersecretaryUuids.includes(user.uuid)}
                        onChange={() => toggleUndersecretary(user.uuid)}
                        color="success"
                      />
                    }
                    label={formatUserOption(user)}
                  />
                ))
              )}
            </FormGroup>
            <Typography sx={{ mt: 1.5, fontSize: '0.8125rem', color: portalColors.textMuted }}>
              Selected: {selectedUndersecretaryUuids.length}
            </Typography>
          </Box>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={saving} sx={portalOutlinedButtonSx}>
          Cancel
        </Button>
        <Button onClick={() => void handleSave()} variant="contained" disabled={saving || loadingUsers} sx={portalPrimaryButtonSx}>
          {saving ? 'Saving…' : 'Save Assignment'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
