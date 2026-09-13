import { Autocomplete, Stack, TextField } from '@mui/material'
import { useMemo } from 'react'
import { useGetMavHsCategoriesQuery, useGetMavHsPickerDetailsQuery } from '../api/mavApi'
import type { MavHsCategoryListItem, MavHsDetailListItem } from '../api/mavApi'

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
  disabled?: boolean
  required?: boolean
  hsPlaceholder?: string
}

export function HsCodePicker({
  value,
  onChange,
  agencyId,
  disabled,
  required = true,
  hsPlaceholder = 'Search HS code',
}: HsCodePickerProps) {
  const { data: categoriesData, isLoading: loadingCategories } = useGetMavHsCategoriesQuery({ agencyId })
  const categories = useMemo(
    () => (categoriesData?.data ?? []).filter((category) => category.isActive),
    [categoriesData?.data],
  )

  const { data: detailsData, isLoading: loadingDetails } = useGetMavHsPickerDetailsQuery(
    { agencyId, categoryUuid: value.categoryUuid || undefined },
    { skip: !value.categoryUuid },
  )
  const details = useMemo(
    () => (detailsData?.data ?? []).filter((detail) => detail.isActive),
    [detailsData?.data],
  )

  const selectedCategory = categories.find((category) => category.uuid === value.categoryUuid) ?? null
  const selectedDetail = details.find((detail) => detail.uuid === value.detailUuid) ?? null

  const handleCategoryChange = (category: MavHsCategoryListItem | null) => {
    onChange({
      categoryUuid: category?.uuid ?? '',
      detailUuid: '',
      hsCode: '',
      commodityName: '',
    })
  }

  const handleDetailChange = (detail: MavHsDetailListItem | null) => {
    onChange({
      categoryUuid: value.categoryUuid,
      detailUuid: detail?.uuid ?? '',
      hsCode: detail ? `${detail.hsCode}${detail.headingNumber}` : '',
      commodityName: detail?.description ?? '',
    })
  }

  return (
    <Stack spacing={2}>
      <Autocomplete
        options={categories}
        value={selectedCategory}
        loading={loadingCategories}
        disabled={disabled || loadingCategories}
        getOptionLabel={(option) => `${option.hsCode} — ${option.description}`}
        isOptionEqualToValue={(option, selected) => option.uuid === selected.uuid}
        filterOptions={(options, state) => {
          const query = state.inputValue.trim().toLowerCase()
          if (!query) return options
          return options.filter((option) =>
            option.hsCode.toLowerCase().includes(query)
            || option.description.toLowerCase().includes(query)
            || (option.agencyCode?.toLowerCase().includes(query) ?? false),
          )
        }}
        onChange={(_event, category) => handleCategoryChange(category)}
        renderInput={(params) => (
          <TextField
            {...params}
            label="HS Code"
            required={required}
            placeholder={hsPlaceholder}
            helperText={selectedCategory ? `${selectedCategory.hsCode} · ${selectedCategory.agencyCode ?? 'Shared'}` : 'Type to search the HS code'}
          />
        )}
      />

      {value.categoryUuid ? (
        <Autocomplete
          options={details}
          value={selectedDetail}
          loading={loadingDetails}
          disabled={disabled || loadingDetails}
          getOptionLabel={(option) => option.displayLabel}
          isOptionEqualToValue={(option, selected) => option.uuid === selected.uuid}
          filterOptions={(options, state) => {
            const query = state.inputValue.trim().toLowerCase()
            if (!query) return options
            return options.filter((option) =>
              option.description.toLowerCase().includes(query)
              || option.displayLabel.toLowerCase().includes(query)
              || option.hsCode.toLowerCase().includes(query),
            )
          }}
          onChange={(_event, detail) => handleDetailChange(detail)}
          renderInput={(params) => (
            <TextField
              {...params}
              label="Commodity"
              required={required}
              placeholder="Select commodity for this HS code"
              helperText={value.hsCode ? `Full HS ${value.hsCode}` : 'Commodities under the selected HS code'}
            />
          )}
        />
      ) : null}
    </Stack>
  )
}
