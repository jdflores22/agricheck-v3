import { Box, Typography } from '@mui/material'
import { useMemo, useState } from 'react'
import { DynamicFormRenderer } from '../../client/components/DynamicFormRenderer'
import { portalColors } from '../../../components/portal/portalTheme'
import { parseFormSchema, serializeFormSchema, type FormFieldSchema } from '../../forms/formSchema'

interface FormPreviewPanelProps {
  fields: FormFieldSchema[]
}

export function FormPreviewPanel({ fields }: FormPreviewPanelProps) {
  const [values, setValues] = useState<Record<string, string>>({})
  const schemaJson = useMemo(() => serializeFormSchema(fields), [fields])

  if (fields.length === 0) {
    return (
      <Typography sx={{ py: 4, textAlign: 'center', color: portalColors.textMuted, fontSize: '0.875rem' }}>
        Add fields to preview the form.
      </Typography>
    )
  }

  return (
    <Box sx={{ p: 2, borderRadius: '0.75rem', border: `1px solid ${portalColors.border}`, bgcolor: '#fff' }}>
      <DynamicFormRenderer
        schemaJson={schemaJson}
        values={values}
        onChange={(name, value) => setValues((prev) => ({ ...prev, [name]: value }))}
      />
    </Box>
  )
}

export function parseFieldsForBuilder(schemaJson: string): FormFieldSchema[] {
  return parseFormSchema(schemaJson)
}
