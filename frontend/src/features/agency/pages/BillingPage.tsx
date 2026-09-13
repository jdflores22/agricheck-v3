import { useMemo, useState } from 'react'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
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
  Stack,
  Tab,
  Tabs,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import {
  useGetAgencyBillingsQuery,
  useGetAgencyDashboardQuery,
  useGetEntriesAwaitingBillingQuery,
  useGetPendingCashPaymentsQuery,
  useIssueBillingMutation,
  useMarkBillingPaidMutation,
  useVerifyBillingPaymentMutation,
  useVerifyCashPaymentMutation,
  type AgencyBillingItem,
  type AgencyEntryListItem,
} from '../api/agencyApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

const tabs = [
  { key: 'queue', label: 'Billing queue' },
  { key: 'billings', label: 'Billings' },
  { key: 'cash', label: 'Cash (OR)' },
]

const billingStatusFilters = [
  { key: 'all', label: 'All' },
  { key: 'Draft', label: 'Draft' },
  { key: 'Issued', label: 'Issued' },
  { key: 'PaymentPending', label: 'Payment pending' },
  { key: 'Paid', label: 'Paid' },
]

function formatMoney(amount: number) {
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(amount)
}

function formatStatus(status: string) {
  const labels: Record<string, string> = {
    Draft: 'Draft',
    Issued: 'Issued',
    PaymentPending: 'Payment pending',
    Paid: 'Paid',
  }
  return labels[status] ?? status.replace(/([a-z])([A-Z])/g, '$1 $2')
}

function ApplicantCell({ entry }: { entry: AgencyEntryListItem }) {
  return (
    <TableCell>
      <Typography variant="body2" sx={{ fontWeight: 600 }}>
        {entry.companyName || entry.applicantName}
      </Typography>
      {entry.companyName && (
        <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
          {entry.applicantName}
        </Typography>
      )}
    </TableCell>
  )
}

export function BillingPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const activeTab = searchParams.get('tab') ?? 'queue'
  const statusFilter = searchParams.get('status') ?? 'all'

  const { data: dashboardData, isLoading: dashboardLoading } = useGetAgencyDashboardQuery()
  const { data, isLoading: billingsLoading } = useGetAgencyBillingsQuery({})
  const { data: approvedData, isLoading: queueLoading } = useGetEntriesAwaitingBillingQuery({})
  const { data: pendingCashData, isLoading: cashLoading } = useGetPendingCashPaymentsQuery()

  const [issueBilling, { isLoading: issuing }] = useIssueBillingMutation()
  const [markPaid, { isLoading: markingPaid }] = useMarkBillingPaidMutation()
  const [verifyPayment, { isLoading: verifying }] = useVerifyBillingPaymentMutation()
  const [verifyCash, { isLoading: verifyingCash }] = useVerifyCashPaymentMutation()

  const [verifyTarget, setVerifyTarget] = useState<AgencyBillingItem | null>(null)
  const [issueTarget, setIssueTarget] = useState<AgencyBillingItem | null>(null)
  const [verifyNotes, setVerifyNotes] = useState('')
  const [message, setMessage] = useState<{ severity: 'success' | 'error' | 'info'; text: string } | null>(null)

  const dashboard = dashboardData?.data
  const items = data?.data?.items ?? []
  const approvedEntries = approvedData?.data?.items ?? []
  const pendingCash = pendingCashData?.data ?? []

  const tabIndex = useMemo(() => {
    const index = tabs.findIndex((tab) => tab.key === activeTab)
    return index >= 0 ? index : 0
  }, [activeTab])

  const filteredBillings = useMemo(() => {
    if (statusFilter === 'all') return items
    return items.filter((bill) => bill.status === statusFilter)
  }, [items, statusFilter])

  const handleVerify = async (approved: boolean) => {
    if (!verifyTarget) return
    try {
      await verifyPayment({ uuid: verifyTarget.uuid, approved, notes: verifyNotes.trim() || undefined }).unwrap()
      setMessage({
        severity: approved ? 'success' : 'info',
        text: approved ? 'Payment approved.' : 'Payment rejected.',
      })
      setVerifyTarget(null)
      setVerifyNotes('')
    } catch {
      setMessage({ severity: 'error', text: 'Unable to verify payment.' })
    }
  }

  const handleConfirmIssue = async () => {
    if (!issueTarget) return
    try {
      await issueBilling(issueTarget.uuid).unwrap()
      setMessage({ severity: 'success', text: `${issueTarget.billNumber} issued to client.` })
      setIssueTarget(null)
    } catch {
      setMessage({ severity: 'error', text: 'Unable to issue billing.' })
    }
  }

  const handleMarkPaid = async (uuid: string, billNumber: string) => {
    try {
      await markPaid(uuid).unwrap()
      setMessage({ severity: 'success', text: `${billNumber} marked as paid.` })
    } catch {
      setMessage({ severity: 'error', text: 'Unable to mark billing as paid.' })
    }
  }

  const handleCashVerify = async (billUuid: string, approved: boolean) => {
    try {
      await verifyCash({ billUuid, approved }).unwrap()
      setMessage({
        severity: approved ? 'success' : 'info',
        text: approved ? 'Cash payment approved.' : 'Cash payment rejected.',
      })
    } catch {
      setMessage({ severity: 'error', text: 'Unable to verify cash payment.' })
    }
  }

  const tabLabel = (label: string, count?: number) =>
    count && count > 0 ? `${label} (${count})` : label

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Billing"
        title="Agency Billing"
        subtitle={
          dashboard
            ? `${dashboard.agencyName} — create bills for approved entries, issue to clients, and verify payments`
            : 'Create bills for approved entries, issue to clients, and verify payments'
        }
        actions={
          <>
            <Button
              component={RouterLink}
              to="/agency/billing/create"
              variant="contained"
              sx={portalPrimaryButtonSx}
            >
              Create billing
            </Button>
            <Button component={RouterLink} to="/agency/billing-reports" variant="outlined" sx={portalOutlinedButtonSx}>
              Revenue reports
            </Button>
          </>
        }
      />

      {message && (
        <Alert severity={message.severity} sx={{ mb: 2 }} onClose={() => setMessage(null)}>
          {message.text}
        </Alert>
      )}

      <Box
        sx={{
          mb: 3,
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, 1fr)' },
        }}
      >
        <PortalStatCard
          label="Awaiting billing"
          value={dashboardLoading ? '…' : String(dashboard?.awaitingBilling ?? approvedEntries.length)}
          caption="Evaluator-approved entries"
        />
        <PortalStatCard
          label="Open billings"
          value={dashboardLoading ? '…' : String(dashboard?.openBillings ?? 0)}
          caption="Draft, issued, or pending payment"
        />
        <PortalStatCard
          label="Cash (OR) pending"
          value={dashboardLoading ? '…' : String(dashboard?.pendingCashPayments ?? pendingCash.length)}
          caption="Kahera submissions to verify"
        />
        <PortalStatCard
          label="Paid billings"
          value={dashboardLoading ? '…' : String(dashboard?.paidBillings ?? 0)}
          caption="Completed agency revenue"
        />
      </Box>

      <Tabs
        value={tabIndex}
        onChange={(_, index) => {
          const nextTab = tabs[index]?.key ?? 'queue'
          const params: Record<string, string> = { tab: nextTab }
          if (nextTab === 'billings' && statusFilter !== 'all') params.status = statusFilter
          setSearchParams(params)
        }}
        sx={{
          mb: 2,
          borderBottom: `1px solid ${portalColors.border}`,
          '& .MuiTab-root': { textTransform: 'none', fontWeight: 500, minHeight: 44 },
        }}
      >
        <Tab label={tabLabel('Billing queue', dashboard?.awaitingBilling ?? approvedEntries.length)} />
        <Tab label={tabLabel('Billings', dashboard?.openBillings)} />
        <Tab label={tabLabel('Cash (OR)', pendingCash.length)} />
      </Tabs>

      {activeTab === 'queue' && (
        <PortalTablePanel
          title="Approved entries awaiting billing"
          columns={['Reference', 'Applicant', 'Commodity', 'Submitted', 'Action']}
          isLoading={queueLoading}
          isEmpty={!queueLoading && approvedEntries.length === 0}
          emptyMessage="No evaluator-approved entries waiting for billing."
        >
          {approvedEntries.map((entry) => (
            <TableRow key={entry.uuid} hover>
              <TableCell>
                <Typography sx={{ fontWeight: 600 }}>{entry.referenceNo}</Typography>
              </TableCell>
              <ApplicantCell entry={entry} />
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
      )}

      {activeTab === 'billings' && (
        <Box>
          <Stack direction="row" useFlexGap sx={{ mb: 2, gap: 1, flexWrap: 'wrap' }}>
            {billingStatusFilters.map((filter) => (
              <Chip
                key={filter.key}
                label={filter.label}
                clickable
                aria-pressed={statusFilter === filter.key}
                onClick={() => setSearchParams({ tab: 'billings', status: filter.key })}
                sx={{
                  fontWeight: 500,
                  bgcolor: statusFilter === filter.key ? portalColors.primary : portalColors.bgMuted,
                  color: statusFilter === filter.key ? '#fff' : portalColors.textDark,
                  '&:hover': {
                    bgcolor: statusFilter === filter.key ? portalColors.primaryDark : portalColors.bgMuted,
                  },
                }}
              />
            ))}
          </Stack>

          <PortalTablePanel
            title="Billing records"
            columns={['Bill #', 'Entry', 'Summary', 'Amount', 'Status', 'Actions']}
            isLoading={billingsLoading}
            isEmpty={!billingsLoading && filteredBillings.length === 0}
            emptyMessage={
              statusFilter === 'all'
                ? 'No billing records yet.'
                : `No ${formatStatus(statusFilter).toLowerCase()} billings.`
            }
          >
            {filteredBillings.map((bill) => (
              <TableRow key={bill.uuid} hover>
                <TableCell>
                  <Typography
                    component={RouterLink}
                    to={`/agency/billing/${bill.uuid}`}
                    sx={{
                      fontWeight: 600,
                      color: portalColors.primary,
                      textDecoration: 'none',
                      '&:hover': { textDecoration: 'underline' },
                    }}
                  >
                    {bill.billNumber}
                  </Typography>
                </TableCell>
                <TableCell>{bill.entryReferenceNo ?? '—'}</TableCell>
                <TableCell>
                  <Typography variant="body2">{bill.description}</Typography>
                  {(bill.charges?.length ?? 0) > 0 && (
                    <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
                      {bill.charges.length} charge{bill.charges.length === 1 ? '' : 's'}
                    </Typography>
                  )}
                </TableCell>
                <TableCell sx={{ fontVariantNumeric: 'tabular-nums' }}>{formatMoney(bill.amount)}</TableCell>
                <TableCell>
                  <Chip size="small" label={formatStatus(bill.status)} sx={portalStatusChipSx(bill.status)} />
                </TableCell>
                <TableCell align="right">
                  <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end', flexWrap: 'wrap' }}>
                    <Button
                      component={RouterLink}
                      to={`/agency/billing/${bill.uuid}`}
                      size="small"
                      variant="outlined"
                      sx={portalOutlinedButtonSx}
                    >
                      {bill.status === 'Draft' ? 'View / edit' : 'View'}
                    </Button>
                    {bill.status === 'Draft' && (
                      <Button
                        size="small"
                        variant="contained"
                        sx={portalPrimaryButtonSx}
                        disabled={issuing}
                        onClick={() => setIssueTarget(bill)}
                      >
                        Issue
                      </Button>
                    )}
                    {bill.status === 'Issued' && (
                      <Button
                        size="small"
                        variant="outlined"
                        sx={portalOutlinedButtonSx}
                        disabled={markingPaid}
                        onClick={() => handleMarkPaid(bill.uuid, bill.billNumber)}
                      >
                        Mark paid
                      </Button>
                    )}
                    {bill.status === 'PaymentPending' && (
                      <Button
                        size="small"
                        variant="contained"
                        sx={portalPrimaryButtonSx}
                        onClick={() => setVerifyTarget(bill)}
                      >
                        Verify payment
                      </Button>
                    )}
                    {!['Draft', 'Issued', 'PaymentPending'].includes(bill.status) && (
                      <Typography variant="body2" sx={{ color: portalColors.textMuted }}>—</Typography>
                    )}
                  </Stack>
                </TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
        </Box>
      )}

      {activeTab === 'cash' && (
        <PortalTablePanel
          title="Pending cash payments (kahera)"
          columns={['Bill #', 'Entry', 'Client', 'OR #', 'Amount', 'Submitted', 'Actions']}
          isLoading={cashLoading}
          isEmpty={!cashLoading && pendingCash.length === 0}
          emptyMessage="No cash payments awaiting OR verification."
        >
          {pendingCash.map((item) => (
            <TableRow key={item.billUuid} hover>
              <TableCell>
                <Typography sx={{ fontWeight: 600 }}>{item.billNumber}</Typography>
              </TableCell>
              <TableCell>{item.entryReferenceNo ?? '—'}</TableCell>
              <TableCell>{item.clientName}</TableCell>
              <TableCell>{item.externalReference ?? '—'}</TableCell>
              <TableCell sx={{ fontVariantNumeric: 'tabular-nums' }}>{formatMoney(item.amount)}</TableCell>
              <TableCell>{new Date(item.submittedAt).toLocaleDateString()}</TableCell>
              <TableCell align="right">
                <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                  <Button
                    size="small"
                    variant="contained"
                    sx={portalPrimaryButtonSx}
                    disabled={verifyingCash}
                    onClick={() => handleCashVerify(item.billUuid, true)}
                  >
                    Approve
                  </Button>
                  <Button
                    size="small"
                    color="error"
                    variant="outlined"
                    disabled={verifyingCash}
                    onClick={() => handleCashVerify(item.billUuid, false)}
                  >
                    Reject
                  </Button>
                </Stack>
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      )}

      <Dialog
        aria-labelledby="issue-billing-dialog-title"
        open={Boolean(issueTarget)}
        onClose={() => !issuing && setIssueTarget(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle id="issue-billing-dialog-title">Issue billing to client</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted, mb: 2 }}>
            Issue <strong>{issueTarget?.billNumber}</strong>
            {issueTarget?.entryReferenceNo ? ` for entry ${issueTarget.entryReferenceNo}` : ''} to the client?
            After issuing, the bill can no longer be edited and the client will be notified to pay.
          </Typography>

          {issueTarget?.description && (
            <Box sx={{ mb: 2 }}>
              <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
                Bill title
              </Typography>
              <Typography sx={{ fontWeight: 600 }}>{issueTarget.description}</Typography>
            </Box>
          )}

          {(issueTarget?.charges?.length ?? 0) > 0 && (
            <Stack spacing={1} sx={{ mb: 2 }}>
              {issueTarget!.charges.map((charge) => (
                <Box
                  key={charge.uuid}
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
          )}

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
              {formatMoney(issueTarget?.amount ?? 0)}
            </Typography>
          </Box>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setIssueTarget(null)} disabled={issuing} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={handleConfirmIssue} disabled={issuing}>
            {issuing ? <CircularProgress size={20} color="inherit" /> : 'Yes, issue to client'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog
        aria-labelledby="verify-payment-dialog-title"
        open={Boolean(verifyTarget)}
        onClose={() => !verifying && setVerifyTarget(null)}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle id="verify-payment-dialog-title">Verify client payment</DialogTitle>
        <DialogContent>
          <Typography sx={{ mb: 2, color: portalColors.textMuted }}>
            Review the uploaded payment proof for <strong>{verifyTarget?.billNumber}</strong>
            {verifyTarget?.entryReferenceNo ? ` (${verifyTarget.entryReferenceNo})` : ''}.
          </Typography>
          <TextField
            label="Notes (optional)"
            value={verifyNotes}
            onChange={(e) => setVerifyNotes(e.target.value)}
            fullWidth
            multiline
            minRows={2}
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button
            onClick={() => {
              setVerifyTarget(null)
              setVerifyNotes('')
            }}
            disabled={verifying}
            sx={portalOutlinedButtonSx}
          >
            Cancel
          </Button>
          <Button color="error" onClick={() => handleVerify(false)} disabled={verifying}>
            Reject
          </Button>
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => handleVerify(true)} disabled={verifying}>
            {verifying ? 'Saving…' : 'Approve'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
