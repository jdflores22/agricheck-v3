import { MenuItem, Stack, TextField } from '@mui/material'
import { useGetMavHsCategoriesQuery, useGetMavHsPickerDetailsQuery } from '../api/mavApi'

export interface HsCodeSelection {
  categoryUuid: string
  detailUuid: string
  hsCode: string
  commodityName: string
}

interface HsCodePickerProps {
  value: HsCodeSelection
  onChange: (value: HsCodeSelection) => void
  agencyId?: number
}

export function HsCodePicker({ value, onChange, agencyId }: HsCodePickerProps) {
  const { data: categoriesData, isLoading: loadingCategories } = useGetMavHsCategoriesQuery({ agencyId })
  const categories = categoriesData?.data ?? []

  const { data: detailsData, isLoading: loadingDetails } = useGetMavHsPickerDetailsQuery(
    { agencyId, categoryUuid: value.categoryUuid || undefined },
    { skip: !value.categoryUuid },
  )
  const details = detailsData?.data ?? []

  const handleCategoryChange = (categoryUuid: string) => {
    onChange({ categoryUuid, detailUuid: '', hsCode: '', commodityName: '' })
  }

  const handleDetailChange = (detailUuid: string) => {
    const detail = details.find((item) => item.uuid === detailUuid)
    onChange({
      categoryUuid: value.categoryUuid,
      detailUuid,
      hsCode: detail?.hsCode ?? '',
      commodityName: detail?.description ?? '',
    })
  }

  return (
    <Stack spacing={2}>
      <TextField
        select
        label="HS Category"
        value={value.categoryUuid}
        onChange={(e) => handleCategoryChange(e.target.value)}
        required
        fullWidth
        disabled={loadingCategories}
      >
        {categories.map((category) => (
          <MenuItem key={category.uuid} value={category.uuid}>
            {category.hsCode} — {category.description}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        select
        label="Commodity"
        value={value.detailUuid}
        onChange={(e) => handleDetailChange(e.target.value)}
        required
        fullWidth
        disabled={!value.categoryUuid || loadingDetails}
        helperText={value.hsCode ? `HS ${value.hsCode}` : undefined}
      >
        {details.map((detail) => (
          <MenuItem key={detail.uuid} value={detail.uuid}>
            {detail.displayLabel}
          </MenuItem>
        ))}
      </TextField>
    </Stack>
  )

}
