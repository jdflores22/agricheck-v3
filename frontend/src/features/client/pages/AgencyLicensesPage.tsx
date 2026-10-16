import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import BadgeOutlinedIcon from '@mui/icons-material/BadgeOutlined'
import SendOutlinedIcon from '@mui/icons-material/SendOutlined'
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
  MenuItem,
  Stack,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { FormEvent, useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import type { ApiEnvelope } from '../../auth/types'
import {
  useCreateAgencyLicenseApplicationMutation,
  useGetAgenciesQuery,
  useGetAgencyLicenseTypesQuery,
  useGetAgencyLicensesOverviewQuery,
  useRegisterAgencyLicenseMutation,
  useSubmitAgencyLicenseApplicationMutation,
} from '../api/clientApi'

function getApiErrorMessage(error: unknown): string | null {
  if (!error || typeof error !== 'object' || !('data' in error)) return null
  const data = (error as FetchBaseQueryError).data as ApiEnvelope<unknown> | undefined
  return data?.errors?.[0]?.message ?? null
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString()
}

function statusChip(status: string, isActive?: boolean) {
  if (isActive) {
    return <Chip size="small" label="Active" color="success" />
  }
  if (status === 'Active') {
    return <Chip size="small" label="Active" color="success" />
  }
  if (status === 'Expired') {
    return <Chip size="small" label="Expired" color="default" />
  }
  if (status === 'Draft') {
    return <Chip size="small" label="Draft" />
  }
  if (status === 'Submitted' || status === 'UnderReview') {
    return <Chip size="small" label={status.replace(/([A-Z])/g, ' $1').trim()} color="warning" />
  }
  return <Chip size="small" label={status.replace(/([A-Z])/g, ' $1').trim()} />
}

export function AgencyLicensesPage() {
  const { data: overviewData, isLoading } = useGetAgencyLicensesOverviewQuery()
  const { data: agenciesData } = useGetAgenciesQuery()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [dialogTab, setDialogTab] = useState<'register' | 'apply'>('register')
  const [agencyId, setAgencyId] = useState('')
  const [licenseTypeId, setLicenseTypeId] = useState('')
  const [licenseNumber, setLicenseNumber] = useState('')
  const [issuedAt, setIssuedAt] = useState('')
  const [expiresAt, setExpiresAt] = useState('')
  const [notes, setNotes] = useState('')
  const [remarks, setRemarks] = useState('')

  const agencyIdNum = agencyId ? Number(agencyId) : 0
  const { data: typesData } = useGetAgencyLicenseTypesQuery(agencyIdNum, { skip: !agencyIdNum })

  const [registerLicense, { isLoading: registering, error: registerError }] = useRegisterAgencyLicenseMutation()
  const [createApplication, { isLoading: creatingApp, error: createAppError }] = useCreateAgencyLicenseApplicationMutation()
  const [submitApplication, { isLoading: submitting }] = useSubmitAgencyLicenseApplicationMutation()

  const agencies = useMemo(
    () => (agenciesData?.data ?? []).filter((a) => ['BAI', 'BPI', 'BFAR'].includes(a.code)),
    [agenciesData],
  )
  const licenseTypes = typesData?.data ?? []
  const licenses = overviewData?.data?.licenses ?? []
  const applications = overviewData?.data?.applications ?? []

  const resetForm = () => {
    setAgencyId('')
    setLicenseTypeId('')
    setLicenseNumber('')
    setIssuedAt('')
    setExpiresAt('')
    setNotes('')
    setRemarks('')
    setDialogTab('register')
  }

  const handleOpenDialog = () => {
    resetForm()
    setDialogOpen(true)
  }

  const handleRegister = async (event: FormEvent) => {
    event.preventDefault()
    if (!agencyIdNum || !licenseTypeId) return
    await registerLicense({
      agencyId: agencyIdNum,
      agencyLicenseTypeId: Number(licenseTypeId),
      licenseNumber,
      issuedAt: new Date(issuedAt).toISOString(),
      expiresAt: new Date(expiresAt).toISOString(),
      notes: notes.trim() || undefined,
    }).unwrap()
    setDialogOpen(false)
    resetForm()
  }

  const handleApply = async (event: FormEvent) => {
    event.preventDefault()
    if (!agencyIdNum || !licenseTypeId) return
    const created = await createApplication({
      agencyId: agencyIdNum,
      agencyLicenseTypeId: Number(licenseTypeId),
      formDataJson: JSON.stringify({ remarks: remarks.trim() }),
    }).unwrap()
    const uuid = (created.data as { uuid?: string })?.uuid
    if (uuid) {
      await submitApplication(uuid).unwrap()
    }
    setDialogOpen(false)
    resetForm()
  }

  const dialogError = getApiErrorMessage(registerError) ?? getApiErrorMessage(createAppError)

  return (
    <>
      <PortalPageHeader
        eyebrow="Compliance"
        title="My Bureau Licenses"
        subtitle="LTO and LTI per bureau (BAI, BPI, BFAR) are required before SPS Import Clearance under DA AC 06."
        actions={
          <Button variant="contained" startIcon={<AddOutlinedIcon />} sx={portalPrimaryButtonSx} onClick={handleOpenDialog}>
            Add license
          </Button>
        }
      />

      <Alert severity="info" sx={{ mb: 3, borderRadius: '0.75rem' }}>
        Register a license you already hold from the bureau, or submit a new license application for bureau review (coming to agency portal).
      </Alert>

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress size={32} sx={{ color: portalColors.primary }} />
        </Box>
      ) : (
        <Stack spacing={3}>
          <PortalPanel title="Active & recorded licenses">
            <Box sx={{ px: 2.5, py: 2 }}>
              {licenses.length === 0 ? (
                <Typography sx={{ color: portalColors.textMuted, fontSize: '0.875rem' }}>
                  No bureau licenses on file yet. Use &quot;Add license&quot; to register your LTO or LTI.
                </Typography>
              ) : (
                <Stack spacing={1.5}>
                  {licenses.map((license) => (
                    <Box
                      key={license.uuid}
                      sx={{
                        display: 'flex',
                        flexWrap: 'wrap',
                        alignItems: 'center',
                        gap: 2,
                        p: 2,
                        borderRadius: '0.5rem',
                        border: `1px solid ${portalColors.border}`,
                      }}
                    >
                      <BadgeOutlinedIcon sx={{ color: portalColors.primary }} />
                      <Box sx={{ flex: 1, minWidth: 200 }}>
                        <Typography sx={{ fontWeight: 700 }}>
                          {license.agencyCode} — {license.licenseTypeCode}
                        </Typography>
                        <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                          {license.licenseNumber} · {license.licenseTypeName}
                        </Typography>
                        <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.25 }}>
                          Valid {formatDate(license.issuedAt)} – {formatDate(license.expiresAt)}
                        </Typography>
                      </Box>
                      {statusChip(license.status, license.isActive)}
                    </Box>
                  ))}
                </Stack>
              )}
            </Box>
          </PortalPanel>

          <PortalPanel title="License applications">
            <Box sx={{ px: 2.5, py: 2 }}>
              {applications.length === 0 ? (
                <Typography sx={{ color: portalColors.textMuted, fontSize: '0.875rem' }}>
                  No applications in progress.
                </Typography>
              ) : (
                <Stack spacing={1.5}>
                  {applications.map((app) => (
                    <Box
                      key={app.uuid}
                      sx={{
                        display: 'flex',
                        flexWrap: 'wrap',
                        alignItems: 'center',
                        gap: 2,
                        p: 2,
                        borderRadius: '0.5rem',
                        border: `1px solid ${portalColors.border}`,
                      }}
                    >
                      <Box sx={{ flex: 1, minWidth: 200 }}>
                        <Typography sx={{ fontWeight: 700 }}>
                          {app.agencyCode} — {app.licenseTypeCode}
                        </Typography>
                        <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                          {app.licenseTypeName}
                        </Typography>
                        {app.submittedAt && (
                          <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
                            Submitted {formatDate(app.submittedAt)}
                          </Typography>
                        )}
                      </Box>
                      {statusChip(app.status)}
                      {app.status === 'Draft' && (
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<SendOutlinedIcon />}
                          disabled={submitting}
                          sx={portalOutlinedButtonSx}
                          onClick={() => void submitApplication(app.uuid).unwrap()}
                        >
                          Submit
                        </Button>
                      )}
                    </Box>
                  ))}
                </Stack>
              )}
            </Box>
          </PortalPanel>
        </Stack>
      )}

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Add bureau license</DialogTitle>
        <Tabs value={dialogTab} onChange={(_, value) => setDialogTab(value)} sx={{ px: 3 }}>
          <Tab value="register" label="Register existing" />
          <Tab value="apply" label="Apply for new" />
        </Tabs>
        <Box component="form" onSubmit={dialogTab === 'register' ? handleRegister : handleApply}>
          <DialogContent>
            {dialogError && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {dialogError}
              </Alert>
            )}
            <Stack spacing={2}>
              <TextField
                select
                label="Bureau"
                size="small"
                value={agencyId}
                onChange={(e) => {
                  setAgencyId(e.target.value)
                  setLicenseTypeId('')
                }}
                required
                fullWidth
              >
                {agencies.map((agency) => (
                  <MenuItem key={agency.id} value={String(agency.id)}>
                    {agency.code} — {agency.name}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                select
                label="License type"
                size="small"
                value={licenseTypeId}
                onChange={(e) => setLicenseTypeId(e.target.value)}
                required
                fullWidth
                disabled={!agencyIdNum}
              >
                {licenseTypes.map((type) => (
                  <MenuItem key={type.id} value={String(type.id)}>
                    {type.code} — {type.name}
                  </MenuItem>
                ))}
              </TextField>
              {dialogTab === 'register' ? (
                <>
                  <TextField
                    label="License number"
                    size="small"
                    value={licenseNumber}
                    onChange={(e) => setLicenseNumber(e.target.value)}
                    required
                    fullWidth
                  />
                  <TextField
                    label="Issued date"
                    type="date"
                    size="small"
                    value={issuedAt}
                    onChange={(e) => setIssuedAt(e.target.value)}
                    required
                    fullWidth
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
                  <TextField
                    label="Expiry date"
                    type="date"
                    size="small"
                    value={expiresAt}
                    onChange={(e) => setExpiresAt(e.target.value)}
                    required
                    fullWidth
                    slotProps={{ inputLabel: { shrink: true } }}
                  />
                  <TextField
                    label="Notes (optional)"
                    size="small"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    fullWidth
                    multiline
                    minRows={2}
                  />
                </>
              ) : (
                <TextField
                  label="Remarks for bureau"
                  size="small"
                  value={remarks}
                  onChange={(e) => setRemarks(e.target.value)}
                  fullWidth
                  multiline
                  minRows={3}
                  helperText="Creates and submits an application for bureau review."
                />
              )}
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => setDialogOpen(false)} sx={portalOutlinedButtonSx}>
              Cancel
            </Button>
            <Button
              type="submit"
              variant="contained"
              sx={portalPrimaryButtonSx}
              disabled={registering || creatingApp || submitting}
            >
              {dialogTab === 'register' ? 'Save license' : 'Submit application'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </>
  )
}
