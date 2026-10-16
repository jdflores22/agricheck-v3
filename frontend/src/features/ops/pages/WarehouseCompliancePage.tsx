import { FormEvent, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetWarehouseComplianceDetailQuery,
  useGetWarehouseComplianceFacilitiesQuery,
  useSubmitWarehouseLtoApplicationMutation,
  useSubmitWarehouseRsasMutation,
  useUpsertWarehouseRsasMutation,
} from '../api/opsApi'
import { getApiErrorMessage } from '../utils/apiErrorMessage'
import {
  parseFacilityScopeJson,
  scopeViolatesSegregation,
  toggleScopeClass,
  WAREHOUSE_COMMODITY_CLASS_OPTIONS,
} from '../utils/warehouseProductScope'

function statusChip(label: string, tone: 'success' | 'warning' | 'default' = 'default') {
  const colors =
    tone === 'success'
      ? { bg: portalColors.successSoft, fg: portalColors.successText }
      : tone === 'warning'
        ? { bg: portalColors.bgMuted, fg: portalColors.textMuted }
        : { bg: portalColors.bgMuted, fg: portalColors.textMuted }
  return (
    <Chip
      size="small"
      label={label}
      sx={{ bgcolor: colors.bg, color: colors.fg, fontWeight: 600 }}
    />
  )
}

export function WarehouseCompliancePage() {
  const { data: listData, isLoading: listLoading } = useGetWarehouseComplianceFacilitiesQuery()
  const facilities = listData?.data ?? []
  const [selectedId, setSelectedId] = useState<number | ''>('')

  const singleFacility = facilities.length === 1 ? facilities[0] : null
  const activeId =
    selectedId === '' ? (singleFacility?.warehouseFacilityId ?? facilities[0]?.warehouseFacilityId) : selectedId
  const { data: detailData, isLoading: detailLoading } = useGetWarehouseComplianceDetailQuery(activeId!, {
    skip: !activeId,
  })
  const detail = detailData?.data

  const [ltoOpen, setLtoOpen] = useState(false)
  const [rsasOpen, setRsasOpen] = useState(false)
  const [operatorName, setOperatorName] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [contactPhone, setContactPhone] = useState('')
  const [ltoNotes, setLtoNotes] = useState('')
  const [ownerName, setOwnerName] = useState('')
  const [rsasRef, setRsasRef] = useState('')
  const [capacityKg, setCapacityKg] = useState('')
  const [bmbeExempt, setBmbeExempt] = useState(false)
  const [rsasProductScope, setRsasProductScope] = useState<string[]>([])

  const [submitLto, { isLoading: submittingLto, error: ltoError, isSuccess: ltoSuccess }] =
    useSubmitWarehouseLtoApplicationMutation()
  const [upsertRsas, { isLoading: savingRsas }] = useUpsertWarehouseRsasMutation()
  const [submitRsas] = useSubmitWarehouseRsasMutation()

  const canApplyLto = useMemo(() => {
    if (!detail) return false
    return !detail.hasActiveWarehouseLto && detail.pendingLtoApplication?.status !== 'Pending'
  }, [detail])

  const rsasStatus = detail?.rsasRegistration?.status ?? 'NotStarted'

  const canEditRsas = useMemo(() => {
    if (!detail?.hasActiveWarehouseLto) return false
    return rsasStatus === 'Draft' || rsasStatus === 'Rejected' || rsasStatus === 'NotStarted'
  }, [detail, rsasStatus])

  const destinationBlockers = useMemo(() => {
    if (!detail || detail.destinationReady) return []
    const items: string[] = []
    if (!detail.hasActiveWarehouseLto) {
      items.push('Active DA warehouse operating license (LTO)')
    }
    if (rsasStatus !== 'Registered') {
      items.push(
        rsasStatus === 'Submitted'
          ? 'RSAS approved by DA (your registration is pending review)'
          : 'RSAS registration completed and approved by DA',
      )
    }
    if (detail.cswStatus !== 'Active') {
      items.push('CSW accreditation recorded by DA (lead agency / cold storage certificate)')
    }
    return items
  }, [detail, rsasStatus])

  const handleLtoSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!activeId) return
    await submitLto({
      warehouseFacilityId: activeId,
      operatorLegalName: operatorName,
      contactEmail: contactEmail || undefined,
      contactPhone: contactPhone || undefined,
      notes: ltoNotes || undefined,
    }).unwrap()
    setLtoOpen(false)
  }

  const handleRsasSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!activeId) return
    if (scopeViolatesSegregation(rsasProductScope)) return

    const result = await upsertRsas({
      warehouseFacilityId: activeId,
      ownerName,
      rsasExternalRef: rsasRef.trim() || undefined,
      maxCapacityKg: capacityKg ? Number(capacityKg) : undefined,
      isBmbeExempt: bmbeExempt,
      productsJson: rsasProductScope.length > 0 ? JSON.stringify(rsasProductScope) : undefined,
    }).unwrap()
    const uuid = (result.data as { uuid?: string })?.uuid
    if (uuid && rsasRef.trim()) {
      await submitRsas(uuid).unwrap()
    }
    setRsasOpen(false)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title={singleFacility ? singleFacility.warehouseName : 'Facility compliance'}
        subtitle={
          singleFacility
            ? `${singleFacility.warehouseCode} — Apply for DA warehouse LTO, then complete RSAS. CSW is recorded by DA after RSAS approval.`
            : 'Apply for DA warehouse operating LTO, then complete RSAS as the facility operator. CSW is recorded by DA after RSAS approval.'
        }
      />

      {facilities.length === 0 && !listLoading && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          No warehouses are assigned to your account. Ask your DA or system administrator to link your warehouse staff user to a facility.
        </Alert>
      )}

      <PortalPanel title={singleFacility ? 'Compliance status' : 'Your facilities'}>
        <Stack spacing={2} sx={{ p: 2.5, maxWidth: 720 }}>
          {facilities.length > 1 && (
            <TextField
              select
              label="Facility"
              value={selectedId === '' ? (facilities[0]?.warehouseFacilityId ?? '') : selectedId}
              onChange={(e) => setSelectedId(Number(e.target.value))}
              fullWidth
            >
              {facilities.map((f) => (
                <MenuItem key={f.warehouseFacilityId} value={f.warehouseFacilityId}>
                  {f.warehouseCode} — {f.warehouseName}
                </MenuItem>
              ))}
            </TextField>
          )}

          {singleFacility?.warehouseLocation && (
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>{singleFacility.warehouseLocation}</Typography>
          )}

          {detailLoading && <Typography sx={{ color: portalColors.textMuted }}>Loading compliance…</Typography>}

          {detail && (
            <Stack spacing={2}>
              <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                {detail.hasActiveWarehouseLto
                  ? statusChip(`LTO: ${detail.activeLicense?.licenseNumber ?? 'Active'}`, 'success')
                  : detail.pendingLtoApplication?.status === 'Pending'
                    ? statusChip('LTO application: Pending DA review', 'warning')
                    : statusChip('LTO: Not issued', 'warning')}
                {statusChip(`RSAS: ${detail.rsasRegistration?.status ?? 'NotStarted'}`)}
                {statusChip(`CSW: ${detail.cswStatus}`)}
                {detail.destinationReady
                  ? statusChip('Destination ready', 'success')
                  : statusChip('Destination incomplete', 'warning')}
              </Stack>

              {detail.hasActiveWarehouseLto && detail.activeLicense && (
                <>
                  <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                    LTO expires {new Date(detail.activeLicense.expiresAt).toLocaleDateString()}
                  </Typography>
                  <Alert severity="success">
                    This facility already has an active LTO ({detail.activeLicense.licenseNumber}). It was recorded by DA
                    (e.g. dev backfill or direct issue on the warehouse detail page)—not via the application queue. You do
                    not need to apply again.
                  </Alert>
                </>
              )}

              <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                <Button
                  variant="contained"
                  sx={portalPrimaryButtonSx}
                  disabled={!canApplyLto}
                  onClick={() => {
                    setOperatorName('')
                    setContactEmail('')
                    setContactPhone('')
                    setLtoNotes('')
                    setLtoOpen(true)
                  }}
                >
                  Apply for DA warehouse LTO
                </Button>
                <Button
                  variant="outlined"
                  sx={portalOutlinedButtonSx}
                  disabled={!canEditRsas}
                  onClick={() => {
                    setOwnerName(detail.rsasRegistration?.ownerName ?? '')
                    setRsasRef(detail.rsasRegistration?.rsasExternalRef ?? '')
                    setCapacityKg(detail.rsasRegistration?.maxCapacityKg?.toString() ?? '')
                    setBmbeExempt(detail.rsasRegistration?.isBmbeExempt ?? false)
                    setRsasProductScope(
                      parseFacilityScopeJson(detail.rsasRegistration?.productsJson),
                    )
                    setRsasOpen(true)
                  }}
                >
                  {detail.rsasRegistration ? 'Update RSAS registration' : 'Start RSAS registration'}
                </Button>
              </Stack>

              {!detail.hasActiveWarehouseLto && (
                <Alert severity="info">
                  Submit an <strong>LTO application</strong> first. After DA approves and issues the license, you can complete{' '}
                  <strong>RSAS</strong> here (rsas.da.gov.ph reference). Importers will then link your licensed facility under My Facilities.
                </Alert>
              )}

              {detail.latestLtoApplication?.status === 'Rejected' && detail.latestLtoApplication.reviewComments && (
                <Alert severity="error">
                  LTO application rejected: {detail.latestLtoApplication.reviewComments}
                </Alert>
              )}

              {ltoSuccess && <Alert severity="success">LTO application submitted to DA.</Alert>}

              {rsasStatus === 'Submitted' && (
                <Alert severity="info">
                  RSAS is <strong>pending DA verification</strong>. You cannot edit it until DA approves or rejects. CSW cannot be recorded until RSAS is <strong>Registered</strong>.
                </Alert>
              )}

              {rsasStatus === 'Registered' && detail.cswStatus !== 'Active' && (
                <Alert severity="info">
                  <strong>CSW is not applied in the Warehouse Portal.</strong> After physical/TR A accreditation, DA records your CSW on the warehouse oversight record (same place they verify RSAS). Your operator role ends at RSAS; importers then see &quot;destination ready&quot; when LTO + RSAS + active CSW are all on file.
                </Alert>
              )}

              {destinationBlockers.length > 0 && (
                <Alert severity="warning">
                  <Typography sx={{ fontWeight: 600, mb: 0.5 }}>Destination incomplete for importers</Typography>
                  <Typography sx={{ fontSize: '0.875rem' }}>
                    Importers can only use this warehouse on entries when all of the following are satisfied:
                  </Typography>
                  <ul style={{ margin: '0.5rem 0 0', paddingLeft: '1.25rem', fontSize: '0.875rem' }}>
                    {destinationBlockers.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                </Alert>
              )}
            </Stack>
          )}
        </Stack>
      </PortalPanel>

      <Dialog open={ltoOpen} onClose={() => setLtoOpen(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleLtoSubmit}>
          <DialogTitle>Apply for DA warehouse operating LTO</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              {ltoError && (
                <Alert severity="error">
                  {getApiErrorMessage(ltoError) ??
                    'Could not submit application. If this warehouse already has an LTO, DA must record changes on the warehouse detail page.'}
                </Alert>
              )}
              <TextField
                label="Operator legal name"
                value={operatorName}
                onChange={(e) => setOperatorName(e.target.value)}
                required
                fullWidth
              />
              <TextField label="Contact email" type="email" value={contactEmail} onChange={(e) => setContactEmail(e.target.value)} fullWidth />
              <TextField label="Contact phone" value={contactPhone} onChange={(e) => setContactPhone(e.target.value)} fullWidth />
              <TextField
                label="Notes for DA"
                value={ltoNotes}
                onChange={(e) => setLtoNotes(e.target.value)}
                multiline
                minRows={2}
                fullWidth
              />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setLtoOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={submittingLto || !operatorName.trim()}>
              Submit to DA
            </Button>
          </DialogActions>
        </form>
      </Dialog>

      <Dialog open={rsasOpen} onClose={() => setRsasOpen(false)} maxWidth="sm" fullWidth>
        <form onSubmit={handleRsasSubmit}>
          <DialogTitle>RSAS facility registration (operator)</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Facility owner name" value={ownerName} onChange={(e) => setOwnerName(e.target.value)} required fullWidth />
              <TextField
                label="RSAS reference number"
                value={rsasRef}
                onChange={(e) => setRsasRef(e.target.value)}
                fullWidth
                helperText="From rsas.da.gov.ph — submits for DA verification when provided."
              />
              <TextField
                label="Max capacity (kg)"
                type="number"
                value={capacityKg}
                onChange={(e) => setCapacityKg(e.target.value)}
                fullWidth
              />
              <FormControlLabel
                control={<Switch checked={bmbeExempt} onChange={(e) => setBmbeExempt(e.target.checked)} />}
                label="BMBE-exempt micro facility"
              />
              <Box>
                <Typography variant="subtitle2" sx={{ mb: 0.5 }}>Registered product scope (RSAS)</Typography>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                  Select what this cold storage may handle. Meat (BAI/pork) and fish (BFAR) cannot be combined — separate
                  facilities or chambers only.
                </Typography>
                {scopeViolatesSegregation(rsasProductScope) && (
                  <Alert severity="error" sx={{ mb: 1 }}>
                    Remove either meat or fish — they cannot be registered on the same warehouse.
                  </Alert>
                )}
                <Stack>
                  {WAREHOUSE_COMMODITY_CLASS_OPTIONS.map((opt) => (
                    <FormControlLabel
                      key={opt.value}
                      control={
                        <Checkbox
                          size="small"
                          checked={rsasProductScope.includes(opt.value)}
                          onChange={() =>
                            setRsasProductScope((prev) => toggleScopeClass(prev, opt.value, WAREHOUSE_COMMODITY_CLASS_OPTIONS.map((o) => o.value)))
                          }
                        />
                      }
                      label={opt.label}
                    />
                  ))}
                </Stack>
              </Box>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setRsasOpen(false)}>Cancel</Button>
            <Button
              type="submit"
              variant="contained"
              sx={portalPrimaryButtonSx}
              disabled={savingRsas || !ownerName.trim() || scopeViolatesSegregation(rsasProductScope)}
            >
              Save & submit
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  )
}
