import AddIcon from '@mui/icons-material/Add'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import ContentCopyOutlinedIcon from '@mui/icons-material/ContentCopyOutlined'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import ToggleOffOutlinedIcon from '@mui/icons-material/ToggleOffOutlined'
import ToggleOnOutlinedIcon from '@mui/icons-material/ToggleOnOutlined'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tab,
  Tabs,
  TableCell,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { FormEvent, useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useAppSelector } from '../../../app/hooks'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  CERTIFICATE_DELETE_LIVE_TOOLTIP,
  downloadAdminCertificateExport,
  formatCertificateDate,
  getCertificateActivateTooltip,
  isCertificateTemplateLive,
} from '../adminCertificateUtils'
import { adminFormActionIconSx, adminFormDeleteIconSx } from '../adminFormUtils'
import { AdminCertificateDeleteDialog } from '../components/AdminCertificateDeleteDialog'
import { getAdminApiErrorMessage } from '../components/adminAgencyUtils'
import {
  CERTIFICATE_PROCESS_TYPES,
  useCloneAdminCertificateTemplateMutation,
  useDeleteAdminCertificateTemplateMutation,
  useGetAdminAgenciesQuery,
  useGetAdminCertificateTemplatesQuery,
  useImportAdminCertificateTemplateMutation,
  useSaveAdminCertificateTemplateMutation,
  useSetAdminCertificateTemplateActiveMutation,
  type CertificateTemplateListItem,
} from '../api/adminApi'

import { DEFAULT_LAYOUT } from '../components/certificateBuilder/certificateBuilderUtils'

function formatProcessType(type: string) {
  if (type === 'ImportEntry') return 'Import Entry'
  if (type === 'ExportEntry') return 'Export Entry'
  return type.replace(/([A-Z])/g, ' $1').trim()
}

export function AdminCertificateTemplatesPage() {
  const navigate = useNavigate()
  const accessToken = useAppSelector((state) => state.auth.accessToken)
  const { data, isLoading, isError } = useGetAdminCertificateTemplatesQuery()
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [saveTemplate, { isLoading: saving }] = useSaveAdminCertificateTemplateMutation()
  const [cloneTemplate, { isLoading: cloning }] = useCloneAdminCertificateTemplateMutation()
  const [importTemplate, { isLoading: importing }] = useImportAdminCertificateTemplateMutation()
  const [setTemplateActive] = useSetAdminCertificateTemplateActiveMutation()
  const [deleteTemplate, { isLoading: deleting }] = useDeleteAdminCertificateTemplateMutation()

  const [open, setOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)
  const [importError, setImportError] = useState('')
  const [cloneTarget, setCloneTarget] = useState<CertificateTemplateListItem | null>(null)
  const [cloneName, setCloneName] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<CertificateTemplateListItem | null>(null)
  const [exportingUuid, setExportingUuid] = useState<string | null>(null)
  const [togglingUuid, setTogglingUuid] = useState<string | null>(null)
  const [actionError, setActionError] = useState('')
  const [tab, setTab] = useState<'ALL' | (typeof CERTIFICATE_PROCESS_TYPES)[number]>('ALL')
  const [statusFilter, setStatusFilter] = useState('')
  const [form, setForm] = useState({
    name: '',
    description: '',
    agencyId: '' as string | number,
    processTypes: ['ImportEntry'] as string[],
  })

  const templates = data?.data ?? []
  const agencies = agenciesData?.data ?? []

  const filteredTemplates = useMemo(() => {
    let result = templates
    if (tab !== 'ALL') {
      result = result.filter((item) => item.processTypes.includes(tab))
    }
    if (statusFilter === '1') {
      result = result.filter((item) => item.isActive)
    } else if (statusFilter === '0') {
      result = result.filter((item) => !item.isActive)
    }
    return result
  }, [templates, tab, statusFilter])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const result = await saveTemplate({
      name: form.name,
      description: form.description || undefined,
      agencyId: form.agencyId ? Number(form.agencyId) : undefined,
      elements: [],
      layoutJson: JSON.stringify(DEFAULT_LAYOUT),
      processTypes: form.processTypes,
      publish: true,
      isActive: true,
    }).unwrap()
    setOpen(false)
    navigate(`/admin/certificate-templates/${result.data.uuid}/edit`)
  }

  const handleExport = async (item: CertificateTemplateListItem) => {
    setExportingUuid(item.uuid)
    try {
      await downloadAdminCertificateExport(
        item.uuid,
        `${item.name.replace(/\s+/g, '-').toLowerCase()}.json`,
        accessToken,
      )
    } catch {
      window.alert('Unable to export this template. Restart the API if you recently updated the backend.')
    } finally {
      setExportingUuid(null)
    }
  }

  const handleClone = async () => {
    if (!cloneTarget || !cloneName.trim()) return
    const result = await cloneTemplate({ uuid: cloneTarget.uuid, name: cloneName.trim() }).unwrap()
    setCloneTarget(null)
    setCloneName('')
    navigate(`/admin/certificate-templates/${result.data.uuid}/edit`)
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    setActionError('')
    try {
      await deleteTemplate(deleteTarget.uuid).unwrap()
      setDeleteTarget(null)
    } catch (error) {
      setActionError(getAdminApiErrorMessage(error, `Unable to delete “${deleteTarget.name}”.`))
    }
  }

  const handleSetActive = async (item: CertificateTemplateListItem, isActive: boolean) => {
    setActionError('')
    setTogglingUuid(item.uuid)
    try {
      await setTemplateActive({ uuid: item.uuid, isActive }).unwrap()
    } catch (error) {
      const fallback = isActive
        ? `Unable to activate “${item.name}”.`
        : `Unable to deactivate “${item.name}”.`
      setActionError(getAdminApiErrorMessage(error, fallback))
    } finally {
      setTogglingUuid(null)
    }
  }

  const handleImport = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setImportError('')
    const fileInput = e.currentTarget.elements.namedItem('importFile') as HTMLInputElement
    const file = fileInput.files?.[0]
    if (!file) {
      setImportError('Select a JSON file to import.')
      return
    }

    try {
      const text = await file.text()
      const payload = JSON.parse(text) as Record<string, unknown>
      const result = await importTemplate(payload).unwrap()
      setImportOpen(false)
      navigate(`/admin/certificate-templates/${result.data.uuid}/edit`)
    } catch {
      setImportError('Import failed. Use a valid V3 export file or compatible V2 JSON export.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Certificate Templates"
        subtitle="Design certificate layouts for accreditation and entry approvals."
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button variant="contained" sx={portalPrimaryButtonSx} startIcon={<AddIcon />} onClick={() => setOpen(true)}>
              New template
            </Button>
            <Button variant="outlined" startIcon={<UploadFileOutlinedIcon />} sx={portalOutlinedButtonSx} onClick={() => setImportOpen(true)}>
              Import
            </Button>
            <Button component={RouterLink} to="/admin" variant="outlined" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
              Back
            </Button>
          </Stack>
        }
      />

      <Tabs
        value={tab}
        onChange={(_, value) => setTab(value)}
        sx={{ mb: 2, borderBottom: 1, borderColor: portalColors.border }}
        variant="scrollable"
        scrollButtons="auto"
      >
        <Tab value="ALL" label="All Templates" sx={{ textTransform: 'none', fontWeight: tab === 'ALL' ? 600 : 500 }} />
        {CERTIFICATE_PROCESS_TYPES.map((type) => (
          <Tab
            key={type}
            value={type}
            label={formatProcessType(type)}
            sx={{ textTransform: 'none', fontWeight: tab === type ? 600 : 500 }}
          />
        ))}
      </Tabs>

      <PortalPanel title="Filters">
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ p: 2.5, pt: 0 }}>
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel id="cert-status-filter">Status</InputLabel>
            <Select
              labelId="cert-status-filter"
              label="Status"
              value={statusFilter}
              onChange={(e) => setStatusFilter(String(e.target.value))}
            >
              <MenuItem value="">All statuses</MenuItem>
              <MenuItem value="1">Active</MenuItem>
              <MenuItem value="0">Inactive</MenuItem>
            </Select>
          </FormControl>
          <Typography sx={{ alignSelf: 'center', fontSize: '0.8125rem', color: portalColors.textMuted }}>
            {filteredTemplates.length} template{filteredTemplates.length === 1 ? '' : 's'}. Inactive templates are not used for certificate generation.
          </Typography>
        </Stack>
      </PortalPanel>

      {isError ? (
        <Alert severity="error" sx={{ mt: 2 }}>
          Unable to load certificate templates. Restart the API after backend updates, then refresh.
        </Alert>
      ) : null}

      {actionError ? (
        <Alert severity="error" sx={{ mt: 2 }} onClose={() => setActionError('')}>
          {actionError}
        </Alert>
      ) : null}

      <Box sx={{ mt: 2 }}>
        <PortalTablePanel
          title="Templates"
          columns={['Name', 'Process', 'Agency', 'Version', 'Elements', 'Status', 'Created', 'Actions']}
          isLoading={isLoading}
          isEmpty={!isLoading && filteredTemplates.length === 0}
          emptyMessage="No templates match your filters. Create one with New template or import a JSON export."
        >
          {filteredTemplates.map((item) => {
            const isLive = isCertificateTemplateLive(item)
            return (
              <TableRow key={item.uuid} hover>
                <TableCell>
                  <Typography sx={{ fontWeight: 600 }}>{item.name}</Typography>
                  <Stack direction="row" spacing={0.75} sx={{ mt: 0.75, flexWrap: 'wrap' }} useFlexGap>
                    <Chip
                      size="small"
                      label={item.isActive ? 'Active' : 'Inactive'}
                      sx={portalStatusChipSx(item.isActive ? 'Approved' : 'Draft')}
                    />
                    {isLive ? <Chip size="small" label="Live" sx={getStatusBadgeStyle('ACTIVE')} /> : null}
                  </Stack>
                </TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                    {item.processTypes.map((type) => (
                      <Chip key={type} size="small" label={formatProcessType(type)} sx={getStatusBadgeStyle('Submitted')} />
                    ))}
                  </Stack>
                </TableCell>
                <TableCell>
                  <Chip size="small" label={item.agencyCode ?? 'Global'} sx={getStatusBadgeStyle('Approved')} />
                </TableCell>
                <TableCell>v{item.latestVersion}</TableCell>
                <TableCell>{item.elementCount}</TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={item.isActive ? 'Active' : 'Inactive'}
                    sx={getStatusBadgeStyle(item.isActive ? 'ACTIVE' : 'PENDING')}
                  />
                </TableCell>
                <TableCell sx={{ color: portalColors.textMuted, whiteSpace: 'nowrap' }}>
                  {formatCertificateDate(item.createdAt)}
                </TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
                    <Tooltip title={getCertificateActivateTooltip(item)}>
                      <span>
                        <Button
                          size="small"
                          variant={item.isActive ? 'outlined' : 'contained'}
                          startIcon={item.isActive ? <ToggleOffOutlinedIcon /> : <ToggleOnOutlinedIcon />}
                          sx={item.isActive ? portalOutlinedButtonSx : portalPrimaryButtonSx}
                          disabled={togglingUuid === item.uuid || (!item.isActive && item.elementCount === 0)}
                          onClick={() => void handleSetActive(item, !item.isActive)}
                        >
                          {item.isActive ? 'Deactivate' : 'Activate'}
                        </Button>
                      </span>
                    </Tooltip>
                    <Tooltip title="View details">
                      <IconButton
                        component={RouterLink}
                        to={`/admin/certificate-templates/${item.uuid}`}
                        size="small"
                        sx={adminFormActionIconSx}
                      >
                        <VisibilityOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit builder">
                      <IconButton
                        component={RouterLink}
                        to={`/admin/certificate-templates/${item.uuid}/edit`}
                        size="small"
                        sx={adminFormActionIconSx}
                      >
                        <EditOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Export JSON">
                      <span>
                        <IconButton
                          size="small"
                          sx={adminFormActionIconSx}
                          disabled={exportingUuid === item.uuid}
                          onClick={() => void handleExport(item)}
                        >
                          <DownloadOutlinedIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                    <Tooltip title="Clone">
                      <IconButton
                        size="small"
                        sx={adminFormActionIconSx}
                        onClick={() => {
                          setCloneTarget(item)
                          setCloneName(`${item.name} (Copy)`)
                        }}
                      >
                        <ContentCopyOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title={isLive ? CERTIFICATE_DELETE_LIVE_TOOLTIP : 'Delete'}>
                      <span>
                        <IconButton
                          size="small"
                          sx={adminFormDeleteIconSx}
                          disabled={isLive}
                          onClick={() => setDeleteTarget(item)}
                        >
                          <DeleteOutlinedIcon fontSize="small" />
                        </IconButton>
                      </span>
                    </Tooltip>
                  </Stack>
                </TableCell>
              </TableRow>
            )
          })}
        </PortalTablePanel>
      </Box>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleSubmit}>
          <DialogTitle>New certificate template</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Template name"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                required
                fullWidth
                autoFocus
              />
              <TextField
                label="Description"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                fullWidth
                multiline
                rows={2}
              />
              <FormControl fullWidth>
                <InputLabel>Agency (optional)</InputLabel>
                <Select
                  label="Agency (optional)"
                  value={form.agencyId}
                  onChange={(e) => setForm({ ...form, agencyId: e.target.value as string | number })}
                >
                  <MenuItem value="">Global (all agencies)</MenuItem>
                  {agencies.map((agency) => (
                    <MenuItem key={agency.id} value={agency.id}>
                      {agency.code} — {agency.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <FormControl fullWidth>
                <InputLabel>Process types</InputLabel>
                <Select
                  multiple
                  label="Process types"
                  value={form.processTypes}
                  onChange={(e) => setForm({ ...form, processTypes: e.target.value as string[] })}
                  renderValue={(selected) => selected.map(formatProcessType).join(', ')}
                >
                  {CERTIFICATE_PROCESS_TYPES.map((type) => (
                    <MenuItem key={type} value={type}>
                      {formatProcessType(type)}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={saving || !form.name.trim() || form.processTypes.length === 0}>
              Create & open builder
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={importOpen} onClose={() => setImportOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleImport}>
          <DialogTitle>Import template</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <Alert severity="info">
                Upload a JSON export from AgriCheck V3, or a compatible V2 certificate template export. Imported templates start as inactive drafts.
              </Alert>
              <Button variant="outlined" component="label" sx={portalOutlinedButtonSx}>
                Choose JSON file
                <input hidden name="importFile" type="file" accept=".json,application/json" />
              </Button>
              {importError ? <Alert severity="error">{importError}</Alert> : null}
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setImportOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={importing}>
              Import
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={Boolean(cloneTarget)} onClose={() => setCloneTarget(null)} maxWidth="xs" fullWidth>
        <DialogTitle>Clone template</DialogTitle>
        <DialogContent>
          <Typography sx={{ mb: 2, color: portalColors.textMuted, fontSize: '0.875rem' }}>
            Create a copy of <strong>{cloneTarget?.name}</strong> including layout and elements.
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
          <Button onClick={() => setCloneTarget(null)}>Cancel</Button>
          <Button variant="contained" sx={portalPrimaryButtonSx} disabled={cloning || !cloneName.trim()} onClick={() => void handleClone()}>
            Clone
          </Button>
        </DialogActions>
      </Dialog>

      <AdminCertificateDeleteDialog
        open={Boolean(deleteTarget)}
        templateName={deleteTarget?.name ?? ''}
        deleting={deleting}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </Box>
  )
}
