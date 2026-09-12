import { Alert, Button, MenuItem, TextField } from '@mui/material'
import { useCallback, useEffect, useMemo } from 'react'
import { useGetRegisteredWarehousesQuery } from '../addresses/addressApi'
import { getWarehouseAutofillTarget, type FormFieldSchema } from '../forms/formSchema'
import { portalColors } from '../../components/portal/portalTheme'

interface WarehouseFieldRendererProps {
  field: FormFieldSchema
  values: Record<string, string>
  onChange: (name: string, value: string) => void
  onBatchChange?: (updates: Record<string, string>) => void
  disabled?: boolean
  fieldError?: string
}

const fieldInputSx = {
  '& .MuiOutlinedInput-root': {
    borderRadius: '0.5rem',
    bgcolor: portalColors.bgWhite,
  },
} as const

export function WarehouseFieldRenderer({
  field,
  values,
  onChange,
  onBatchChange,
  disabled,
  fieldError,
}: WarehouseFieldRendererProps) {
  const { data, isLoading, isError, refetch } = useGetRegisteredWarehousesQuery()
  const warehouses = data?.data ?? []
  const value = values[field.name] ?? ''
  const labelKey = `${field.name}_label`
  const autofillTarget = useMemo(() => getWarehouseAutofillTarget(field), [field])

  const applyUpdates = useCallback((updates: Record<string, string>) => {
    if (onBatchChange) {
      onBatchChange(updates)
      return
    }

    for (const [name, nextValue] of Object.entries(updates)) {
      onChange(name, nextValue)
    }
  }, [onBatchChange, onChange])

  useEffect(() => {
    if (!value || warehouses.length === 0) return

    const selected = warehouses.find((item) => String(item.id) === value)
    if (!selected) return

    const updates: Record<string, string> = {}
    if (!values[labelKey]) {
      updates[labelKey] = selected.name
    }
    if (autofillTarget && !values[autofillTarget]) {
      updates[autofillTarget] = selected.formattedAddress
    }

    if (Object.keys(updates).length > 0) {
      applyUpdates(updates)
    }
  }, [applyUpdates, autofillTarget, labelKey, value, warehouses, values])

  const handleChange = (warehouseId: string) => {
    if (!warehouseId) {
      const updates: Record<string, string> = {
        [field.name]: '',
        [labelKey]: '',
      }
      if (autofillTarget) {
        updates[autofillTarget] = ''
      }
      applyUpdates(updates)
      return
    }

    const selected = warehouses.find((item) => String(item.id) === warehouseId)
    const updates: Record<string, string> = {
      [field.name]: warehouseId,
      [labelKey]: selected?.name ?? '',
    }
    if (autofillTarget) {
      updates[autofillTarget] = selected?.formattedAddress ?? ''
    }
    applyUpdates(updates)
  }

  const helperText = fieldError
    ?? (isError
      ? 'Unable to load registered warehouses. Check that the API is running.'
      : isLoading
        ? 'Loading registered warehouses…'
        : warehouses.length === 0
          ? 'No registered warehouses found.'
          : undefined)

  return (
    <>
      {isError ? (
        <Alert
          severity="warning"
          sx={{ mb: 1.5 }}
          action={
            <Button color="inherit" size="small" onClick={() => refetch()}>
              Retry
            </Button>
          }
        >
          Warehouse list failed to load.
        </Alert>
      ) : null}
      <TextField
        select
        fullWidth
        size="small"
        label={field.label}
        value={value}
        required={field.required}
        disabled={disabled || isLoading || isError}
        error={Boolean(fieldError) || isError}
        helperText={helperText}
        onChange={(event) => handleChange(event.target.value)}
        slotProps={{
          inputLabel: { shrink: true },
          select: { displayEmpty: true },
        }}
        sx={fieldInputSx}
      >
        <MenuItem value="">
          <em>{isLoading ? 'Loading warehouses…' : field.placeholder ?? 'Select warehouse'}</em>
        </MenuItem>
        {warehouses.map((warehouse) => (
          <MenuItem key={warehouse.id} value={String(warehouse.id)}>
            {warehouse.name}
          </MenuItem>
        ))}
      </TextField>
    </>
  )
}
