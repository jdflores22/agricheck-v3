import {
  Alert,
  Box,
  Checkbox,
  FormControlLabel,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import { portalColors } from '../../../components/portal/portalTheme'
import {
  COLUMN_WIDTHS,
  slugifyFieldName,
  type FormFieldSchema,
} from '../../forms/formSchema'

interface FormFieldEditorProps {
  field: FormFieldSchema | null
  allFields: FormFieldSchema[]
  onChange: (field: FormFieldSchema) => void
}

export function FormFieldEditor({ field, allFields, onChange }: FormFieldEditorProps) {
  if (!field) {
    return (
      <Typography sx={{ py: 4, textAlign: 'center', color: portalColors.textMuted, fontSize: '0.875rem' }}>
        Select a field to edit its properties.
      </Typography>
    )
  }

  const update = (patch: Partial<FormFieldSchema>) => onChange({ ...field, ...patch })
  const isSection = field.type === 'section'
  const isAddress = field.type === 'address'
  const isWarehouse = field.type === 'warehouse'
  const isCommodity = field.type === 'commodity'
  const hasOptions = field.type === 'select' || field.type === 'radio'
  const otherFields = allFields.filter((item) => item.id !== field.id && item.type !== 'section')

  return (
    <Stack spacing={2}>
      <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
        Field properties
      </Typography>

      <TextField
        label="Label"
        value={field.label}
        fullWidth
        size="small"
        onChange={(e) => {
          const label = e.target.value
          update({ label, name: field.name === slugifyFieldName(field.label) ? slugifyFieldName(label) : field.name })
        }}
      />

      <TextField
        label="Field name (key)"
        value={field.name}
        fullWidth
        size="small"
        helperText="Used when saving form data"
        onChange={(e) => update({ name: slugifyFieldName(e.target.value) })}
      />

      {!isSection && !isAddress && !isWarehouse && !isCommodity && (
        <>
          <TextField
            label="Placeholder"
            value={field.placeholder ?? ''}
            fullWidth
            size="small"
            onChange={(e) => update({ placeholder: e.target.value })}
          />
          <TextField
            label="Help text"
            value={field.helpText ?? ''}
            fullWidth
            size="small"
            multiline
            minRows={2}
            onChange={(e) => update({ helpText: e.target.value })}
          />
          <TextField
            label="Tooltip"
            value={field.tooltip ?? ''}
            fullWidth
            size="small"
            onChange={(e) => update({ tooltip: e.target.value })}
          />
        </>
      )}

      {isAddress && (
        <>
          <Alert severity="info">
            This field renders cascading selects for Region, Province, City/Municipality, Barangay, auto-filled Zip Code, plus a street address line.
          </Alert>
          <TextField
            label="Street placeholder"
            value={field.placeholder ?? ''}
            fullWidth
            size="small"
            onChange={(e) => update({ placeholder: e.target.value })}
          />
          <TextField
            label="Help text"
            value={field.helpText ?? ''}
            fullWidth
            size="small"
            multiline
            minRows={2}
            onChange={(e) => update({ helpText: e.target.value })}
          />
        </>
      )}

      {isCommodity && (
        <>
          <Alert severity="info">
            Connected to the MAV HS Code library. On the entry form the importer searches an HS code first,
            then picks a commodity under that code — same cascade as Region → Province.
            HS codes are filtered by the agency tagged on this form template (plus shared library codes).
            Assign categories to BAI / BFAR / BPI in MAV → HS Code Library.
          </Alert>
          <TextField
            label="HS search placeholder"
            value={field.placeholder ?? ''}
            fullWidth
            size="small"
            onChange={(e) => update({ placeholder: e.target.value })}
          />
          <TextField
            label="Help text"
            value={field.helpText ?? ''}
            fullWidth
            size="small"
            multiline
            minRows={2}
            onChange={(e) => update({ helpText: e.target.value })}
          />
        </>
      )}

      {isWarehouse && (
        <>
          <Alert severity="info">
            Loads registered DA warehouses. Selecting one auto-fills the linked address field.
          </Alert>
          <TextField
            label="Auto-fill address field key"
            value={String(field.validationRules?.autofillTarget ?? 'textarea_warehouse_address')}
            fullWidth
            size="small"
            helperText="Field name of the textarea that receives the warehouse address"
            onChange={(e) =>
              update({
                validationRules: {
                  ...(field.validationRules ?? {}),
                  autofillTarget: e.target.value,
                },
              })
            }
          />
          <TextField
            label="Help text"
            value={field.helpText ?? ''}
            fullWidth
            size="small"
            multiline
            minRows={2}
            onChange={(e) => update({ helpText: e.target.value })}
          />
        </>
      )}

      <TextField
        select
        label="Column width"
        value={field.columnWidth ?? 12}
        fullWidth
        size="small"
        onChange={(e) => update({ columnWidth: Number(e.target.value) })}
      >
        {COLUMN_WIDTHS.map((width) => (
          <MenuItem key={width} value={width}>
            {width}/12
          </MenuItem>
        ))}
      </TextField>

      {!isSection && (
        <FormControlLabel
          control={
            <Checkbox
              checked={Boolean(field.required)}
              onChange={(e) => update({ required: e.target.checked })}
            />
          }
          label="Required field"
        />
      )}

      {hasOptions && (
        <Box>
          <Typography sx={{ mb: 1, fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
            Options
          </Typography>
          <Stack spacing={1}>
            {(field.options ?? []).map((option, index) => (
              <Stack key={index} direction="row" spacing={1}>
                <TextField
                  label="Label"
                  size="small"
                  value={option.label}
                  fullWidth
                  onChange={(e) => {
                    const options = [...(field.options ?? [])]
                    options[index] = { ...options[index], label: e.target.value }
                    update({ options })
                  }}
                />
                <TextField
                  label="Value"
                  size="small"
                  value={option.value}
                  fullWidth
                  onChange={(e) => {
                    const options = [...(field.options ?? [])]
                    options[index] = { ...options[index], value: e.target.value }
                    update({ options })
                  }}
                />
                <IconButton
                  size="small"
                  color="error"
                  onClick={() => update({ options: (field.options ?? []).filter((_, i) => i !== index) })}
                >
                  <DeleteOutlinedIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))}
            <IconButton
              size="small"
              sx={{ alignSelf: 'flex-start' }}
              onClick={() =>
                update({
                  options: [
                    ...(field.options ?? []),
                    { label: `Option ${(field.options?.length ?? 0) + 1}`, value: `option_${(field.options?.length ?? 0) + 1}` },
                  ],
                })
              }
            >
              <AddIcon fontSize="small" />
            </IconButton>
          </Stack>
        </Box>
      )}

      {!isSection && (
        <Box>
          <Typography sx={{ mb: 1, fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
            Conditional visibility
          </Typography>
          <Stack spacing={1.25}>
            <TextField
              select
              label="Show when field"
              value={field.conditionalLogic?.showIf?.field ?? ''}
              fullWidth
              size="small"
              onChange={(e) =>
                update({
                  conditionalLogic: e.target.value
                    ? {
                        showIf: {
                          field: e.target.value,
                          operator: field.conditionalLogic?.showIf?.operator ?? 'equals',
                          value: field.conditionalLogic?.showIf?.value,
                        },
                      }
                    : undefined,
                })
              }
            >
              <MenuItem value="">Always visible</MenuItem>
              {otherFields.map((item) => (
                <MenuItem key={item.id} value={item.name}>
                  {item.label}
                </MenuItem>
              ))}
            </TextField>

            {field.conditionalLogic?.showIf?.field && (
              <>
                <TextField
                  select
                  label="Operator"
                  value={field.conditionalLogic.showIf.operator}
                  fullWidth
                  size="small"
                  onChange={(e) =>
                    update({
                      conditionalLogic: {
                        showIf: {
                          ...field.conditionalLogic!.showIf!,
                          operator: e.target.value as 'equals' | 'not_equals' | 'contains' | 'greater_than' | 'less_than' | 'is_empty' | 'is_not_empty',
                        },
                      },
                    })
                  }
                >
                  <MenuItem value="equals">Equals</MenuItem>
                  <MenuItem value="not_equals">Not equals</MenuItem>
                  <MenuItem value="contains">Contains</MenuItem>
                  <MenuItem value="greater_than">Greater than</MenuItem>
                  <MenuItem value="less_than">Less than</MenuItem>
                  <MenuItem value="is_empty">Is empty</MenuItem>
                  <MenuItem value="is_not_empty">Is not empty</MenuItem>
                </TextField>
                {!['is_empty', 'is_not_empty'].includes(field.conditionalLogic.showIf.operator) && (
                  <TextField
                    label="Value"
                    value={field.conditionalLogic.showIf.value ?? ''}
                    fullWidth
                    size="small"
                    onChange={(e) =>
                      update({
                        conditionalLogic: {
                          showIf: { ...field.conditionalLogic!.showIf!, value: e.target.value },
                        },
                      })
                    }
                  />
                )}
              </>
            )}
          </Stack>
        </Box>
      )}
    </Stack>
  )
}
