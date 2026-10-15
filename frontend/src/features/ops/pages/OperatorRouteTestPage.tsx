import ContentCopyIcon from '@mui/icons-material/ContentCopy'
import RefreshIcon from '@mui/icons-material/Refresh'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { OperatorRoutePinsPanel } from '../components/OperatorRoutePinsPanel'
import {
  useAssignDriverToContainerMutation,
  useGetClaimableContainersQuery,
  useGetOperatorDriversQuery,
  useGetOperatorVehiclesQuery,
  useProvisionRouteTestContainerMutation,
  type OpsContainerListItem,
} from '../api/opsApi'

type TestLoad = { uuid: string; containerNumber: string }

function pickRandomClaimable(rows: OpsContainerListItem[]): TestLoad | null {
  if (!rows.length) return null
  const row = rows[Math.floor(Math.random() * rows.length)]
  return { uuid: row.uuid, containerNumber: row.containerNumber }
}

export function OperatorRouteTestPage() {
  const [testLoad, setTestLoad] = useState<TestLoad | null>(null)
  const [provisionError, setProvisionError] = useState('')
  const [pageMessage, setPageMessage] = useState('')
  const [assignError, setAssignError] = useState('')
  const [pinsSaved, setPinsSaved] = useState(false)
  const [driverUuid, setDriverUuid] = useState('')
  const [vehicleUuid, setVehicleUuid] = useState('')
  const [loadingLoad, setLoadingLoad] = useState(true)
  const bootedRef = useRef(false)

  const { data: claimableData, refetch: refetchClaimable } = useGetClaimableContainersQuery()
  const { data: driversData } = useGetOperatorDriversQuery()
  const { data: vehiclesData } = useGetOperatorVehiclesQuery()
  const [provision] = useProvisionRouteTestContainerMutation()
  const [assignDriver, { isLoading: assigning }] = useAssignDriverToContainerMutation()

  const drivers = driversData?.data ?? []
  const vehicles = (vehiclesData?.data ?? []).filter((v) => v.isActive)
  const claimable = claimableData?.data ?? []

  const loadTestContainer = async (regenerate: boolean) => {
    setLoadingLoad(true)
    if (regenerate) {
      setPinsSaved(false)
      setPageMessage('')
      setAssignError('')
    }
    setProvisionError('')

    try {
      const created = await provision().unwrap()
      setTestLoad({ uuid: created.data.uuid, containerNumber: created.data.containerNumber })
      void refetchClaimable()
      return
    } catch {
      const fallback = pickRandomClaimable(claimable)
      if (fallback) {
        setTestLoad(fallback)
        setProvisionError(
          'Server auto-create is not available yet (deploy latest API). Using a random claimable load from your list.',
        )
        return
      }
      setTestLoad(null)
      setProvisionError(
        'Could not create a test load. Deploy the latest API to Railway, or ensure at least one claimable container exists.',
      )
    } finally {
      setLoadingLoad(false)
    }
  }

  useEffect(() => {
    if (bootedRef.current) return
    bootedRef.current = true
    void loadTestContainer(false)
  }, [])

  useEffect(() => {
    if (testLoad || loadingLoad) return
    if (!claimableData) return
    void loadTestContainer(false)
  }, [claimableData, testLoad, loadingLoad])

  const copyUuid = async () => {
    if (!testLoad?.uuid) return
    try {
      await navigator.clipboard.writeText(testLoad.uuid)
      setPageMessage('UUID copied to clipboard.')
    } catch {
      setPageMessage('Copy failed — select the UUID field and copy manually.')
    }
  }

  const handleVehicleChange = (nextVehicleUuid: string) => {
    setVehicleUuid(nextVehicleUuid)
    const vehicle = vehicles.find((v) => v.uuid === nextVehicleUuid)
    if (vehicle?.defaultDriverUuid) {
      setDriverUuid(vehicle.defaultDriverUuid)
    }
  }

  const handleAssign = async () => {
    if (!testLoad || !driverUuid || !vehicleUuid) return
    setAssignError('')
    setPageMessage('')
    try {
      await assignDriver({
        uuid: testLoad.uuid,
        driverUserUuid: driverUuid,
        vehicleUuid,
      }).unwrap()
      setPageMessage('Driver assigned. Open AgriTrack on the driver phone to test navigation.')
    } catch {
      setAssignError('Assign failed. Save route pins first, then pick a driver and vehicle from your fleet.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="AgriTrack"
        title="Manual route booking"
        subtitle="A test container and UUID are prepared for you. Place pickup and warehouse on the map, save, then assign a driver."
      />

      <Box sx={{ mb: 2 }}>
        <Button component={RouterLink} to="/operator/containers" variant="outlined" sx={portalOutlinedButtonSx}>
          Back to containers
        </Button>
      </Box>

      <PortalPanel title="Test load (auto)">
        <Stack spacing={2} sx={{ p: 2.5 }}>
          {loadingLoad && !testLoad ? (
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <CircularProgress size={22} />
              <Typography variant="body2">Creating test container…</Typography>
            </Stack>
          ) : testLoad ? (
            <>
              <Typography variant="body2" color="text.secondary">
                Use these values for driver testing — you only need to set pins on the map below.
              </Typography>
              <TextField
                label="Container UUID"
                value={testLoad.uuid}
                fullWidth
                size="small"
                slotProps={{
                  input: {
                    readOnly: true,
                    endAdornment: (
                      <Tooltip title="Copy UUID">
                        <IconButton onClick={copyUuid} edge="end" aria-label="Copy container UUID">
                          <ContentCopyIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    ),
                  },
                }}
              />
              <TextField
                label="Container number"
                value={testLoad.containerNumber}
                fullWidth
                size="small"
                slotProps={{ input: { readOnly: true } }}
              />
              <Button
                variant="outlined"
                sx={portalOutlinedButtonSx}
                startIcon={<RefreshIcon />}
                onClick={() => void loadTestContainer(true)}
                disabled={loadingLoad}
              >
                {loadingLoad ? 'Creating…' : 'New random test load'}
              </Button>
            </>
          ) : null}
          {provisionError && <Alert severity="warning">{provisionError}</Alert>}
          {pageMessage && (
            <Alert severity="success" onClose={() => setPageMessage('')}>{pageMessage}</Alert>
          )}
        </Stack>
      </PortalPanel>

      {testLoad && (
        <>
          <PortalPanel title="Route pins">
            <Box sx={{ p: 2.5 }}>
              <OperatorRoutePinsPanel
                key={testLoad.uuid}
                containerUuid={testLoad.uuid}
                containerNumber={testLoad.containerNumber}
                onSaved={() => setPinsSaved(true)}
              />
            </Box>
          </PortalPanel>

          <PortalPanel title="Assign driver">
            <Stack spacing={2} sx={{ p: 2.5 }}>
              {!pinsSaved && (
                <Alert severity="info">After the route looks good, save pins above, then assign your driver here.</Alert>
              )}
              {assignError && <Alert severity="error">{assignError}</Alert>}
              {drivers.length === 0 && (
                <Typography color="warning.main" variant="body2">
                  No drivers in your fleet yet.
                </Typography>
              )}
              {vehicles.length === 0 && (
                <Typography color="warning.main" variant="body2">
                  No vehicles registered.
                </Typography>
              )}
              <FormControl fullWidth size="small">
                <InputLabel>Vehicle</InputLabel>
                <Select label="Vehicle" value={vehicleUuid} onChange={(e) => handleVehicleChange(e.target.value)}>
                  {vehicles.map((vehicle) => (
                    <MenuItem key={vehicle.uuid} value={vehicle.uuid}>
                      {vehicle.plateNumber} — {vehicle.vehicleType}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl fullWidth size="small">
                <InputLabel>Driver</InputLabel>
                <Select label="Driver" value={driverUuid} onChange={(e) => setDriverUuid(e.target.value)}>
                  {drivers.map((driver) => (
                    <MenuItem key={driver.userUuid} value={driver.userUuid}>
                      {driver.fullName} ({driver.email})
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Button
                variant="contained"
                sx={portalPrimaryButtonSx}
                onClick={handleAssign}
                disabled={assigning || !driverUuid || !vehicleUuid}
              >
                {assigning ? 'Assigning…' : 'Assign driver'}
              </Button>
            </Stack>
          </PortalPanel>
        </>
      )}
    </Box>
  )
}
