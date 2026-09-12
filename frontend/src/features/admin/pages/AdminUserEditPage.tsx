import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useEffect, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalSection } from '../../../components/portal/PortalSection'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { AdminUserRolePicker } from '../components/AdminUserRolePicker'
import { adminUserStatuses, buildRoleAgenciesPayload } from '../components/adminUserUtils'
import {
  useGetAdminAgenciesQuery,
  useGetAdminRolesQuery,
  useGetAdminUserQuery,
  useUpdateAdminUserMutation,
} from '../api/adminApi'

export function AdminUserEditPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const { data, isLoading, error } = useGetAdminUserQuery(uuid, { skip: !uuid })
  const { data: rolesData } = useGetAdminRolesQuery()
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [updateUser, { isLoading: saving, isSuccess, error: saveError }] = useUpdateAdminUserMutation()

  const user = data?.data
  const roles = rolesData?.data ?? []
  const agencies = agenciesData?.data ?? []

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    status: 'Active',
    roleCodes: [] as string[],
    roleAgencies: {} as Record<string, number[]>,
  })

  useEffect(() => {
    if (!user) return
    setForm({
      firstName: user.firstName,
      lastName: user.lastName,
      status: user.status,
      roleCodes: [...user.roles],
      roleAgencies: Object.fromEntries(
        Object.entries(user.roleAgencies).map(([key, value]) => [key, [...value]]),
      ),
    })
  }, [user])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    await updateUser({
      uuid,
      firstName: form.firstName,
      lastName: form.lastName,
      status: form.status,
      roleCodes: form.roleCodes,
      roleAgencies: buildRoleAgenciesPayload(form.roleCodes, form.roleAgencies),
    }).unwrap()
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  if (error || !user) {
    return (
      <Box>
        <PortalPageHeader eyebrow="Management" title="Edit User" subtitle="User account not found." />
        <Alert severity="error">Could not load user details.</Alert>
        <Button component={RouterLink} to="/admin/users" sx={{ mt: 2 }}>
          Back to Users
        </Button>
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Edit User"
        subtitle="Modify user account settings and permissions."
        action={{ label: 'Back to Users', to: '/admin/users' }}
      />

      {isSuccess && <Alert severity="success" sx={{ mb: 2 }}>User updated successfully.</Alert>}
      {saveError && <Alert severity="error" sx={{ mb: 2 }}>Could not save user changes.</Alert>}

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <PortalSection icon={<EditOutlinedIcon fontSize="small" />} title="User Information">
            <Box component="form" onSubmit={handleSubmit}>
              <Stack spacing={3}>
                <Box>
                  <TextField label="Email" value={user.email} disabled fullWidth />
                  <Typography sx={{ mt: 0.75, fontSize: '0.75rem', color: portalColors.textMuted }}>
                    Email cannot be changed
                  </Typography>
                </Box>

                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                  <TextField
                    label="First Name"
                    value={form.firstName}
                    onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                    required
                    fullWidth
                  />
                  <TextField
                    label="Last Name"
                    value={form.lastName}
                    onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                    required
                    fullWidth
                  />
                </Stack>

                <FormControl fullWidth>
                  <InputLabel>Status</InputLabel>
                  <Select
                    label="Status"
                    value={form.status}
                    onChange={(e) => setForm({ ...form, status: e.target.value })}
                  >
                    {adminUserStatuses.map((status) => (
                      <MenuItem key={status} value={status}>
                        {status}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

                <Box>
                  <Typography sx={{ mb: 1, fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
                    Roles & Permissions
                  </Typography>
                  <Typography sx={{ mb: 2, fontSize: '0.8125rem', color: portalColors.textMuted }}>
                    Assign roles for this account. Agency assignment applies only to agency-scoped roles. DA Accreditation Officer is not tied to any agency.
                  </Typography>

                  <AdminUserRolePicker
                    roles={roles}
                    agencies={agencies}
                    selectedRoles={form.roleCodes}
                    roleAgencies={form.roleAgencies}
                    onRolesChange={(roleCodes) => setForm({ ...form, roleCodes })}
                    onRoleAgenciesChange={(roleAgencies) => setForm({ ...form, roleAgencies })}
                  />
                </Box>

                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                  <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={saving || form.roleCodes.length === 0}>
                    {saving ? 'Saving…' : 'Save Changes'}
                  </Button>
                  <Button variant="outlined" sx={portalOutlinedButtonSx} component={RouterLink} to="/admin/users">
                    Cancel
                  </Button>
                </Stack>
              </Stack>
            </Box>
          </PortalSection>
        </Grid>

        <Grid size={{ xs: 12, lg: 4 }}>
          <PortalSection icon={<InfoOutlinedIcon fontSize="small" />} title="Account Details">
            <Stack spacing={2}>
              <Box>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, color: portalColors.textMuted }}>Created</Typography>
                <Typography sx={{ mt: 0.5, fontSize: '0.875rem' }}>
                  {new Date(user.createdAt).toLocaleString()}
                </Typography>
              </Box>
              <Box>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, color: portalColors.textMuted }}>User ID</Typography>
                <Typography sx={{ mt: 0.5, fontSize: '0.875rem', fontFamily: 'monospace' }}>{user.uuid}</Typography>
              </Box>
              {user.lastLoginAt && (
                <Box>
                  <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, color: portalColors.textMuted }}>Last Login</Typography>
                  <Typography sx={{ mt: 0.5, fontSize: '0.875rem' }}>
                    {new Date(user.lastLoginAt).toLocaleString()}
                  </Typography>
                </Box>
              )}
              {(user.phone || user.companyName) && (
                <Box sx={{ pt: 1, borderTop: `1px solid ${portalColors.border}` }}>
                  {user.phone && (
                    <Typography sx={{ fontSize: '0.875rem', mb: 0.5 }}>
                      Phone: {user.phone}
                    </Typography>
                  )}
                  {user.companyName && (
                    <Typography sx={{ fontSize: '0.875rem' }}>
                      Company: {user.companyName}
                    </Typography>
                  )}
                </Box>
              )}
              <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={() => navigate('/admin/users')}>
                Back to Users
              </Button>
            </Stack>
          </PortalSection>
        </Grid>
      </Grid>
    </Box>
  )
}
