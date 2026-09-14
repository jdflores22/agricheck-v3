import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import {
  useCreateOperatorVehicleMutation,
  useGetOperatorDriversQuery,
  useGetOperatorVehiclesQuery,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function OperatorVehiclesPage() {
  const { data, isLoading, refetch } = useGetOperatorVehiclesQuery()
  const { data: driversData } = useGetOperatorDriversQuery()
  const [createVehicle, { isLoading: creating }] = useCreateOperatorVehicleMutation()

  const [plateNumber, setPlateNumber] = useState('')
  const [vehicleType, setVehicleType] = useState('')
  const [description, setDescription] = useState('')
  const [defaultDriverUuid, setDefaultDriverUuid] = useState('')
  const [message, setMessage] = useState('')

  const vehicles = data?.data ?? []
  const drivers = driversData?.data ?? []

  const handleCreate = async () => {
    if (!plateNumber.trim() || !vehicleType.trim()) return
    try {
      await createVehicle({
        plateNumber: plateNumber.trim(),
        vehicleType: vehicleType.trim(),
        description: description.trim() || undefined,
        defaultDriverUuid: defaultDriverUuid || undefined,
      }).unwrap()
      setPlateNumber('')
      setVehicleType('')
      setDescription('')
      setDefaultDriverUuid('')
      setMessage('Vehicle added to your fleet.')
      refetch()
    } catch {
      setMessage('Failed to add vehicle. Check plate number is unique.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="AgriTrack"
        title="Fleet vehicles"
        subtitle="Register trucks and vans used for container delivery. Assign these when dispatching drivers."
      />

      {message && (
        <Alert severity="info" sx={{ mb: 2 }} onClose={() => setMessage('')}>
          {message}
        </Alert>
      )}

      <PortalPanel title="Add vehicle">
        <Stack spacing={2} sx={{ p: 2.5, maxWidth: 720 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label="Plate number"
              value={plateNumber}
              onChange={(e) => setPlateNumber(e.target.value)}
              fullWidth
            />
            <TextField
              label="Vehicle type"
              value={vehicleType}
              onChange={(e) => setVehicleType(e.target.value)}
              placeholder="e.g. 6-wheeler, wing van"
              fullWidth
            />
          </Stack>
          <TextField
            label="Description (optional)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            fullWidth
          />
          <FormControl fullWidth>
            <InputLabel>Default driver (optional)</InputLabel>
            <Select
              label="Default driver (optional)"
              value={defaultDriverUuid}
              onChange={(e) => setDefaultDriverUuid(e.target.value)}
            >
              <MenuItem value="">None</MenuItem>
              {drivers.map((driver) => (
                <MenuItem key={driver.userUuid} value={driver.userUuid}>
                  {driver.fullName}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          <Button
            variant="contained"
            sx={portalPrimaryButtonSx}
            onClick={handleCreate}
            disabled={creating || !plateNumber.trim() || !vehicleType.trim()}
          >
            Add vehicle
          </Button>
        </Stack>
      </PortalPanel>

      <PortalTablePanel
        title="Registered vehicles"
        columns={['Plate', 'Type', 'Default driver', 'Description']}
        isLoading={isLoading}
        isEmpty={!isLoading && vehicles.length === 0}
        emptyMessage="No vehicles registered yet."
      >
        {vehicles.map((vehicle) => (
          <TableRow key={vehicle.uuid} hover>
            <TableCell>{vehicle.plateNumber}</TableCell>
            <TableCell>{vehicle.vehicleType}</TableCell>
            <TableCell>{vehicle.defaultDriverName ?? '—'}</TableCell>
            <TableCell>{vehicle.description ?? '—'}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
