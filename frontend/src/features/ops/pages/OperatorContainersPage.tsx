import { useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tab,
  TableCell,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner'
import {
  useAssignDriverToContainerMutation,
  useClaimOperatorContainerMutation,
  useGetClaimableContainersQuery,
  useGetClaimedContainersQuery,
  useGetOperatorDriversQuery,
  useGetOperatorVehiclesQuery,
  useScanAndClaimOperatorContainerMutation,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function OperatorContainersPage() {
  const [tab, setTab] = useState(0)
  const [search, setSearch] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [qrData, setQrData] = useState('')
  const [message, setMessage] = useState('')
  const [assignTarget, setAssignTarget] = useState<string | null>(null)
  const [driverUuid, setDriverUuid] = useState('')
  const [vehicleUuid, setVehicleUuid] = useState('')

  const { data: claimableData, isLoading: loadingClaimable } = useGetClaimableContainersQuery(search || undefined)
  const { data: claimedData, isLoading: loadingClaimed } = useGetClaimedContainersQuery()
  const { data: driversData } = useGetOperatorDriversQuery()
  const { data: vehiclesData } = useGetOperatorVehiclesQuery()

  const [claimContainer, { isLoading: claiming }] = useClaimOperatorContainerMutation()
  const [scanAndClaim, { isLoading: scanning }] = useScanAndClaimOperatorContainerMutation()
  const [assignDriver, { isLoading: assigning }] = useAssignDriverToContainerMutation()

  const claimable = claimableData?.data ?? []
  const claimed = claimedData?.data ?? []
  const drivers = driversData?.data ?? []
  const vehicles = (vehiclesData?.data ?? []).filter((v) => v.isActive)

  const pendingAssignment = useMemo(
    () => claimed.filter((c) => !c.assignedDriverUuid),
    [claimed],
  )

  const handleSearch = () => setSearch(searchInput.trim())

  const handleClaim = async (uuid: string) => {
    try {
      await claimContainer(uuid).unwrap()
      setMessage('Container claimed. Assign a driver from My loads.')
      setTab(1)
    } catch {
      setMessage('Failed to claim container.')
    }
  }

  const handleScanClaim = async () => {
    if (!qrData.trim()) return
    try {
      await scanAndClaim({ qrData: qrData.trim() }).unwrap()
      setMessage('Container claimed from transport QR.')
      setQrData('')
      setTab(1)
    } catch {
      setMessage('Invalid or expired transport QR. Paste the full QR payload from the agency transport tag.')
    }
  }

  const openAssign = (uuid: string) => {
    setAssignTarget(uuid)
    setDriverUuid('')
    setVehicleUuid('')
  }

  const handleVehicleChange = (nextVehicleUuid: string) => {
    setVehicleUuid(nextVehicleUuid)
    const vehicle = vehicles.find((v) => v.uuid === nextVehicleUuid)
    if (vehicle?.defaultDriverUuid) {
      setDriverUuid(vehicle.defaultDriverUuid)
    }
  }

  const handleAssign = async () => {
    if (!assignTarget || !driverUuid || !vehicleUuid) return
    try {
      await assignDriver({
        uuid: assignTarget,
        driverUserUuid: driverUuid,
        vehicleUuid,
      }).unwrap()
      setMessage('Driver and vehicle assigned successfully.')
      setAssignTarget(null)
      setDriverUuid('')
      setVehicleUuid('')
    } catch {
      setMessage('Failed to assign. Ensure the driver is registered under your fleet.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="AgriTrack"
        title="Container loads"
        subtitle="Claim transport-tagged containers, then assign your driver and truck."
      />

      {message && (
        <Alert severity="info" sx={{ mb: 2 }} onClose={() => setMessage('')}>
          {message}
        </Alert>
      )}

      <Tabs value={tab} onChange={(_, value) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label={`Claim (${claimable.length})`} />
        <Tab label={`My loads (${claimed.length})`} />
      </Tabs>

      {tab === 0 && (
        <>
          <PortalPanel title="Find or scan transport QR">
            <Stack spacing={2} sx={{ p: 2.5 }}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Search container or entry ref"
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  fullWidth
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                />
                <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={handleSearch}>
                  Search
                </Button>
              </Stack>
              <TextField
                label="Transport QR payload"
                value={qrData}
                onChange={(e) => setQrData(e.target.value)}
                fullWidth
                multiline
                minRows={2}
                placeholder="Paste scanned QR text from the agency transport tag"
              />
              <Button
                variant="contained"
                startIcon={<QrCodeScannerIcon />}
                sx={portalPrimaryButtonSx}
                onClick={handleScanClaim}
                disabled={scanning || !qrData.trim()}
              >
                {scanning ? 'Claiming…' : 'Scan & claim'}
              </Button>
            </Stack>
          </PortalPanel>

          <PortalTablePanel
            title="Awaiting operator claim"
            columns={['Container', 'Entry', 'Status', 'Actions']}
            isLoading={loadingClaimable}
            isEmpty={!loadingClaimable && claimable.length === 0}
            emptyMessage="No claimable containers. Agency must tag the container and set status to awaiting confirmation."
          >
            {claimable.map((container) => (
              <TableRow key={container.uuid} hover>
                <TableCell>{container.containerNumber}</TableCell>
                <TableCell>{container.entryReference}</TableCell>
                <TableCell>{container.status}</TableCell>
                <TableCell>
                  <Button
                    size="small"
                    variant="contained"
                    sx={portalPrimaryButtonSx}
                    onClick={() => handleClaim(container.uuid)}
                    disabled={claiming}
                  >
                    Claim
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
        </>
      )}

      {tab === 1 && (
        <>
          {pendingAssignment.length > 0 && (
            <Alert severity="warning" sx={{ mb: 2 }}>
              {pendingAssignment.length} load(s) need driver and vehicle assignment.
            </Alert>
          )}

          <PortalTablePanel
            title="My claimed loads"
            columns={['Container', 'Entry', 'Status', 'Driver', 'Vehicle', 'Actions']}
            isLoading={loadingClaimed}
            isEmpty={!loadingClaimed && claimed.length === 0}
            emptyMessage="No claimed containers yet. Claim a container from the Claim tab."
          >
            {claimed.map((container) => (
              <TableRow key={container.uuid} hover>
                <TableCell>{container.containerNumber}</TableCell>
                <TableCell>{container.entryReference}</TableCell>
                <TableCell>{container.status}</TableCell>
                <TableCell>{container.assignedDriverName ?? '—'}</TableCell>
                <TableCell>{container.assignedVehiclePlate ?? '—'}</TableCell>
                <TableCell>
                  {!container.assignedDriverUuid && (
                    <Button
                      size="small"
                      variant="outlined"
                      sx={portalOutlinedButtonSx}
                      onClick={() => openAssign(container.uuid)}
                    >
                      Assign
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
        </>
      )}

      <Dialog open={Boolean(assignTarget)} onClose={() => !assigning && setAssignTarget(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Assign driver and vehicle</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {drivers.length === 0 && (
              <Typography color="warning.main" variant="body2">
                No drivers in your fleet yet. Share an invite code so drivers can register under you.
              </Typography>
            )}
            {vehicles.length === 0 && (
              <Typography color="warning.main" variant="body2">
                No vehicles registered. Add trucks under Fleet vehicles first.
              </Typography>
            )}
            <FormControl fullWidth>
              <InputLabel>Vehicle</InputLabel>
              <Select
                label="Vehicle"
                value={vehicleUuid}
                onChange={(e) => handleVehicleChange(e.target.value)}
              >
                {vehicles.map((vehicle) => (
                  <MenuItem key={vehicle.uuid} value={vehicle.uuid}>
                    {vehicle.plateNumber} — {vehicle.vehicleType}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <FormControl fullWidth>
              <InputLabel>Driver</InputLabel>
              <Select
                label="Driver"
                value={driverUuid}
                onChange={(e) => setDriverUuid(e.target.value)}
              >
                {drivers.map((driver) => (
                  <MenuItem key={driver.userUuid} value={driver.userUuid}>
                    {driver.fullName} ({driver.email})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setAssignTarget(null)} disabled={assigning} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button
            onClick={handleAssign}
            variant="contained"
            disabled={assigning || !driverUuid || !vehicleUuid}
            sx={portalPrimaryButtonSx}
          >
            {assigning ? 'Assigning…' : 'Assign'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
