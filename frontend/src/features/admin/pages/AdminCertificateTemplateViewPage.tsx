import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  Stack,
  Typography,
} from '@mui/material'
import { useSelector } from 'react-redux'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { apiUrl } from '../../../app/apiBase'
import type { RootState } from '../../../app/store'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetAdminAgenciesQuery, useGetAdminCertificateTemplateQuery } from '../api/adminApi'
import { parseLayout } from '../components/certificateBuilder/certificateBuilderUtils'

function formatProcessType(type: string) {
  if (type === 'ImportEntry') return 'Import Entry'
  if (type === 'ExportEntry') return 'Export Entry'
  return type.replace(/([A-Z])/g, ' $1').trim()
}

export function AdminCertificateTemplateViewPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading } = useGetAdminCertificateTemplateQuery(uuid, { skip: !uuid })
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const template = data?.data
  const agencies = agenciesData?.data ?? []
  const agency = agencies.find((item) => item.id === template?.agencyId)
  const accessToken = useSelector((state: RootState) => state.auth.accessToken)
  const layout = parseLayout(template?.layoutJson)

  const handlePreview = async () => {
    const response = await fetch(apiUrl(`/admin/certificate-templates/${uuid}/preview`), {
      headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
    })
    if (!response.ok) return
    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    window.open(url, '_blank', 'noopener,noreferrer')
  }

  if (isLoading || !template) {
    return <Typography sx={{ color: portalColors.textMuted }}>{isLoading ? 'Loading template…' : 'Template not found.'}</Typography>
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Certificate Templates"
        title={template.name}
        subtitle="Certificate template details"
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button component={RouterLink} to="/admin/certificate-templates" variant="outlined" sx={portalOutlinedButtonSx}>
              Back to Templates
            </Button>
            <Button variant="outlined" sx={portalOutlinedButtonSx} startIcon={<VisibilityOutlinedIcon />} onClick={() => void handlePreview()}>
              Preview
            </Button>
            <Button component={RouterLink} to={`/admin/certificate-templates/${uuid}/edit`} variant="contained" sx={portalPrimaryButtonSx} startIcon={<EditOutlinedIcon />}>
              Edit Template
            </Button>
          </Stack>
        }
      />

      <Stack direction="row" spacing={1} sx={{ mb: 3, flexWrap: 'wrap' }}>
        <Chip size="small" label={template.isActive ? 'Active' : 'Inactive'} sx={portalStatusChipSx(template.isActive ? 'Approved' : 'Draft')} />
        {template.processTypes.map((type) => (
          <Chip key={type} size="small" label={formatProcessType(type)} sx={portalStatusChipSx('Submitted')} />
        ))}
        <Chip size="small" variant="outlined" label={`${template.elements.length} elements`} />
        <Chip size="small" variant="outlined" label={`v${template.versionNumber}`} />
      </Stack>

      {template.name === 'Accreditation Certificate - Importer' ? (
        <Alert severity="success" sx={{ mb: 3 }}>
          Migrated from AgriCheck V2 and wired for accreditation certificate generation on approval.
        </Alert>
      ) : null}

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: '2fr 1fr' }, gap: 3 }}>
        <PortalPanel title="Template Information">
          <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
            <Box>
              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 0.5 }}>Name</Typography>
              <Typography>{template.name}</Typography>
            </Box>
            {template.description ? (
              <Box>
                <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 0.5 }}>Description</Typography>
                <Typography>{template.description}</Typography>
              </Box>
            ) : null}
            <Box>
              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 0.5 }}>Agency</Typography>
              <Typography>{agency ? `${agency.code} — ${agency.name}` : 'Global (all agencies)'}</Typography>
            </Box>
            <Box>
              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 0.5 }}>Paper Size</Typography>
              <Typography>{layout.paperSize}</Typography>
            </Box>
            <Box>
              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 0.5 }}>Orientation</Typography>
              <Typography>{layout.orientation}</Typography>
            </Box>
            <Box>
              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 0.5 }}>Margins (mm)</Typography>
              <Typography>
                Top {layout.marginTop} · Right {layout.marginRight} · Bottom {layout.marginBottom} · Left {layout.marginLeft}
              </Typography>
            </Box>
            <Box>
              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 0.5 }}>Background Color</Typography>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Box sx={{ width: 24, height: 24, borderRadius: 1, bgcolor: layout.backgroundColor, border: `1px solid ${portalColors.border}` }} />
                <Typography>{layout.backgroundColor}</Typography>
              </Stack>
            </Box>
          </Stack>
        </PortalPanel>

        <Stack spacing={3}>
          <PortalPanel title="Summary">
            <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
              <Typography variant="body2"><strong>Status:</strong> {template.isPublished ? 'Published' : 'Draft'}</Typography>
              <Typography variant="body2"><strong>Elements:</strong> {template.elements.length}</Typography>
              <Typography variant="body2"><strong>Processes:</strong> {template.processTypes.map(formatProcessType).join(', ') || 'None assigned'}</Typography>
            </Stack>
          </PortalPanel>

          <PortalPanel title="Quick Actions">
            <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
              <Button component={RouterLink} to={`/admin/certificate-templates/${uuid}/edit`} variant="contained" sx={portalPrimaryButtonSx}>
                Open Visual Builder
              </Button>
              <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={() => void handlePreview()}>
                Preview PDF
              </Button>
            </Stack>
          </PortalPanel>
        </Stack>
      </Box>
    </Box>
  )
}
