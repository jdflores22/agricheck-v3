import AddIcon from '@mui/icons-material/Add'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import ArrowDropDownIcon from '@mui/icons-material/ArrowDropDown'
import ContentCopyOutlinedIcon from '@mui/icons-material/ContentCopyOutlined'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
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
  Menu,
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
import { Link as RouterLink, useNavigate, useSearchParams } from 'react-router-dom'
import { useAppSelector } from '../../../app/hooks'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  AdminFormDeleteDialog,
  FORM_DELETE_LIVE_TOOLTIP,
  isFormTemplateLive,
} from '../components/AdminFormDeleteDialog'
import {
  adminFormActionIconSx,
  adminFormDeleteIconSx,
  downloadAdminFormExport,
  formatFormDate,
  FORM_TYPE_TABS,
  getFormTypeLabel,
  parseFormTypeTab,
} from '../adminFormUtils'
import {
  useCloneAdminFormMutation,
  useDeleteAdminFormMutation,
  useGetAdminAgenciesQuery,
  useGetAdminFormsQuery,
  useImportAdminFormMutation,
  useSaveAdminFormMutation,
  type AdminAgency,
  type FormTemplateListItem,
} from '../api/adminApi'
import { FORM_TYPES, type FormType } from '../../forms/formSchema'

function resolveFormAgencies(agencyIds: number[], agencies: AdminAgency[]) {
  return agencyIds
    .map((id) => agencies.find((agency) => agency.id === id))
    .filter((agency): agency is AdminAgency => Boolean(agency))
}

export function AdminFormsPage() {
  const navigate = useNavigate()
  const accessToken = useAppSelector((state) => state.auth.accessToken)
  const [searchParams, setSearchParams] = useSearchParams()
  const { data, isLoading, isError } = useGetAdminFormsQuery()
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [saveForm, { isLoading: saving }] = useSaveAdminFormMutation()
  const [cloneForm, { isLoading: cloning }] = useCloneAdminFormMutation()
  const [deleteForm, { isLoading: deleting }] = useDeleteAdminFormMutation()
  const [importForm, { isLoading: importing }] = useImportAdminFormMutation()

  const tab = parseFormTypeTab(searchParams.get('formType'))
  const statusFilter = searchParams.get('active') ?? ''

  const [createMenuAnchor, setCreateMenuAnchor] = useState<null | HTMLElement>(null)
  const [createOpen, setCreateOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)
  const [importError, setImportError] = useState('')
  const [cloneTarget, setCloneTarget] = useState<FormTemplateListItem | null>(null)
  const [cloneName, setCloneName] = useState('')
  const [deleteTarget, setDeleteTarget] = useState<FormTemplateListItem | null>(null)
  const [exportingUuid, setExportingUuid] = useState<string | null>(null)

  const [form, setForm] = useState({
    name: '',
    formType: 'ENTRY' as FormType,
    agencyIds: [] as number[],
    publish: false,
  })

  const forms = data?.data ?? []
  const agencies = agenciesData?.data ?? []

  const filteredForms = useMemo(() => {
    let result = forms
    if (tab !== 'ALL') {
      result = result.filter((item) => item.formType === tab)
    }
    if (statusFilter === '1') {
      result = result.filter((item) => item.isActive)
    } else if (statusFilter === '0') {
      result = result.filter((item) => !item.isActive)
    }
    return result
  }, [forms, tab, statusFilter])

  const handleTabChange = (_: unknown, value: string) => {
    const next = new URLSearchParams(searchParams)
    if (value === 'ALL') next.delete('formType')
    else next.set('formType', value)
    setSearchParams(next)
  }

  const handleStatusFilter = (value: string) => {
    const next = new URLSearchParams(searchParams)
    if (value) next.set('active', value)
    else next.delete('active')
    setSearchParams(next)
  }

  const openCreate = (formType: FormType) => {
    setCreateMenuAnchor(null)
    setForm({ name: '', formType, agencyIds: [], publish: false })
    setCreateOpen(true)
  }

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault()
    const result = await saveForm({
      name: form.name,
      formType: form.formType,
      schemaJson: '[]',
      agencyIds: form.agencyIds,
      publish: form.publish,
    }).unwrap()
    setCreateOpen(false)
    navigate(`/admin/forms/${result.data.uuid}/edit`)
  }

  const handleExport = async (item: FormTemplateListItem) => {
    setExportingUuid(item.uuid)
    try {
      await downloadAdminFormExport(item.uuid, `${item.name.replace(/\s+/g, '-').toLowerCase()}.json`, accessToken)
    } catch {
      window.alert('Unable to export this template. Restart the API if you recently updated the backend.')
    } finally {
      setExportingUuid(null)
    }
  }

  const handleClone = async () => {
    if (!cloneTarget || !cloneName.trim()) return
    const result = await cloneForm({ uuid: cloneTarget.uuid, name: cloneName.trim() }).unwrap()
    setCloneTarget(null)
    setCloneName('')
    navigate(`/admin/forms/${result.data.uuid}/edit`)
  }

  const handleDelete = async () => {
    if (!deleteTarget) return
    await deleteForm(deleteTarget.uuid).unwrap()
    setDeleteTarget(null)
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
      const result = await importForm(payload).unwrap()
      setImportOpen(false)
      navigate(`/admin/forms/${result.data.uuid}/edit`)
    } catch {
      setImportError('Import failed. Use a valid V3 export file or compatible V2 JSON export.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Templates"
        title="Form Builder"
        subtitle="Design and publish dynamic forms for accreditation, entries, containers, inspections, and MAV."
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              endIcon={<ArrowDropDownIcon />}
              sx={portalPrimaryButtonSx}
              onClick={(e) => setCreateMenuAnchor(e.currentTarget)}
            >
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

      <Menu anchorEl={createMenuAnchor} open={Boolean(createMenuAnchor)} onClose={() => setCreateMenuAnchor(null)}>
        {FORM_TYPES.map((type) => (
          <MenuItem key={type} onClick={() => openCreate(type)}>
            {getFormTypeLabel(type)} form
          </MenuItem>
        ))}
      </Menu>

      <Tabs
        value={tab}
        onChange={handleTabChange}
        sx={{ mb: 2, borderBottom: `1px solid ${portalColors.border}` }}
        variant="scrollable"
        scrollButtons="auto"
      >
        {FORM_TYPE_TABS.map(({ value, label }) => (
          <Tab key={value} value={value} label={label} sx={{ textTransform: 'none', fontWeight: tab === value ? 600 : 500 }} />
        ))}
      </Tabs>

      <PortalPanel title="Filters">
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ p: 2.5, pt: 0 }}>
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel id="form-status-filter">Status</InputLabel>
            <Select
              labelId="form-status-filter"
              label="Status"
              value={statusFilter}
              onChange={(e) => handleStatusFilter(String(e.target.value))}
            >
              <MenuItem value="">All statuses</MenuItem>
              <MenuItem value="1">Active</MenuItem>
              <MenuItem value="0">Inactive</MenuItem>
            </Select>
          </FormControl>
          <Typography sx={{ alignSelf: 'center', fontSize: '0.8125rem', color: portalColors.textMuted }}>
            {filteredForms.length} template{filteredForms.length === 1 ? '' : 's'}
          </Typography>
        </Stack>
      </PortalPanel>

      {isError ? (
        <Alert severity="error" sx={{ mt: 2 }}>
          Unable to load form templates. Restart the API after backend updates, then refresh.
        </Alert>
      ) : null}

      <Box sx={{ mt: 2 }}>
        <PortalTablePanel
          title="Templates"
          columns={['Name', 'Type', 'Agencies', 'Version', 'Fields', 'Status', 'Created', 'Actions']}
          isLoading={isLoading}
          isEmpty={!isLoading && filteredForms.length === 0}
          emptyMessage="No templates match your filters. Create one with New template."
        >
          {filteredForms.map((item) => {
            const taggedAgencies = resolveFormAgencies(item.agencyIds ?? [], agencies)
            const isLive = isFormTemplateLive(item)
            return (
              <TableRow key={item.uuid} hover>
                <TableCell>
                  <Typography sx={{ fontWeight: 600 }}>{item.name}</Typography>
                  <Stack direction="row" spacing={0.75} sx={{ mt: 0.75, flexWrap: 'wrap' }} useFlexGap>
                    <Chip size="small" label={item.status} sx={portalStatusChipSx(item.status)} />
                    {item.hasPublishedVersion ? (
                      <Chip size="small" label="Live" sx={getStatusBadgeStyle('ACTIVE')} />
                    ) : null}
                  </Stack>
                </TableCell>
                <TableCell>
                  <Chip size="small" label={getFormTypeLabel(item.formType)} sx={getStatusBadgeStyle('Submitted')} />
                </TableCell>
                <TableCell sx={{ maxWidth: 220 }}>
                  {item.formType === 'MAV' ? (
                    <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>—</Typography>
                  ) : taggedAgencies.length > 0 ? (
                    <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                      {taggedAgencies.map((agency) => (
                        <Chip key={agency.id} size="small" label={agency.code} sx={getStatusBadgeStyle('Approved')} />
                      ))}
                    </Stack>
                  ) : (
                    <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>None</Typography>
                  )}
                </TableCell>
                <TableCell>v{item.latestVersion}</TableCell>
                <TableCell>{item.fieldCount ?? 0}</TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={item.isActive ? 'Active' : 'Inactive'}
                    sx={getStatusBadgeStyle(item.isActive ? 'ACTIVE' : 'PENDING')}
                  />
                </TableCell>
                <TableCell sx={{ color: portalColors.textMuted, whiteSpace: 'nowrap' }}>
                  {formatFormDate(item.createdAt)}
                </TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5}>
                    <Tooltip title="View details">
                      <IconButton component={RouterLink} to={`/admin/forms/${item.uuid}/view`} size="small" sx={adminFormActionIconSx}>
                        <VisibilityOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Edit builder">
                      <IconButton component={RouterLink} to={`/admin/forms/${item.uuid}/edit`} size="small" sx={adminFormActionIconSx}>
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
                    <Tooltip title={isLive ? FORM_DELETE_LIVE_TOOLTIP : 'Delete'}>
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

      <Dialog open={createOpen} onClose={() => setCreateOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleCreate}>
          <DialogTitle>New {getFormTypeLabel(form.formType)} template</DialogTitle>
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
                select
                label="Form type"
                value={form.formType}
                onChange={(e) => setForm({ ...form, formType: e.target.value as FormType })}
                fullWidth
              >
                {FORM_TYPES.map((type) => (
                  <MenuItem key={type} value={type}>
                    {getFormTypeLabel(type)}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                select
                label="Agencies"
                value={form.agencyIds}
                fullWidth
                slotProps={{
                  select: {
                    multiple: true,
                    renderValue: (selected: unknown) =>
                      (selected as number[])
                        .map((id) => agencies.find((agency) => agency.id === id)?.code ?? id)
                        .join(', ') || 'Optional',
                  },
                }}
                onChange={(e) => {
                  const value = e.target.value
                  setForm({
                    ...form,
                    agencyIds: typeof value === 'string' ? value.split(',').map(Number) : (value as number[]),
                  })
                }}
              >
                {agencies.map((agency) => (
                  <MenuItem key={agency.id} value={agency.id}>
                    {agency.code} — {agency.name}
                  </MenuItem>
                ))}
              </TextField>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setCreateOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={saving || !form.name.trim()}>
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
                Upload a JSON export from AgriCheck V3, or a compatible V2 form export. Imported templates start as inactive drafts.
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
            Create a copy of <strong>{cloneTarget?.name}</strong>.
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

      <AdminFormDeleteDialog
        open={Boolean(deleteTarget)}
        templateName={deleteTarget?.name ?? ''}
        submissionCount={deleteTarget?.submissionCount ?? 0}
        deleting={deleting}
        onClose={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </Box>
  )
}
