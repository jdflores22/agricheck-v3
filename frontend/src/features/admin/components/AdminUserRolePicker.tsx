import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import {
  Box,
  Checkbox,
  FormControlLabel,
  FormGroup,
  Typography,
} from '@mui/material'
import type { AdminAgency, AdminRole } from '../api/adminApi'
import { portalColors } from '../../../components/portal/portalTheme'
import { groupAdminRoles, roleRequiresAgency, toggleAgencyForRole, toggleRoleCode } from './adminUserUtils'

interface AdminUserRolePickerProps {
  roles: AdminRole[]
  agencies: AdminAgency[]
  selectedRoles: string[]
  roleAgencies: Record<string, number[]>
  onRolesChange: (roles: string[]) => void
  onRoleAgenciesChange: (roleAgencies: Record<string, number[]>) => void
}

export function AdminUserRolePicker({
  roles,
  agencies,
  selectedRoles,
  roleAgencies,
  onRolesChange,
  onRoleAgenciesChange,
}: AdminUserRolePickerProps) {
  const groupedRoles = groupAdminRoles(roles)

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      {Object.entries(groupedRoles).map(([group, groupRoles]) =>
        groupRoles.length === 0 ? null : (
          <Box key={group}>
            <Typography
              sx={{
                mb: 1,
                fontSize: '0.75rem',
                fontWeight: 600,
                letterSpacing: '0.08em',
                textTransform: 'uppercase',
                color: portalColors.textMuted,
              }}
            >
              {group}
            </Typography>

            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
              {groupRoles.map((role) => {
                const checked = selectedRoles.includes(role.code)
                const selectedAgencies = roleAgencies[role.code] ?? []

                return (
                  <Box
                    key={role.code}
                    sx={{
                      borderRadius: '0.75rem',
                      border: `1px solid ${portalColors.border}`,
                      bgcolor: portalColors.bgMuted,
                      p: 2,
                    }}
                  >
                    <FormControlLabel
                      control={
                        <Checkbox
                          checked={checked}
                          onChange={() => onRolesChange(toggleRoleCode(role.code, selectedRoles))}
                          color="success"
                        />
                      }
                      label={
                        <Box>
                          <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
                            {role.name}
                          </Typography>
                          {role.description && (
                            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                              {role.description}
                            </Typography>
                          )}
                        </Box>
                      }
                    />

                    {checked && roleRequiresAgency(role) && (
                      <Box sx={{ ml: 4, mt: 1 }}>
                        <Typography sx={{ mb: 1, fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
                          <BusinessOutlinedIcon sx={{ fontSize: 16, mr: 0.5, verticalAlign: 'text-bottom' }} />
                          Assigned Agencies
                        </Typography>
                        <FormGroup sx={{ maxHeight: 180, overflowY: 'auto' }}>
                          {agencies.map((agency) => (
                            <FormControlLabel
                              key={agency.id}
                              control={
                                <Checkbox
                                  size="small"
                                  checked={selectedAgencies.includes(agency.id)}
                                  onChange={() =>
                                    onRoleAgenciesChange(toggleAgencyForRole(role.code, agency.id, roleAgencies))
                                  }
                                  color="success"
                                />
                              }
                              label={`${agency.code} — ${agency.name}`}
                            />
                          ))}
                        </FormGroup>
                        <Typography sx={{ mt: 1, fontSize: '0.75rem', color: portalColors.textMuted }}>
                          Select at least one agency for agency-scoped roles.
                        </Typography>
                      </Box>
                    )}
                  </Box>
                )
              })}
            </Box>
          </Box>
        ),
      )}
    </Box>
  )
}
