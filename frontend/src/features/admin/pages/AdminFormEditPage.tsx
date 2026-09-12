import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import PreviewIcon from '@mui/icons-material/Preview'
import SaveIcon from '@mui/icons-material/Save'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Grid,
  MenuItem,
  Snackbar,
  Stack,
  TextField,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetAdminAgenciesQuery,
  useGetAdminFormQuery,
  useSaveAdminFormMutation,
} from '../api/adminApi'
import { FormCanvas } from '../forms/FormCanvas'
import { FormFieldEditor } from '../forms/FormFieldEditor'
import { FormFieldPalette } from '../forms/FormFieldPalette'
import { FormPreviewPanel } from '../forms/FormPreviewPanel'
import {
  FORM_TYPES,
  createField,
  parseFormSchema,
  serializeFormSchema,
  type FormFieldSchema,
  type FormFieldType,
  type FormType,
} from '../../forms/formSchema'

export function AdminFormEditPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading, isError } = useGetAdminFormQuery(uuid, { skip: !uuid })
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [saveForm, { isLoading: saving }] = useSaveAdminFormMutation()

  const [name, setName] = useState('')
  const [formType, setFormType] = useState<FormType>('ENTRY')
  const [agencyIds, setAgencyIds] = useState<number[]>([])
  const [fields, setFields] = useState<FormFieldSchema[]>([])
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [previewMode, setPreviewMode] = useState(false)
  const [publish, setPublish] = useState(false)
  const [toast, setToast] = useState<string | null>(null)
  const [loadedVersion, setLoadedVersion] = useState(0)

  const agencies = agenciesData?.data ?? []
  const selectedField = useMemo(
    () => fields.find((field) => field.id === selectedId) ?? null,
    [fields, selectedId],
  )

  useEffect(() => {
    if (!data?.data) return
    const template = data.data
    if (loadedVersion === template.versionNumber) return

    setName(template.name)
    setFormType((FORM_TYPES.includes(template.formType as FormType) ? template.formType : 'ENTRY') as FormType)
    setAgencyIds(template.agencyIds)
    const parsed = parseFormSchema(template.schemaJson)
    setFields(parsed)
    setSelectedId(parsed[0]?.id ?? null)
    setPublish(template.status === 'Published')
    setLoadedVersion(template.versionNumber)
  }, [data?.data, loadedVersion])

  const addField = (type: FormFieldType, index?: number) => {
    const existingNames = new Set(fields.map((field) => field.name))
    const next = createField(type, fields.length + 1, existingNames)
    setFields((prev) => {
      if (index === undefined || index >= prev.length) {
        return [...prev, next]
      }
      const copy = [...prev]
      copy.splice(index, 0, next)
      return copy
    })
    setSelectedId(next.id)
  }

  const updateField = (updated: FormFieldSchema) => {
    setFields((prev) => prev.map((field) => (field.id === updated.id ? updated : field)))
  }

  const removeField = (id: string) => {
    setFields((prev) => prev.filter((field) => field.id !== id))
    if (selectedId === id) {
      setSelectedId(null)
    }
  }

  const reorderFields = (fromIndex: number, toIndex: number) => {
    setFields((prev) => {
      const copy = [...prev]
      const [moved] = copy.splice(fromIndex, 1)
      copy.splice(toIndex, 0, moved)
      return copy
    })
  }

  const handleSave = async (shouldPublish: boolean) => {
    if (!name.trim()) {
      setToast('Form name is required.')
      return
    }
    if (fields.length === 0) {
      setToast('Add at least one field before saving.')
      return
    }

    await saveForm({
      uuid,
      name: name.trim(),
      formType,
      schemaJson: serializeFormSchema(fields),
      agencyIds,
      publish: shouldPublish,
    }).unwrap()

    setPublish(shouldPublish)
    setToast(shouldPublish ? 'Form saved and published.' : 'Draft saved.')
  }

  if (isLoading || (data?.data && loadedVersion === 0)) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress size={32} />
      </Box>
    )
  }

  if (isError || !data?.data) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Form template not found.</Alert>
        <Button sx={{ mt: 2 }} component={RouterLink} to="/admin/forms" startIcon={<ArrowBackIcon />}>
          Back to forms
        </Button>
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Form Builder"
        title={name || 'Edit form'}
        subtitle={`${formType} · v${data.data.versionNumber} · ${data.data.status}`}
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button component={RouterLink} to={`/admin/forms/${uuid}/view`} sx={portalOutlinedButtonSx} startIcon={<VisibilityOutlinedIcon />}>
              View details
            </Button>
            <Button component={RouterLink} to="/admin/forms" sx={portalOutlinedButtonSx} startIcon={<ArrowBackIcon />}>
              Back
            </Button>
            <Button
              sx={portalOutlinedButtonSx}
              startIcon={<PreviewIcon />}
              onClick={() => setPreviewMode((prev) => !prev)}
            >
              {previewMode ? 'Edit mode' : 'Preview'}
            </Button>
            <Button sx={portalOutlinedButtonSx} disabled={saving} onClick={() => handleSave(false)}>
              Save draft
            </Button>
            <Button variant="contained" sx={portalPrimaryButtonSx} disabled={saving} startIcon={<SaveIcon />} onClick={() => handleSave(true)}>
              Save & publish
            </Button>
          </Stack>
        }
      />

      <Box sx={{ mb: 2 }}>
      <PortalPanel title="Form settings">
        <Box sx={{ p: 2.5 }}>
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField label="Form name" value={name} fullWidth size="small" onChange={(e) => setName(e.target.value)} />
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField
              select
              label="Form type"
              value={formType}
              fullWidth
              size="small"
              onChange={(e) => setFormType(e.target.value as FormType)}
            >
              {FORM_TYPES.map((type) => (
                <MenuItem key={type} value={type}>{type}</MenuItem>
              ))}
            </TextField>
          </Grid>
          <Grid size={{ xs: 12, md: 4 }}>
            <TextField
              select
              label="Agencies"
              value={agencyIds}
              fullWidth
              size="small"
              slotProps={{
                select: {
                  multiple: true,
                  renderValue: (selected: unknown) =>
                    (selected as number[]).map((id) => agencies.find((a) => a.id === id)?.code ?? id).join(', '),
                },
              }}
              onChange={(e) => {
                const value = e.target.value
                setAgencyIds(typeof value === 'string' ? value.split(',').map(Number) : (value as number[]))
              }}
            >
              {agencies.map((agency) => (
                <MenuItem key={agency.id} value={agency.id}>
                  {agency.code} — {agency.name}
                </MenuItem>
              ))}
            </TextField>
          </Grid>
        </Grid>
        <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
          <Chip label={publish ? 'Published' : 'Draft'} color={publish ? 'success' : 'default'} size="small" />
          <Chip label={`${fields.length} fields`} size="small" variant="outlined" />
        </Stack>
        </Box>
      </PortalPanel>
      </Box>

      {previewMode ? (
        <PortalPanel title="Form preview">
          <Box sx={{ p: 2.5 }}>
          <FormPreviewPanel fields={fields} />
          </Box>
        </PortalPanel>
      ) : (
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, lg: 3 }}>
            <PortalPanel title="Add fields">
              <Box sx={{ p: 2.5 }}>
              <FormFieldPalette onAddField={(type) => addField(type)} />
              </Box>
            </PortalPanel>
          </Grid>
          <Grid size={{ xs: 12, lg: 5 }}>
            <PortalPanel title="Form canvas">
              <Box sx={{ p: 2.5 }}>
              <FormCanvas
                fields={fields}
                selectedId={selectedId}
                previewMode={false}
                onSelect={setSelectedId}
                onRemove={removeField}
                onReorder={reorderFields}
                onDropNewField={addField}
              />
              </Box>
            </PortalPanel>
          </Grid>
          <Grid size={{ xs: 12, lg: 4 }}>
            <PortalPanel title="Properties">
              <Box sx={{ p: 2.5 }}>
              <FormFieldEditor field={selectedField} allFields={fields} onChange={updateField} />
              </Box>
            </PortalPanel>
          </Grid>
        </Grid>
      )}

      <Snackbar open={Boolean(toast)} autoHideDuration={4000} onClose={() => setToast(null)} message={toast} />
    </Box>
  )
}
