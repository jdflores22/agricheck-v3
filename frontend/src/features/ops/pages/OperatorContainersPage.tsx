import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import {
  useAssignDriverToContainerMutation,
  useClaimOperatorContainerMutation,
  useGetClaimableContainersQuery,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function OperatorContainersPage() {
  const { data, isLoading } = useGetClaimableContainersQuery()
  const [claimContainer] = useClaimOperatorContainerMutation()
  const [assignDriver, { isLoading: assigning }] = useAssignDriverToContainerMutation()
  const [message, setMessage] = useState('')
  const [assignTarget, setAssignTarget] = useState<string | null>(null)
  const [driverUuid, setDriverUuid] = useState('')

  const containers = data?.data ?? []

  const handleClaim = async (uuid: string) => {
    try {
      await claimContainer(uuid).unwrap()
      setMessage('Container claimed successfully.')
    } catch {
      setMessage('Failed to claim container.')
    }
  }

  const handleAssign = async () => {
    if (!assignTarget || !driverUuid.trim()) return
    try {
      await assignDriver({ uuid: assignTarget, driverUserUuid: driverUuid.trim() }).unwrap()
      setMessage('Driver assigned successfully.')
      setAssignTarget(null)
      setDriverUuid('')
    } catch {
      setMessage('Failed to assign driver. Check the driver user UUID.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Claimable Containers"
        subtitle="Claim tagged containers and assign drivers for delivery."
      />
      {message && <Alert severity="info" sx={{ mb: 2 }} onClose={() => setMessage('')}>{message}</Alert>}
      <PortalTablePanel
        title="Awaiting confirmation"
        columns={['Container', 'Entry', 'Status', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && containers.length === 0}
        emptyMessage="No claimable containers right now."
      >
        {containers.map((container) => (
          <TableRow key={container.uuid} hover>
            <TableCell>{container.containerNumber}</TableCell>
            <TableCell>{container.entryReference}</TableCell>
            <TableCell>{container.status}</TableCell>
            <TableCell>
              <Stack direction="row" spacing={1}>
                <Button size="small" variant="contained" sx={portalPrimaryButtonSx} onClick={() => handleClaim(container.uuid)}>
                  Claim
                </Button>
                <Button size="small" variant="outlined" sx={portalOutlinedButtonSx} onClick={() => setAssignTarget(container.uuid)}>
                  Assign driver
                </Button>
              </Stack>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={Boolean(assignTarget)} onClose={() => !assigning && setAssignTarget(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Assign driver</DialogTitle>
        <DialogContent>
          <TextField
            label="Driver user UUID"
            value={driverUuid}
            onChange={(e) => setDriverUuid(e.target.value)}
            fullWidth
            sx={{ mt: 1 }}
            helperText="Enter the driver's account UUID from user management."
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setAssignTarget(null)} disabled={assigning} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleAssign} variant="contained" disabled={assigning || !driverUuid.trim()} sx={portalPrimaryButtonSx}>
            {assigning ? 'Assigning…' : 'Assign'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
