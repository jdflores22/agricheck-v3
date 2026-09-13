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
import { useAppSelector } from '../../../app/hooks'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { portalColors } from '../../../components/portal/portalTheme'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  AdminFormDeleteDialog,
  FORM_DELETE_LIVE_TOOLTIP,
} from '../components/AdminFormDeleteDialog'
import { downloadAdminFormExport, formatFormDate, getFormActivateTooltip, getFormTypeLabel } from '../adminFormUtils'
import { getAdminApiErrorMessage } from '../components/adminAgencyUtils'
import {
  useCloneAdminFormMutation,
  useDeleteAdminFormMutation,
  useGetAdminAgenciesQuery,
  useGetAdminFormQuery,
  useSetAdminFormActiveMutation,
} from '../api/adminApi'
import { FormPreviewPanel } from '../forms/FormPreviewPanel'
import { FIELD_TYPE_DEFINITIONS, parseFormSchema } from '../../forms/formSchema'

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

export function AdminFormViewPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const accessToken = useAppSelector((state) => state.auth.accessToken)
  const { data, isLoading, isError } = useGetAdminFormQuery(uuid, { skip: !uuid })
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [cloneForm, { isLoading: cloning }] = useCloneAdminFormMutation()
  const [deleteForm, { isLoading: deleting }] = useDeleteAdminFormMutation()
  const [setFormActive, { isLoading: togglingActive }] = useSetAdminFormActiveMutation()

  const [cloneOpen, setCloneOpen] = useState(false)
  const [cloneName, setCloneName] = useState('')
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [exporting, setExporting] = useState(false)
  const [actionError, setActionError] = useState('')

  const template = data?.data
  const hasPublishedVersion = template?.versions.some((version) => version.isPublished) ?? false
  const isLive = Boolean(template?.isActive && hasPublishedVersion)
  const agencies = agenciesData?.data ?? []

  const fields = useMemo(
    () => (template?.schemaJson ? parseFormSchema(template.schemaJson) : []),
    [template?.schemaJson],
  )

  const taggedAgencies = useMemo(() => {
    if (!template) return []
    return template.agencyIds
      .map((id) => agencies.find((agency) => agency.id === id))
      .filter((agency): agency is NonNullable<typeof agency> => Boolean(agency))
  }, [agencies, template])

  const handleExport = async () => {
    if (!template) return
    setExporting(true)
    try {
      await downloadAdminFormExport(
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
    const result = await cloneForm({ uuid: template.uuid, name: cloneName.trim() }).unwrap()
    setCloneOpen(false)
    navigate(`/admin/forms/${result.data.uuid}/edit`)
  }

  const handleDelete = async () => {
    if (!template) return
    await deleteForm(template.uuid).unwrap()
    setDeleteOpen(false)
    navigate('/admin/forms')
  }

  const handleSetActive = async (isActive: boolean) => {
    if (!template) return
    setActionError('')
    try {
      await setFormActive({ uuid: template.uuid, isActive }).unwrap()
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
        <Alert severity="error">Form template not found.</Alert>
        <Button component={RouterLink} to="/admin/forms" sx={{ mt: 2 }} startIcon={<ArrowBackIcon />}>
          Back to forms
        </Button>
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Form Builder"
        title={template.name}
        subtitle={`${getFormTypeLabel(template.formType)} template · v${template.versionNumber}`}
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Tooltip
              title={getFormActivateTooltip({
                isActive: template.isActive,
                fieldCount: fields.length,
                hasPublishedVersion,
              })}
            >
              <span>
                <Button
                  variant={template.isActive ? 'outlined' : 'contained'}
                  startIcon={template.isActive ? <ToggleOffOutlinedIcon /> : <ToggleOnOutlinedIcon />}
                  sx={template.isActive ? portalOutlinedButtonSx : portalPrimaryButtonSx}
                  disabled={togglingActive || (!template.isActive && fields.length === 0)}
                  onClick={() => void handleSetActive(!template.isActive)}
                >
                  {template.isActive ? 'Deactivate' : 'Activate'}
                </Button>
              </span>
            </Tooltip>
            <Button
              component={RouterLink}
              to={`/admin/forms/${uuid}/edit`}
              variant="contained"
              startIcon={<EditOutlinedIcon />}
              sx={portalPrimaryButtonSx}
            >
              Edit fields
            </Button>
            <Button component={RouterLink} to="/admin/forms" variant="outlined" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
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
        <Chip size="small" label={template.status} sx={getStatusBadgeStyle(template.status)} />
        {isLive ? <Chip size="small" label="Live" sx={getStatusBadgeStyle('ACTIVE')} /> : null}
        <Chip size="small" label={getFormTypeLabel(template.formType)} sx={getStatusBadgeStyle('Submitted')} />
      </Stack>

      <Grid container spacing={2}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <Stack spacing={2}>
            <PortalPanel title="Template details">
              <Box sx={{ p: 2.5 }}>
                <Grid container spacing={2.5}>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Form type">{getFormTypeLabel(template.formType)}</DetailField>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Version">v{template.versionNumber}</DetailField>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Created">{formatFormDate(template.createdAt)}</DetailField>
                  </Grid>
                  <Grid size={{ xs: 12, sm: 6 }}>
                    <DetailField label="Last updated">{formatFormDate(template.updatedAt)}</DetailField>
                  </Grid>
                  <Grid size={{ xs: 12 }}>
                    <DetailField label="Agencies">
                      {taggedAgencies.length > 0 ? (
                        <Stack direction="row" spacing={0.75} useFlexGap sx={{ flexWrap: 'wrap' }}>
                          {taggedAgencies.map((agency) => (
                            <Chip key={agency.id} size="small" label={`${agency.code} — ${agency.name}`} sx={getStatusBadgeStyle('Approved')} />
                          ))}
                        </Stack>
                      ) : (
                        <Typography sx={{ color: portalColors.textMuted, fontSize: '0.875rem' }}>None assigned</Typography>
                      )}
                    </DetailField>
                  </Grid>
                </Grid>
              </Box>
            </PortalPanel>

            <PortalPanel title={`Form fields (${fields.length})`}>
              <Box sx={{ p: 2.5 }}>
                {fields.length === 0 ? (
                  <Typography sx={{ py: 3, textAlign: 'center', color: portalColors.textMuted, fontSize: '0.875rem' }}>
                    No fields yet. Open the builder to add fields.
                  </Typography>
                ) : (
                  <Stack spacing={1.25}>
                    {fields.map((field, index) => {
                      const typeLabel = FIELD_TYPE_DEFINITIONS.find((item) => item.type === field.type)?.label ?? field.type
                      return (
                        <Box
                          key={field.id}
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
                                <Typography sx={{ fontWeight: 600 }}>{field.label}</Typography>
                                {field.required ? (
                                  <Chip size="small" label="Required" sx={getStatusBadgeStyle('SUSPENDED')} />
                                ) : null}
                              </Stack>
                              <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                                {field.name} · {typeLabel}
                              </Typography>
                              {field.helpText ? (
                                <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mt: 0.75 }}>
                                  {field.helpText}
                                </Typography>
                              ) : null}
                            </Box>
                            <Chip size="small" label={typeLabel} sx={getStatusBadgeStyle('Submitted')} />
                          </Stack>
                        </Box>
                      )
                    })}
                  </Stack>
                )}
              </Box>
            </PortalPanel>

            <PortalPanel title="Live preview">
              <Box sx={{ p: 2.5 }}>
                <FormPreviewPanel fields={fields} />
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
                            {formatFormDate(version.createdAt)}
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
                <PortalStatCard label="Fields" value={fields.length} />
              </Grid>
              <Grid size={{ xs: 6 }}>
                <PortalStatCard label="Version" value={template.versionNumber} />
              </Grid>
            </Grid>

            <PortalPanel title="Actions">
              <Stack spacing={1.25} sx={{ p: 2.5, pt: 0 }}>
                <Tooltip
                  title={getFormActivateTooltip({
                    isActive: template.isActive,
                    fieldCount: fields.length,
                    hasPublishedVersion,
                  })}
                >
                  <span>
                    <Button
                      variant={template.isActive ? 'outlined' : 'contained'}
                      fullWidth
                      startIcon={template.isActive ? <ToggleOffOutlinedIcon /> : <ToggleOnOutlinedIcon />}
                      sx={template.isActive ? portalOutlinedButtonSx : portalPrimaryButtonSx}
                      disabled={togglingActive || (!template.isActive && fields.length === 0)}
                      onClick={() => void handleSetActive(!template.isActive)}
                    >
                      {template.isActive ? 'Deactivate' : 'Activate'}
                    </Button>
                  </span>
                </Tooltip>
                <Button
                  component={RouterLink}
                  to={`/admin/forms/${uuid}/edit`}
                  variant="contained"
                  fullWidth
                  startIcon={<EditOutlinedIcon />}
                  sx={portalPrimaryButtonSx}
                >
                  Edit fields
                </Button>
                <Button
                  variant="outlined"
                  fullWidth
                  startIcon={<PreviewOutlinedIcon />}
                  sx={portalOutlinedButtonSx}
                  onClick={() => {
                    document.getElementById('form-live-preview')?.scrollIntoView({ behavior: 'smooth' })
                  }}
                >
                  Jump to preview
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
                <Tooltip title={isLive ? FORM_DELETE_LIVE_TOOLTIP : 'Delete this template'}>
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

      <Box id="form-live-preview" />

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

      <AdminFormDeleteDialog
        open={deleteOpen}
        templateName={template.name}
        submissionCount={template.submissionCount ?? 0}
        deleting={deleting}
        onClose={() => setDeleteOpen(false)}
        onConfirm={handleDelete}
      />
    </Box>
  )
}
