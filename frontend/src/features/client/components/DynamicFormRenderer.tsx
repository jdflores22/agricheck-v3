import AttachFileOutlinedIcon from '@mui/icons-material/AttachFileOutlined'
import CloudUploadOutlinedIcon from '@mui/icons-material/CloudUploadOutlined'
import {
  Box,
  Button,
  Checkbox,
  FormControl,
  FormControlLabel,
  FormHelperText,
  FormLabel,
  Grid,
  MenuItem,
  Radio,
  RadioGroup,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  evaluateConditional,
  getWarehouseAutofillTarget,
  isMavControlField,
  sanitizeDisplayText,
  type FormFieldSchema,
  parseFormSchema,
} from '../../forms/formSchema'
import { AddressFieldRenderer } from '../../addresses/AddressFieldRenderer'
import { CommodityHsFieldRenderer } from '../../mav/components/CommodityHsFieldRenderer'
import { WarehouseFieldRenderer } from '../../warehouses/WarehouseFieldRenderer'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'

function formatDisplayFileName(fileName: string, maxLength = 42): string {
  if (fileName.length <= maxLength) return fileName

  const extensionIndex = fileName.lastIndexOf('.')
  if (extensionIndex <= 0) {
    return `${fileName.slice(0, maxLength - 1)}…`
  }

  const extension = fileName.slice(extensionIndex)
  const baseLength = Math.max(8, maxLength - extension.length - 1)
  return `${fileName.slice(0, baseLength)}…${extension}`
}

interface DynamicFormRendererProps {
  schemaJson: string
  values: Record<string, string>
  onChange: (name: string, value: string) => void
  onBatchChange?: (updates: Record<string, string>) => void
  disabled?: boolean
  onFileUpload?: (fieldName: string, file: File) => Promise<void>
  onFileDownload?: (fileUuid: string, fileName: string) => void
  uploadingField?: string | null
  /** When true, file fields show a hint that uploads unlock after the draft is saved. */
  filesPendingDraft?: boolean
  fieldErrors?: Record<string, string>
  agencyId?: number
  mavRequired?: boolean
  showMavFields?: boolean
  showCommodityHs?: boolean
}

export type { FormFieldSchema }
export { parseFormSchema }

const fieldInputSx = {
  '& .MuiOutlinedInput-root': {
    borderRadius: '0.5rem',
    bgcolor: portalColors.bgWhite,
  },
} as const

function getOptions(field: FormFieldSchema): Array<{ label: string; value: string }> {
  return (field.options ?? []).map((option) =>
    typeof option === 'string' ? { label: option, value: option } : option,
  )
}

function getFieldGridSize(field: FormFieldSchema) {
  if (field.type === 'section') {
    return { xs: 12 as const }
  }

  if (field.type === 'file' || field.type === 'geotag_photo') {
    return { xs: 12 as const, sm: 6 as const, lg: 4 as const }
  }

  if (field.type === 'textarea' || field.type === 'address' || field.type === 'commodity') {
    return { xs: 12 as const }
  }

  const width = field.columnWidth ?? 12
  if (width <= 3) return { xs: 12 as const, sm: 6 as const, lg: 3 as const }
  if (width <= 4) return { xs: 12 as const, sm: 6 as const, lg: 4 as const }
  if (width <= 6) return { xs: 12 as const, md: 6 as const }
  return { xs: 12 as const, md: width as 12 | 9 | 8 | 6 | 4 | 3 }
}

function getAcceptedFileTypes(field: FormFieldSchema): string | undefined {
  const types = field.validationRules?.file_types
  if (!Array.isArray(types)) return undefined
  return types
    .map((type) => String(type).trim().toLowerCase())
    .filter(Boolean)
    .map((type) => (type.startsWith('.') ? type : `.${type}`))
    .join(',')
}

function FieldWrapper({
  field,
  children,
  isFirstSection,
  hideHelpText = false,
}: {
  field: FormFieldSchema
  children: React.ReactNode
  isFirstSection?: boolean
  hideHelpText?: boolean
}) {
  if (field.type === 'section') {
    const helpText = sanitizeDisplayText(field.helpText)
    return (
      <Grid size={{ xs: 12 }}>
        <Box
          sx={{
            pt: isFirstSection ? 0 : 2.5,
            mt: isFirstSection ? 0 : 1,
            pb: 1,
            borderBottom: `2px solid ${portalColors.border}`,
          }}
        >
          <Typography
            sx={{
              fontSize: '0.9375rem',
              fontWeight: 700,
              color: portalColors.textDark,
              letterSpacing: '-0.01em',
            }}
          >
            {field.label}
          </Typography>
          {helpText && (
            <Typography sx={{ mt: 0.5, fontSize: '0.8125rem', color: portalColors.textMuted, lineHeight: 1.5 }}>
              {helpText}
            </Typography>
          )}
        </Box>
      </Grid>
    )
  }

  const gridSize = getFieldGridSize(field)
  const helpText = sanitizeDisplayText(field.helpText)

  return (
    <Grid size={gridSize}>
      {children}
      {helpText && !hideHelpText && field.type !== 'file' && field.type !== 'geotag_photo' && (
        <FormHelperText sx={{ mx: 0, mt: 0.75, color: portalColors.textMuted }}>{helpText}</FormHelperText>
      )}
    </Grid>
  )
}

function FileUploadCard({
  field,
  value,
  fileUuid,
  disabled,
  isUploading,
  filesPendingDraft,
  onFileUpload,
  onFileDownload,
  onChange,
}: {
  field: FormFieldSchema
  value: string
  fileUuid: string
  disabled?: boolean
  isUploading: boolean
  filesPendingDraft?: boolean
  onFileUpload?: (fieldName: string, file: File) => Promise<void>
  onFileDownload?: (fileUuid: string, fileName: string) => void
  onChange: (name: string, value: string) => void
}) {
  const accept = getAcceptedFileTypes(field)
  const maxSize = field.validationRules?.file_max_size
  const maxSizeMb = typeof maxSize === 'number' ? Math.round(maxSize / (1024 * 1024)) : null
  const canUpload = Boolean(onFileUpload) && !disabled && !filesPendingDraft

  return (
    <Box
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        gap: 1.25,
        p: 2,
        borderRadius: '0.75rem',
        border: `1px dashed ${value ? portalColors.primary : portalColors.borderStrong}`,
        bgcolor: value ? portalColors.successSoft : portalColors.bgMuted,
        transition: 'border-color 0.15s ease, background-color 0.15s ease',
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start' }}>
        <AttachFileOutlinedIcon sx={{ fontSize: 18, color: portalColors.primary, mt: 0.25 }} />
        <Box sx={{ minWidth: 0, flex: 1 }}>
          <Typography sx={{ fontSize: '0.8125rem', fontWeight: 700, color: portalColors.textDark, lineHeight: 1.35 }}>
            {field.label}
            {field.required ? ' *' : ''}
          </Typography>
          {(accept || maxSizeMb) && (
            <Typography sx={{ mt: 0.35, fontSize: '0.75rem', color: portalColors.textMuted }}>
              {[accept ? accept.replace(/\./g, '').toUpperCase() : null, maxSizeMb ? `Max ${maxSizeMb} MB` : null]
                .filter(Boolean)
                .join(' · ')}
            </Typography>
          )}
        </Box>
      </Stack>

      {canUpload ? (
        <Button
          variant="outlined"
          component="label"
          disabled={isUploading}
          startIcon={<CloudUploadOutlinedIcon />}
          sx={{ ...portalOutlinedButtonSx, alignSelf: 'flex-start', minHeight: 40 }}
        >
          {isUploading ? 'Uploading…' : value ? 'Replace file' : 'Choose file'}
          <input
            hidden
            type="file"
            accept={accept}
            onChange={async (e) => {
              const file = e.target.files?.[0]
              if (file && onFileUpload) {
                await onFileUpload(field.name, file)
              }
              e.target.value = ''
            }}
          />
        </Button>
      ) : filesPendingDraft ? (
        <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, lineHeight: 1.45 }}>
          Save draft first to attach this document.
        </Typography>
      ) : !disabled ? (
        <TextField
          fullWidth
          type="file"
          size="small"
          disabled={disabled}
          slotProps={{
            inputLabel: { shrink: true },
            htmlInput: { accept },
          }}
          onChange={(e) => {
            const file = (e.target as HTMLInputElement).files?.[0]
            onChange(field.name, file?.name ?? '')
          }}
          sx={fieldInputSx}
        />
      ) : null}

      {value ? (
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', mt: 'auto' }}>
          <Typography
            sx={{ fontSize: '0.8125rem', color: portalColors.successText, fontWeight: 500 }}
            title={value}
          >
            {formatDisplayFileName(value)}
          </Typography>
          {fileUuid && onFileDownload && (
            <Button size="small" onClick={() => onFileDownload(fileUuid, value)} sx={{ textTransform: 'none' }}>
              Download
            </Button>
          )}
        </Stack>
      ) : (
        <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 'auto' }}>
          No file uploaded
        </Typography>
      )}
    </Box>
  )
}

export function DynamicFormRenderer({
  schemaJson,
  values,
  onChange,
  onBatchChange,
  disabled,
  onFileUpload,
  onFileDownload,
  uploadingField,
  filesPendingDraft,
  fieldErrors,
  agencyId,
  mavRequired = false,
  showMavFields,
  showCommodityHs,
}: DynamicFormRendererProps) {
  const includeMavFields = showMavFields ?? mavRequired
  const includeCommodityHs = showCommodityHs ?? mavRequired
  const fields = parseFormSchema(schemaJson)
  if (fields.length === 0) return null

  const warehouseAutofillTargets = new Set<string>()
  for (const field of fields) {
    const target = getWarehouseAutofillTarget(field)
    if (target) warehouseAutofillTargets.add(target)
  }

  let sectionIndex = 0

  return (
    <Grid container spacing={2.5}>
      {fields.map((field) => {
        if (!evaluateConditional(field, values)) return null
        if (isMavControlField(field.name) && !includeMavFields) return null

        const value = values[field.name] ?? ''
        const treatAsCommodity = field.type === 'commodity' || (includeCommodityHs && field.name === 'commodityName')
        const renderedField = treatAsCommodity
          ? { ...field, type: 'commodity' as const, required: includeCommodityHs || field.required }
          : isMavControlField(field.name) && includeMavFields
            ? { ...field, required: true }
            : field
        const isFirstSection = field.type === 'section' && sectionIndex++ === 0
        const fieldError = fieldErrors?.[field.name]

        if (field.type === 'section') {
          return (
            <FieldWrapper key={field.id} field={field} isFirstSection={isFirstSection}>
              {null}
            </FieldWrapper>
          )
        }

        if (field.type === 'checkbox') {
          return (
            <FieldWrapper key={field.id} field={field}>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={value === 'true' || value === '1' || value === 'yes'}
                    disabled={disabled}
                    onChange={(e) => onChange(field.name, e.target.checked ? 'yes' : 'no')}
                  />
                }
                label={field.label}
              />
            </FieldWrapper>
          )
        }

        if (field.type === 'radio') {
          const options = getOptions(field)
          return (
            <FieldWrapper key={field.id} field={field}>
              <FormControl required={field.required} disabled={disabled}>
                <FormLabel sx={{ color: portalColors.textDark, fontWeight: 600, mb: 0.5 }}>{field.label}</FormLabel>
                <RadioGroup value={value} onChange={(e) => onChange(field.name, e.target.value)}>
                  {options.map((option) => (
                    <FormControlLabel key={option.value} value={option.value} control={<Radio />} label={option.label} />
                  ))}
                </RadioGroup>
              </FormControl>
            </FieldWrapper>
          )
        }

        if (field.type === 'select') {
          const options = getOptions(field)
          return (
            <FieldWrapper key={field.id} field={field}>
              <TextField
                select
                fullWidth
                size="small"
                label={field.label}
                value={value}
                required={field.required}
                disabled={disabled}
                placeholder={field.placeholder}
                error={Boolean(fieldError)}
                helperText={fieldError}
                onChange={(e) => onChange(field.name, e.target.value)}
                sx={fieldInputSx}
              >
                {options.map((option) => (
                  <MenuItem key={option.value} value={option.value}>
                    {option.label}
                  </MenuItem>
                ))}
              </TextField>
            </FieldWrapper>
          )
        }

        if (field.type === 'file' || field.type === 'geotag_photo') {
          const fileUuid = values[`${field.name}_file_uuid`] ?? ''
          return (
            <FieldWrapper key={field.id} field={field}>
              <FileUploadCard
                field={renderedField}
                value={value}
                fileUuid={fileUuid}
                disabled={disabled}
                isUploading={uploadingField === field.name}
                filesPendingDraft={filesPendingDraft}
                onFileUpload={onFileUpload}
                onFileDownload={onFileDownload}
                onChange={onChange}
              />
            </FieldWrapper>
          )
        }

        if (field.type === 'address') {
          return (
            <FieldWrapper key={field.id} field={field}>
              <AddressFieldRenderer field={field} values={values} onChange={onChange} disabled={disabled} />
            </FieldWrapper>
          )
        }

        if (field.type === 'warehouse') {
          return (
            <FieldWrapper key={field.id} field={field} hideHelpText>
              <WarehouseFieldRenderer
                field={field}
                values={values}
                onChange={onChange}
                onBatchChange={onBatchChange}
                disabled={disabled}
                fieldError={fieldError}
              />
              {sanitizeDisplayText(field.helpText) ? (
                <FormHelperText sx={{ mx: 0, mt: 0.75, color: portalColors.textMuted }}>
                  {sanitizeDisplayText(field.helpText)}
                </FormHelperText>
              ) : null}
            </FieldWrapper>
          )
        }

        if (treatAsCommodity) {
          return (
            <FieldWrapper key={field.id} field={renderedField} hideHelpText>
              <CommodityHsFieldRenderer
                field={renderedField}
                values={values}
                onChange={onChange}
                onBatchChange={onBatchChange}
                agencyId={agencyId}
                disabled={disabled}
              />
              {fieldError ? (
                <FormHelperText error sx={{ mx: 0, mt: 0.75 }}>
                  {fieldError}
                </FormHelperText>
              ) : null}
            </FieldWrapper>
          )
        }

        const isWarehouseAutofillAddress = field.type === 'textarea' && warehouseAutofillTargets.has(field.name)

        return (
          <FieldWrapper
            key={field.id}
            field={field}
            hideHelpText={isWarehouseAutofillAddress}
          >
            <TextField
              fullWidth
              size="small"
              label={field.label}
              type={
                field.type === 'number'
                  ? 'number'
                  : field.type === 'email'
                    ? 'email'
                    : field.type === 'date'
                      ? 'date'
                      : 'text'
              }
              multiline={field.type === 'textarea'}
              rows={field.type === 'textarea' ? 4 : undefined}
              value={value}
              required={field.required}
              disabled={disabled}
              placeholder={field.placeholder}
              error={Boolean(fieldError)}
              helperText={
                fieldError
                ?? (isWarehouseAutofillAddress ? 'Auto-filled from selected warehouse. This field cannot be edited manually.' : undefined)
              }
              slotProps={{
                ...(field.type === 'date' ? { inputLabel: { shrink: true } } : {}),
                input: isWarehouseAutofillAddress
                  ? { readOnly: true }
                  : undefined,
              }}
              onChange={isWarehouseAutofillAddress ? undefined : (e) => onChange(field.name, e.target.value)}
              sx={{
                ...fieldInputSx,
                ...(isWarehouseAutofillAddress
                  ? {
                      '& .MuiOutlinedInput-root': {
                        borderRadius: '0.5rem',
                        bgcolor: portalColors.bgMuted,
                      },
                    }
                  : {}),
              }}
            />
          </FieldWrapper>
        )
      })}
    </Grid>
  )
}

export function mapDynamicValuesToEntryDetail(values: Record<string, string>, commodityId?: string, unit = 'kg') {
  const commodityName =
    Object.entries(values).find(([key, value]) => key.endsWith('_commodity_name') && value.trim())?.[1]
    ?? values.commodityName
    ?? values.commodity
    ?? ''

  return {
    commodityId: commodityId ? Number(commodityId) : null,
    commodityName,
    description: values.description ?? '',
    quantity: Number(values.quantity ?? values.txt_volume_weight ?? 1),
    unit: values.unit ?? unit,
    originCountry: values.originCountry ?? '',
    destinationCountry: values.destinationCountry ?? 'Philippines',
    portOfEntry: values.portOfEntry ?? '',
  }
}
