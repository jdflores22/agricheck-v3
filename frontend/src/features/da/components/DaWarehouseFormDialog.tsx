import {
  Alert,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Grid,
  MenuItem,
  Stack,
  TextField,
} from '@mui/material'
import { useEffect, useState } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetBarangaysQuery,
  useGetCitiesQuery,
  useGetProvincesQuery,
  useGetRegionsQuery,
} from '../../addresses/addressApi'
import {
  useCreateDaWarehouseMutation,
  useUpdateDaWarehouseMutation,
  type DaWarehouse,
  type SaveDaWarehouseRequest,
} from '../api/daApi'

interface DaWarehouseFormDialogProps {
  open: boolean
  warehouse?: DaWarehouse | null
  onClose: () => void
}

const emptyForm: SaveDaWarehouseRequest = {
  code: '',
  name: '',
  capacity: 0,
  regionId: 0,
  provinceId: 0,
  cityId: 0,
  barangayId: 0,
  streetAddress: '',
  zipCode: '',
  latitude: undefined,
  longitude: undefined,
  isActive: true,
}

export function DaWarehouseFormDialog({ open, warehouse, onClose }: DaWarehouseFormDialogProps) {
  const [form, setForm] = useState<SaveDaWarehouseRequest>(emptyForm)
  const [regionId, setRegionId] = useState('')
  const [provinceId, setProvinceId] = useState('')
  const [cityId, setCityId] = useState('')
  const [barangayId, setBarangayId] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const { data: regionsData } = useGetRegionsQuery(undefined, { skip: !open })
  const { data: provincesData } = useGetProvincesQuery(Number(regionId), { skip: !open || !regionId })
  const { data: citiesData } = useGetCitiesQuery(Number(provinceId), { skip: !open || !provinceId })
  const { data: barangaysData } = useGetBarangaysQuery(Number(cityId), { skip: !open || !cityId })

  const [createWarehouse, { isLoading: creating }] = useCreateDaWarehouseMutation()
  const [updateWarehouse, { isLoading: updating }] = useUpdateDaWarehouseMutation()

  const regions = regionsData?.data ?? []
  const provinces = provincesData?.data ?? []
  const cities = citiesData?.data ?? []
  const barangays = barangaysData?.data ?? []
  const saving = creating || updating

  useEffect(() => {
    if (!open) return

    setErrorMessage(null)
    if (warehouse) {
      setForm({
        code: warehouse.code,
        name: warehouse.name,
        capacity: warehouse.capacity,
        regionId: warehouse.regionId ?? 0,
        provinceId: warehouse.provinceId ?? 0,
        cityId: warehouse.cityId ?? 0,
        barangayId: warehouse.barangayId ?? 0,
        streetAddress: warehouse.streetAddress ?? '',
        zipCode: warehouse.zipCode ?? '',
        latitude: warehouse.latitude ?? undefined,
        longitude: warehouse.longitude ?? undefined,
        isActive: warehouse.isActive,
      })
      setRegionId(String(warehouse.regionId ?? ''))
      setProvinceId(String(warehouse.provinceId ?? ''))
      setCityId(String(warehouse.cityId ?? ''))
      setBarangayId(String(warehouse.barangayId ?? ''))
      return
    }

    setForm(emptyForm)
    setRegionId('')
    setProvinceId('')
    setCityId('')
    setBarangayId('')
  }, [open, warehouse])

  const handleSave = async () => {
    setErrorMessage(null)

    if (!form.code.trim() || !form.name.trim()) {
      setErrorMessage('Warehouse code and name are required.')
      return
    }

    if (!regionId || !provinceId || !cityId || !barangayId) {
      setErrorMessage('Complete the location hierarchy: region, province, city/municipality, and barangay.')
      return
    }

    const payload: SaveDaWarehouseRequest = {
      ...form,
      code: form.code.trim().toUpperCase(),
      name: form.name.trim(),
      regionId: Number(regionId),
      provinceId: Number(provinceId),
      cityId: Number(cityId),
      barangayId: Number(barangayId),
      streetAddress: form.streetAddress?.trim() || undefined,
      zipCode: form.zipCode?.trim() || undefined,
      latitude: form.latitude === undefined || form.latitude === null || Number.isNaN(Number(form.latitude))
        ? undefined
        : Number(form.latitude),
      longitude: form.longitude === undefined || form.longitude === null || Number.isNaN(Number(form.longitude))
        ? undefined
        : Number(form.longitude),
    }

    try {
      if (warehouse) {
        await updateWarehouse({ id: warehouse.id, body: payload }).unwrap()
      } else {
        await createWarehouse(payload).unwrap()
      }
      onClose()
    } catch {
      setErrorMessage('Unable to save warehouse. Check the code is unique and location fields are valid.')
    }
  }

  return (
    <Dialog open={open} onClose={() => !saving && onClose()} maxWidth="md" fullWidth>
      <DialogTitle sx={{ bgcolor: portalColors.primary, color: '#fff' }}>
        {warehouse ? 'Edit Registered Warehouse' : 'Add Registered Warehouse'}
      </DialogTitle>
      <DialogContent sx={{ pt: '20px !important' }}>
        <Stack spacing={2.5}>
          {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : null}

          <Grid container spacing={2}>
            <Grid size={{ xs: 12, md: 4 }}>
              <TextField
                label="Warehouse Code"
                value={form.code}
                onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })}
                required
                fullWidth
                placeholder="WH-MNL"
              />
            </Grid>
            <Grid size={{ xs: 12, md: 8 }}>
              <TextField
                label="Warehouse Name"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                required
                fullWidth
              />
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <TextField
                label="Capacity"
                type="number"
                value={form.capacity}
                onChange={(e) => setForm({ ...form, capacity: Number(e.target.value) || 0 })}
                fullWidth
              />
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={form.isActive}
                    onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                    color="success"
                  />
                }
                label="Active warehouse"
              />
            </Grid>
          </Grid>

          <Stack spacing={2}>
            <TextField
              select
              label="Region"
              value={regionId}
              onChange={(e) => {
                setRegionId(e.target.value)
                setProvinceId('')
                setCityId('')
                setBarangayId('')
              }}
              required
              fullWidth
            >
              <MenuItem value="">
                <em>Select region</em>
              </MenuItem>
              {regions.map((item) => (
                <MenuItem key={item.id} value={String(item.id)}>
                  {item.name}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Province"
              value={provinceId}
              onChange={(e) => {
                setProvinceId(e.target.value)
                setCityId('')
                setBarangayId('')
              }}
              required
              fullWidth
              disabled={!regionId}
            >
              <MenuItem value="">
                <em>Select province</em>
              </MenuItem>
              {provinces.map((item) => (
                <MenuItem key={item.id} value={String(item.id)}>
                  {item.name}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="City / Municipality"
              value={cityId}
              onChange={(e) => {
                setCityId(e.target.value)
                setBarangayId('')
              }}
              required
              fullWidth
              disabled={!provinceId}
            >
              <MenuItem value="">
                <em>Select city/municipality</em>
              </MenuItem>
              {cities.map((item) => (
                <MenuItem key={item.id} value={String(item.id)}>
                  {item.name}
                </MenuItem>
              ))}
            </TextField>

            <TextField
              select
              label="Barangay"
              value={barangayId}
              onChange={(e) => {
                const nextId = e.target.value
                setBarangayId(nextId)
                const selected = barangays.find((item) => String(item.id) === nextId)
                if (selected?.zipCode) {
                  setForm((current) => ({ ...current, zipCode: selected.zipCode }))
                }
              }}
              required
              fullWidth
              disabled={!cityId}
            >
              <MenuItem value="">
                <em>Select barangay</em>
              </MenuItem>
              {barangays.map((item) => (
                <MenuItem key={item.id} value={String(item.id)}>
                  {item.name}
                </MenuItem>
              ))}
            </TextField>
          </Stack>

          <Grid container spacing={2}>
            <Grid size={{ xs: 12 }}>
              <TextField
                label="Street Address"
                value={form.streetAddress ?? ''}
                onChange={(e) => setForm({ ...form, streetAddress: e.target.value })}
                fullWidth
                multiline
                minRows={2}
              />
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <TextField
                label="Zip Code"
                value={form.zipCode ?? ''}
                onChange={(e) => setForm({ ...form, zipCode: e.target.value })}
                fullWidth
              />
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <TextField
                label="Latitude"
                type="number"
                value={form.latitude ?? ''}
                onChange={(e) => setForm({ ...form, latitude: e.target.value === '' ? undefined : Number(e.target.value) })}
                fullWidth
              />
            </Grid>
            <Grid size={{ xs: 12, md: 4 }}>
              <TextField
                label="Longitude"
                type="number"
                value={form.longitude ?? ''}
                onChange={(e) => setForm({ ...form, longitude: e.target.value === '' ? undefined : Number(e.target.value) })}
                fullWidth
              />
            </Grid>
          </Grid>
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose} disabled={saving} sx={portalOutlinedButtonSx}>
          Cancel
        </Button>
        <Button onClick={() => void handleSave()} variant="contained" disabled={saving} sx={portalPrimaryButtonSx}>
          {saving ? 'Saving…' : warehouse ? 'Save Changes' : 'Add Warehouse'}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
