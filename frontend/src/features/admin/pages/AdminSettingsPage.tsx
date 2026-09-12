import ArticleOutlinedIcon from '@mui/icons-material/ArticleOutlined'
import BuildOutlinedIcon from '@mui/icons-material/BuildOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import CategoryOutlinedIcon from '@mui/icons-material/CategoryOutlined'
import DescriptionOutlinedIcon from '@mui/icons-material/DescriptionOutlined'
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import InsertDriveFileOutlinedIcon from '@mui/icons-material/InsertDriveFileOutlined'
import NotificationsOutlinedIcon from '@mui/icons-material/NotificationsOutlined'
import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined'
import PaletteOutlinedIcon from '@mui/icons-material/PaletteOutlined'
import ShieldOutlinedIcon from '@mui/icons-material/ShieldOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  FormControl,
  FormControlLabel,
  InputLabel,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  MenuItem,
  Select,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'
import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  adminSettingLabels,
  adminSettingsGroups,
  type AdminSettingsTab,
  useGetAdminSettingsQuery,
  useUpdateAdminSettingsMutation,
  useUploadBrandingAssetMutation,
} from '../api/adminApi'
import { resolveBrandingAssetUrl } from '../../system/systemBrandingApi'
import { useSystemBranding } from '../../system/SystemBrandingProvider'

const tabMeta: { id: AdminSettingsTab; label: string; icon: ReactNode }[] = [
  { id: 'general', label: 'General', icon: <InfoOutlinedIcon fontSize="small" /> },
  { id: 'branding', label: 'Branding & Appearance', icon: <PaletteOutlinedIcon fontSize="small" /> },
  { id: 'email', label: 'Email Configuration', icon: <EmailOutlinedIcon fontSize="small" /> },
  { id: 'files', label: 'File Uploads', icon: <InsertDriveFileOutlinedIcon fontSize="small" /> },
  { id: 'security', label: 'Security', icon: <ShieldOutlinedIcon fontSize="small" /> },
  { id: 'notifications', label: 'Notifications', icon: <NotificationsOutlinedIcon fontSize="small" /> },
  { id: 'forms', label: 'Forms', icon: <ArticleOutlinedIcon fontSize="small" /> },
  { id: 'accreditation', label: 'Accreditation', icon: <VerifiedOutlinedIcon fontSize="small" /> },
  { id: 'maintenance', label: 'Maintenance', icon: <BuildOutlinedIcon fontSize="small" /> },
]

const quickLinks = [
  { label: 'Form Builder', to: '/admin/forms', icon: <ArticleOutlinedIcon fontSize="small" /> },
  { label: 'Certificate Templates', to: '/admin/certificate-templates', icon: <DescriptionOutlinedIcon fontSize="small" /> },
  { label: 'Agencies', to: '/admin/agencies', icon: <BusinessOutlinedIcon fontSize="small" /> },
  { label: 'Commodities', to: '/admin/commodities', icon: <CategoryOutlinedIcon fontSize="small" /> },
]

const booleanKeys = new Set([
  'require_uppercase',
  'require_numbers',
  'require_special_chars',
  'enable_email_notifications',
  'notify_on_submission',
  'notify_on_approval',
  'enable_inapp_notifications',
  'maintenance_mode',
])

const multilineKeys = new Set(['system_description', 'footer_text', 'maintenance_message'])

const fullWidthKeys = new Set([
  'system_name',
  'system_description',
  'footer_text',
  'maintenance_message',
  'maintenance_allowed_ips',
  'allowed_file_types',
  'system_logo_path',
  'spinner_logo_path',
  'favicon_path',
])

const readOnlyKeys = new Set(['system_logo_path', 'spinner_logo_path', 'favicon_path'])

function SettingsNavButton({
  label,
  icon,
  selected,
  onClick,
}: {
  label: string
  icon: ReactNode
  selected: boolean
  onClick: () => void
}) {
  return (
    <ListItemButton
      selected={selected}
      onClick={onClick}
      sx={{
        borderRadius: '0.5rem',
        mb: 0.5,
        px: 1.5,
        py: 1.25,
        borderLeft: '4px solid',
        borderColor: selected ? portalColors.primary : 'transparent',
        bgcolor: selected ? portalColors.successSoft : 'transparent',
        color: selected ? portalColors.primary : portalColors.textMuted,
        '&:hover': {
          bgcolor: selected ? portalColors.successSoft : portalColors.bgMuted,
          color: selected ? portalColors.primary : portalColors.textDark,
        },
        '&.Mui-selected': {
          bgcolor: portalColors.successSoft,
          color: portalColors.primary,
          '&:hover': { bgcolor: portalColors.successSoft },
        },
      }}
    >
      <ListItemIcon sx={{ minWidth: 32, color: 'inherit' }}>{icon}</ListItemIcon>
      <ListItemText
        primary={label}
        slotProps={{ primary: { sx: { fontSize: '0.875rem', fontWeight: selected ? 600 : 500 } } }}
      />
    </ListItemButton>
  )
}

export function AdminSettingsPage() {
  const { data, isLoading } = useGetAdminSettingsQuery()
  const [updateSettings, { isLoading: saving, isSuccess, error }] = useUpdateAdminSettingsMutation()
  const [uploadBrandingAsset, { isLoading: uploadingAsset }] = useUploadBrandingAssetMutation()
  const { spinnerColor, spinnerLogoUrl } = useSystemBranding()
  const [activeTab, setActiveTab] = useState<AdminSettingsTab>('general')
  const [draft, setDraft] = useState<Record<string, string>>({})

  useEffect(() => {
    if (data?.data?.settings) {
      setDraft(data.data.settings)
    }
  }, [data])

  const activeKeys = useMemo(() => adminSettingsGroups[activeTab], [activeTab])
  const activeTabMeta = tabMeta.find((t) => t.id === activeTab)
  const textFields = activeKeys.filter((key) => !booleanKeys.has(key) && !readOnlyKeys.has(key))
  const readOnlyFields = activeTab === 'branding' ? activeKeys.filter((key) => readOnlyKeys.has(key)) : []
  const toggleFields = activeKeys.filter((key) => booleanKeys.has(key))

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const settings: Record<string, string> = {}
    for (const key of activeKeys) {
      settings[key] = draft[key] ?? ''
    }
    await updateSettings({ settings }).unwrap()
  }

  const setField = (key: string, value: string) => {
    setDraft((prev) => ({ ...prev, [key]: value }))
  }

  const resetTab = () => {
    if (data?.data?.settings) {
      setDraft(data.data.settings)
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="System"
        title="System Settings"
        subtitle="Configure system-wide settings and preferences."
        action={{ label: 'Back to Dashboard', to: '/admin' }}
      />

      <Box
        sx={{
          borderRadius: '0.75rem',
          border: `1px solid ${portalColors.border}`,
          bgcolor: portalColors.bgWhite,
          overflow: 'hidden',
        }}
      >
        <Box
          sx={{
            display: { xs: 'block', lg: 'grid' },
            gridTemplateColumns: { lg: '280px minmax(0, 1fr)' },
            minHeight: { lg: 560 },
          }}
        >
          {/* Sidebar */}
          <Box
            sx={{
              borderBottom: { xs: `1px solid ${portalColors.border}`, lg: 'none' },
              borderRight: { lg: `1px solid ${portalColors.border}` },
              bgcolor: portalColors.bgWhite,
              p: { xs: 2, lg: 2 },
              position: { lg: 'sticky' },
              top: { lg: 16 },
              alignSelf: { lg: 'start' },
            }}
          >
            <Typography
              sx={{
                display: { xs: 'none', lg: 'block' },
                px: 1.5,
                mb: 1,
                fontSize: '0.75rem',
                fontWeight: 600,
                letterSpacing: '0.08em',
                textTransform: 'uppercase',
                color: portalColors.textMuted,
              }}
            >
              Sections
            </Typography>

            <FormControl fullWidth sx={{ display: { xs: 'block', lg: 'none' }, mb: 2 }}>
              <InputLabel>Settings section</InputLabel>
              <Select
                label="Settings section"
                value={activeTab}
                onChange={(e) => setActiveTab(e.target.value as AdminSettingsTab)}
              >
                {tabMeta.map((tab) => (
                  <MenuItem key={tab.id} value={tab.id}>
                    {tab.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <List disablePadding sx={{ display: { xs: 'none', lg: 'block' } }}>
              {tabMeta.map((tab) => (
                <SettingsNavButton
                  key={tab.id}
                  label={tab.label}
                  icon={tab.icon}
                  selected={activeTab === tab.id}
                  onClick={() => setActiveTab(tab.id)}
                />
              ))}
            </List>

            <Divider sx={{ my: 2 }} />

            <Typography
              sx={{
                px: 1.5,
                mb: 1,
                fontSize: '0.75rem',
                fontWeight: 600,
                letterSpacing: '0.08em',
                textTransform: 'uppercase',
                color: portalColors.textMuted,
              }}
            >
              Quick links
            </Typography>
            <Stack spacing={0.5}>
              {quickLinks.map((link) => (
                <Button
                  key={link.to}
                  component={RouterLink}
                  to={link.to}
                  variant="text"
                  startIcon={link.icon}
                  endIcon={<OpenInNewOutlinedIcon sx={{ fontSize: 14 }} />}
                  sx={{
                    justifyContent: 'flex-start',
                    px: 1.5,
                    py: 1,
                    color: portalColors.textMuted,
                    textTransform: 'none',
                    fontWeight: 500,
                    fontSize: '0.875rem',
                    '&:hover': { bgcolor: portalColors.bgMuted, color: portalColors.primary },
                  }}
                >
                  {link.label}
                </Button>
              ))}
            </Stack>
          </Box>

          {/* Content */}
          <Box sx={{ display: 'flex', flexDirection: 'column', minWidth: 0 }}>
            <Box
              sx={{
                px: { xs: 2, md: 3 },
                py: 2,
                borderBottom: `1px solid ${portalColors.border}`,
                bgcolor: portalColors.bgMuted,
              }}
            >
              <Typography sx={{ fontSize: '1rem', fontWeight: 600, color: portalColors.textDark }}>
                {activeTabMeta?.label ?? 'Settings'}
              </Typography>
              <Typography sx={{ mt: 0.5, fontSize: '0.875rem', color: portalColors.textMuted }}>
                Update values for this section, then save your changes.
              </Typography>
            </Box>

            <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', flex: 1 }}>
              <Box sx={{ px: { xs: 2, md: 3 }, py: { xs: 2.5, md: 3 }, flex: 1 }}>
                {isLoading ? (
                  <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
                    <CircularProgress size={32} sx={{ color: portalColors.primary }} />
                  </Box>
                ) : (
                  <Stack spacing={3}>
                    {isSuccess && <Alert severity="success">Settings saved successfully.</Alert>}
                    {error && <Alert severity="error">Could not save settings.</Alert>}

                    {activeTab === 'branding' && (
                      <Box
                        sx={{
                          borderRadius: '0.75rem',
                          border: `1px solid ${portalColors.border}`,
                          bgcolor: portalColors.bgMuted,
                          p: { xs: 2, md: 2.5 },
                        }}
                      >
                        <Typography sx={{ mb: 2, fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
                          Brand assets
                        </Typography>
                        <Stack spacing={2}>
                          <BrandingUploadField
                            label="System Logo"
                            assetType="system-logo"
                            currentPath={draft.system_logo_path}
                            uploading={uploadingAsset}
                            onUpload={async (file) => {
                              const result = await uploadBrandingAsset({ assetType: 'system-logo', file }).unwrap()
                              if (result.data?.settings) setDraft(result.data.settings)
                            }}
                          />
                          <BrandingUploadField
                            label="Spinner Logo"
                            assetType="spinner-logo"
                            currentPath={draft.spinner_logo_path}
                            uploading={uploadingAsset}
                            onUpload={async (file) => {
                              const result = await uploadBrandingAsset({ assetType: 'spinner-logo', file }).unwrap()
                              if (result.data?.settings) setDraft(result.data.settings)
                            }}
                          />
                          <BrandingUploadField
                            label="Favicon"
                            assetType="favicon"
                            currentPath={draft.favicon_path}
                            uploading={uploadingAsset}
                            onUpload={async (file) => {
                              const result = await uploadBrandingAsset({ assetType: 'favicon', file }).unwrap()
                              if (result.data?.settings) setDraft(result.data.settings)
                            }}
                          />
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, pt: 1 }}>
                            <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
                              Spinner preview
                            </Typography>
                            <Box sx={{ position: 'relative', width: 72, height: 72 }}>
                              <CircularProgress size={72} thickness={3.5} sx={{ color: draft.spinner_color || spinnerColor }} />
                              {(resolveBrandingAssetUrl(draft.spinner_logo_path) ?? spinnerLogoUrl) ? (
                                <Box
                                  component="img"
                                  src={resolveBrandingAssetUrl(draft.spinner_logo_path) ?? spinnerLogoUrl ?? undefined}
                                  alt="Spinner logo preview"
                                  sx={{
                                    position: 'absolute',
                                    top: '50%',
                                    left: '50%',
                                    transform: 'translate(-50%, -50%)',
                                    maxWidth: 40,
                                    maxHeight: 40,
                                  }}
                                />
                              ) : null}
                            </Box>
                          </Box>
                        </Stack>
                      </Box>
                    )}

                    {readOnlyFields.length > 0 && (
                      <Stack spacing={2}>
                        {readOnlyFields.map((key) => (
                          <TextField
                            key={key}
                            label={adminSettingLabels[key] ?? key}
                            value={draft[key] ?? ''}
                            fullWidth
                            slotProps={{ input: { readOnly: true } }}
                          />
                        ))}
                      </Stack>
                    )}

                    {textFields.length > 0 && (
                      <Box
                        sx={{
                          display: 'grid',
                          gap: 2.5,
                          gridTemplateColumns: {
                            xs: '1fr',
                            md: 'repeat(2, minmax(0, 1fr))',
                          },
                        }}
                      >
                        {textFields.map((key) => {
                          const label = adminSettingLabels[key] ?? key
                          const isFullWidth = fullWidthKeys.has(key) || multilineKeys.has(key)

                          return (
                            <TextField
                              key={key}
                              label={label}
                              value={draft[key] ?? ''}
                              onChange={(e) => setField(key, e.target.value)}
                              fullWidth
                              multiline={multilineKeys.has(key)}
                              minRows={multilineKeys.has(key) ? 3 : undefined}
                              sx={{ gridColumn: isFullWidth ? '1 / -1' : undefined }}
                            />
                          )
                        })}
                      </Box>
                    )}

                    {toggleFields.length > 0 && (
                      <Box
                        sx={{
                          borderRadius: '0.75rem',
                          border: `1px solid ${portalColors.border}`,
                          bgcolor: portalColors.bgMuted,
                          p: { xs: 2, md: 2.5 },
                        }}
                      >
                        <Typography sx={{ mb: 2, fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
                          Toggle options
                        </Typography>
                        <Box
                          sx={{
                            display: 'grid',
                            gap: 1,
                            gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
                          }}
                        >
                          {toggleFields.map((key) => (
                            <FormControlLabel
                              key={key}
                              control={
                                <Switch
                                  checked={(draft[key] ?? '0') === '1'}
                                  onChange={(_, checked) => setField(key, checked ? '1' : '0')}
                                  color="success"
                                />
                              }
                              label={adminSettingLabels[key] ?? key}
                              sx={{
                                m: 0,
                                px: 1.5,
                                py: 1,
                                borderRadius: '0.5rem',
                                bgcolor: portalColors.bgWhite,
                                border: `1px solid ${portalColors.border}`,
                              }}
                            />
                          ))}
                        </Box>
                      </Box>
                    )}
                  </Stack>
                )}
              </Box>

              <Box
                sx={{
                  display: 'flex',
                  flexWrap: 'wrap',
                  gap: 1.5,
                  justifyContent: 'flex-end',
                  px: { xs: 2, md: 3 },
                  py: 2,
                  borderTop: `1px solid ${portalColors.border}`,
                  bgcolor: portalColors.bgMuted,
                }}
              >
                <Button type="button" variant="outlined" sx={portalOutlinedButtonSx} onClick={resetTab} disabled={isLoading || saving}>
                  Reset
                </Button>
                <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={saving || isLoading}>
                  {saving ? 'Saving…' : `Save ${activeTabMeta?.label ?? 'Settings'}`}
                </Button>
              </Box>
            </Box>
          </Box>
        </Box>
      </Box>
    </Box>
  )
}

function BrandingUploadField({
  label,
  currentPath,
  uploading,
  onUpload,
}: {
  label: string
  assetType: 'system-logo' | 'spinner-logo' | 'favicon'
  currentPath?: string
  uploading: boolean
  onUpload: (file: File) => Promise<void>
}) {
  const previewUrl = resolveBrandingAssetUrl(currentPath)

  return (
    <Box
      sx={{
        display: 'flex',
        flexWrap: 'wrap',
        gap: 2,
        alignItems: 'center',
        justifyContent: 'space-between',
        p: 1.5,
        borderRadius: '0.5rem',
        bgcolor: portalColors.bgWhite,
        border: `1px solid ${portalColors.border}`,
      }}
    >
      <Box>
        <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>{label}</Typography>
        <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
          {currentPath || 'No file uploaded yet'}
        </Typography>
      </Box>
      <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
        {previewUrl ? (
          <Box component="img" src={previewUrl} alt={`${label} preview`} sx={{ width: 48, height: 48, objectFit: 'contain' }} />
        ) : null}
        <Button variant="outlined" component="label" sx={portalOutlinedButtonSx} disabled={uploading}>
          {uploading ? 'Uploading…' : 'Upload'}
          <input
            hidden
            type="file"
            accept="image/png,image/jpeg,image/jpg,image/gif,image/svg+xml,image/webp,.ico"
            onChange={async (e) => {
              const file = e.target.files?.[0]
              if (!file) return
              await onUpload(file)
              e.target.value = ''
            }}
          />
        </Button>
      </Stack>
    </Box>
  )
}
