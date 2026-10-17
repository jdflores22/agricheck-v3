import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  TableCell,
  TableRow,
  Typography,
} from '@mui/material'
import {
  useGetDriverContainersQuery,
  useGetDriverContainerTrackQuery,
  useRecordContainerLocationMutation,
  useUpdateContainerStatusMutation,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { DriverDeliveryTrackCards } from '../components/DriverDeliveryTrackCards'

export function DriverContainersPage() {
  const { data, isLoading } = useGetDriverContainersQuery()
  const [updateStatus] = useUpdateContainerStatusMutation()
  const [recordLocation] = useRecordContainerLocationMutation()
  const [message, setMessage] = useState('')
  const [selectedUuid, setSelectedUuid] = useState<string | null>(null)

  const {
    data: trackData,
    isLoading: trackLoading,
    isFetching: trackFetching,
  } = useGetDriverContainerTrackQuery(selectedUuid ?? '', {
    skip: !selectedUuid,
    pollingInterval: selectedUuid ? 15_000 : 0,
  })

  const containers = data?.data ?? []
  const track = trackData?.data

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
        subtitle="Update status, record GPS, and view trip + DA NMIS handoff progress (aligned with doctor QR scan and inspection)."
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
          <TableRow key={c.uuid} hover selected={selectedUuid === c.uuid}>
            <TableCell>{c.containerNumber}</TableCell>
            <TableCell>{c.entryReference}</TableCell>
            <TableCell>{c.status}</TableCell>
            <TableCell>
              {c.lastLatitude != null ? `${c.lastLatitude}, ${c.lastLongitude}` : '—'}
            </TableCell>
            <TableCell>
              <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                <Button
                  size="small"
                  variant={selectedUuid === c.uuid ? 'contained' : 'outlined'}
                  onClick={() => setSelectedUuid(selectedUuid === c.uuid ? null : c.uuid)}
                >
                  Trip & DA NMIS
                </Button>
                <Button size="small" onClick={() => handleStatus(c.uuid, 'InTransit')}>In Transit</Button>
                <Button size="small" onClick={() => handleStatus(c.uuid, 'AtWarehouse')}>At Warehouse</Button>
                <Button size="small" onClick={() => handleLocation(c.uuid)}>Record GPS</Button>
              </Stack>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      {selectedUuid && (
        <Box sx={{ mt: 2 }}>
          {trackLoading && !track ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <CircularProgress size={20} />
              <span>Loading trip status…</span>
            </Stack>
          ) : track ? (
            <>
              {trackFetching && (
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                  Refreshing…
                </Typography>
              )}
              <DriverDeliveryTrackCards track={track} />
            </>
          ) : (
            <Alert severity="warning">Could not load trip status for this container.</Alert>
          )}
        </Box>
      )}
    </Box>
  )
}
