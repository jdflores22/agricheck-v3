import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import {
  useGetReceivableContainersQuery,
  useGetWarehouseFacilitiesQuery,
  useReceiveContainerMutation,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function WarehouseReceivePage() {
  const { data: containersData } = useGetReceivableContainersQuery()
  const { data: facilitiesData } = useGetWarehouseFacilitiesQuery()
  const [receiveContainer, { isLoading, isSuccess, isError }] = useReceiveContainerMutation()
  const [selectedContainer, setSelectedContainer] = useState('')
  const [facilityId, setFacilityId] = useState('')
  const [locationCode, setLocationCode] = useState('')

  const containers = containersData?.data ?? []
  const facilities = facilitiesData?.data ?? []

  const handleSubmit = async () => {
    if (!selectedContainer || !facilityId) return
    await receiveContainer({
      containerUuid: selectedContainer,
      warehouseFacilityId: Number(facilityId),
      locationCode: locationCode || undefined,
    })
    setSelectedContainer('')
    setLocationCode('')
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Receive Container"
        subtitle="Receive inbound containers into warehouse inventory."
      />
      {isSuccess && <Alert severity="success" sx={{ mb: 2 }}>Container received into inventory.</Alert>}
      {isError && <Alert severity="error" sx={{ mb: 2 }}>Failed to receive container.</Alert>}

      <PortalPanel title="Receive form">
        <Stack spacing={2} sx={{ maxWidth: 480, p: 2.5 }}>
          <TextField select label="Container" value={selectedContainer} onChange={(e) => setSelectedContainer(e.target.value)} required fullWidth>
            {containers.map((c) => (
              <MenuItem key={c.uuid} value={c.uuid}>{c.containerNumber} — {c.entryReference}</MenuItem>
            ))}
          </TextField>
          <TextField select label="Warehouse Facility" value={facilityId} onChange={(e) => setFacilityId(e.target.value)} required fullWidth>
            {facilities.map((f) => (
              <MenuItem key={f.id} value={String(f.id)}>{f.name}</MenuItem>
            ))}
          </TextField>
          <TextField label="Location Code" value={locationCode} onChange={(e) => setLocationCode(e.target.value)} fullWidth />
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={handleSubmit} disabled={isLoading || !selectedContainer || !facilityId}>
            Receive Container
          </Button>
        </Stack>
      </PortalPanel>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="Awaiting receipt"
          columns={['Container', 'Entry', 'Status']}
          isEmpty={containers.length === 0}
          emptyMessage="No containers awaiting receipt."
        >
          {containers.map((c) => (
            <TableRow key={c.uuid} hover>
              <TableCell>{c.containerNumber}</TableCell>
              <TableCell>{c.entryReference}</TableCell>
              <TableCell>{c.status}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
