import { useEffect, useMemo, useState } from 'react'
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
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
  IconButton,
  InputAdornment,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import AddOutlinedIcon from '@mui/icons-material/AddOutlined'
import DeleteOutlineOutlinedIcon from '@mui/icons-material/DeleteOutlineOutlined'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import {
  useCreateAgencyBillingMutation,
  useGetAgencyBillingDetailQuery,
  useGetBillingEntryContextQuery,
  useGetEntriesAwaitingBillingQuery,
  useUpdateAgencyBillingMutation,
  type AgencyBillingEntryContext,
  type AgencyEntryListItem,
} from '../api/agencyApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import type { ApiEnvelope } from '../../auth/types'

function getApiErrorCode(error: unknown): string | null {
  if (!error || typeof error !== 'object' || !('data' in error)) return null
  const envelope = (error as FetchBaseQueryError).data as ApiEnvelope<unknown> | undefined
  return envelope?.errors?.[0]?.code ?? null
}

type ChargeRow = {
  id: string
  description: string
  amount: string
}

function formatMoney(amount: number, currency = 'PHP') {
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency }).format(amount)
}

function formatBillingStatus(status: string) {
  const labels: Record<string, string> = {
    Draft: 'Draft',
    Issued: 'Issued',
    PaymentPending: 'Payment pending',
    Paid: 'Paid',
    Cancelled: 'Cancelled',
  }
  return labels[status] ?? status.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function formatQuantity(quantity: number, unit: string) {
  return `${quantity.toLocaleString(undefined, { maximumFractionDigits: 3 })} ${unit}`
}

function createChargeRow(description = '', amount = ''): ChargeRow {
  return {
    id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    description,
    amount,
  }
}

function InfoField({ label, value }: { label: string; value?: string | null }) {
  return (
    <Box>
      <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
        {label}
      </Typography>
      <Typography sx={{ fontWeight: label === 'Commodity' ? 600 : 400 }}>{value?.trim() || '—'}</Typography>
    </Box>
  )
}

function BillingEntryContextPanels({ context }: { context: AgencyBillingEntryContext }) {
  const commodity = context.commodity

  return (
    <Box
      sx={{
        display: 'grid',
        gap: 3,
        gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' },
      }}
    >
      <PortalPanel title="Entry & applicant">
        <Box
          sx={{
            px: 2.5,
            py: 2,
            display: 'grid',
            gap: 2,
            gridTemplateColumns: '1fr 1fr',
          }}
        >
          <InfoField label="Reference" value={context.referenceNo} />
          <InfoField label="Entry type" value={context.entryType} />
          <InfoField label="Applicant" value={context.companyName || context.applicantName} />
          {context.companyName && (
            <InfoField label="Contact person" value={context.applicantName} />
          )}
          <InfoField
            label="Submitted"
            value={context.submittedAt ? new Date(context.submittedAt).toLocaleString() : undefined}
          />
          <InfoField label="Payment status" value={context.paymentStatus} />
          {context.mavNo && <InfoField label="MAV no." value={context.mavNo} />}
          {context.importTrack && <InfoField label="Import track" value={context.importTrack} />}
        </Box>
      </PortalPanel>

      <PortalPanel title="Commodity & shipment">
        <Box sx={{ px: 2.5, py: 2 }}>
          {!commodity ? (
            <Typography sx={{ color: portalColors.textMuted, fontSize: '0.875rem' }}>
              No commodity details were captured for this entry.
            </Typography>
          ) : (
            <Box
              sx={{
                display: 'grid',
                gap: 2,
                gridTemplateColumns: '1fr 1fr',
              }}
            >
              <InfoField label="Commodity" value={commodity.commodityName} />
              <Box>
                <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
                  HS / commodity code
                </Typography>
                <Typography sx={{ fontWeight: 600 }}>
                  {commodity.hsCode?.trim() || commodity.commodityCode?.trim() || '—'}
                </Typography>
                {commodity.mavHsLabel && (
                  <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block', mt: 0.25 }}>
                    MAV library: {commodity.mavHsLabel}
                  </Typography>
                )}
              </Box>
              <InfoField label="Category" value={commodity.categoryName} />
              <InfoField label="Quantity" value={formatQuantity(commodity.quantity, commodity.unit)} />
              <InfoField label="Origin" value={commodity.originCountry} />
              <InfoField label="Destination" value={commodity.destinationCountry} />
              <InfoField label="Port of entry" value={commodity.portOfEntry} />
              {commodity.description && (
                <Box sx={{ gridColumn: '1 / -1' }}>
                  <InfoField label="Product description" value={commodity.description} />
                </Box>
              )}
            </Box>
          )}

          {context.micUtilizations.length > 0 && (
            <Box sx={{ mt: 2.5, pt: 2, borderTop: `1px solid ${portalColors.border}` }}>
              <Typography
                sx={{
                  mb: 1.5,
                  fontSize: '0.75rem',
                  fontWeight: 600,
                  letterSpacing: '0.08em',
                  textTransform: 'uppercase',
                  color: portalColors.textMuted,
                }}
              >
                MIC utilizations
              </Typography>
              <Stack spacing={1}>
                {context.micUtilizations.map((mic) => (
                  <Box
                    key={`${mic.certificateNumber}-${mic.utilizedAt}`}
                    sx={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      gap: 2,
                      flexWrap: 'wrap',
                      px: 1.5,
                      py: 1,
                      borderRadius: '0.5rem',
                      bgcolor: portalColors.bgMuted,
                    }}
                  >
                    <Box>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>
                        {mic.commodityName}
                      </Typography>
                      <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
                        MIC {mic.certificateNumber} · HS {mic.hsCode}
                      </Typography>
                    </Box>
                    <Typography variant="body2" sx={{ fontVariantNumeric: 'tabular-nums' }}>
                      {mic.volume.toLocaleString()} {commodity?.unit ?? 'kg'}
                    </Typography>
                  </Box>
                ))}
              </Stack>
            </Box>
          )}
        </Box>
      </PortalPanel>
    </Box>
  )
}

export function CreateBillingPage() {
  const navigate = useNavigate()
  const { entryUuid, billingUuid } = useParams()
  const isBillingDetailMode = Boolean(billingUuid)
  const { data, isLoading: listLoading } = useGetEntriesAwaitingBillingQuery(
    {},
    { skip: Boolean(entryUuid) || isBillingDetailMode },
  )
  const {
    data: contextData,
    isLoading: contextLoading,
    isError: contextError,
    error: contextQueryError,
  } = useGetBillingEntryContextQuery(entryUuid ?? '', { skip: !entryUuid || isBillingDetailMode })
  const {
    data: billingDetailData,
    isLoading: billingDetailLoading,
    isError: billingDetailError,
  } = useGetAgencyBillingDetailQuery(billingUuid ?? '', { skip: !billingUuid })
  const [createBilling, { isLoading: creating }] = useCreateAgencyBillingMutation()
  const [updateBilling, { isLoading: updating }] = useUpdateAgencyBillingMutation()

  const approvedEntries = data?.data?.items ?? []
  const billingDetail = billingDetailData?.data
  const billingContext = isBillingDetailMode ? billingDetail?.entry ?? null : contextData?.data
  const contextErrorCode = getApiErrorCode(contextQueryError)
  const billingAlreadyExists = contextErrorCode === 'BILLING_EXISTS'
  const isDraftBilling = billingDetail?.status === 'Draft'
  const isReadOnlyBilling = isBillingDetailMode && !isDraftBilling
  const isSaving = creating || updating

  const [title, setTitle] = useState('')
  const [charges, setCharges] = useState<ChargeRow[]>([createChargeRow('Agency service fee', '500')])
  const [error, setError] = useState('')
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [pendingCharges, setPendingCharges] = useState<
    { id: string; description: string; amount: number }[]
  >([])
  const [chargesInitializedFor, setChargesInitializedFor] = useState<string | null>(null)

  useBreadcrumbLabel(
    billingDetail?.billNumber ?? billingContext?.referenceNo ?? (isBillingDetailMode ? 'View billing' : 'Create billing'),
  )

  useEffect(() => {
    if (!billingUuid || !billingDetail || chargesInitializedFor === billingUuid) return

    setCharges(
      billingDetail.charges.length > 0
        ? billingDetail.charges.map((charge) => createChargeRow(charge.description, String(charge.amount)))
        : [createChargeRow()],
    )
    setTitle(billingDetail.description)
    setChargesInitializedFor(billingUuid)
  }, [billingDetail, billingUuid, chargesInitializedFor])

  useEffect(() => {
    if (!entryUuid || isBillingDetailMode || !billingContext || chargesInitializedFor === entryUuid) return

    const fee = billingContext.suggestedProcessingFee
    const commodityLabel = billingContext.commodity?.commodityName
    const hsCode = billingContext.commodity?.hsCode
    const defaultDescription = commodityLabel
      ? hsCode
        ? `${billingContext.entryType} service fee — ${hsCode} ${commodityLabel}`
        : `${billingContext.entryType} service fee — ${commodityLabel}`
      : `${billingContext.entryType} agency service fee`

    setCharges([createChargeRow(defaultDescription, fee ? String(fee) : '500')])
    setTitle('')
    setChargesInitializedFor(entryUuid)
  }, [billingContext, chargesInitializedFor, entryUuid, isBillingDetailMode])

  const totalAmount = useMemo(
    () => charges.reduce((sum, charge) => sum + (Number(charge.amount) || 0), 0),
    [charges],
  )

  const updateCharge = (id: string, patch: Partial<ChargeRow>) => {
    setCharges((current) => current.map((charge) => (charge.id === id ? { ...charge, ...patch } : charge)))
  }

  const addCharge = () => {
    setCharges((current) => [...current, createChargeRow()])
  }

  const removeCharge = (id: string) => {
    setCharges((current) => (current.length === 1 ? current : current.filter((charge) => charge.id !== id)))
  }

  const getNormalizedCharges = () =>
    charges
      .map((charge) => ({
        id: charge.id,
        description: charge.description.trim(),
        amount: Number(charge.amount),
      }))
      .filter((charge) => charge.description && charge.amount > 0)

  const handleOpenConfirm = () => {
    if (!billingContext) return

    const normalizedCharges = getNormalizedCharges()
    if (normalizedCharges.length === 0) {
      setError('Add at least one charge with a description and amount greater than zero.')
      return
    }

    setError('')
    setPendingCharges(normalizedCharges)
    setConfirmOpen(true)
  }

  const handleConfirmSave = async () => {
    if (pendingCharges.length === 0) return
    if (!isBillingDetailMode && !billingContext) return
    if (isBillingDetailMode && !billingUuid) return

    const chargePayload = pendingCharges.map(({ description, amount }) => ({ description, amount }))

    try {
      if (isBillingDetailMode) {
        await updateBilling({
          uuid: billingUuid!,
          title: title.trim() || undefined,
          charges: chargePayload,
        }).unwrap()
      } else {
        await createBilling({
          entryUuid: billingContext!.uuid,
          title: title.trim() || undefined,
          charges: chargePayload,
        }).unwrap()
      }
      setConfirmOpen(false)
      navigate('/agency/billing?tab=billings&status=Draft', { replace: true })
    } catch {
      setConfirmOpen(false)
      setError(
        isBillingDetailMode
          ? 'Unable to save billing changes. Please review the charges and try again.'
          : 'Unable to create billing. Please review the charges and try again.',
      )
    }
  }

  if (!entryUuid && !billingUuid) {
    return (
      <Box>
        <PortalPageHeader
          eyebrow="Billing"
          title="Create agency billing"
          subtitle="Select an evaluator-approved entry to start a new draft bill."
          action={{ label: 'Back to billing', to: '/agency/billing?tab=queue' }}
        />
        <PortalTablePanel
          title="Approved entries awaiting billing"
          columns={['Reference', 'Applicant', 'Commodity', 'Submitted', 'Action']}
          isLoading={listLoading}
          isEmpty={!listLoading && approvedEntries.length === 0}
          emptyMessage="No evaluator-approved entries waiting for billing."
        >
          {approvedEntries.map((entry: AgencyEntryListItem) => (
            <TableRow key={entry.uuid} hover>
              <TableCell>
                <Typography sx={{ fontWeight: 600 }}>{entry.referenceNo}</Typography>
              </TableCell>
              <TableCell>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {entry.companyName || entry.applicantName}
                </Typography>
              </TableCell>
              <TableCell>{entry.commodityName ?? '—'}</TableCell>
              <TableCell>
                {entry.submittedAt ? new Date(entry.submittedAt).toLocaleDateString() : '—'}
              </TableCell>
              <TableCell align="right">
                <Button
                  component={RouterLink}
                  to={`/agency/billing/create/${entry.uuid}`}
                  size="small"
                  variant="contained"
                  sx={portalPrimaryButtonSx}
                >
                  Create bill
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    )
  }

  if (contextLoading || billingDetailLoading) {
    return <Typography sx={{ color: portalColors.textMuted }}>Loading billing details…</Typography>
  }

  if (billingDetailError || (isBillingDetailMode && !billingDetail)) {
    return (
      <Box>
        <Alert severity="error" sx={{ mb: 2 }}>
          This billing record could not be loaded.
        </Alert>
        <Button component={RouterLink} to="/agency/billing?tab=billings" sx={portalOutlinedButtonSx}>
          Back to billings
        </Button>
      </Box>
    )
  }

  if (!isBillingDetailMode && (contextError || !billingContext)) {
    return (
      <Box>
        <Alert severity={billingAlreadyExists ? 'info' : 'error'} sx={{ mb: 2 }}>
          {billingAlreadyExists
            ? 'A draft bill already exists for this entry. Open the Billings tab to review or issue it.'
            : 'This entry is not available for billing. It may already have an active bill or is no longer approved.'}
        </Alert>
        <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
          {billingAlreadyExists && (
            <Button
              component={RouterLink}
              to="/agency/billing?tab=billings&status=Draft"
              variant="contained"
              sx={portalPrimaryButtonSx}
            >
              Open draft bill
            </Button>
          )}
          <Button component={RouterLink} to="/agency/billing/create" sx={portalOutlinedButtonSx}>
            Choose another entry
          </Button>
        </Stack>
      </Box>
    )
  }

  if (!billingContext) {
    return (
      <Box>
        <Alert severity="error" sx={{ mb: 2 }}>
          Entry details are not available for this billing record.
        </Alert>
        <Button component={RouterLink} to="/agency/billing?tab=billings" sx={portalOutlinedButtonSx}>
          Back to billings
        </Button>
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Billing"
        title={
          isBillingDetailMode
            ? isReadOnlyBilling
              ? `Billing ${billingDetail?.billNumber}`
              : `Edit draft bill ${billingDetail?.billNumber}`
            : 'Create agency billing'
        }
        subtitle={
          isBillingDetailMode
            ? isReadOnlyBilling
              ? billingContext.referenceNo
              : `Update draft bill for ${billingContext.referenceNo}. Review commodity details before saving changes.`
            : `Draft bill for ${billingContext.referenceNo}. Review commodity details before setting charges.`
        }
        actions={
          <>
            {isBillingDetailMode && billingDetail && (
              <Chip
                label={formatBillingStatus(billingDetail.status)}
                sx={{
                  alignSelf: 'center',
                  height: 28,
                  fontWeight: 600,
                  ...portalStatusChipSx(billingDetail.status),
                }}
              />
            )}
            <Button
              component={RouterLink}
              to="/agency/billing?tab=billings&status=Draft"
              variant="outlined"
              sx={portalOutlinedButtonSx}
            >
              Back to billing
            </Button>
          </>
        }
      />

      {error && (
        <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
          {error}
        </Alert>
      )}

      <Stack spacing={3}>
        <BillingEntryContextPanels context={billingContext} />

        {billingContext.suggestedProcessingFee && (
          <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
            Configured {billingContext.entryType.toLowerCase()} processing fee for this agency:{' '}
            <strong>
              {formatMoney(
                billingContext.suggestedProcessingFee,
                billingContext.suggestedFeeCurrency ?? 'PHP',
              )}
            </strong>
            . Use this as a reference when composing charges.
          </Alert>
        )}

        <PortalPanel title={isReadOnlyBilling ? 'Bill charges' : 'Bill charges'}>
          <Box sx={{ px: 2.5, py: 2 }}>
            {isReadOnlyBilling ? (
              <Stack spacing={1.5} sx={{ mb: 2 }}>
                {billingDetail?.description && (
                  <Box>
                    <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
                      Bill title
                    </Typography>
                    <Typography sx={{ fontWeight: 600 }}>{billingDetail.description}</Typography>
                  </Box>
                )}
                {charges.map((charge, index) => (
                  <Box
                    key={charge.id}
                    sx={{
                      display: 'flex',
                      justifyContent: 'space-between',
                      gap: 2,
                      px: 1.5,
                      py: 1,
                      borderRadius: '0.5rem',
                      bgcolor: portalColors.bgMuted,
                    }}
                  >
                    <Typography variant="body2">
                      {index + 1}. {charge.description}
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                      {formatMoney(Number(charge.amount) || 0)}
                    </Typography>
                  </Box>
                ))}
              </Stack>
            ) : (
              <>
                <TextField
                  label="Bill title (optional)"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  fullWidth
                  sx={{ mb: 2 }}
                  helperText="Leave blank to auto-generate from the charge list."
                />

                <Stack spacing={1.5}>
                  {charges.map((charge, index) => (
                    <Box
                      key={charge.id}
                      sx={{
                        display: 'grid',
                        gap: 1.5,
                        gridTemplateColumns: { xs: '1fr', md: 'minmax(0, 1fr) 180px 44px' },
                        alignItems: 'start',
                      }}
                    >
                      <TextField
                        label={`Charge ${index + 1}`}
                        value={charge.description}
                        onChange={(e) => updateCharge(charge.id, { description: e.target.value })}
                        fullWidth
                      />
                      <TextField
                        label="Amount"
                        type="number"
                        value={charge.amount}
                        onChange={(e) => updateCharge(charge.id, { amount: e.target.value })}
                        slotProps={{
                          input: {
                            startAdornment: <InputAdornment position="start">₱</InputAdornment>,
                          },
                        }}
                        fullWidth
                      />
                      <IconButton
                        aria-label={`Remove charge ${index + 1}`}
                        onClick={() => removeCharge(charge.id)}
                        disabled={charges.length === 1}
                        sx={{ mt: { xs: 0, md: 0.5 } }}
                      >
                        <DeleteOutlineOutlinedIcon />
                      </IconButton>
                    </Box>
                  ))}
                </Stack>

                <Button
                  startIcon={<AddOutlinedIcon />}
                  onClick={addCharge}
                  sx={{ mt: 2, textTransform: 'none', fontWeight: 600 }}
                >
                  Add charge
                </Button>
              </>
            )}

            <Box
              sx={{
                mt: 3,
                pt: 2,
                borderTop: `1px solid ${portalColors.border}`,
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                gap: 2,
                flexWrap: 'wrap',
              }}
            >
              <Box>
                <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
                  Total amount
                </Typography>
                <Typography sx={{ fontSize: '1.5rem', fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                  {formatMoney(totalAmount)}
                </Typography>
              </Box>
              <Stack direction="row" spacing={1} useFlexGap sx={{ flexWrap: 'wrap' }}>
                <Button component={RouterLink} to="/agency/billing?tab=billings&status=Draft" sx={portalOutlinedButtonSx}>
                  {isReadOnlyBilling ? 'Back to billings' : 'Cancel'}
                </Button>
                {!isReadOnlyBilling && (
                  <Button
                    variant="contained"
                    sx={portalPrimaryButtonSx}
                    disabled={isSaving || totalAmount <= 0}
                    onClick={handleOpenConfirm}
                  >
                    {isBillingDetailMode ? 'Save changes' : 'Save draft bill'}
                  </Button>
                )}
              </Stack>
            </Box>
          </Box>
        </PortalPanel>
      </Stack>

      <Dialog
        open={confirmOpen}
        onClose={() => !isSaving && setConfirmOpen(false)}
        aria-labelledby="confirm-billing-dialog-title"
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle id="confirm-billing-dialog-title">
          {isBillingDetailMode ? 'Confirm billing changes' : 'Confirm draft bill'}
        </DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted, mb: 2 }}>
            {isBillingDetailMode ? (
              <>
                Save changes to draft bill <strong>{billingDetail?.billNumber}</strong> for{' '}
                <strong>{billingContext.referenceNo}</strong>?
              </>
            ) : (
              <>
                Create a draft agency bill for <strong>{billingContext.referenceNo}</strong>? Review the charges
                below before saving.
              </>
            )}
          </Typography>

          {title.trim() && (
            <Box sx={{ mb: 2 }}>
              <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
                Bill title
              </Typography>
              <Typography sx={{ fontWeight: 600 }}>{title.trim()}</Typography>
            </Box>
          )}

          <Stack spacing={1} sx={{ mb: 2 }}>
            {pendingCharges.map((charge) => (
              <Box
                key={charge.id}
                sx={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  gap: 2,
                  px: 1.5,
                  py: 1,
                  borderRadius: '0.5rem',
                  bgcolor: portalColors.bgMuted,
                }}
              >
                <Typography variant="body2">{charge.description}</Typography>
                <Typography variant="body2" sx={{ fontWeight: 600, fontVariantNumeric: 'tabular-nums' }}>
                  {formatMoney(charge.amount)}
                </Typography>
              </Box>
            ))}
          </Stack>

          <Box
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              pt: 1.5,
              borderTop: `1px solid ${portalColors.border}`,
            }}
          >
            <Typography sx={{ fontWeight: 600 }}>Total</Typography>
            <Typography sx={{ fontSize: '1.125rem', fontWeight: 700, fontVariantNumeric: 'tabular-nums' }}>
              {formatMoney(pendingCharges.reduce((sum, charge) => sum + charge.amount, 0))}
            </Typography>
          </Box>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setConfirmOpen(false)} disabled={isSaving} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button
            onClick={handleConfirmSave}
            variant="contained"
            disabled={isSaving}
            sx={portalPrimaryButtonSx}
          >
            {isSaving ? (
              <CircularProgress size={20} color="inherit" />
            ) : isBillingDetailMode ? (
              'Yes, save changes'
            ) : (
              'Yes, save draft bill'
            )}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
