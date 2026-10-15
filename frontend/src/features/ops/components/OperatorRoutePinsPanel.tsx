import { Alert, Button, Stack, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import {
  useBookOperatorDeliveryStopsMutation,
  useGetOperatorDeliveryStopsQuery,
  type BookOperatorDeliveryStopsBody,
} from '../api/opsApi'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { OperatorDeliveryPinMap, type DeliveryPinCoords, type DeliveryPinRole } from './OperatorDeliveryPinMap'

const DEFAULT_PICKUP: DeliveryPinCoords = { lat: 14.5862, lng: 120.9681 }
const DEFAULT_WAREHOUSE: DeliveryPinCoords = { lat: 14.5547, lng: 120.9872 }

interface OperatorRoutePinsPanelProps {
  containerUuid: string
  containerNumber: string
  onSaved?: () => void
}

export function OperatorRoutePinsPanel({ containerUuid, containerNumber, onSaved }: OperatorRoutePinsPanelProps) {
  const { data, isLoading, isError, refetch } = useGetOperatorDeliveryStopsQuery(containerUuid)
  const [bookStops, { isLoading: saving }] = useBookOperatorDeliveryStopsMutation()

  const [pickupLabel, setPickupLabel] = useState('Pickup point')
  const [warehouseLabel, setWarehouseLabel] = useState('Warehouse')
  const [pickup, setPickup] = useState<DeliveryPinCoords>(DEFAULT_PICKUP)
  const [warehouse, setWarehouse] = useState<DeliveryPinCoords>(DEFAULT_WAREHOUSE)
  const [activePin, setActivePin] = useState<DeliveryPinRole>('pickup')
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    const stops = data?.data
    if (!stops) return
    setPickupLabel(stops.pickup.name || 'Pickup point')
    setPickup({ lat: Number(stops.pickup.latitude), lng: Number(stops.pickup.longitude) })
    setWarehouseLabel(stops.destination.name || 'Warehouse')
    setWarehouse({
      lat: Number(stops.destination.latitude),
      lng: Number(stops.destination.longitude),
    })
  }, [data])

  const handleSave = async () => {
    setError('')
    setMessage('')
    const body: BookOperatorDeliveryStopsBody = {
      pickupLabel: pickupLabel.trim(),
      pickupLatitude: pickup.lat,
      pickupLongitude: pickup.lng,
      destinationLabel: warehouseLabel.trim(),
      destinationLatitude: warehouse.lat,
      destinationLongitude: warehouse.lng,
    }
    try {
      await bookStops({ uuid: containerUuid, body }).unwrap()
      setMessage(`Saved route pins for ${containerNumber}. Assign a driver so AgriTrack map uses these coordinates.`)
      onSaved?.()
      void refetch()
    } catch {
      setError('Could not save pins. Container may be in a status that blocks changes, or you may need operator access.')
    }
  }

  return (
    <Stack spacing={2}>
      <Typography variant="body2" color="text.secondary">
        Container <strong>{containerNumber}</strong> — set pickup and warehouse for driver navigation testing.
        Saving without a prior claim will claim the load for you when status is awaiting confirmation.
      </Typography>

      {isError && (
        <Alert severity="warning">Could not load existing pins. You can still place new ones and save.</Alert>
      )}

      <OperatorDeliveryPinMap
        pickup={pickup}
        warehouse={warehouse}
        activePin={activePin}
        onActivePinChange={setActivePin}
        onPickupChange={setPickup}
        onWarehouseChange={setWarehouse}
      />

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          label="Pickup label"
          value={pickupLabel}
          onChange={(e) => setPickupLabel(e.target.value)}
          fullWidth
          size="small"
        />
        <TextField
          label="Warehouse label"
          value={warehouseLabel}
          onChange={(e) => setWarehouseLabel(e.target.value)}
          fullWidth
          size="small"
        />
      </Stack>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          label="Pickup lat"
          value={pickup.lat}
          onChange={(e) => setPickup({ ...pickup, lat: Number(e.target.value) })}
          fullWidth
          size="small"
          type="number"
          slotProps={{ htmlInput: { step: 'any' } }}
        />
        <TextField
          label="Pickup lng"
          value={pickup.lng}
          onChange={(e) => setPickup({ ...pickup, lng: Number(e.target.value) })}
          fullWidth
          size="small"
          type="number"
          slotProps={{ htmlInput: { step: 'any' } }}
        />
      </Stack>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <TextField
          label="Warehouse lat"
          value={warehouse.lat}
          onChange={(e) => setWarehouse({ ...warehouse, lat: Number(e.target.value) })}
          fullWidth
          size="small"
          type="number"
          slotProps={{ htmlInput: { step: 'any' } }}
        />
        <TextField
          label="Warehouse lng"
          value={warehouse.lng}
          onChange={(e) => setWarehouse({ ...warehouse, lng: Number(e.target.value) })}
          fullWidth
          size="small"
          type="number"
          slotProps={{ htmlInput: { step: 'any' } }}
        />
      </Stack>

      {message && <Alert severity="success" onClose={() => setMessage('')}>{message}</Alert>}
      {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}

      <Stack direction="row" spacing={1}>
        <Button
          variant="contained"
          sx={portalPrimaryButtonSx}
          onClick={handleSave}
          disabled={saving || isLoading || !pickupLabel.trim() || !warehouseLabel.trim()}
        >
          {saving ? 'Saving…' : 'Save route pins'}
        </Button>
        <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={() => void refetch()} disabled={isLoading}>
          Reload
        </Button>
      </Stack>
    </Stack>
  )
}
