import { Alert, Stack, Typography } from '@mui/material'
import { useMemo } from 'react'
import { getCommodityHsValueKeys, type FormFieldSchema } from '../../forms/formSchema'
import { HsCodePicker, type HsCodeSelection } from './HsCodePicker'

interface CommodityHsFieldRendererProps {
  field: FormFieldSchema
  values: Record<string, string>
  onChange: (name: string, value: string) => void
  onBatchChange?: (updates: Record<string, string>) => void
  agencyId?: number
  disabled?: boolean
}

export function CommodityHsFieldRenderer({
  field,
  values,
  onChange,
  onBatchChange,
  agencyId,
  disabled,
}: CommodityHsFieldRendererProps) {
  const keys = useMemo(() => getCommodityHsValueKeys(field.name), [field.name])
  const selection: HsCodeSelection = {
    categoryUuid: values[keys.categoryUuid] ?? '',
    detailUuid: values[keys.detailUuid] ?? '',
    hsCode: values[keys.hsCode] ?? '',
    commodityName: values[keys.commodityName] ?? '',
  }

  const applySelection = (next: HsCodeSelection) => {
    const summary = next.hsCode && next.commodityName ? `${next.hsCode} — ${next.commodityName}` : ''
    const updates = {
      [keys.categoryUuid]: next.categoryUuid,
      [keys.detailUuid]: next.detailUuid,
      [keys.hsCode]: next.hsCode,
      [keys.commodityName]: next.commodityName,
      [field.name]: summary,
    }
    if (onBatchChange) {
      onBatchChange(updates)
      return
    }
    Object.entries(updates).forEach(([name, value]) => onChange(name, value))
  }

  return (
    <Stack spacing={1.5}>
      <Typography sx={{ fontWeight: 600, fontSize: '0.9375rem' }}>{field.label}</Typography>
      {field.helpText ? (
        <Typography sx={{ fontSize: '0.8125rem', color: 'text.secondary', mt: -0.5 }}>{field.helpText}</Typography>
      ) : null}
      {!agencyId ? (
        <Alert severity="info">Open this entry from an agency so HS codes can be filtered for that agency.</Alert>
      ) : null}
      <HsCodePicker
        value={selection}
        onChange={applySelection}
        agencyId={agencyId}
        disabled={disabled}
        required={field.required}
        hsPlaceholder={field.placeholder ?? 'Search HS code'}
      />
    </Stack>
  )
}
