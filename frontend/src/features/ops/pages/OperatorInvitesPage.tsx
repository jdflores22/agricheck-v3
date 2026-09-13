import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import {
  useCreateOperatorInviteCodeMutation,
  useGetOperatorInviteCodesQuery,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function OperatorInvitesPage() {
  const { data, isLoading, refetch } = useGetOperatorInviteCodesQuery()
  const [createInvite, { isLoading: creating }] = useCreateOperatorInviteCodeMutation()
  const [label, setLabel] = useState('')
  const [message, setMessage] = useState('')

  const invites = data?.data ?? []

  const copyCode = async (code: string) => {
    await navigator.clipboard.writeText(code)
    setMessage(`Copied ${code} to clipboard.`)
  }

  const handleCreate = async () => {
    try {
      await createInvite({ label: label.trim() || undefined }).unwrap()
      setLabel('')
      setMessage('New invite code created.')
      refetch()
    } catch {
      setMessage('Failed to create invite code.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="AgriTrack"
        title="Driver invite codes"
        subtitle="Share these codes with drivers so they can register in the AgriTrack Android app and join your fleet."
      />

      {message && <Alert severity="info" sx={{ mb: 2 }} onClose={() => setMessage('')}>{message}</Alert>}

      <PortalPanel title="Create invite code">
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ p: 2.5, maxWidth: 720 }}>
          <TextField
            label="Label (optional)"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            placeholder="e.g. Manila fleet drivers"
            fullWidth
          />
          <Button
            variant="contained"
            sx={portalPrimaryButtonSx}
            onClick={handleCreate}
            disabled={creating}
          >
            Generate code
          </Button>
        </Stack>
      </PortalPanel>

      <PortalTablePanel
        title="Active invite codes"
        loading={isLoading}
        emptyMessage="No invite codes yet. Generate one for your drivers."
        columns={['Code', 'Label', 'Uses', 'Status', '']}
      >
        {invites.map((invite) => (
          <TableRow key={invite.code}>
            <TableCell>
              <Typography fontFamily="monospace" fontWeight={700}>{invite.code}</Typography>
            </TableCell>
            <TableCell>{invite.label ?? '—'}</TableCell>
            <TableCell>
              {invite.maxUses > 0 ? `${invite.usedCount}/${invite.maxUses}` : invite.usedCount}
            </TableCell>
            <TableCell>
              <Chip
                size="small"
                label={invite.isActive ? 'Active' : 'Inactive'}
                color={invite.isActive ? 'success' : 'default'}
              />
            </TableCell>
            <TableCell align="right">
              <Button
                size="small"
                startIcon={<ContentCopyIcon />}
                onClick={() => copyCode(invite.code)}
              >
                Copy
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
