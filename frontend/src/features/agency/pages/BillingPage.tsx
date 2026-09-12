import { useState } from 'react'
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
  Typography,
} from '@mui/material'
import {
  useCreateAgencyBillingMutation,
  useGetAgencyBillingsQuery,
  useGetApprovedEntriesQuery,
  useIssueBillingMutation,
  useMarkBillingPaidMutation,
  useVerifyBillingPaymentMutation,
} from '../api/agencyApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function BillingPage() {
  const { data, isLoading } = useGetAgencyBillingsQuery({})
  const { data: approvedData } = useGetApprovedEntriesQuery({})
  const [createBilling] = useCreateAgencyBillingMutation()
  const [issueBilling] = useIssueBillingMutation()
  const [markPaid] = useMarkBillingPaidMutation()
  const [verifyPayment, { isLoading: verifying }] = useVerifyBillingPaymentMutation()
  const [selectedEntryUuid, setSelectedEntryUuid] = useState('')
  const [amount, setAmount] = useState('500')
  const [description, setDescription] = useState('Agency service fee')
  const [verifyTarget, setVerifyTarget] = useState<string | null>(null)
  const [verifyNotes, setVerifyNotes] = useState('')
  const [verifyMessage, setVerifyMessage] = useState('')
  const items = data?.data?.items ?? []
  const approvedEntries = approvedData?.data?.items ?? []

  const handleVerify = async (approved: boolean) => {
    if (!verifyTarget) return
    try {
      await verifyPayment({ uuid: verifyTarget, approved, notes: verifyNotes.trim() || undefined }).unwrap()
      setVerifyMessage(approved ? 'Payment approved.' : 'Payment rejected.')
      setVerifyTarget(null)
      setVerifyNotes('')
    } catch {
      setVerifyMessage('Unable to verify payment.')
    }
  }

  const handleCreate = async () => {
    if (!selectedEntryUuid) return
    await createBilling({
      entryUuid: selectedEntryUuid,
      amount: Number(amount),
      description,
    }).unwrap()
    setSelectedEntryUuid('')
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Agency Billing"
        subtitle="Create and manage billing for approved entries."
      />

      {verifyMessage && (
        <Alert severity="info" sx={{ mb: 2 }} onClose={() => setVerifyMessage('')}>{verifyMessage}</Alert>
      )}

      <PortalPanel title="Create billing for approved entry">
        <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
          {approvedEntries.length === 0 ? (
            <Typography variant="body2" sx={{ color: portalColors.textMuted }}>No approved entries available.</Typography>
          ) : (
            <Stack spacing={1}>
              {approvedEntries.map((entry) => (
                <Box key={entry.uuid} sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
                  <Button
                    size="small"
                    variant={selectedEntryUuid === entry.uuid ? 'contained' : 'outlined'}
                    sx={selectedEntryUuid === entry.uuid ? portalPrimaryButtonSx : undefined}
                    onClick={() => setSelectedEntryUuid(entry.uuid)}
                  >
                    {entry.referenceNo}
                  </Button>
                  <Typography variant="body2">{entry.applicantName}</Typography>
                </Box>
              ))}
            </Stack>
          )}
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField label="Amount" value={amount} onChange={(e) => setAmount(e.target.value)} />
            <TextField label="Description" value={description} onChange={(e) => setDescription(e.target.value)} fullWidth />
          </Stack>
          <Button variant="contained" sx={portalPrimaryButtonSx} disabled={!selectedEntryUuid} onClick={handleCreate}>
            Create Draft
          </Button>
        </Stack>
      </PortalPanel>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="All billing records"
          columns={['Bill #', 'Entry', 'Description', 'Amount', 'Status', 'Actions']}
          isLoading={isLoading}
          isEmpty={!isLoading && items.length === 0}
          emptyMessage="No billing records."
        >
          {items.map((bill) => (
            <TableRow key={bill.uuid} hover>
              <TableCell>{bill.billNumber}</TableCell>
              <TableCell>{bill.entryReferenceNo ?? '—'}</TableCell>
              <TableCell>{bill.description}</TableCell>
              <TableCell>₱{bill.amount.toLocaleString()}</TableCell>
              <TableCell>
                <Chip size="small" label={bill.status} sx={portalStatusChipSx(bill.status)} />
              </TableCell>
              <TableCell align="right">
                {bill.status === 'Draft' && (
                  <Button size="small" onClick={() => issueBilling(bill.uuid)}>Issue</Button>
                )}
                {bill.status === 'Issued' && (
                  <Button size="small" onClick={() => markPaid(bill.uuid)}>Mark Paid</Button>
                )}
                {bill.status === 'PaymentPending' && (
                  <Button size="small" onClick={() => setVerifyTarget(bill.uuid)}>Verify Payment</Button>
                )}
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Dialog open={Boolean(verifyTarget)} onClose={() => !verifying && setVerifyTarget(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Verify DA payment proof</DialogTitle>
        <DialogContent>
          <Typography sx={{ mb: 2, color: portalColors.textMuted }}>
            Review the uploaded payment proof and approve or reject the client submission.
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
          <Button onClick={() => setVerifyTarget(null)} disabled={verifying}>Cancel</Button>
          <Button color="error" onClick={() => handleVerify(false)} disabled={verifying}>Reject</Button>
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => handleVerify(true)} disabled={verifying}>
            {verifying ? 'Saving…' : 'Approve'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
