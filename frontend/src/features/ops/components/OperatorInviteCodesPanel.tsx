import { useState } from 'react'
import {
  Alert,
  Button,
  Chip,
  Stack,
  Typography,
} from '@mui/material'
import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import { Link as RouterLink } from 'react-router-dom'
import { useGetOperatorInviteCodesQuery } from '../api/opsApi'
import { PortalPanel } from '../../../components/portal/PortalPanel'

export function OperatorInviteCodesPanel() {
  const { data, isLoading } = useGetOperatorInviteCodesQuery()
  const [message, setMessage] = useState('')
  const invites = data?.data ?? []
  const primary = invites[0]

  const copyCode = async (code: string) => {
    await navigator.clipboard.writeText(code)
    setMessage(`Copied ${code}.`)
  }

  if (isLoading) {
    return (
      <PortalPanel title="Driver invite code">
        <Typography sx={{ p: 2.5 }} color="text.secondary">Loading invite codes…</Typography>
      </PortalPanel>
    )
  }

  return (
    <PortalPanel title="Driver invite code">
      <Stack spacing={2} sx={{ p: 2.5 }}>
        <Typography color="text.secondary">
          Drivers need this code to register in the AgriTrack Android app under your fleet.
        </Typography>
        {message && <Alert severity="success" onClose={() => setMessage('')}>{message}</Alert>}
        {primary ? (
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }} useFlexGap>
            <Typography sx={{ fontFamily: 'monospace', fontWeight: 700, fontSize: 20 }}>
              {primary.code}
            </Typography>
            <Chip size="small" label={primary.isActive ? 'Active' : 'Inactive'} color="success" />
            <Button size="small" startIcon={<ContentCopyIcon />} onClick={() => copyCode(primary.code)}>
              Copy
            </Button>
          </Stack>
        ) : (
          <Typography color="text.secondary">No invite code yet.</Typography>
        )}
        <Button component={RouterLink} to="/operator/invites" size="small">
          Manage all invite codes
        </Button>
      </Stack>
    </PortalPanel>
  )
}
