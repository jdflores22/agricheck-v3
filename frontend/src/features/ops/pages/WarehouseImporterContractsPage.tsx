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
  FormControlLabel,
  Checkbox,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetWarehouseComplianceFacilitiesQuery,
  useGetWarehouseImporterContractsQuery,
  useUpsertWarehouseImporterContractMutation,
  type WarehouseImporterContractListItem,
} from '../api/opsApi'
import { getApiErrorMessage } from '../utils/apiErrorMessage'
import {
  defaultContractScope,
  isClassAllowedAtFacility,
  toggleScopeClass,
  WAREHOUSE_COMMODITY_CLASS_OPTIONS,
} from '../utils/warehouseProductScope'

function formatMassKg(kg: number | null | undefined): string {
  if (kg == null || Number.isNaN(kg)) return '—'
  if (kg >= 1000) return `${(kg / 1000).toLocaleString(undefined, { maximumFractionDigits: 2 })} MT`
  return `${Math.round(kg).toLocaleString()} kg`
}

function contractStatusTone(status: string | null | undefined): 'success' | 'warning' | 'default' {
  if (!status) return 'warning'
  if (status === 'Active') return 'success'
  if (status === 'Suspended' || status === 'Expired') return 'warning'
  return 'default'
}

type EditDraft = {
  allocationUuid: string | null
  warehouseFacilityId: number
  importerUserUuid: string
  importerLabel: string
  committedKg: number
  contractReference: string
  allocatedMt: string
  allowedClasses: string[]
  status: string
  facilityAllowed: string[]
  facilityScopeHint?: string | null
}

export function WarehouseImporterContractsPage() {
  const { data: facilitiesData } = useGetWarehouseComplianceFacilitiesQuery()
  const facilities = facilitiesData?.data ?? []
  const [facilityFilter, setFacilityFilter] = useState<number | ''>('')

  const singleFacility = facilities.length === 1 ? facilities[0] : null
  const activeFacilityId =
    facilityFilter === '' ? (singleFacility?.warehouseFacilityId ?? undefined) : facilityFilter

  const { data, isLoading } = useGetWarehouseImporterContractsQuery(activeFacilityId)
  const rows = data?.data ?? []

  const [editOpen, setEditOpen] = useState(false)
  const [editDraft, setEditDraft] = useState<EditDraft | null>(null)
  const [upsertContract, { isLoading: saving, error: saveError, reset: resetSave }] =
    useUpsertWarehouseImporterContractMutation()

  const showFacilityColumn = facilities.length > 1 && facilityFilter === ''

  const summary = useMemo(() => {
    const withContract = rows.filter((r) => r.allocatedCapacityKg != null)
    const totalAllocated = withContract.reduce((sum, r) => sum + (r.allocatedCapacityKg ?? 0), 0)
    const totalCommitted = rows.reduce((sum, r) => sum + r.committedKg, 0)
    return { importers: rows.length, withContract: withContract.length, totalAllocated, totalCommitted }
  }, [rows])

  const openEdit = (row: WarehouseImporterContractListItem) => {
    resetSave()
    const defaultRef =
      row.contractReference?.trim()
      || `CTR-${row.warehouseCode}-${new Date().getFullYear()}`
    setEditDraft({
      allocationUuid: row.allocationUuid ?? null,
      warehouseFacilityId: row.warehouseFacilityId,
      importerUserUuid: row.importerUserUuid,
      importerLabel: `${row.importerDisplayName} (${row.importerEmail})`,
      committedKg: row.committedKg,
      contractReference: defaultRef,
      allocatedMt: row.allocatedCapacityKg
        ? String(row.allocatedCapacityKg / 1000)
        : '',
      allowedClasses: row.allowedCommodityClasses.length > 0
        ? [...row.allowedCommodityClasses]
        : defaultContractScope(row.facilityAllowedCommodityClasses ?? []),
      status: row.allocationStatus ?? 'Active',
      facilityAllowed: row.facilityAllowedCommodityClasses ?? [],
      facilityScopeHint: row.facilityScopeHint,
    })
    setEditOpen(true)
  }

  const closeEdit = () => {
    setEditOpen(false)
    setEditDraft(null)
    resetSave()
  }

  const handleSave = async (e: FormEvent) => {
    e.preventDefault()
    if (!editDraft) return

    const mt = Number(editDraft.allocatedMt)
    if (!Number.isFinite(mt) || mt <= 0) return

    const allocatedCapacityKg = mt * 1000
    if (allocatedCapacityKg < editDraft.committedKg) return
    if (editDraft.allowedClasses.length === 0) return

    await upsertContract({
      allocationUuid: editDraft.allocationUuid,
      warehouseFacilityId: editDraft.warehouseFacilityId,
      importerUserUuid: editDraft.importerUserUuid,
      contractReference: editDraft.contractReference.trim(),
      allocatedCapacityKg,
      allowedCommodityClasses: editDraft.allowedClasses,
      status: editDraft.status,
    }).unwrap()

    closeEdit()
  }

  const saveErrorMessage = getApiErrorMessage(saveError)
  const draftMt = editDraft ? Number(editDraft.allocatedMt) : 0
  const draftBelowCommitted =
    editDraft != null && Number.isFinite(draftMt) && draftMt > 0 && draftMt * 1000 < editDraft.committedKg

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Importer storage contracts"
        subtitle="Importers and consignees linked to your warehouse, with capacity you allocated and volume already committed on entries."
      />

      {facilities.length > 1 && (
        <Box sx={{ mb: 2, maxWidth: 360 }}>
          <TextField
            select
            fullWidth
            size="small"
            label="Warehouse facility"
            value={facilityFilter}
            onChange={(e) => {
              const v = e.target.value
              setFacilityFilter(v === '' ? '' : Number(v))
            }}
          >
            <MenuItem value="">All my facilities</MenuItem>
            {facilities.map((f) => (
              <MenuItem key={f.warehouseFacilityId} value={f.warehouseFacilityId}>
                {f.warehouseCode} — {f.warehouseName}
              </MenuItem>
            ))}
          </TextField>
        </Box>
      )}

      <Box sx={{ mb: 2, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
        <Chip size="small" label={`${summary.importers} importer link(s)`} />
        <Chip size="small" label={`${summary.withContract} with contract`} />
        <Chip size="small" label={`Allocated: ${formatMassKg(summary.totalAllocated)}`} />
        <Chip size="small" label={`Committed: ${formatMassKg(summary.totalCommitted)}`} />
      </Box>

      <PortalTablePanel
        title="Contracts & linked importers"
        columns={[
          ...(showFacilityColumn ? ['Facility'] : []),
          'Importer / consignee',
          'Email',
          'Contract ref.',
          'Allocated',
          'Committed',
          'Headroom',
          'Product scope',
          'Status',
          '',
        ]}
        isLoading={isLoading}
        isEmpty={!isLoading && rows.length === 0}
        emptyMessage="No importer links or storage contracts yet. Importers link your warehouse under My Facilities; then set capacity here."
      >
        {rows.map((row) => {
          const key = `${row.warehouseFacilityId}-${row.importerUserUuid}-${row.allocationUuid ?? 'link'}`
          const overCapacity =
            row.allocatedCapacityKg != null && row.committedKg > row.allocatedCapacityKg
          return (
            <TableRow key={key} hover>
              {showFacilityColumn && (
                <TableCell>
                  <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>{row.warehouseCode}</Typography>
                  <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{row.warehouseName}</Typography>
                </TableCell>
              )}
              <TableCell>
                <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>{row.importerDisplayName}</Typography>
                {!row.contractReference && (
                  <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>Linked — no contract on file</Typography>
                )}
              </TableCell>
              <TableCell>{row.importerEmail}</TableCell>
              <TableCell>{row.contractReference ?? '—'}</TableCell>
              <TableCell>{formatMassKg(row.allocatedCapacityKg)}</TableCell>
              <TableCell sx={{ color: overCapacity ? 'error.main' : undefined, fontWeight: overCapacity ? 600 : 400 }}>
                {formatMassKg(row.committedKg)}
              </TableCell>
              <TableCell sx={{ color: (row.headroomKg ?? 0) < 0 ? 'error.main' : undefined }}>
                {row.allocatedCapacityKg != null ? formatMassKg(row.headroomKg) : '—'}
              </TableCell>
              <TableCell>
                {row.allowedCommodityClasses.length > 0
                  ? row.allowedCommodityClasses.join(', ')
                  : '—'}
              </TableCell>
              <TableCell>
                {row.allocationStatus ? (
                  <Chip size="small" label={row.allocationStatus} color={contractStatusTone(row.allocationStatus)} />
                ) : (
                  <Chip size="small" label="No contract" color="warning" variant="outlined" />
                )}
              </TableCell>
              <TableCell align="right">
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<EditOutlinedIcon />}
                  onClick={() => openEdit(row)}
                  sx={portalOutlinedButtonSx}
                >
                  {row.allocationUuid ? 'Edit' : 'Set capacity'}
                </Button>
              </TableCell>
            </TableRow>
          )
        })}
      </PortalTablePanel>

      <Dialog open={editOpen} onClose={closeEdit} fullWidth maxWidth="sm">
        <Box component="form" onSubmit={handleSave}>
          <DialogTitle>
            {editDraft?.allocationUuid ? 'Update storage contract' : 'Create storage contract'}
          </DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              {saveErrorMessage && (
                <Alert severity="error">{saveErrorMessage}</Alert>
              )}
              {editDraft && (
                <Typography variant="body2" color="text.secondary">
                  Importer: {editDraft.importerLabel}
                </Typography>
              )}
              {editDraft && editDraft.committedKg > 0 && (
                <Alert severity="info">
                  Already committed on entries: {formatMassKg(editDraft.committedKg)}. Allocated capacity cannot be lower than this.
                </Alert>
              )}
              <TextField
                label="Contract reference"
                size="small"
                required
                value={editDraft?.contractReference ?? ''}
                onChange={(e) =>
                  setEditDraft((d) => (d ? { ...d, contractReference: e.target.value } : d))
                }
              />
              <TextField
                label="Allocated capacity (MT)"
                size="small"
                required
                type="number"
                slotProps={{ htmlInput: { min: 0, step: 0.01 } }}
                value={editDraft?.allocatedMt ?? ''}
                onChange={(e) =>
                  setEditDraft((d) => (d ? { ...d, allocatedMt: e.target.value } : d))
                }
                error={draftBelowCommitted}
                helperText={
                  draftBelowCommitted
                    ? `Minimum ${formatMassKg(editDraft?.committedKg)} based on committed entries.`
                    : 'Metric tons (MT) of storage headroom for this importer at your warehouse.'
                }
              />
              <TextField
                select
                label="Contract status"
                size="small"
                value={editDraft?.status ?? 'Active'}
                onChange={(e) =>
                  setEditDraft((d) => (d ? { ...d, status: e.target.value } : d))
                }
              >
                <MenuItem value="Active">Active</MenuItem>
                <MenuItem value="Suspended">Suspended</MenuItem>
                <MenuItem value="Expired">Expired</MenuItem>
                <MenuItem value="Pending">Pending</MenuItem>
              </TextField>
              {editDraft?.facilityScopeHint && (
                <Alert severity="info">{editDraft.facilityScopeHint}</Alert>
              )}
              {editDraft && editDraft.facilityAllowed.length === 0 && (
                <Alert severity="warning">
                  Warehouse product scope is not registered yet. Under Compliance (LTO / RSAS), declare whether this
                  facility is meat-only (BAI) or fish-only (BFAR). Meat and fish cannot be combined in one warehouse.
                </Alert>
              )}
              <Box>
                <Typography variant="subtitle2" sx={{ mb: 1 }}>Allowed product scope (contract)</Typography>
                <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 1 }}>
                  Must match your RSAS/CSW registration. Pork/meat and fish cannot be selected together.
                </Typography>
                <Stack>
                  {WAREHOUSE_COMMODITY_CLASS_OPTIONS.map((opt) => {
                    const facilityAllowed = editDraft?.facilityAllowed ?? []
                    const permitted = isClassAllowedAtFacility(opt.value, facilityAllowed)
                    return (
                      <FormControlLabel
                        key={opt.value}
                        control={
                          <Checkbox
                            size="small"
                            disabled={!permitted}
                            checked={editDraft?.allowedClasses.includes(opt.value) ?? false}
                            onChange={() => {
                              setEditDraft((d) => {
                                if (!d) return d
                                return {
                                  ...d,
                                  allowedClasses: toggleScopeClass(
                                    d.allowedClasses,
                                    opt.value,
                                    d.facilityAllowed,
                                  ),
                                }
                              })
                            }}
                          />
                        }
                        label={opt.label}
                      />
                    )
                  })}
                </Stack>
              </Box>
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={closeEdit} sx={portalOutlinedButtonSx}>Cancel</Button>
            <Button
              type="submit"
              variant="contained"
              disabled={
                saving
                || draftBelowCommitted
                || !editDraft?.contractReference.trim()
                || (editDraft?.allowedClasses.length ?? 0) === 0
              }
              sx={portalPrimaryButtonSx}
            >
              {saving ? 'Saving…' : 'Save contract'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
