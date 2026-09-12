import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { formatRoleLabel, getRoleBadgeStyle } from '../../../components/portal/portalUtils'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { AdminUserRolePicker } from '../components/AdminUserRolePicker'
import { buildRoleAgenciesPayload } from '../components/adminUserUtils'
import {
  useCreateAdminUserMutation,
  useGetAdminAgenciesQuery,
  useGetAdminRolesQuery,
  useGetAdminUsersQuery,
} from '../api/adminApi'

const emptyForm = {
  email: '',
  password: '',
  firstName: '',
  lastName: '',
  roleCodes: ['ROLE_EVALUATOR'] as string[],
  roleAgencies: {} as Record<string, number[]>,
}

export function AdminUsersPage() {
  const { data, isLoading } = useGetAdminUsersQuery({ page: 1 })
  const { data: rolesData } = useGetAdminRolesQuery()
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [createUser, { isLoading: creating, error: createError }] = useCreateAdminUserMutation()
  const [createOpen, setCreateOpen] = useState(false)
  const [createForm, setCreateForm] = useState(emptyForm)

  const roles = rolesData?.data ?? []
  const agencies = agenciesData?.data ?? []
  const users = data?.data?.items ?? []

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault()
    await createUser({
      email: createForm.email,
      password: createForm.password,
      firstName: createForm.firstName,
      lastName: createForm.lastName,
      roleCodes: createForm.roleCodes,
      roleAgencies: buildRoleAgenciesPayload(createForm.roleCodes, createForm.roleAgencies),
    }).unwrap()
    setCreateOpen(false)
    setCreateForm(emptyForm)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Users"
        subtitle="Manage system accounts and role assignments."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setCreateOpen(true)}>
            Add User
          </Button>
        }
      />

      <PortalTablePanel
        title="All users"
        columns={['Name', 'Email', 'Status', 'Roles', 'Created', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && users.length === 0}
        emptyMessage="No users found."
      >
        {users.map((user) => (
          <TableRow key={user.uuid} hover>
            <TableCell>{user.fullName}</TableCell>
            <TableCell>{user.email}</TableCell>
            <TableCell>{user.status}</TableCell>
            <TableCell>
              <Stack direction="row" spacing={0.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                {user.roles.filter((r) => r !== 'ROLE_USER').map((role) => (
                  <Chip
                    key={role}
                    size="small"
                    label={formatRoleLabel(role)}
                    sx={getRoleBadgeStyle(role)}
                  />
                ))}
              </Stack>
            </TableCell>
            <TableCell>{new Date(user.createdAt).toLocaleDateString()}</TableCell>
            <TableCell>
              <Button
                size="small"
                component={RouterLink}
                to={`/admin/users/${user.uuid}/edit`}
              >
                Edit
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={createOpen} onClose={() => setCreateOpen(false)} maxWidth="md" fullWidth>
        <Box component="form" onSubmit={handleCreate}>
          <DialogTitle>Create User</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              {createError && <Alert severity="error">Could not create user.</Alert>}
              <TextField label="Email" value={createForm.email} onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })} required fullWidth />
              <TextField label="Password" type="password" value={createForm.password} onChange={(e) => setCreateForm({ ...createForm, password: e.target.value })} required fullWidth />
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField label="First Name" value={createForm.firstName} onChange={(e) => setCreateForm({ ...createForm, firstName: e.target.value })} required fullWidth />
                <TextField label="Last Name" value={createForm.lastName} onChange={(e) => setCreateForm({ ...createForm, lastName: e.target.value })} required fullWidth />
              </Stack>

              <AdminUserRolePicker
                roles={roles}
                agencies={agencies}
                selectedRoles={createForm.roleCodes}
                roleAgencies={createForm.roleAgencies}
                onRolesChange={(roleCodes) => setCreateForm({ ...createForm, roleCodes })}
                onRoleAgenciesChange={(roleAgencies) => setCreateForm({ ...createForm, roleAgencies })}
              />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setCreateOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={creating || createForm.roleCodes.length === 0}>
              Create
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
