import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import ContentCopyOutlinedIcon from '@mui/icons-material/ContentCopyOutlined'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import PreviewOutlinedIcon from '@mui/icons-material/PreviewOutlined'
import ToggleOffOutlinedIcon from '@mui/icons-material/ToggleOffOutlined'
import ToggleOnOutlinedIcon from '@mui/icons-material/ToggleOnOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { apiUrl } from '../../../app/apiBase'
import { useAppSelector } from '../../../app/hooks'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { portalColors } from '../../../components/portal/portalTheme'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  CERTIFICATE_DELETE_LIVE_TOOLTIP,
  downloadAdminCertificateExport,
  formatCertificateDate,
  formatCertificateProcessType,
  getCertificateActivateTooltip,
  isCertificateTemplateLive,
} from '../adminCertificateUtils'
import { AdminCertificateDeleteDialog } from '../components/AdminCertificateDeleteDialog'
import { getAdminApiErrorMessage } from '../components/adminAgencyUtils'
import {
  useCloneAdminCertificateTemplateMutation,
  useDeleteAdminCertificateTemplateMutation,
  useGetAdminAgenciesQuery,
  useGetAdminCertificateTemplateQuery,
  useSetAdminCertificateTemplateActiveMutation,
} from '../api/adminApi'
import { PALETTE_ITEMS, parseLayout } from '../components/certificateBuilder/certificateBuilderUtils'

function DetailField({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <Box>
      <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textMuted, mb: 0.5 }}>
        {label}
      </Typography>
      <Box sx={{ fontSize: '0.9375rem', color: portalColors.textDark }}>{children}</Box>
    </Box>
  )
}

function getElementTypeLabel(type: string) {
  return PALETTE_ITEMS.find((item) => item.type === type)?.label ?? type
}

export function AdminCertificateTemplateViewPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const accessToken = useAppSelector((state) => state.auth.accessToken)
  const { data, isLoading, isError } = useGetAdminCertificateTemplateQuery(uuid, { skip: !uuid })
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [cloneTemplate, { isLoading: cloning }] = useCloneAdminCertificateTemplateMutation()
  const [deleteTemplate, { isLoading: deleting }] = useDeleteAdminCertificateTemplateMutation()
  const [setTemplateActive, { isLoading: togglingActive }] = useSetAdminCertificateTemplateActiveMutation()

  const [cloneOpen, setCloneOpen] = useState(false)
  const [cloneName, setCloneName] = useState('')
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [actionError, setActionError] = useState('')

  const template = data?.data
  const agencies = agenciesData?.data ?? []
  const hasPublishedVersion = template?.versions.some((version) => version.isPublished) ?? template?.isPublished ?? false
  const isLive = template ? isCertificateTemplateLive({ isActive: template.isActive, hasPublishedVersion }) : false
  const layout = useMemo(() => parseLayout(template?.layoutJson), [template?.layoutJson])
  const agency = agencies.find((item) => item.id === template?.agencyId)
  const elements = template?.elements ?? []

  const handlePreview = async () => {
    const response = await fetch(apiUrl(`/admin/certificate-templates/${uuid}/preview`), {
      headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
    })
    if (!response.ok) return
    const blob = await response.blob()
    window.open(URL.createObjectURL(blob), '_blank', 'noopener,noreferrer')
  }

  const handleExport = async () => {
    if (!template) return
    setExporting(true)
    try {
      await downloadAdminCertificateExport(
        template.uuid,
        `${template.name.replace(/\s+/g, '-').toLowerCase()}.json`,
        accessToken,
      )
    } catch {
      window.alert('Unable to export this template.')
    } finally {
      setExporting(false)
    }
  }

  const handleClone = async () => {
    if (!template || !cloneName.trim()) return
    const result = await cloneTemplate({ uuid: template.uuid, name: cloneName.trim() }).unwrap()
    setCloneOpen(false)
    navigate(`/admin/certificate-templates/${result.data.uuid}/edit`)
  }

  const handleDelete = async () => {
    if (!template) return
    setActionError('')
    try {
      await deleteTemplate(template.uuid).unwrap()
      setDeleteOpen(false)
      navigate('/admin/certificate-templates')
    } catch (error) {
      setActionError(getAdminApiErrorMessage(error, 'Unable to delete this template.'))
      setDeleteOpen(false)
    }
  }

  const handleSetActive = async (isActive: boolean) => {
    if (!template) return
    setActionError('')
    try {
      await setTemplateActive({ uuid: template.uuid, isActive }).unwrap()
    } catch (error) {
      setActionError(
        getAdminApiErrorMessage(
          error,
          isActive ? 'Unable to activate this template.' : 'Unable to deactivate this template.',
        ),
      )
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  if (isError || !template) {
    return (
      <Box>
        <Alert severity="error">Certificate template not found.</Alert>
        <Button component={RouterLink} to="/admin/certificate-templates" sx={{ mt: 2 }} startIcon={<ArrowBackIcon />}>
          Back to templates
        </Button>
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Certificate Builder"
        title={template.name}
        subtitle={`${template.processTypes.map(formatCertificateProcessType).join(', ') || 'No process'} · v${template.versionNumber}`}
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Tooltip
              title={getCertificateActivateTooltip({
                isActive: template.isActive,
                elementCount: elements.length,
                hasPublishedVersion,
              })}
            >
              <span>
                <Button
                  variant={template.isActive ? 'outlined' : 'contained'}
                  startIcon={template.isActive ? <ToggleOffOutlinedIcon /> : <ToggleOnOutlinedIcon />}
                  sx={template.isActive ? portalOutlinedButtonSx : portalPrimaryButtonSx}
                  disabled={togglingActive || (!template.isActive && elements.length === 0)}
                  onClick={() => void handleSetActive(!template.isActive)}
                >
                  {template.isActive ? 'Deactivate' : 'Activate'}
                </Button>
              </span>
            </Tooltip>
            <Button
              component={RouterLink}
              to={`/admin/certificate-templates/${uuid}/edit`}
              variant="contained"
              startIcon={<EditOutlinedIcon />}
              sx={portalPrimaryButtonSx}
            >
              Edit builder
            </Button>
            <Button component={RouterLink} to="/admin/certificate-templates" variant="outlined" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
              Back
            </Button>
          </Stack>
        }
      />

      {actionError ? (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setActionError('')}>
          {actionError}
        </Alert>
      ) : null}

      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
        <Chip size="small" label={template.isActive ? 'Active' : 'Inactive'} sx={getStatusBadgeStyle(template.isActive ? 'ACTIVE' : 'PENDING')} />
        <Chip size="small" label={template.isPublished ? 'Published' : 'Draft'} sx={getStatusBadgeStyle(template.isPublished ? 'Approved' : 'Draft')} />
        {isLive ? <Chip size="small" label="Live" sx={getStatusBadgeStyle('ACTIVE')} /> : null}
        {template.processTypes.map((type) => (
          <Chip key={type} size="small" label={formatCertificateProcessType(type)} sx={getStatusBadgeStyle('Submitted')} />
        ))}
      </Stack>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Stack spacing={2}>
            <PortalPanel title="Template details">
              <Box sx={{ p: 2.5 }}>
                <Grid container spacing={2.5}>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Process types">
                      {template.processTypes.length > 0
                        ? template.processTypes.map(formatCertificateProcessType).join(', ')
                        : 'None assigned'}
                    </DetailField>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Version">v{template.versionNumber}</DetailField>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Created">{formatCertificateDate(template.createdAt)}</DetailField>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Last updated">{formatCertificateDate(template.updatedAt)}</DetailField>
                  </Grid>
                  <Grid size={{ xs: 12 }}>
                    <DetailField label="Agency">
                      {agency ? (
                        <Chip size="small" label={`${agency.code} — ${agency.name}`} sx={getStatusBadgeStyle('Approved')} />
                      ) : (
                        <Typography sx={{ color: portalColors.textMuted, fontSize: '0.875rem' }}>Global (all agencies)</Typography>
                      )}
                    </DetailField>
                  </Grid>
                  {template.description ? (
                    <Grid size={{ xs: 12 }}>
                      <DetailField label="Description">{template.description}</DetailField>
                    </Grid>
                  ) : null}
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Paper size">{layout.paperSize}</DetailField>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Orientation">{layout.orientation}</DetailField>
                  </Grid>
                </Grid>
              </Box>
            </PortalPanel>

            <PortalPanel title={`Elements (${elements.length})`}>
              <Box sx={{ p: 2.5 }}>
                {elements.length === 0 ? (
                  <Typography sx={{ py: 3, textAlign: 'center', color: portalColors.textMuted, fontSize: '0.875rem' }}>
                    No elements yet. Open the builder to add elements.
                  </Typography>
                ) : (
                  <Stack spacing={1.25}>
                    {elements.map((element, index) => (
                      <Box
                        key={element.id}
                        sx={{
                          border: `1px solid ${portalColors.border}`,
                          borderRadius: '0.625rem',
                          p: 1.75,
                          bgcolor: portalColors.bgWhite,
                        }}
                      >
                        <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }}>
                          <Box>
                            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 0.5 }}>
                              <Chip size="small" label={index + 1} sx={{ minWidth: 28, height: 24, ...getStatusBadgeStyle('PENDING') }} />
                              <Typography sx={{ fontWeight: 600 }}>{element.label}</Typography>
                            </Stack>
                            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                              {getElementTypeLabel(element.elementType)} · Order {element.sortOrder}
                            </Typography>
                          </Box>
                          <Chip size="small" label={getElementTypeLabel(element.elementType)} sx={getStatusBadgeStyle('Submitted')} />
                        </Stack>
                      </Box>
                    ))}
                  </Stack>
                )}
              </Box>
            </PortalPanel>

            <PortalPanel title="PDF preview">
              <Box sx={{ p: 2.5 }}>
                <Typography sx={{ mb: 2, color: portalColors.textMuted, fontSize: '0.875rem' }}>
                  Generate a sample PDF using preview variables for the assigned process type.
                </Typography>
                <Button variant="outlined" startIcon={<PreviewOutlinedIcon />} sx={portalOutlinedButtonSx} onClick={() => void handlePreview()}>
                  Open PDF preview
                </Button>
              </Box>
            </PortalPanel>

            <PortalPanel title="Version history">
              <Box sx={{ p: 2.5 }}>
                {template.versions.length === 0 ? (
                  <Typography sx={{ color: portalColors.textMuted, fontSize: '0.875rem' }}>No version history.</Typography>
                ) : (
                  <Stack spacing={1.25}>
                    {template.versions.slice(0, 5).map((version) => (
                      <Box
                        key={version.versionNumber}
                        sx={{
                          display: 'flex',
                          justifyContent: 'space-between',
                          alignItems: 'center',
                          border: `1px solid ${portalColors.border}`,
                          borderRadius: '0.625rem',
                          p: 1.75,
                        }}
                      >
                        <Box>
                          <Typography sx={{ fontWeight: 600 }}>Version {version.versionNumber}</Typography>
                          <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                            {formatCertificateDate(version.createdAt)}
                            {version.isPublished ? ' · Published' : ' · Draft'}
                          </Typography>
                        </Box>
                        {version.versionNumber === template.versionNumber ? (
                          <Chip size="small" label="Current" sx={getStatusBadgeStyle('ACTIVE')} />
                        ) : null}
                      </Box>
                    ))}
                  </Stack>
                )}
              </Box>
            </PortalPanel>
          </Stack>
        </Grid>

        <Grid size={{ xs: 12, lg: 4 }}>
          <Stack spacing={2}>
            <Grid container spacing={1.5}>
              <Grid size={{ xs: 6 }}>
                <PortalStatCard label="Elements" value={elements.length} />
              </Grid>
              <Grid size={{ xs: 6 }}>
                <PortalStatCard label="Version" value={template.versionNumber} />
              </Grid>
            </Grid>

            <PortalPanel title="Actions">
              <Stack spacing={1.25} sx={{ p: 2.5, pt: 0 }}>
                <Tooltip
                  title={getCertificateActivateTooltip({
                    isActive: template.isActive,
                    elementCount: elements.length,
                    hasPublishedVersion,
                  })}
                >
                  <span>
                    <Button
                      variant={template.isActive ? 'outlined' : 'contained'}
                      fullWidth
                      startIcon={template.isActive ? <ToggleOffOutlinedIcon /> : <ToggleOnOutlinedIcon />}
                      sx={template.isActive ? portalOutlinedButtonSx : portalPrimaryButtonSx}
                      disabled={togglingActive || (!template.isActive && elements.length === 0)}
                      onClick={() => void handleSetActive(!template.isActive)}
                    >
                      {template.isActive ? 'Deactivate' : 'Activate'}
                    </Button>
                  </span>
                </Tooltip>
                <Button
                  component={RouterLink}
                  to={`/admin/certificate-templates/${uuid}/edit`}
                  variant="contained"
                  fullWidth
                  startIcon={<EditOutlinedIcon />}
                  sx={portalPrimaryButtonSx}
                >
                  Edit builder
                </Button>
                <Button
                  variant="outlined"
                  fullWidth
                  startIcon={<PreviewOutlinedIcon />}
                  sx={portalOutlinedButtonSx}
                  onClick={() => void handlePreview()}
                >
                  Open PDF preview
                </Button>
                <Button variant="outlined" fullWidth startIcon={<DownloadOutlinedIcon />} sx={portalOutlinedButtonSx} disabled={exporting} onClick={() => void handleExport()}>
                  Export JSON
                </Button>
                <Button
                  variant="outlined"
                  fullWidth
                  startIcon={<ContentCopyOutlinedIcon />}
                  sx={portalOutlinedButtonSx}
                  onClick={() => {
                    setCloneName(`${template.name} (Copy)`)
                    setCloneOpen(true)
                  }}
                >
                  Clone template
                </Button>
                <Tooltip title={isLive ? CERTIFICATE_DELETE_LIVE_TOOLTIP : 'Delete this template'}>
                  <span>
                    <Button
                      variant="outlined"
                      fullWidth
                      color="error"
                      startIcon={<DeleteOutlinedIcon />}
                      sx={portalOutlinedButtonSx}
                      disabled={isLive}
                      onClick={() => setDeleteOpen(true)}
                    >
                      Delete template
                    </Button>
                  </span>
                </Tooltip>
                {isLive ? (
                  <Alert severity="info" sx={{ fontSize: '0.8125rem' }}>
                    Live templates cannot be deleted. Deactivate the template first.
                  </Alert>
                ) : null}
              </Stack>
            </PortalPanel>
          </Stack>
        </Grid>
      </Grid>

      <Dialog open={cloneOpen} onClose={() => setCloneOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Clone template</DialogTitle>
        <DialogContent>
          <Typography sx={{ mb: 2, color: portalColors.textMuted, fontSize: '0.875rem' }}>
            Create a copy of this template as an inactive draft.
          </Typography>
          <TextField
            label="New template name"
            value={cloneName}
            onChange={(e) => setCloneName(e.target.value)}
            fullWidth
            autoFocus
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setCloneOpen(false)}>Cancel</Button>
          <Button variant="contained" sx={portalPrimaryButtonSx} disabled={cloning || !cloneName.trim()} onClick={() => void handleClone()}>
            Clone
          </Button>
        </DialogActions>
      </Dialog>

      <AdminCertificateDeleteDialog
        open={deleteOpen}
        templateName={template.name}
        deleting={deleting}
        onClose={() => setDeleteOpen(false)}
        onConfirm={handleDelete}
      />
    </Box>
  )
}
