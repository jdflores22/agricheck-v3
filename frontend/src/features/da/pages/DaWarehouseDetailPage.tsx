import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import ChevronRightIcon from '@mui/icons-material/ChevronRight'
import HistoryOutlinedIcon from '@mui/icons-material/HistoryOutlined'
import InboxOutlinedIcon from '@mui/icons-material/InboxOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined'
import MapOutlinedIcon from '@mui/icons-material/MapOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  LinearProgress,
  Stack,
  Tab,
  TableCell,
  TableRow,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { DaWarehouseLocationMap } from '../components/DaWarehouseLocationMap'
import {
  useGetDaWarehouseDetailQuery,
  useGetDaWarehouseOperatingLicenseQuery,
  useIssueDaWarehouseOperatingLicenseMutation,
  useRegisterDaWarehouseCswMutation,
  useVerifyDaFacilityRegistrationMutation,
} from '../api/daApi'

function LocationStep({ label, value }: { label: string; value?: string | null }) {
  if (!value) return null
  return (
    <Box sx={{ minWidth: 0 }}>
      <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 0.25 }}>
        {label}
      </Typography>
      <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>{value}</Typography>
    </Box>
  )
}

export function DaWarehouseDetailPage() {
  const { id = '' } = useParams()
  const warehouseId = Number(id)
  const [inventoryTab, setInventoryTab] = useState<'stored' | 'history'>('stored')
  const { data, isLoading, error, refetch } = useGetDaWarehouseDetailQuery(warehouseId, { skip: !warehouseId })
  const [verifyRegistration, { isLoading: verifying }] = useVerifyDaFacilityRegistrationMutation()
  const [registerCsw, { isLoading: registeringCsw }] = useRegisterDaWarehouseCswMutation()
  const [cswOpen, setCswOpen] = useState(false)
  const [cswNumber, setCswNumber] = useState('')
  const [cswExpires, setCswExpires] = useState('')
  const [ltoOpen, setLtoOpen] = useState(false)
  const [ltoNumber, setLtoNumber] = useState('')
  const [ltoIssued, setLtoIssued] = useState('')
  const [ltoExpires, setLtoExpires] = useState('')
  const [ltoNotes, setLtoNotes] = useState('')
  const { data: ltoData, refetch: refetchLto } = useGetDaWarehouseOperatingLicenseQuery(warehouseId, { skip: !warehouseId })
  const [issueLto, { isLoading: issuingLto }] = useIssueDaWarehouseOperatingLicenseMutation()
  const detail = data?.data

  if (!warehouseId || Number.isNaN(warehouseId)) {
    return <Alert severity="error">Invalid warehouse id.</Alert>
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  if (error || !detail) {
    return <Alert severity="error">Warehouse not found.</Alert>
  }

  const { warehouse, storedContainers, releasedContainers, capacity, utilizationPercent, totalVolumeKg, commodities, inventory } = detail
  const storedInventory = inventory.filter((item) => item.status === 'Stored')
  const releasedInventory = inventory.filter((item) => item.status !== 'Stored')
  const availableSlots = Math.max(capacity - storedContainers, 0)
  const locationSteps = [
    warehouse.regionName,
    warehouse.provinceName,
    warehouse.cityName,
    warehouse.barangayName,
  ].filter(Boolean)

  return (
    <Box>
      <Box
        sx={{
          mb: 3,
          borderRadius: '0.875rem',
          border: `1px solid ${portalColors.border}`,
          bgcolor: portalColors.bgWhite,
          overflow: 'hidden',
        }}
      >
        <Box
          sx={{
            px: { xs: 2, md: 3 },
            py: { xs: 2, md: 2.5 },
            background: `linear-gradient(135deg, ${portalColors.successSoft} 0%, ${portalColors.bgWhite} 55%)`,
            borderBottom: `1px solid ${portalColors.border}`,
          }}
        >
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ alignItems: { md: 'flex-start' }, justifyContent: 'space-between' }}>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start', minWidth: 0 }}>
              <Box
                sx={{
                  width: 52,
                  height: 52,
                  borderRadius: '0.875rem',
                  bgcolor: portalColors.primary,
                  color: '#fff',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                  boxShadow: '0 8px 20px rgba(22, 101, 52, 0.22)',
                }}
              >
                <WarehouseOutlinedIcon />
              </Box>
              <Box sx={{ minWidth: 0 }}>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: portalColors.primary, mb: 0.5 }}>
                  Warehouse Oversight
                </Typography>
                <Typography sx={{ fontSize: { xs: '1.25rem', md: '1.5rem' }, fontWeight: 700, color: portalColors.textDark, mb: 0.75 }}>
                  {warehouse.name}
                </Typography>
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mb: 1 }}>
                  <Chip size="small" label={warehouse.code} sx={{ fontWeight: 700, bgcolor: portalColors.bgWhite }} />
                  <Chip
                    size="small"
                    label={warehouse.isActive ? 'Active facility' : 'Inactive facility'}
                    sx={{
                      bgcolor: warehouse.isActive ? portalColors.successSoft : portalColors.bgMuted,
                      color: warehouse.isActive ? portalColors.successText : portalColors.textMuted,
                      fontWeight: 600,
                    }}
                  />
                  <Chip
                    size="small"
                    label={
                      warehouse.hasActiveWarehouseLto
                        ? `LTO: ${warehouse.warehouseLtoNumber ?? 'Active'}`
                        : 'LTO: not issued'
                    }
                    color={warehouse.hasActiveWarehouseLto ? 'success' : 'warning'}
                    sx={{ fontWeight: 600 }}
                  />
                  <Chip size="small" label={`RSAS: ${warehouse.rsasStatus ?? 'NotStarted'}`} sx={{ fontWeight: 600 }} />
                  <Chip size="small" label={`CSW: ${warehouse.cswStatus ?? 'NotStarted'}`} sx={{ fontWeight: 600 }} />
                  <Chip
                    size="small"
                    label={warehouse.destinationReady ? 'Destination ready' : 'Destination incomplete'}
                    color={warehouse.destinationReady ? 'success' : 'warning'}
                    variant="outlined"
                    sx={{ fontWeight: 600 }}
                  />
                </Stack>
                <Stack direction="row" spacing={1} sx={{ mb: 1, flexWrap: 'wrap' }}>
                  <Button size="small" variant="contained" sx={portalPrimaryButtonSx} onClick={() => setLtoOpen(true)}>
                    {warehouse.hasActiveWarehouseLto ? 'Record new warehouse LTO' : 'Issue warehouse LTO (DA)'}
                  </Button>
                  {ltoData?.data && (
                    <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, alignSelf: 'center' }}>
                      Current LTO expires {new Date(ltoData.data.expiresAt).toLocaleDateString()}
                    </Typography>
                  )}
                </Stack>
                {warehouse.rsasStatus === 'Submitted' ? (
                  <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
                    <Button
                      size="small"
                      variant="contained"
                      disabled={verifying}
                      sx={portalOutlinedButtonSx}
                      onClick={async () => {
                        await verifyRegistration({ warehouseId, approve: true })
                        refetch()
                      }}
                    >
                      Approve RSAS registration
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      color="error"
                      disabled={verifying}
                      onClick={async () => {
                        await verifyRegistration({ warehouseId, approve: false, reviewComments: 'Rejected by DA oversight' })
                        refetch()
                      }}
                    >
                      Reject
                    </Button>
                  </Stack>
                ) : null}
                {warehouse.rsasStatus === 'Registered' && !warehouse.destinationReady ? (
                  <Button
                    size="small"
                    variant="outlined"
                    sx={portalOutlinedButtonSx}
                    onClick={() => setCswOpen(true)}
                  >
                    Record CSW accreditation
                  </Button>
                ) : null}
                <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', color: portalColors.textMuted }}>
                  <LocationOnOutlinedIcon sx={{ fontSize: 16 }} />
                  <Typography sx={{ fontSize: '0.875rem' }}>{warehouse.formattedAddress}</Typography>
                </Stack>
              </Box>
            </Stack>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ flexShrink: 0 }}>
              <Button
                component={RouterLink}
                to={warehouse.regionId ? `/da/reports/stock?regionId=${warehouse.regionId}` : '/da/reports/stock'}
                variant="outlined"
                sx={portalOutlinedButtonSx}
              >
                Region stock
              </Button>
              <Button
                component={RouterLink}
                to="/da/warehouses"
                variant="outlined"
                startIcon={<ArrowBackIcon />}
                sx={portalOutlinedButtonSx}
              >
                Back to registry
              </Button>
            </Stack>
          </Stack>
        </Box>

        <Box sx={{ px: { xs: 2, md: 3 }, py: 2 }}>
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, alignItems: 'center', mb: 1.5 }}>
            <MapOutlinedIcon sx={{ fontSize: 18, color: portalColors.primary }} />
            <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
              Administrative location
            </Typography>
            {locationSteps.map((step, index) => (
              <Stack key={`${step}-${index}`} direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
                {index > 0 ? <ChevronRightIcon sx={{ fontSize: 16, color: portalColors.textLight }} /> : null}
                <Chip size="small" label={step} sx={{ bgcolor: portalColors.bgMuted, fontWeight: 600 }} />
              </Stack>
            ))}
          </Stack>

          <Box sx={{ mb: 0.5 }}>
            <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between', mb: 0.75 }}>
              <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
                Capacity utilization
              </Typography>
              <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                {storedContainers.toLocaleString()} / {capacity.toLocaleString()} slots ({utilizationPercent}%)
              </Typography>
            </Stack>
            <LinearProgress
              variant="determinate"
              value={Math.min(utilizationPercent, 100)}
              sx={{
                height: 8,
                borderRadius: 999,
                bgcolor: portalColors.bgMuted,
                '& .MuiLinearProgress-bar': {
                  borderRadius: 999,
                  bgcolor: utilizationPercent >= 90 ? '#b45309' : portalColors.primary,
                },
              }}
            />
          </Box>
        </Box>
      </Box>

      <PortalStatGrid
        items={[
          { key: 'storedContainers', label: 'Stored Containers', caption: `${availableSlots.toLocaleString()} slots available` },
          { key: 'releasedContainers', label: 'Released (All Time)', caption: 'Historical throughput' },
          { key: 'utilizationPercent', label: 'Capacity Used (%)', caption: `${capacity.toLocaleString()} total slots` },
          { key: 'totalVolumeKg', label: 'Stored commodity (KG)', caption: `${commodities.length} commodity type${commodities.length === 1 ? '' : 's'}` },
        ]}
        stats={{
          storedContainers,
          releasedContainers,
          utilizationPercent,
          totalVolumeKg,
        }}
        isLoading={false}
        columns={{ xs: '1fr 1fr', md: 'repeat(4, 1fr)' }}
      />

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="Commodities in this warehouse (MAV HS library)"
          columns={['Commodity', 'HS Code', 'Volume', 'Containers']}
          isLoading={false}
          isEmpty={commodities.length === 0}
          emptyMessage="No stored commodity yet. HS codes resolve from the MAV library or the importer entry form after warehouse receive."
        >
          {commodities.map((commodity) => (
            <TableRow key={`${commodity.hsCode}-${commodity.commodityName}`} hover>
              <TableCell sx={{ fontWeight: 700 }}>{commodity.commodityName}</TableCell>
              <TableCell>{commodity.hsCode}</TableCell>
              <TableCell>{commodity.volumeKg.toLocaleString()} KG</TableCell>
              <TableCell>{commodity.storedContainers}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Grid container spacing={3} sx={{ mt: 3 }}>
        <Grid size={{ xs: 12, md: 7 }}>
          <Box
            sx={{
              borderRadius: '0.875rem',
              border: `1px solid ${portalColors.border}`,
              bgcolor: portalColors.bgWhite,
              overflow: 'hidden',
            }}
          >
            <Box sx={{ px: 2.5, py: 2, borderBottom: `1px solid ${portalColors.border}` }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <MapOutlinedIcon sx={{ color: portalColors.primary }} />
                <Box>
                  <Typography sx={{ fontWeight: 700, color: portalColors.textDark }}>Facility location</Typography>
                  <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                    Pinpoint view of this warehouse on the map.
                  </Typography>
                </Box>
              </Stack>
            </Box>
            <Box sx={{ p: 2.5 }}>
              <DaWarehouseLocationMap warehouse={warehouse} height={300} />
            </Box>
          </Box>
        </Grid>

        <Grid size={{ xs: 12, md: 5 }}>
          <Box
            sx={{
              height: '100%',
              borderRadius: '0.875rem',
              border: `1px solid ${portalColors.border}`,
              bgcolor: portalColors.bgWhite,
              p: 2.5,
            }}
          >
            <Typography sx={{ fontWeight: 700, color: portalColors.textDark, mb: 2 }}>
              Facility snapshot
            </Typography>
            <Stack spacing={2.25}>
              <LocationStep label="Warehouse code" value={warehouse.code} />
              <LocationStep label="Region" value={warehouse.regionName} />
              <LocationStep label="Province" value={warehouse.provinceName} />
              <LocationStep label="City / Municipality" value={warehouse.cityName} />
              <LocationStep label="Barangay" value={warehouse.barangayName} />
              <LocationStep label="Street address" value={warehouse.streetAddress ?? warehouse.formattedAddress} />
              {warehouse.latitude != null && warehouse.longitude != null ? (
                <LocationStep
                  label="Coordinates"
                  value={`${Number(warehouse.latitude).toFixed(4)}, ${Number(warehouse.longitude).toFixed(4)}`}
                />
              ) : null}
            </Stack>
          </Box>
        </Grid>
      </Grid>

      <Box
        sx={{
          mt: 3,
          borderRadius: '0.875rem',
          border: `1px solid ${portalColors.border}`,
          bgcolor: portalColors.bgWhite,
          overflow: 'hidden',
        }}
      >
        <Box sx={{ px: 2.5, pt: 1.5, borderBottom: `1px solid ${portalColors.border}` }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ alignItems: { sm: 'center' }, justifyContent: 'space-between', pb: 1.5 }}>
            <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
              <Inventory2OutlinedIcon sx={{ color: portalColors.primary }} />
              <Box>
                <Typography sx={{ fontWeight: 700, color: portalColors.textDark }}>Inventory oversight</Typography>
                <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                  Monitor containers stored and recently released from this facility.
                </Typography>
              </Box>
            </Stack>
            <Tabs
              value={inventoryTab}
              onChange={(_, value: 'stored' | 'history') => setInventoryTab(value)}
              sx={{
                minHeight: 40,
                '& .MuiTab-root': { minHeight: 40, textTransform: 'none', fontWeight: 600 },
                '& .Mui-selected': { color: portalColors.primary },
                '& .MuiTabs-indicator': { bgcolor: portalColors.primary },
              }}
            >
              <Tab value="stored" label={`Stored (${storedInventory.length})`} />
              <Tab value="history" label={`Release history (${releasedInventory.length})`} />
            </Tabs>
          </Stack>
        </Box>

        {inventoryTab === 'stored' ? (
          storedInventory.length === 0 ? (
            <Box sx={{ textAlign: 'center', py: 6, px: 2 }}>
              <InboxOutlinedIcon sx={{ fontSize: 44, color: portalColors.textMuted, mb: 1.5 }} />
              <Typography sx={{ fontWeight: 600, mb: 0.5 }}>No stored containers</Typography>
              <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, maxWidth: 420, mx: 'auto' }}>
                This warehouse has no containers in storage right now. New arrivals will appear here once received by warehouse staff.
              </Typography>
            </Box>
          ) : (
            <PortalTablePanel
              title=""
              columns={['Container', 'Commodity', 'HS Code', 'Volume', 'Entry', 'Location', 'Status']}
              isLoading={false}
              isEmpty={false}
            >
              {storedInventory.map((item) => (
                <TableRow key={item.uuid} hover>
                  <TableCell>
                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                      <Inventory2OutlinedIcon sx={{ fontSize: 18, color: portalColors.primary }} />
                      <Typography sx={{ fontWeight: 600 }}>{item.containerNumber}</Typography>
                    </Stack>
                  </TableCell>
                  <TableCell>{item.commodityName ?? '—'}</TableCell>
                  <TableCell>{item.hsCode ?? '—'}</TableCell>
                  <TableCell>{item.volumeKg != null ? `${item.volumeKg.toLocaleString()} KG` : '—'}</TableCell>
                  <TableCell>{item.entryReference}</TableCell>
                  <TableCell>{item.locationCode ?? '—'}</TableCell>
                  <TableCell>
                    <Chip size="small" label={item.status} sx={portalStatusChipSx(item.status)} />
                  </TableCell>
                </TableRow>
              ))}
            </PortalTablePanel>
          )
        ) : releasedInventory.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 6, px: 2 }}>
            <HistoryOutlinedIcon sx={{ fontSize: 44, color: portalColors.textMuted, mb: 1.5 }} />
            <Typography sx={{ fontWeight: 600, mb: 0.5 }}>No release history yet</Typography>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, maxWidth: 420, mx: 'auto' }}>
              Released containers from this warehouse will be listed here for audit and oversight.
            </Typography>
          </Box>
        ) : (
          <PortalTablePanel
            title=""
            columns={['Container', 'Entry', 'Location', 'Status', 'Received', 'Received by']}
            isLoading={false}
            isEmpty={false}
          >
            {releasedInventory.map((item) => (
              <TableRow key={item.uuid} hover>
                <TableCell>{item.containerNumber}</TableCell>
                <TableCell>{item.entryReference}</TableCell>
                <TableCell>{item.locationCode ?? '—'}</TableCell>
                <TableCell>
                  <Chip size="small" label={item.status} sx={portalStatusChipSx(item.status)} />
                </TableCell>
                <TableCell>{new Date(item.receivedAt).toLocaleString()}</TableCell>
                <TableCell>{item.receivedByName ?? '—'}</TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
        )}
      </Box>

      <Dialog open={cswOpen} onClose={() => setCswOpen(false)} maxWidth="sm" fullWidth>
        <Box
          component="form"
          onSubmit={async (e: FormEvent) => {
            e.preventDefault()
            const accreditedAt = new Date().toISOString()
            const expiresAt = cswExpires ? new Date(cswExpires).toISOString() : ''
            if (!expiresAt) return
            await registerCsw({
              warehouseId,
              warehouseFacilityId: warehouseId,
              certificateNumber: cswNumber,
              accreditedAt,
              expiresAt,
            })
            setCswOpen(false)
            refetch()
          }}
        >
          <DialogTitle>Record CSW accreditation</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField
                label="CSW certificate number"
                size="small"
                required
                value={cswNumber}
                onChange={(e) => setCswNumber(e.target.value)}
                fullWidth
              />
              <TextField
                label="Expiry date"
                type="date"
                size="small"
                required
                value={cswExpires}
                onChange={(e) => setCswExpires(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                fullWidth
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => setCswOpen(false)} sx={portalOutlinedButtonSx}>Cancel</Button>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={registeringCsw}>
              Save CSW
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={ltoOpen} onClose={() => setLtoOpen(false)} maxWidth="sm" fullWidth>
        <Box
          component="form"
          onSubmit={async (e: FormEvent) => {
            e.preventDefault()
            if (!ltoIssued || !ltoExpires || !ltoNumber.trim()) return
            await issueLto({
              warehouseId,
              body: {
                licenseNumber: ltoNumber.trim(),
                issuedAt: new Date(ltoIssued).toISOString(),
                expiresAt: new Date(ltoExpires).toISOString(),
                notes: ltoNotes.trim() || undefined,
              },
            })
            setLtoOpen(false)
            refetch()
            refetchLto()
          }}
        >
          <DialogTitle>Warehouse operating license (LTO)</DialogTitle>
          <DialogContent>
            <Alert severity="info" sx={{ mb: 2 }}>
              Mother-unit DA action. Importers can only link and use warehouses with an active LTO for entry destinations and RSAS.
            </Alert>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField
                label="LTO number"
                size="small"
                required
                value={ltoNumber}
                onChange={(e) => setLtoNumber(e.target.value)}
                fullWidth
              />
              <TextField
                label="Issued date"
                type="date"
                size="small"
                required
                value={ltoIssued}
                onChange={(e) => setLtoIssued(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                fullWidth
              />
              <TextField
                label="Expiry date"
                type="date"
                size="small"
                required
                value={ltoExpires}
                onChange={(e) => setLtoExpires(e.target.value)}
                slotProps={{ inputLabel: { shrink: true } }}
                fullWidth
              />
              <TextField
                label="Notes (optional)"
                size="small"
                value={ltoNotes}
                onChange={(e) => setLtoNotes(e.target.value)}
                fullWidth
                multiline
                minRows={2}
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => setLtoOpen(false)} sx={portalOutlinedButtonSx}>Cancel</Button>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={issuingLto}>
              Save LTO
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
