import { Box, Typography } from '@mui/material'
import { apiUrl } from '../../../app/apiBase'
import { useSelector } from 'react-redux'
import { useParams } from 'react-router-dom'
import type { RootState } from '../../../app/store'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import {
  useGetAdminCertificateTemplateQuery,
  useSaveAdminCertificateTemplateMutation,
} from '../api/adminApi'
import { CertificateBuilder } from '../components/certificateBuilder/CertificateBuilder'

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
  const { data, isLoading, refetch } = useGetAdminCertificateTemplateQuery(uuid, { skip: !uuid })
  const [saveTemplate, { isLoading: saving }] = useSaveAdminCertificateTemplateMutation()
  const template = data?.data

  if (isLoading || !template) {
    return <Typography sx={{ color: portalColors.textMuted }}>{isLoading ? 'Loading template…' : 'Template not found.'}</Typography>
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Certificate Builder"
        title={template.name}
        subtitle={`Visual editor · Version ${template.versionNumber}`}
      />

      <CertificateBuilder
        template={template}
        saving={saving}
        onSave={async ({ elements, layoutJson }) => {
          await saveTemplate({
            uuid,
            name: template.name,
            description: template.description,
            agencyId: template.agencyId,
            elements,
            processTypes: template.processTypes,
            publish: true,
            isActive: template.isActive,
            layoutJson,
          }).unwrap()
          refetch()
        }}
        onPreview={() => fetchCertificatePreview(uuid, accessToken)}
        onUploadImage={(file) => uploadCertificateImage(uuid, file, accessToken)}
      />
    </Box>
  )
}
