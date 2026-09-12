import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Stack,
  TableCell,
  TableRow,
} from '@mui/material'
import {
  useGetDriverContainersQuery,
  useRecordContainerLocationMutation,
  useUpdateContainerStatusMutation,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'

export function DriverContainersPage() {
  const { data, isLoading } = useGetDriverContainersQuery()
  const [updateStatus] = useUpdateContainerStatusMutation()
  const [recordLocation] = useRecordContainerLocationMutation()
  const [message, setMessage] = useState('')

  const containers = data?.data ?? []

  const handleStatus = async (uuid: string, status: string) => {
    try {
      await updateStatus({ uuid, status }).unwrap()
      setMessage(`Container marked as ${status}.`)
    } catch {
      setMessage('Failed to update container status.')
    }
  }

  const handleLocation = async (uuid: string) => {
    try {
      await recordLocation({ uuid, latitude: 14.5995, longitude: 120.9842 }).unwrap()
      setMessage('Location recorded (demo coordinates: Manila).')
    } catch {
      setMessage('Failed to record location.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="My Containers"
        subtitle="Update status and record GPS location for assigned containers."
      />
      {message && <Alert severity="info" sx={{ mb: 2 }} onClose={() => setMessage('')}>{message}</Alert>}
      <PortalTablePanel
        title="Assigned containers"
        columns={['Container', 'Entry', 'Status', 'Last Location', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && containers.length === 0}
        emptyMessage="No assigned containers."
      >
        {containers.map((c) => (
          <TableRow key={c.uuid} hover>
            <TableCell>{c.containerNumber}</TableCell>
            <TableCell>{c.entryReference}</TableCell>
            <TableCell>{c.status}</TableCell>
            <TableCell>
              {c.lastLatitude != null ? `${c.lastLatitude}, ${c.lastLongitude}` : '—'}
            </TableCell>
            <TableCell>
              <Stack direction="row" spacing={1}>
                <Button size="small" onClick={() => handleStatus(c.uuid, 'InTransit')}>In Transit</Button>
                <Button size="small" onClick={() => handleStatus(c.uuid, 'AtWarehouse')}>At Warehouse</Button>
                <Button size="small" onClick={() => handleLocation(c.uuid)}>Record GPS</Button>
              </Stack>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
