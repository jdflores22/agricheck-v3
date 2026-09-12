import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import DragIndicatorIcon from '@mui/icons-material/DragIndicator'
import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown'
import KeyboardArrowUpIcon from '@mui/icons-material/KeyboardArrowUp'
import { Box, Chip, IconButton, Stack, Typography } from '@mui/material'
import { portalColors } from '../../../components/portal/portalTheme'
import type { FormFieldSchema, FormFieldType } from '../../forms/formSchema'
import { FIELD_TYPE_DEFINITIONS } from '../../forms/formSchema'

interface FormCanvasProps {
  fields: FormFieldSchema[]
  selectedId: string | null
  previewMode: boolean
  onSelect: (id: string) => void
  onRemove: (id: string) => void
  onReorder: (fromIndex: number, toIndex: number) => void
  onDropNewField: (type: FormFieldType, index?: number) => void
}

function fieldTypeLabel(type: FormFieldType): string {
  return FIELD_TYPE_DEFINITIONS.find((item) => item.type === type)?.label ?? type
}

export function FormCanvas({
  fields,
  selectedId,
  previewMode,
  onSelect,
  onRemove,
  onReorder,
  onDropNewField,
}: FormCanvasProps) {
  const handleDrop = (e: React.DragEvent, index?: number) => {
    e.preventDefault()
    const newType = e.dataTransfer.getData('application/x-form-field-type') as FormFieldType
    const moveId = e.dataTransfer.getData('application/x-form-field-id')
    if (newType) {
      onDropNewField(newType, index)
      return
    }
    if (moveId) {
      const fromIndex = fields.findIndex((field) => field.id === moveId)
      const toIndex = index ?? fields.length - 1
      if (fromIndex >= 0 && fromIndex !== toIndex) {
        onReorder(fromIndex, toIndex)
      }
    }
  }

  return (
    <Box
      onDragOver={(e) => e.preventDefault()}
      onDrop={(e) => handleDrop(e, fields.length)}
      sx={{
        minHeight: 420,
        p: 2,
        borderRadius: '0.75rem',
        border: `2px dashed ${portalColors.border}`,
        bgcolor: portalColors.bgMuted,
      }}
    >
      {fields.length === 0 && (
        <Typography sx={{ py: 8, textAlign: 'center', color: portalColors.textMuted, fontSize: '0.875rem' }}>
          Drag field types here or click a field type to add your first field.
        </Typography>
      )}

      <Stack spacing={1.25}>
        {fields.map((field, index) => {
          const selected = field.id === selectedId
          return (
            <Box
              key={field.id}
              draggable={!previewMode}
              onDragStart={(e) => {
                e.dataTransfer.setData('application/x-form-field-id', field.id)
                e.dataTransfer.effectAllowed = 'move'
              }}
              onDragOver={(e) => e.preventDefault()}
              onDrop={(e) => {
                e.stopPropagation()
                handleDrop(e, index)
              }}
              onClick={() => !previewMode && onSelect(field.id)}
              sx={{
                px: 1.5,
                py: 1.25,
                borderRadius: '0.5rem',
                border: `1px solid ${selected ? portalColors.primary : portalColors.border}`,
                bgcolor: '#fff',
                boxShadow: selected ? `0 0 0 2px ${portalColors.primary}33` : 'none',
                cursor: previewMode ? 'default' : 'pointer',
              }}
            >
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                {!previewMode && (
                  <DragIndicatorIcon sx={{ color: portalColors.textMuted, fontSize: 18 }} />
                )}
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
                    <Typography sx={{ fontWeight: 600, fontSize: '0.875rem', color: portalColors.textDark }}>
                      {field.label}
                    </Typography>
                    <Chip size="small" label={fieldTypeLabel(field.type)} sx={{ height: 22, fontSize: '0.6875rem' }} />
                    {field.required && field.type !== 'section' && (
                      <Chip size="small" color="error" variant="outlined" label="Required" sx={{ height: 22, fontSize: '0.6875rem' }} />
                    )}
                    <Chip size="small" variant="outlined" label={`${field.columnWidth ?? 12}/12`} sx={{ height: 22, fontSize: '0.6875rem' }} />
                  </Stack>
                  <Typography sx={{ mt: 0.25, fontSize: '0.75rem', color: portalColors.textMuted }}>
                    {field.name}
                    {field.placeholder ? ` · ${field.placeholder}` : ''}
                  </Typography>
                </Box>
                {!previewMode && (
                  <Stack direction="row" spacing={0.25}>
                    <IconButton
                      size="small"
                      disabled={index === 0}
                      onClick={(e) => {
                        e.stopPropagation()
                        onReorder(index, index - 1)
                      }}
                    >
                      <KeyboardArrowUpIcon fontSize="small" />
                    </IconButton>
                    <IconButton
                      size="small"
                      disabled={index === fields.length - 1}
                      onClick={(e) => {
                        e.stopPropagation()
                        onReorder(index, index + 1)
                      }}
                    >
                      <KeyboardArrowDownIcon fontSize="small" />
                    </IconButton>
                    <IconButton
                      size="small"
                      color="error"
                      onClick={(e) => {
                        e.stopPropagation()
                        onRemove(field.id)
                      }}
                    >
                      <DeleteOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Stack>
                )}
              </Stack>
            </Box>
          )
        })}
      </Stack>
    </Box>
  )
}
