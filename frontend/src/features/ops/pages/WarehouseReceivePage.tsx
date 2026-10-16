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
  Typography,
} from '@mui/material'
import {
  useGetReceivableContainersQuery,
  useGetWarehouseFacilitiesQuery,
  useReceiveContainerMutation,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { formatMavLibraryCommodity } from '../utils/mavCommodityDisplay'

export function WarehouseReceivePage() {
  const { data: containersData, isError: containersError } = useGetReceivableContainersQuery()
  const { data: facilitiesData, isError: facilitiesError } = useGetWarehouseFacilitiesQuery()
  const [receiveContainer, { isLoading, isSuccess, isError }] = useReceiveContainerMutation()
  const [selectedContainer, setSelectedContainer] = useState('')
  const [facilityId, setFacilityId] = useState('')
  const [locationCode, setLocationCode] = useState('')

  const containers = containersData?.data ?? []
  const facilities = facilitiesData?.data ?? []
  const selected = containers.find((c) => c.uuid === selectedContainer)
  const selectedMav = selected ? formatMavLibraryCommodity(selected) : null

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
      {facilitiesError && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Could not load warehouse facilities. Ensure you are signed in as warehouse staff.
        </Alert>
      )}
      {isSuccess && <Alert severity="success" sx={{ mb: 2 }}>Container received into inventory.</Alert>}
      {isError && <Alert severity="error" sx={{ mb: 2 }}>Failed to receive container.</Alert>}

      <PortalPanel title="Receive form">
        <Stack spacing={2} sx={{ maxWidth: 480, p: 2.5 }}>
          <TextField select label="Container" value={selectedContainer} onChange={(e) => setSelectedContainer(e.target.value)} required fullWidth>
            {containers.map((c) => {
              const mav = formatMavLibraryCommodity(c)
              return (
                <MenuItem key={c.uuid} value={c.uuid}>
                  {c.containerNumber} — {c.entryReference} — {mav.primary}
                </MenuItem>
              )
            })}
          </TextField>
          {selectedMav && (
            <Alert severity={selected?.mavLibraryLinked ? 'info' : 'warning'}>
              <Typography sx={{ fontWeight: 600 }}>MAV commodity: {selectedMav.primary}</Typography>
              {selectedMav.secondary && (
                <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>{selectedMav.secondary}</Typography>
              )}
              {!selected?.mavLibraryLinked && (
                <Typography sx={{ fontSize: '0.8125rem', mt: 0.5 }}>
                  Receive is allowed, but TIS/TOS will not have a library commodity until the entry HS is selected from the MAV catalog.
                </Typography>
              )}
            </Alert>
          )}
          <TextField select label="Warehouse Facility" value={facilityId} onChange={(e) => setFacilityId(e.target.value)} required fullWidth>
            {facilities.length === 0 ? (
              <MenuItem value="" disabled>No active facilities</MenuItem>
            ) : (
              facilities.map((f) => (
                <MenuItem key={f.id} value={String(f.id)}>{f.name}</MenuItem>
              ))
            )}
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
          columns={['Container', 'Entry', 'MAV commodity', 'Status']}
          isEmpty={containers.length === 0}
          emptyMessage={containersError ? 'Failed to load receivable containers.' : 'No containers awaiting receipt.'}
        >
          {containers.map((c) => {
            const mav = formatMavLibraryCommodity(c)
            return (
              <TableRow key={c.uuid} hover>
                <TableCell>{c.containerNumber}</TableCell>
                <TableCell>{c.entryReference}</TableCell>
                <TableCell>
                  <Typography sx={{ fontSize: '0.875rem' }}>{mav.primary}</Typography>
                  {mav.secondary && (
                    <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{mav.secondary}</Typography>
                  )}
                </TableCell>
                <TableCell>{c.status}</TableCell>
              </TableRow>
            )
          })}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
