import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import LightbulbOutlinedIcon from '@mui/icons-material/LightbulbOutlined'
import {
  Box,
  Button,
  FormControl,
  Grid,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import type { FormEvent, ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import type { AdminAgency } from '../api/adminApi'
import { AdminSectionCard } from '../components/AdminSectionCard'

export interface AdminAgencyFormValues {
  code: string
  name: string
  parentId: number | ''
  description: string
  address: string
  contactNumber: string
  email: string
  isActive: boolean
}

interface AdminAgencyFormProps {
  mode: 'create' | 'edit'
  form: AdminAgencyFormValues
  agencies: AdminAgency[]
  agencyId?: number
  saving?: boolean
  onChange: (next: AdminAgencyFormValues) => void
  onSubmit: (event: FormEvent) => void
}

function FieldHint({ children }: { children: ReactNode }) {
  return (
    <Typography sx={{ mt: 0.75, fontSize: '0.8125rem', color: portalColors.textMuted }}>
      {children}
    </Typography>
  )
}

export function AdminAgencyForm({
  mode,
  form,
  agencies,
  agencyId,
  saving,
  onChange,
  onSubmit,
}: AdminAgencyFormProps) {
  const parentOptions = agencies.filter((agency) => agency.id !== agencyId)

  return (
    <Box component="form" onSubmit={onSubmit}>
      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 8 }}>
          <AdminSectionCard title={mode === 'create' ? 'Create Agency' : 'Edit Agency'}>
            <Stack spacing={2.5}>
              <Box>
                <TextField
                  label="Agency Code"
                  value={form.code}
                  onChange={(event) => onChange({ ...form, code: event.target.value.toUpperCase() })}
                  required
                  fullWidth
                  slotProps={{ htmlInput: { maxLength: 10 } }}
                  placeholder="e.g., BAI, BFAR, BPI"
                />
                <FieldHint>Unique code for the agency (max 10 characters)</FieldHint>
              </Box>

              <Box>
                <TextField
                  label="Agency Name"
                  value={form.name}
                  onChange={(event) => onChange({ ...form, name: event.target.value })}
                  required
                  fullWidth
                  slotProps={{ htmlInput: { maxLength: 100 } }}
                  placeholder="e.g., Bureau of Animal Industry"
                />
                <FieldHint>Full name of the agency (max 100 characters)</FieldHint>
              </Box>

              <Box>
                <FormControl fullWidth>
                  <InputLabel id="agency-parent-label">Parent Agency</InputLabel>
                  <Select
                    labelId="agency-parent-label"
                    label="Parent Agency"
                    value={form.parentId === '' ? '' : String(form.parentId)}
                    onChange={(event) =>
                      onChange({
                        ...form,
                        parentId: event.target.value === '' ? '' : Number(event.target.value),
                      })
                    }
                  >
                    <MenuItem value="">-- None (Top Level) --</MenuItem>
                    {parentOptions.map((agency) => (
                      <MenuItem key={agency.id} value={String(agency.id)}>
                        {agency.name} ({agency.code})
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <FieldHint>Select parent agency if this is a bureau/sub-agency</FieldHint>
              </Box>

              <Box>
                <TextField
                  label="Description"
                  value={form.description}
                  onChange={(event) => onChange({ ...form, description: event.target.value })}
                  fullWidth
                  multiline
                  minRows={3}
                  placeholder="Optional description of the agency's role and responsibilities..."
                />
                <FieldHint>Optional description</FieldHint>
              </Box>

              <Box>
                <TextField
                  label="Address"
                  value={form.address}
                  onChange={(event) => onChange({ ...form, address: event.target.value })}
                  fullWidth
                  multiline
                  minRows={2}
                  placeholder="Physical address of the agency..."
                />
              </Box>

              <Grid container spacing={2}>
                <Grid size={{ xs: 12, md: 6 }}>
                  <TextField
                    label="Contact Number"
                    value={form.contactNumber}
                    onChange={(event) => onChange({ ...form, contactNumber: event.target.value })}
                    fullWidth
                    slotProps={{ htmlInput: { maxLength: 50 } }}
                    placeholder="e.g., +63 2 8888 8888"
                  />
                </Grid>
                <Grid size={{ xs: 12, md: 6 }}>
                  <TextField
                    label="Email Address"
                    type="email"
                    value={form.email}
                    onChange={(event) => onChange({ ...form, email: event.target.value })}
                    fullWidth
                    slotProps={{ htmlInput: { maxLength: 100 } }}
                    placeholder="e.g., info@agency.gov.ph"
                  />
                </Grid>
              </Grid>

              <Stack direction="row" spacing={1.5} sx={{ pt: 1, borderTop: `1px solid ${portalColors.border}` }}>
                <Button type="submit" variant="contained" disabled={saving} sx={portalPrimaryButtonSx}>
                  {mode === 'create' ? 'Create Agency' : 'Save Changes'}
                </Button>
                <Button component={RouterLink} to="/admin/agencies" variant="outlined" sx={portalOutlinedButtonSx}>
                  Cancel
                </Button>
              </Stack>
            </Stack>
          </AdminSectionCard>
        </Grid>

        <Grid size={{ xs: 12, lg: 4 }}>
          {mode === 'edit' && agencyId ? (
            <AdminSectionCard title="Agency Details">
              <Stack spacing={2}>
                <Box>
                  <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mb: 0.5 }}>Agency ID</Typography>
                  <Typography sx={{ fontFamily: 'monospace', fontSize: '0.875rem' }}>{agencyId}</Typography>
                </Box>
                <Box>
                  <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mb: 0.5 }}>Current Code</Typography>
                  <Typography sx={{ fontWeight: 600 }}>{form.code}</Typography>
                </Box>
              </Stack>
            </AdminSectionCard>
          ) : null}

          {mode === 'create' ? (
            <>
              <AdminSectionCard title="Information" icon={<InfoOutlinedIcon />}>
                <Stack spacing={1.5} sx={{ fontSize: '0.875rem', color: '#1d4ed8' }}>
                  <Typography sx={{ fontSize: '0.875rem' }}>
                    Agencies are government bodies responsible for accreditation and certification in specific sectors.
                  </Typography>
                  <Box sx={{ borderTop: '1px solid #bbf7d0', pt: 1.5 }}>
                    <Typography sx={{ fontWeight: 600, mb: 0.75 }}>Examples:</Typography>
                    <Typography sx={{ fontSize: '0.875rem' }}>• BAI — Bureau of Animal Industry</Typography>
                    <Typography sx={{ fontSize: '0.875rem' }}>• BFAR — Bureau of Fisheries and Aquatic Resources</Typography>
                    <Typography sx={{ fontSize: '0.875rem' }}>• BPI — Bureau of Plant Industry</Typography>
                  </Box>
                </Stack>
              </AdminSectionCard>

              <Box sx={{ mt: 2 }}>
                <AdminSectionCard title="Creation Guidelines" icon={<LightbulbOutlinedIcon />}>
                  <Stack spacing={0.75} sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                    <Typography sx={{ fontSize: '0.875rem' }}>• Use clear, descriptive agency names</Typography>
                    <Typography sx={{ fontSize: '0.875rem' }}>• Keep codes short and memorable</Typography>
                    <Typography sx={{ fontSize: '0.875rem' }}>• Set parent agency for bureaus/divisions</Typography>
                    <Typography sx={{ fontSize: '0.875rem' }}>• Include complete contact information</Typography>
                  </Stack>
                </AdminSectionCard>
              </Box>
            </>
          ) : (
            <Box sx={{ mt: mode === 'edit' ? 2 : 0 }}>
              <Box
                sx={{
                  border: '1px solid #bfdbfe',
                  bgcolor: '#eff6ff',
                  borderRadius: '0.75rem',
                  p: 2,
                }}
              >
                <Stack direction="row" spacing={1.5} sx={{ alignItems: 'flex-start' }}>
                  <InfoOutlinedIcon sx={{ color: '#2563eb', mt: 0.25 }} />
                  <Box>
                    <Typography sx={{ fontWeight: 600, color: '#1e3a8a', mb: 1 }}>Editing Tips</Typography>
                    <Stack spacing={0.5} sx={{ fontSize: '0.875rem', color: '#1d4ed8' }}>
                      <Typography sx={{ fontSize: '0.875rem' }}>• Agency code must be unique</Typography>
                      <Typography sx={{ fontSize: '0.875rem' }}>• Use clear, descriptive names</Typography>
                      <Typography sx={{ fontSize: '0.875rem' }}>• Set parent agency for bureaus</Typography>
                      <Typography sx={{ fontSize: '0.875rem' }}>• Contact info helps with coordination</Typography>
                    </Stack>
                  </Box>
                </Stack>
              </Box>
            </Box>
          )}
        </Grid>
      </Grid>
    </Box>
  )
}
