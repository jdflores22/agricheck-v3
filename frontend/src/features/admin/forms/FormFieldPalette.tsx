import { Box, Stack, Typography } from '@mui/material'
import { FIELD_TYPE_DEFINITIONS, type FormFieldType } from '../../forms/formSchema'
import { portalColors } from '../../../components/portal/portalTheme'

interface FormFieldPaletteProps {
  onAddField: (type: FormFieldType) => void
}

export function FormFieldPalette({ onAddField }: FormFieldPaletteProps) {
  return (
    <Stack spacing={1}>
      <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark, mb: 0.5 }}>
        Field types
      </Typography>
      {FIELD_TYPE_DEFINITIONS.map((item) => (
        <Box
          key={item.type}
          draggable
          onDragStart={(e) => {
            e.dataTransfer.setData('application/x-form-field-type', item.type)
            e.dataTransfer.effectAllowed = 'copy'
          }}
          onClick={() => onAddField(item.type)}
          sx={{
            px: 1.5,
            py: 1.25,
            borderRadius: '0.5rem',
            border: `1px solid ${portalColors.border}`,
            bgcolor: '#fff',
            cursor: 'grab',
            transition: 'border-color 0.15s, box-shadow 0.15s',
            '&:hover': {
              borderColor: portalColors.primary,
              boxShadow: `0 0 0 1px ${portalColors.primary}22`,
            },
          }}
        >
          <Stack direction="row" spacing={1.25} sx={{ alignItems: 'center' }}>
            <Box
              sx={{
                width: 28,
                height: 28,
                borderRadius: '0.375rem',
                bgcolor: portalColors.bgMuted,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: '0.75rem',
                fontWeight: 700,
                color: portalColors.primary,
              }}
            >
              {item.icon}
            </Box>
            <Box>
              <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
                {item.label}
              </Typography>
              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{item.description}</Typography>
            </Box>
          </Stack>
        </Box>
      ))}
    </Stack>
  )
}
