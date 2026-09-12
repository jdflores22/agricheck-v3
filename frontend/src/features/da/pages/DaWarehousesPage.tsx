import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Button,
  Chip,
  Grid,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMoreOutlined'
import { useMemo, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetBarangaysQuery, useGetCitiesQuery, useGetProvincesQuery, useGetRegionsQuery } from '../../addresses/addressApi'
import { DaWarehouseFormDialog } from '../components/DaWarehouseFormDialog'
import { useGetDaWarehousesQuery, type DaWarehouse } from '../api/daApi'
import {
  buildRegionSections,
  emptyLocationFilters,
  filterWarehouses,
  formatLocationPath,
  getWarehousesWithCoordinates,
  type LocationFilters,
} from '../utils/daWarehouseUtils'
import { DaWarehouseMap } from '../components/DaWarehouseMap'

export function DaWarehousesPage() {
  const [filters, setFilters] = useState<LocationFilters>(emptyLocationFilters)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editingWarehouse, setEditingWarehouse] = useState<DaWarehouse | null>(null)

  const { data, isLoading } = useGetDaWarehousesQuery()
  const { data: regionsData } = useGetRegionsQuery()
  const { data: provincesData } = useGetProvincesQuery(Number(filters.regionId), { skip: !filters.regionId })
  const { data: citiesData } = useGetCitiesQuery(Number(filters.provinceId), { skip: !filters.provinceId })
  const { data: barangaysData } = useGetBarangaysQuery(Number(filters.cityId), { skip: !filters.cityId })
  const warehouses = data?.data ?? []
  const regions = regionsData?.data ?? []
  const provinces = provincesData?.data ?? []
  const cities = citiesData?.data ?? []
  const barangays = barangaysData?.data ?? []

  const filteredWarehouses = useMemo(
    () => filterWarehouses(warehouses, filters),
    [filters, warehouses],
  )
  const regionSections = useMemo(
    () => buildRegionSections(filteredWarehouses),
    [filteredWarehouses],
  )

  const stats = useMemo(() => ({
    totalWarehouses: warehouses.length,
    activeWarehouses: warehouses.filter((item) => item.isActive).length,
    mappedWarehouses: getWarehousesWithCoordinates(warehouses).length,
    regionsCovered: new Set(warehouses.map((item) => item.regionId).filter(Boolean)).size,
  }), [warehouses])

  const openCreate = () => {
    setEditingWarehouse(null)
    setDialogOpen(true)
  }

  const openEdit = (warehouse: DaWarehouse) => {
    setEditingWarehouse(warehouse)
    setDialogOpen(true)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Registry"
        title="Registered Warehouses"
        subtitle="Maintain the DA warehouse registry by region, province, city/municipality, and barangay. Use the map to oversee warehouse coverage nationwide."
        actions={
          <Button variant="contained" startIcon={<AddOutlinedIcon />} sx={portalPrimaryButtonSx} onClick={openCreate}>
            Add Warehouse
          </Button>
        }
      />

      <PortalStatGrid
        items={[
          { key: 'totalWarehouses', label: 'Total Warehouses' },
          { key: 'activeWarehouses', label: 'Active' },
          { key: 'mappedWarehouses', label: 'Mapped on PH' },
          { key: 'regionsCovered', label: 'Regions Covered' },
        ]}
        stats={stats}
        isLoading={isLoading}
        columns={{ xs: '1fr 1fr', md: 'repeat(4, 1fr)' }}
      />

      <DaWarehouseMap warehouses={filteredWarehouses} />

      <Box sx={{ mt: 3, p: 2.5, borderRadius: '0.75rem', border: `1px solid ${portalColors.border}`, bgcolor: portalColors.bgWhite }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} sx={{ alignItems: { md: 'center' }, mb: 2 }}>
          <LocationOnOutlinedIcon sx={{ color: portalColors.primary }} />
          <Box sx={{ flex: 1 }}>
            <Typography sx={{ fontWeight: 600 }}>Filter by location</Typography>
            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
              Narrow the registry by administrative hierarchy.
            </Typography>
          </Box>
          <Button
            size="small"
            variant="outlined"
            sx={portalOutlinedButtonSx}
            onClick={() => setFilters(emptyLocationFilters)}
          >
            Clear filters
          </Button>
        </Stack>

        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 3 }}>
            <TextField
              select
              fullWidth
              size="small"
              label="Region"
              value={filters.regionId}
              onChange={(e) => setFilters({ regionId: e.target.value, provinceId: '', cityId: '', barangayId: '' })}
            >
              <MenuItem value="">All regions</MenuItem>
              {regions.map((region) => (
                <MenuItem key={region.id} value={String(region.id)}>{region.name}</MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, md: 3 }}>
            <TextField
              select
              fullWidth
              size="small"
              label="Province"
              value={filters.provinceId}
              disabled={!filters.regionId}
              onChange={(e) => setFilters({ ...filters, provinceId: e.target.value, cityId: '', barangayId: '' })}
            >
              <MenuItem value="">All provinces</MenuItem>
              {provinces.map((province) => (
                <MenuItem key={province.id} value={String(province.id)}>{province.name}</MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, md: 3 }}>
            <TextField
              select
              fullWidth
              size="small"
              label="City / Municipality"
              value={filters.cityId}
              disabled={!filters.provinceId}
              onChange={(e) => setFilters({ ...filters, cityId: e.target.value, barangayId: '' })}
            >
              <MenuItem value="">All cities</MenuItem>
              {cities.map((city) => (
                <MenuItem key={city.id} value={String(city.id)}>{city.name}</MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, md: 3 }}>
            <TextField
              select
              fullWidth
              size="small"
              label="Barangay"
              value={filters.barangayId}
              disabled={!filters.cityId}
              onChange={(e) => setFilters({ ...filters, barangayId: e.target.value })}
            >
              <MenuItem value="">All barangays</MenuItem>
              {barangays.map((barangay) => (
                <MenuItem key={barangay.id} value={String(barangay.id)}>{barangay.name}</MenuItem>
              ))}
            </TextField>
          </Grid>
        </Grid>
      </Box>

      <Stack spacing={2} sx={{ mt: 3 }}>
        {isLoading ? (
          <PortalTablePanel
            title="Loading warehouses"
            columns={['Code', 'Warehouse', 'Address', 'Status', '']}
            isLoading
            isEmpty={false}
          />
        ) : regionSections.length === 0 ? (
          <Box sx={{ textAlign: 'center', py: 6, borderRadius: '0.75rem', border: `1px dashed ${portalColors.border}` }}>
            <WarehouseOutlinedIcon sx={{ fontSize: 42, color: portalColors.textMuted, mb: 1 }} />
            <Typography sx={{ fontWeight: 600, mb: 0.5 }}>No warehouses found</Typography>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 2 }}>
              Add a warehouse or adjust your location filters.
            </Typography>
            <Button variant="contained" startIcon={<AddOutlinedIcon />} sx={portalPrimaryButtonSx} onClick={openCreate}>
              Add Warehouse
            </Button>
          </Box>
        ) : (
          regionSections.map((section) => (
            <Accordion
              key={String(section.regionId ?? section.regionName)}
              defaultExpanded
              disableGutters
              sx={{
                border: `1px solid ${portalColors.border}`,
                borderRadius: '0.75rem !important',
                overflow: 'hidden',
                '&:before': { display: 'none' },
                boxShadow: 'none',
              }}
            >
              <AccordionSummary expandIcon={<ExpandMoreIcon />} sx={{ px: 2, bgcolor: portalColors.bgMuted }}>
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                  <Typography sx={{ fontWeight: 700, color: portalColors.textDark }}>{section.regionName}</Typography>
                  <Chip
                    size="small"
                    label={`${section.groups.reduce((count, group) => count + group.warehouses.length, 0)} warehouse(s)`}
                    sx={{ bgcolor: portalColors.bgWhite, fontWeight: 600 }}
                  />
                </Stack>
              </AccordionSummary>
              <AccordionDetails sx={{ px: 2, pb: 2 }}>
                <Stack spacing={2.5}>
                  {section.groups.map((group) => (
                    <Box key={group.key}>
                      <Typography sx={{ fontWeight: 600, color: portalColors.primary, mb: 1 }}>
                        {formatLocationPath(group)}
                      </Typography>
                      <PortalTablePanel
                        title=""
                        columns={['Code', 'Warehouse', 'Address', 'Status', '']}
                        isLoading={false}
                        isEmpty={group.warehouses.length === 0}
                        emptyMessage="No warehouses in this barangay."
                      >
                        {group.warehouses.map((warehouse) => (
                          <TableRow key={warehouse.id} hover>
                            <TableCell>{warehouse.code}</TableCell>
                            <TableCell>
                              <Typography sx={{ fontWeight: 600 }}>{warehouse.name}</Typography>
                            </TableCell>
                            <TableCell sx={{ maxWidth: 360 }}>
                              <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                                {warehouse.formattedAddress}
                              </Typography>
                            </TableCell>
                            <TableCell>
                              <Chip
                                size="small"
                                label={warehouse.isActive ? 'Active' : 'Inactive'}
                                sx={{
                                  bgcolor: warehouse.isActive ? portalColors.successSoft : portalColors.bgMuted,
                                  color: warehouse.isActive ? portalColors.successText : portalColors.textMuted,
                                  fontWeight: 600,
                                }}
                              />
                            </TableCell>
                            <TableCell align="right">
                              <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                                <Button
                                  size="small"
                                  component={RouterLink}
                                  to={`/da/warehouses/${warehouse.id}`}
                                  startIcon={<VisibilityOutlinedIcon />}
                                  sx={portalOutlinedButtonSx}
                                >
                                  View
                                </Button>
                                <Button
                                  size="small"
                                  startIcon={<EditOutlinedIcon />}
                                  onClick={() => openEdit(warehouse)}
                                  sx={portalOutlinedButtonSx}
                                >
                                  Edit
                                </Button>
                              </Stack>
                            </TableCell>
                          </TableRow>
                        ))}
                      </PortalTablePanel>
                    </Box>
                  ))}
                </Stack>
              </AccordionDetails>
            </Accordion>
          ))
        )}
      </Stack>

      <DaWarehouseFormDialog
        open={dialogOpen}
        warehouse={editingWarehouse}
        onClose={() => {
          setDialogOpen(false)
          setEditingWarehouse(null)
        }}
      />
    </Box>
  )
}
