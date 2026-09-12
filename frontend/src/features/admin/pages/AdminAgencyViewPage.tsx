import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import InboxOutlinedIcon from '@mui/icons-material/InboxOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import LocationOnOutlinedIcon from '@mui/icons-material/LocationOnOutlined'
import PhoneOutlinedIcon from '@mui/icons-material/PhoneOutlined'
import ShowChartOutlinedIcon from '@mui/icons-material/ShowChartOutlined'
import StarHalfOutlinedIcon from '@mui/icons-material/StarHalfOutlined'
import StarOutlineOutlinedIcon from '@mui/icons-material/StarOutlineOutlined'
import UploadOutlinedIcon from '@mui/icons-material/UploadOutlined'
import PeopleOutlineOutlinedIcon from '@mui/icons-material/PeopleOutlineOutlined'
import WorkOutlineOutlinedIcon from '@mui/icons-material/WorkOutlineOutlined'
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import { formatRoleLabel, getRoleBadgeStyle, getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalOutlinedButtonSx, portalPrimaryButtonSx, portalTableHeadCellSx } from '../../../components/portal/portalStyles'
import { AdminSectionCard } from '../components/AdminSectionCard'
import { AgencyLeadershipDialog } from '../components/AgencyLeadershipDialog'
import {
  AGENCY_LOGO_ACCEPT,
  getAdminApiErrorMessage,
  resolveAgencyLogoUrl,
  validateAgencyLogoFile,
} from '../components/adminAgencyUtils'
import { useGetAdminAgencyQuery, useUploadAdminAgencyLogoMutation } from '../api/adminApi'

function DetailField({ label, icon, children }: { label: string; icon?: React.ReactNode; children: React.ReactNode }) {
  return (
    <Box>
      <Typography
        component="div"
        sx={{
          fontSize: '0.8125rem',
          fontWeight: 600,
          color: portalColors.textMuted,
          mb: 0.5,
          display: 'flex',
          alignItems: 'center',
          gap: 0.5,
        }}
      >
        {icon}
        {label}
      </Typography>
      <Box sx={{ fontSize: '0.9375rem', color: portalColors.textDark }}>{children}</Box>
    </Box>
  )
}

export function AdminAgencyViewPage() {
  const { id = '' } = useParams()
  const agencyId = Number(id)
  const { data, isLoading, error } = useGetAdminAgencyQuery(agencyId, { skip: !agencyId })
  const [uploadLogo, { isLoading: uploading }] = useUploadAdminAgencyLogoMutation()
  const [logoDialogOpen, setLogoDialogOpen] = useState(false)
  const [logoUploadError, setLogoUploadError] = useState<string | null>(null)
  const [leadershipDialog, setLeadershipDialog] = useState<'secretary' | 'undersecretary' | null>(null)
  const [logoLoadFailed, setLogoLoadFailed] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const agency = data?.data
  const logoUrl = resolveAgencyLogoUrl(agency?.logoUrl)
  const showLogo = Boolean(logoUrl) && !logoLoadFailed

  useEffect(() => {
    setLogoLoadFailed(false)
  }, [agency?.logoUrl, agencyId])

  const handleLogoUpload = async (file: File) => {
    const validationError = validateAgencyLogoFile(file)
    if (validationError) {
      setLogoUploadError(validationError)
      return
    }

    setLogoUploadError(null)
    try {
      await uploadLogo({ id: agencyId, file }).unwrap()
      setLogoLoadFailed(false)
      setLogoDialogOpen(false)
    } catch (error) {
      setLogoUploadError(getAdminApiErrorMessage(error, 'Unable to upload agency logo. Please try again.'))
    }
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  if (error || !agency) {
    return <Alert severity="error">Agency not found.</Alert>
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Agency Management"
        title={agency.name}
        subtitle={agency.description?.trim() || 'Agency management and information'}
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button
              component={RouterLink}
              to={`/admin/agencies/${agencyId}/edit`}
              variant="contained"
              startIcon={<EditOutlinedIcon />}
              sx={portalPrimaryButtonSx}
            >
              Edit Agency
            </Button>
            <Button component={RouterLink} to="/admin/agencies" variant="outlined" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
              Back to Agencies
            </Button>
          </Stack>
        }
      />

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 4 }}>
          <Stack spacing={3}>
            <AdminSectionCard title="Agency Information" icon={<InfoOutlinedIcon />} variant="green">
              <Box sx={{ textAlign: 'center', mb: 3 }}>
                {showLogo ? (
                  <Box
                    component="img"
                    src={logoUrl!}
                    alt={`${agency.name} logo`}
                    onError={() => setLogoLoadFailed(true)}
                    sx={{ maxHeight: 144, width: 'auto', mx: 'auto', borderRadius: '0.5rem', boxShadow: 1 }}
                  />
                ) : (
                  <Box sx={{ bgcolor: portalColors.bgMuted, borderRadius: '0.75rem', p: 4 }}>
                    <BusinessOutlinedIcon sx={{ fontSize: 40, color: portalColors.textMuted, mb: 1 }} />
                    <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>No logo uploaded</Typography>
                  </Box>
                )}
                <Button
                  variant="outlined"
                  size="small"
                  startIcon={<UploadOutlinedIcon />}
                  sx={{ ...portalOutlinedButtonSx, mt: 1.5 }}
                  onClick={() => setLogoDialogOpen(true)}
                >
                  {logoUrl && !logoLoadFailed ? 'Change' : 'Upload'} Logo
                </Button>
              </Box>

              <Stack spacing={2.5} sx={{ borderTop: `1px solid ${portalColors.border}`, pt: 2.5 }}>
                <DetailField label="Code">
                  <Chip size="small" label={agency.code} sx={getStatusBadgeStyle('Submitted')} />
                </DetailField>
                <DetailField label="Name">
                  <Box component="span" sx={{ fontWeight: 600 }}>
                    {agency.name}
                  </Box>
                </DetailField>
                <DetailField label="Description">{agency.description?.trim() || 'No description provided'}</DetailField>
              </Stack>

              <Stack spacing={2.5} sx={{ borderTop: `1px solid ${portalColors.border}`, pt: 2.5, mt: 2.5 }}>
                <DetailField label="Address" icon={<LocationOnOutlinedIcon sx={{ fontSize: 16 }} />}>
                  {agency.address?.trim() || 'Not provided'}
                </DetailField>
                <DetailField label="Contact Number" icon={<PhoneOutlinedIcon sx={{ fontSize: 16 }} />}>
                  {agency.contactNumber?.trim() || 'Not provided'}
                </DetailField>
                <DetailField label="Email" icon={<EmailOutlinedIcon sx={{ fontSize: 16 }} />}>
                  {agency.email ? (
                    <Box component="a" href={`mailto:${agency.email}`} sx={{ color: portalColors.primary, textDecoration: 'none' }}>
                      {agency.email}
                    </Box>
                  ) : (
                    'Not provided'
                  )}
                </DetailField>
              </Stack>

              <Box sx={{ borderTop: `1px solid ${portalColors.border}`, pt: 2.5, mt: 2.5 }}>
                <DetailField label="Agency ID">{agency.id}</DetailField>
              </Box>
            </AdminSectionCard>

            <AdminSectionCard title="Statistics" icon={<ShowChartOutlinedIcon />} variant="green">
              <Stack spacing={2}>
                <Box>
                  <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mb: 0.5 }}>Total Users</Typography>
                  <Typography sx={{ fontSize: '1.75rem', fontWeight: 700 }}>{agency.totalUsers}</Typography>
                </Box>
                <Box>
                  <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mb: 0.5 }}>Total Entries</Typography>
                  <Typography sx={{ fontSize: '1.75rem', fontWeight: 700 }}>{agency.totalEntries}</Typography>
                </Box>
              </Stack>
            </AdminSectionCard>
          </Stack>
        </Grid>

        <Grid size={{ xs: 12, lg: 8 }}>
          <Stack spacing={3}>
            <AdminSectionCard title="Leadership" icon={<WorkOutlineOutlinedIcon />} variant="green">
              <Grid container spacing={2}>
                <Grid size={{ xs: 12, md: 6 }}>
                  <Box sx={{ border: '1px solid #bbf7d0', borderRadius: '0.75rem', p: 2, height: '100%' }}>
                    <Typography sx={{ fontWeight: 600, color: portalColors.primary, mb: 2, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 0.75 }}>
                      <StarOutlineOutlinedIcon sx={{ fontSize: 18 }} />
                      Secretary
                    </Typography>
                    {agency.secretary ? (
                      <Box sx={{ textAlign: 'center', mb: 2 }}>
                        <Typography sx={{ fontWeight: 600 }}>{agency.secretary.fullName}</Typography>
                        <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>{agency.secretary.email}</Typography>
                        <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mt: 0.5 }}>
                          Assigned {new Date(agency.secretary.assignedAt).toLocaleDateString('en-PH')}
                        </Typography>
                      </Box>
                    ) : (
                      <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 2, textAlign: 'center' }}>
                        Not yet assigned
                      </Typography>
                    )}
                    <Button
                      variant="contained"
                      fullWidth
                      sx={portalPrimaryButtonSx}
                      onClick={() => setLeadershipDialog('secretary')}
                    >
                      {agency.secretary ? 'Change Secretary' : 'Assign Secretary'}
                    </Button>
                  </Box>
                </Grid>

                <Grid size={{ xs: 12, md: 6 }}>
                  <Box sx={{ border: '1px solid #bbf7d0', borderRadius: '0.75rem', p: 2, height: '100%' }}>
                    <Typography sx={{ fontWeight: 600, color: portalColors.primary, mb: 2, display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 0.75 }}>
                      <StarHalfOutlinedIcon sx={{ fontSize: 18 }} />
                      Undersecretaries
                    </Typography>
                    {agency.undersecretaries.length > 0 ? (
                      <Stack spacing={1.5} sx={{ mb: 2 }}>
                        {agency.undersecretaries.map((leader) => (
                          <Box key={leader.userUuid} sx={{ textAlign: 'center' }}>
                            <Typography sx={{ fontWeight: 600 }}>{leader.fullName}</Typography>
                            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>{leader.email}</Typography>
                          </Box>
                        ))}
                      </Stack>
                    ) : (
                      <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 2, textAlign: 'center' }}>
                        Not yet assigned
                      </Typography>
                    )}
                    <Button
                      variant="contained"
                      fullWidth
                      sx={portalPrimaryButtonSx}
                      onClick={() => setLeadershipDialog('undersecretary')}
                    >
                      {agency.undersecretaries.length > 0 ? 'Manage Undersecretaries' : 'Assign Undersecretaries'}
                    </Button>
                  </Box>
                </Grid>
              </Grid>
            </AdminSectionCard>

            <AdminSectionCard title="Assigned Users" icon={<PeopleOutlineOutlinedIcon />} variant="green">
              {agency.users.length === 0 ? (
                <Box sx={{ textAlign: 'center', py: 4 }}>
                  <InboxOutlinedIcon sx={{ fontSize: 40, color: portalColors.textMuted, mb: 1 }} />
                  <Typography sx={{ fontWeight: 600, mb: 0.5 }}>No users assigned</Typography>
                  <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                    No users assigned to this agency yet.
                  </Typography>
                </Box>
              ) : (
                <Box sx={{ overflowX: 'auto' }}>
                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        {['User', 'Email', 'Role', 'Assigned Date'].map((column) => (
                          <TableCell key={column} sx={portalTableHeadCellSx}>
                            {column}
                          </TableCell>
                        ))}
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {agency.users.map((user) => (
                        <TableRow key={user.userUuid} hover>
                          <TableCell>
                            <Typography sx={{ fontWeight: 600 }}>{user.fullName}</Typography>
                          </TableCell>
                          <TableCell>{user.email}</TableCell>
                          <TableCell>
                            <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                              {user.roles.filter((role) => role !== 'ROLE_USER').map((role) => (
                                <Chip key={role} size="small" label={formatRoleLabel(role)} sx={getRoleBadgeStyle(role)} />
                              ))}
                            </Stack>
                          </TableCell>
                          <TableCell>{new Date(user.assignedAt).toLocaleDateString('en-PH')}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </Box>
              )}
            </AdminSectionCard>
          </Stack>
        </Grid>
      </Grid>

      <Dialog
        open={logoDialogOpen}
        onClose={() => {
          setLogoDialogOpen(false)
          setLogoUploadError(null)
        }}
        maxWidth="xs"
        fullWidth
      >
        <DialogTitle sx={{ bgcolor: portalColors.primary, color: '#fff' }}>Upload Agency Logo</DialogTitle>
        <DialogContent sx={{ pt: '20px !important' }}>
          <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 2 }}>
            Accepted formats: JPG, PNG, GIF, WEBP, SVG (Max 2MB)
          </Typography>
          {logoUploadError ? (
            <Alert severity="error" sx={{ mb: 2 }}>
              {logoUploadError}
            </Alert>
          ) : null}
          <input
            ref={fileInputRef}
            type="file"
            accept={AGENCY_LOGO_ACCEPT}
            hidden
            onChange={(event) => {
              const file = event.target.files?.[0]
              if (file) void handleLogoUpload(file)
              event.target.value = ''
            }}
          />
          {logoUrl ? (
            <Alert severity="info" sx={{ mb: 2 }}>
              Uploading a new logo will replace the current one.
            </Alert>
          ) : null}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setLogoDialogOpen(false)} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button variant="contained" disabled={uploading} sx={portalPrimaryButtonSx} onClick={() => fileInputRef.current?.click()}>
            {uploading ? 'Uploading…' : 'Choose File'}
          </Button>
        </DialogActions>
      </Dialog>

      {leadershipDialog ? (
        <AgencyLeadershipDialog
          open
          mode={leadershipDialog}
          agencyId={agencyId}
          currentSecretary={agency.secretary}
          currentUndersecretaries={agency.undersecretaries ?? []}
          onClose={() => setLeadershipDialog(null)}
        />
      ) : null}
    </Box>
  )
}
