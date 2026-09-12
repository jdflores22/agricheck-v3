import { Alert, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useMemo } from 'react'
import {
  useGetBarangaysQuery,
  useGetCitiesQuery,
  useGetProvincesQuery,
  useGetRegionsQuery,
} from '../addresses/addressApi'
import { getAddressValueKeys, type FormFieldSchema } from '../forms/formSchema'

interface AddressFieldRendererProps {
  field: FormFieldSchema
  values: Record<string, string>
  onChange: (name: string, value: string) => void
  disabled?: boolean
}

function clearAddressValues(keys: ReturnType<typeof getAddressValueKeys>, onChange: (name: string, value: string) => void, fields: Array<keyof ReturnType<typeof getAddressValueKeys>>) {
  fields.forEach((key) => onChange(keys[key], ''))
}

export function AddressFieldRenderer({ field, values, onChange, disabled }: AddressFieldRendererProps) {
  const keys = useMemo(() => getAddressValueKeys(field.name), [field.name])
  const regionId = values[keys.regionId] ?? ''
  const provinceId = values[keys.provinceId] ?? ''
  const cityId = values[keys.cityId] ?? ''
  const barangayId = values[keys.barangayId] ?? ''
  const zipCode = values[keys.zipCode] ?? ''

  const { data: regionsData, isLoading: regionsLoading } = useGetRegionsQuery()
  const { data: provincesData, isLoading: provincesLoading } = useGetProvincesQuery(Number(regionId), {
    skip: !regionId,
  })
  const { data: citiesData, isLoading: citiesLoading } = useGetCitiesQuery(Number(provinceId), {
    skip: !provinceId,
  })
  const { data: barangaysData, isLoading: barangaysLoading } = useGetBarangaysQuery(Number(cityId), {
    skip: !cityId,
  })

  const regions = regionsData?.data ?? []
  const provinces = provincesData?.data ?? []
  const cities = citiesData?.data ?? []
  const barangays = barangaysData?.data ?? []

  const setRegion = (nextId: string) => {
    const selected = regions.find((item) => String(item.id) === nextId)
    onChange(keys.regionId, nextId)
    onChange(keys.regionName, selected?.name ?? '')
    clearAddressValues(keys, onChange, ['provinceId', 'provinceName', 'cityId', 'cityName', 'barangayId', 'barangayName', 'zipCode'])
  }

  const setProvince = (nextId: string) => {
    const selected = provinces.find((item) => String(item.id) === nextId)
    onChange(keys.provinceId, nextId)
    onChange(keys.provinceName, selected?.name ?? '')
    clearAddressValues(keys, onChange, ['cityId', 'cityName', 'barangayId', 'barangayName', 'zipCode'])
  }

  const setCity = (nextId: string) => {
    const selected = cities.find((item) => String(item.id) === nextId)
    onChange(keys.cityId, nextId)
    onChange(keys.cityName, selected?.name ?? '')
    clearAddressValues(keys, onChange, ['barangayId', 'barangayName', 'zipCode'])
  }

  const setBarangay = (nextId: string) => {
    const selected = barangays.find((item) => String(item.id) === nextId)
    onChange(keys.barangayId, nextId)
    onChange(keys.barangayName, selected?.name ?? '')
    onChange(keys.zipCode, selected?.zipCode ?? '')
  }

  return (
    <Stack spacing={2}>
      <Typography sx={{ fontWeight: 600, fontSize: '0.9375rem' }}>{field.label}</Typography>
      {field.helpText && (
        <Typography sx={{ fontSize: '0.8125rem', color: 'text.secondary', mt: -1 }}>{field.helpText}</Typography>
      )}

      {regions.length === 0 && !regionsLoading && (
        <Alert severity="warning">No address regions found. Restart the API to run address seed data.</Alert>
      )}

      <TextField
        select
        fullWidth
        label="Region"
        value={regionId}
        required={field.required}
        disabled={disabled || regionsLoading}
        onChange={(e) => setRegion(e.target.value)}
      >
        <MenuItem value="">Select region</MenuItem>
        {regions.map((item) => (
          <MenuItem key={item.id} value={String(item.id)}>{item.name}</MenuItem>
        ))}
      </TextField>

      <TextField
        select
        fullWidth
        label="Province"
        value={provinceId}
        required={field.required}
        disabled={disabled || !regionId || provincesLoading}
        onChange={(e) => setProvince(e.target.value)}
      >
        <MenuItem value="">Select province</MenuItem>
        {provinces.map((item) => (
          <MenuItem key={item.id} value={String(item.id)}>{item.name}</MenuItem>
        ))}
      </TextField>

      <TextField
        select
        fullWidth
        label="City / Municipality"
        value={cityId}
        required={field.required}
        disabled={disabled || !provinceId || citiesLoading}
        onChange={(e) => setCity(e.target.value)}
      >
        <MenuItem value="">Select city / municipality</MenuItem>
        {cities.map((item) => (
          <MenuItem key={item.id} value={String(item.id)}>{item.name}</MenuItem>
        ))}
      </TextField>

      <TextField
        select
        fullWidth
        label="Barangay"
        value={barangayId}
        required={field.required}
        disabled={disabled || !cityId || barangaysLoading}
        onChange={(e) => setBarangay(e.target.value)}
      >
        <MenuItem value="">Select barangay</MenuItem>
        {barangays.map((item) => (
          <MenuItem key={item.id} value={String(item.id)}>
            {item.name}{item.zipCode ? ` (${item.zipCode})` : ''}
          </MenuItem>
        ))}
      </TextField>

      <TextField
        fullWidth
        label="Zip Code"
        value={zipCode}
        required={field.required}
        disabled
        placeholder="Auto-filled from barangay"
        helperText={barangayId ? 'Automatically set based on selected barangay' : 'Select a barangay to auto-fill zip code'}
      />

      <TextField
        fullWidth
        label="Street / Building / Unit"
        value={values[keys.street] ?? ''}
        required={field.required}
        disabled={disabled}
        placeholder={field.placeholder ?? 'House no., street, building, unit'}
        multiline
        minRows={2}
        onChange={(e) => onChange(keys.street, e.target.value)}
      />
    </Stack>
  )
}

export function formatAddressSummary(fieldName: string, values: Record<string, string>): string {
  const keys = getAddressValueKeys(fieldName)
  const parts = [
    values[keys.street],
    values[keys.barangayName],
    values[keys.cityName],
    values[keys.provinceName],
    values[keys.regionName],
    values[keys.zipCode],
  ].filter(Boolean)
  return parts.join(', ')
}
