import AddIcon from '@mui/icons-material/Add'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
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
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  CERTIFICATE_PROCESS_TYPES,
  useGetAdminAgenciesQuery,
  useGetAdminCertificateTemplatesQuery,
  useSaveAdminCertificateTemplateMutation,
} from '../api/adminApi'

import { DEFAULT_LAYOUT } from '../components/certificateBuilder/certificateBuilderUtils'

function formatProcessType(type: string) {
  if (type === 'ImportEntry') return 'Import Entry'
  if (type === 'ExportEntry') return 'Export Entry'
  return type.replace(/([A-Z])/g, ' $1').trim()
}

export function AdminCertificateTemplatesPage() {
  const navigate = useNavigate()
  const { data, isLoading } = useGetAdminCertificateTemplatesQuery()
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [saveTemplate, { isLoading: saving }] = useSaveAdminCertificateTemplateMutation()
  const [open, setOpen] = useState(false)
  const [tab, setTab] = useState<'ALL' | (typeof CERTIFICATE_PROCESS_TYPES)[number]>('ALL')
  const [form, setForm] = useState({
    name: '',
    description: '',
    agencyId: '' as string | number,
    processTypes: ['ImportEntry'] as string[],
  })

  const templates = data?.data ?? []
  const agencies = agenciesData?.data ?? []

  const filteredTemplates = useMemo(() => {
    if (tab === 'ALL') return templates
    return templates.filter((item) => item.processTypes.includes(tab))
  }, [templates, tab])

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
    navigate(`/admin/certificate-templates/${result.data.uuid}`)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Certificate Templates"
        subtitle="Design certificate layouts for accreditation and entry approvals — migrated from V2 certificate builder."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} startIcon={<AddIcon />} onClick={() => setOpen(true)}>
            New Template
          </Button>
        }
      />

      <Tabs
        value={tab}
        onChange={(_, value) => setTab(value)}
        sx={{ mb: 2, borderBottom: 1, borderColor: portalColors.border }}
      >
        <Tab value="ALL" label="All Templates" />
        {CERTIFICATE_PROCESS_TYPES.map((type) => (
          <Tab key={type} value={type} label={formatProcessType(type)} />
        ))}
      </Tabs>

      <PortalPanel title="Certificate templates">
        {isLoading && (
          <Typography sx={{ py: 4, textAlign: 'center', color: portalColors.textMuted }}>Loading…</Typography>
        )}
        {!isLoading && filteredTemplates.length === 0 && (
          <Typography sx={{ py: 4, textAlign: 'center', color: portalColors.textMuted }}>
            No certificate templates for this filter.
          </Typography>
        )}
        <Stack spacing={0} divider={<Box sx={{ borderBottom: `1px solid ${portalColors.border}` }} />}>
          {filteredTemplates.map((item) => (
            <Box
              key={item.uuid}
              sx={{
                px: 2.5,
                py: 2,
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', md: 'minmax(0, 1fr) auto' },
                gap: 2,
                alignItems: 'center',
              }}
            >
              <Box>
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', alignItems: 'center', mb: 0.5 }}>
                  <Typography sx={{ fontWeight: 600, color: portalColors.textDark }}>{item.name}</Typography>
                  <Chip
                    size="small"
                    label={item.isActive ? 'Active' : 'Inactive'}
                    sx={portalStatusChipSx(item.isActive ? 'Approved' : 'Draft')}
                  />
                </Stack>
                {item.description ? (
                  <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 1 }}>{item.description}</Typography>
                ) : null}
                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                  <Chip size="small" variant="outlined" label={item.agencyCode ?? 'Global'} />
                  <Chip size="small" variant="outlined" label={`v${item.latestVersion}`} />
                  <Chip size="small" variant="outlined" label={`${item.elementCount} elements`} />
                  {item.processTypes.map((type) => (
                    <Chip key={type} size="small" label={formatProcessType(type)} sx={portalStatusChipSx('Submitted')} />
                  ))}
                </Stack>
              </Box>
              <Button
                component={RouterLink}
                to={`/admin/certificate-templates/${item.uuid}`}
                variant="outlined"
                startIcon={<EditOutlinedIcon />}
                sx={{ justifySelf: { md: 'end' } }}
              >
                Open
              </Button>
            </Box>
          ))}
        </Stack>
      </PortalPanel>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleSubmit}>
          <DialogTitle>Create Certificate Template</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <Alert severity="info">
                New templates start with a basic layout. Open the template after creation to review elements imported from V2 or customize further.
              </Alert>
              <TextField label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required fullWidth />
              <TextField label="Description" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} fullWidth multiline rows={2} />
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
                <InputLabel>Process Types</InputLabel>
                <Select
                  multiple
                  label="Process Types"
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
            <Button type="submit" variant="contained" disabled={saving || form.processTypes.length === 0}>
              Create Template
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
