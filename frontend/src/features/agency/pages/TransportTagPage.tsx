import { FormEvent, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useAddTransportTagMutation } from '../api/agencyApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

const TRANSPORT_TYPES = [
  { value: 'warehouse_nmis', label: 'Warehouse (NMIS)' },
  { value: 'direct_delivery', label: 'Direct delivery' },
  { value: 'cold_chain', label: 'Cold chain' },
]

export function TransportTagPage() {
  const [containerUuid, setContainerUuid] = useState('')
  const [transportType, setTransportType] = useState('warehouse_nmis')
  const [message, setMessage] = useState<{ tone: 'success' | 'error'; text: string } | null>(null)
  const [addTransportTag, { isLoading }] = useAddTransportTagMutation()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setMessage(null)
    if (!containerUuid.trim()) return

    try {
      const result = await addTransportTag({
        containerUuid: containerUuid.trim(),
        transportType,
      }).unwrap()
      setMessage({
        tone: 'success',
        text: `Transport tag added for container ${result.data?.containerNumber ?? containerUuid}. Status: ${result.data?.status ?? 'updated'}.`,
      })
      setContainerUuid('')
    } catch {
      setMessage({
        tone: 'error',
        text: 'Unable to tag container. Ensure it is ready for transport and not already tagged.',
      })
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Workflow"
        title="Transport Tagging"
        subtitle="Tag containers that are ready for transport so operators can claim and assign drivers."
      />

      {message && (
        <Alert severity={message.tone} sx={{ mb: 2 }} onClose={() => setMessage(null)}>
          {message.text}
        </Alert>
      )}

      <PortalPanel title="Add transport tag">
        <Box component="form" onSubmit={handleSubmit} sx={{ px: 2.5, py: 2 }}>
          <Stack spacing={2}>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
              Enter the container UUID for a container in <strong>ReadyForTransport</strong> status.
            </Typography>
            <TextField
              label="Container UUID"
              value={containerUuid}
              onChange={(e) => setContainerUuid(e.target.value)}
              required
              fullWidth
            />
            <TextField
              select
              label="Transport type"
              value={transportType}
              onChange={(e) => setTransportType(e.target.value)}
              fullWidth
            >
              {TRANSPORT_TYPES.map((option) => (
                <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>
              ))}
            </TextField>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={isLoading}>
              {isLoading ? 'Tagging…' : 'Add transport tag'}
            </Button>
          </Stack>
        </Box>
      </PortalPanel>
    </Box>
  )
}
