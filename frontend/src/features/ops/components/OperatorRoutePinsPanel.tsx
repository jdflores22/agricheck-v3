import { Alert, Button, Stack, Step, StepLabel, Stepper, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import {
  useBookOperatorDeliveryStopsMutation,
  useGetOperatorDeliveryStopsQuery,
  type BookOperatorDeliveryStopsBody,
} from '../api/opsApi'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { isContainerUuid } from '../utils/containerUuid'
import { OperatorDeliveryPinMap, type DeliveryPinCoords, type RouteWizardStep } from './OperatorDeliveryPinMap'

const DEFAULT_PICKUP: DeliveryPinCoords = { lat: 14.5862, lng: 120.9681 }
const DEFAULT_WAREHOUSE: DeliveryPinCoords = { lat: 14.5547, lng: 120.9872 }

const STEP_LABELS = ['Pickup point', 'Warehouse', 'Overview']

interface OperatorRoutePinsPanelProps {
  containerUuid?: string
  containerNumber?: string
  onSaved?: () => void
}

export function OperatorRoutePinsPanel({
  containerUuid,
  containerNumber,
  onSaved,
}: OperatorRoutePinsPanelProps) {
  const loadUuid = containerUuid?.trim() || ''
  const uuidValid = isContainerUuid(loadUuid)
  const { data, isLoading, isError, refetch } = useGetOperatorDeliveryStopsQuery(loadUuid, {
    skip: !uuidValid,
  })
  const [bookStops, { isLoading: saving }] = useBookOperatorDeliveryStopsMutation()

  const [wizardStep, setWizardStep] = useState<RouteWizardStep>(0)
  const [pickupLabel, setPickupLabel] = useState('Pickup point')
  const [warehouseLabel, setWarehouseLabel] = useState('Warehouse')
  const [pickup, setPickup] = useState<DeliveryPinCoords>(DEFAULT_PICKUP)
  const [warehouse, setWarehouse] = useState<DeliveryPinCoords>(DEFAULT_WAREHOUSE)
  const [pickupPlaced, setPickupPlaced] = useState(false)
  const [warehousePlaced, setWarehousePlaced] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const saveLabel = containerNumber?.trim() || data?.data?.containerNumber || 'Container'

  useEffect(() => {
    const stops = data?.data
    if (!stops) return
    setPickupLabel(stops.pickup.name || 'Pickup point')
    setPickup({ lat: Number(stops.pickup.latitude), lng: Number(stops.pickup.longitude) })
    setPickupPlaced(true)
    setWarehouseLabel(stops.destination.name || 'Warehouse')
    setWarehouse({
      lat: Number(stops.destination.latitude),
      lng: Number(stops.destination.longitude),
    })
    setWarehousePlaced(true)
  }, [data])

  const handlePickupChange = (coords: DeliveryPinCoords) => {
    setPickup(coords)
    setPickupPlaced(true)
  }

  const handleWarehouseChange = (coords: DeliveryPinCoords) => {
    setWarehouse(coords)
    setWarehousePlaced(true)
  }

  const handleNext = () => {
    setError('')
    if (wizardStep === 0 && !pickupPlaced) {
      setError('Tap the map to set the pickup pin before continuing.')
      return
    }
    if (wizardStep === 1 && !warehousePlaced) {
      setError('Tap the map to set the warehouse pin before continuing.')
      return
    }
    if (wizardStep < 2) setWizardStep((wizardStep + 1) as RouteWizardStep)
  }

  const handleBack = () => {
    setError('')
    if (wizardStep > 0) setWizardStep((wizardStep - 1) as RouteWizardStep)
  }

  const handleSave = async () => {
    setError('')
    setMessage('')
    if (!loadUuid) {
      setError('Enter the container UUID in step 1 above before saving.')
      return
    }
    if (!uuidValid) {
      setError(
        'Container UUID must be a real GUID (e.g. a1b2c3d4-e5f6-7890-abcd-ef1234567890), not the container number or a label.',
      )
      return
    }
    const body: BookOperatorDeliveryStopsBody = {
      pickupLabel: pickupLabel.trim(),
      pickupLatitude: pickup.lat,
      pickupLongitude: pickup.lng,
      destinationLabel: warehouseLabel.trim(),
      destinationLatitude: warehouse.lat,
      destinationLongitude: warehouse.lng,
    }
    try {
      await bookStops({ uuid: loadUuid, body }).unwrap()
      setMessage(`Route pins saved for ${saveLabel}. Assign a driver below so AgriTrack can navigate this load.`)
      onSaved?.()
      void refetch()
    } catch {
      setError(
        'Could not save pins. Use a valid container UUID, ensure status is awaiting confirmation (or already claimed by you), and try again.',
      )
    }
  }

  const hint =
    wizardStep === 0
      ? 'Step 1: Click the map to place the pickup pin (green).'
      : wizardStep === 1
        ? 'Step 2: Pickup is shown in green. Click the map to place the warehouse pin (orange).'
        : 'Step 3: Review both pins and the driving route, then save.'

  const canSave =
    uuidValid && pickupLabel.trim() && warehouseLabel.trim() && pickupPlaced && warehousePlaced

  return (
    <Stack spacing={2}>
      <Typography variant="body2" color="text.secondary">
        {loadUuid ? (
          uuidValid ? (
            <>
              Saving route for container <strong>{saveLabel}</strong>
            </>
          ) : (
            <>
              Fix the container UUID above — <strong>{loadUuid}</strong> is not a valid GUID.
            </>
          )
        ) : (
          'Enter or pick a container UUID above, then place pins and save.'
        )}
      </Typography>

      <Stepper activeStep={wizardStep} alternativeLabel>
        {STEP_LABELS.map((label) => (
          <Step key={label}>
            <StepLabel>{label}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {isError && uuidValid && (
        <Alert severity="warning">Could not load existing pins. You can still place new ones and save.</Alert>
      )}

      <OperatorDeliveryPinMap
        pickup={pickup}
        warehouse={warehouse}
        wizardStep={wizardStep}
        pickupPlaced={pickupPlaced}
        warehousePlaced={warehousePlaced}
        onPickupChange={handlePickupChange}
        onWarehouseChange={handleWarehouseChange}
        hint={hint}
        height={wizardStep === 2 ? 320 : 380}
      />

      {wizardStep === 0 && (
        <TextField
          label="Pickup label"
          value={pickupLabel}
          onChange={(e) => setPickupLabel(e.target.value)}
          fullWidth
          size="small"
        />
      )}
      {wizardStep === 1 && (
        <TextField
          label="Warehouse label"
          value={warehouseLabel}
          onChange={(e) => setWarehouseLabel(e.target.value)}
          fullWidth
          size="small"
        />
      )}
      {wizardStep === 2 && (
        <Stack spacing={1.5}>
          <Typography variant="body2">
            <strong>Pickup:</strong> {pickupLabel} ({pickup.lat.toFixed(5)}, {pickup.lng.toFixed(5)})
          </Typography>
          <Typography variant="body2">
            <strong>Warehouse:</strong> {warehouseLabel} ({warehouse.lat.toFixed(5)}, {warehouse.lng.toFixed(5)})
          </Typography>
        </Stack>
      )}

      {message && <Alert severity="success" onClose={() => setMessage('')}>{message}</Alert>}
      {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}

      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
        {wizardStep > 0 && (
          <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={handleBack}>
            Back
          </Button>
        )}
        {wizardStep < 2 && (
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={handleNext}>
            Next
          </Button>
        )}
        {wizardStep === 2 && (
          <Button
            variant="contained"
            sx={portalPrimaryButtonSx}
            onClick={handleSave}
            disabled={saving || isLoading || !canSave}
          >
            {saving ? 'Saving…' : 'Save route pins'}
          </Button>
        )}
      </Stack>
    </Stack>
  )
}
