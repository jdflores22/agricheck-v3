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
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  Snackbar,
  Stack,
  TextField,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { apiUrl } from '../../../app/apiBase'
import { useSelector } from 'react-redux'
import type { RootState } from '../../../app/store'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  CERTIFICATE_PROCESS_TYPES,
  useGetAdminAgenciesQuery,
  useGetAdminCertificateTemplateQuery,
  useSaveAdminCertificateTemplateMutation,
} from '../api/adminApi'
import { formatCertificateProcessType } from '../adminCertificateUtils'
import {
  CertificateBuilder,
  type CertificateBuilderHandle,
} from '../components/certificateBuilder/CertificateBuilder'

async function fetchCertificatePreview(uuid: string, token: string | null) {
  const response = await fetch(apiUrl(`/admin/certificate-templates/${uuid}/preview`), {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  if (!response.ok) {
    const message = await response.text()
    throw new Error(message || 'Preview failed')
  }
  const blob = await response.blob()
  window.open(URL.createObjectURL(blob), '_blank', 'noopener,noreferrer')
}

async function uploadCertificateImage(uuid: string, file: File, token: string | null) {
  const formData = new FormData()
  formData.append('image', file)
  const response = await fetch(apiUrl(`/admin/certificate-templates/${uuid}/upload-image`), {
    method: 'POST',
    body: formData,
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  if (!response.ok) {
    throw new Error('Upload failed')
  }
  const json = (await response.json()) as { data?: { imagePath?: string } }
  return json.data?.imagePath ?? ''
}

export function AdminCertificateTemplateEditPage() {
  const { uuid = '' } = useParams()
  const accessToken = useSelector((state: RootState) => state.auth.accessToken)
  const builderRef = useRef<CertificateBuilderHandle>(null)
  const { data, isLoading, isError, refetch } = useGetAdminCertificateTemplateQuery(uuid, { skip: !uuid })
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [saveTemplate, { isLoading: saving }] = useSaveAdminCertificateTemplateMutation()

  const [name, setName] = useState('')
  const [processTypes, setProcessTypes] = useState<string[]>([])
  const [agencyId, setAgencyId] = useState<number | ''>('')
  const [description, setDescription] = useState('')
  const [publish, setPublish] = useState(false)
  const [elementCount, setElementCount] = useState(0)
  const [toast, setToast] = useState<string | null>(null)
  const [loadedVersion, setLoadedVersion] = useState(0)

  const agencies = agenciesData?.data ?? []
  const template = data?.data

  useEffect(() => {
    if (!template) return
    if (loadedVersion === template.versionNumber) return

    setName(template.name)
    setProcessTypes(template.processTypes)
    setAgencyId(template.agencyId ?? '')
    setDescription(template.description ?? '')
    setPublish(template.isPublished)
    setElementCount(template.elements.length)
    setLoadedVersion(template.versionNumber)
  }, [template, loadedVersion])

  const handleSave = async (shouldPublish: boolean) => {
    if (!template) return
    if (!name.trim()) {
      setToast('Template name is required.')
      return
    }
    if (processTypes.length === 0) {
      setToast('Select at least one process type.')
      return
    }

    const payload = builderRef.current?.getSavePayload()
    if (!payload) {
      setToast('Unable to read builder state.')
      return
    }

    await saveTemplate({
      uuid,
      name: name.trim(),
      description: description.trim() || undefined,
      agencyId: agencyId === '' ? undefined : agencyId,
      elements: payload.elements,
      processTypes,
      publish: shouldPublish,
      isActive: template.isActive,
      layoutJson: payload.layoutJson,
    }).unwrap()

    setPublish(shouldPublish)
    setToast(shouldPublish ? 'Template saved and published.' : 'Draft saved.')
    refetch()
  }

  if (isLoading || (template && loadedVersion === 0)) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress size={32} />
      </Box>
    )
  }

  if (isError || !template) {
    return (
      <Box sx={{ p: 3 }}>
        <Alert severity="error">Certificate template not found.</Alert>
        <Button sx={{ mt: 2 }} component={RouterLink} to="/admin/certificate-templates" startIcon={<ArrowBackIcon />}>
          Back to templates
        </Button>
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Certificate Builder"
        title={name || 'Edit template'}
        subtitle={`${processTypes.map(formatCertificateProcessType).join(', ') || 'No process'} · v${template.versionNumber} · ${publish ? 'Published' : 'Draft'}`}
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button
              component={RouterLink}
              to={`/admin/certificate-templates/${uuid}`}
              sx={portalOutlinedButtonSx}
              startIcon={<VisibilityOutlinedIcon />}
            >
              View details
            </Button>
            <Button component={RouterLink} to="/admin/certificate-templates" sx={portalOutlinedButtonSx} startIcon={<ArrowBackIcon />}>
              Back
            </Button>
            <Button
              sx={portalOutlinedButtonSx}
              startIcon={<PreviewIcon />}
              onClick={() => void fetchCertificatePreview(uuid, accessToken)}
            >
              Preview PDF
            </Button>
            <Button sx={portalOutlinedButtonSx} disabled={saving} onClick={() => void handleSave(false)}>
              Save draft
            </Button>
            <Button
              variant="contained"
              sx={portalPrimaryButtonSx}
              disabled={saving}
              startIcon={<SaveIcon />}
              onClick={() => void handleSave(true)}
            >
              Save & publish
            </Button>
          </Stack>
        }
      />

      <Box sx={{ mb: 2 }}>
        <PortalPanel title="Certificate settings">
          <Box sx={{ p: 2.5 }}>
            <Grid container spacing={2}>
              <Grid size={{ xs: 12, md: 4 }}>
                <TextField label="Template name" value={name} fullWidth size="small" onChange={(e) => setName(e.target.value)} />
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormControl fullWidth size="small">
                  <InputLabel>Process types</InputLabel>
                  <Select
                    multiple
                    label="Process types"
                    value={processTypes}
                    onChange={(e) => setProcessTypes(e.target.value as string[])}
                    renderValue={(selected) => selected.map(formatCertificateProcessType).join(', ')}
                  >
                    {CERTIFICATE_PROCESS_TYPES.map((type) => (
                      <MenuItem key={type} value={type}>
                        {formatCertificateProcessType(type)}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
              <Grid size={{ xs: 12, md: 4 }}>
                <FormControl fullWidth size="small">
                  <InputLabel>Agency</InputLabel>
                  <Select
                    label="Agency"
                    value={agencyId}
                    onChange={(e) => setAgencyId(e.target.value as number | '')}
                  >
                    <MenuItem value="">Global (all agencies)</MenuItem>
                    {agencies.map((agency) => (
                      <MenuItem key={agency.id} value={agency.id}>
                        {agency.code} — {agency.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              </Grid>
            </Grid>
            <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
              <Chip label={publish ? 'Published' : 'Draft'} color={publish ? 'success' : 'default'} size="small" />
              <Chip label={`${elementCount} elements`} size="small" variant="outlined" />
            </Stack>
          </Box>
        </PortalPanel>
      </Box>

      <CertificateBuilder
        ref={builderRef}
        template={template}
        processTypes={processTypes}
        toolbarMode="canvas-only"
        onElementCountChange={setElementCount}
        onUploadImage={(file) => uploadCertificateImage(uuid, file, accessToken)}
      />

      <Snackbar open={Boolean(toast)} autoHideDuration={4000} onClose={() => setToast(null)} message={toast} />
    </Box>
  )
}
