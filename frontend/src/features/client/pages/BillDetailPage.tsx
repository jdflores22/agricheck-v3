import { Alert, Box, Button, Chip, Stack, TextField, Typography } from '@mui/material'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useRef, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useConfirmBillPaymentMutation,
  useGetBillPaymentOptionsQuery,
  useGetBillQuery,
  useInitiateBillPaymentMutation,
} from '../api/clientApi'
import { downloadAuthenticatedFile } from '../utils/downloadFile'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(amount)
}

export function BillDetailPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const { data, isLoading, refetch } = useGetBillQuery(uuid, { skip: !uuid })
  const { data: optionsData } = useGetBillPaymentOptionsQuery(uuid, { skip: !uuid })
  const [initiatePayment, { isLoading: paying }] = useInitiateBillPaymentMutation()
  const [confirmPayment, { isLoading: syncing }] = useConfirmBillPaymentMutation()
  const [orNumber, setOrNumber] = useState('')
  const [cashSubmitted, setCashSubmitted] = useState(false)
  const syncAttempted = useRef(false)
  const bill = data?.data
  const options = optionsData?.data

  useBreadcrumbLabel(bill?.billNumber)

  useEffect(() => {
    if (!bill || bill.status === 'Paid' || syncAttempted.current) return

    const hasPendingOnlinePayment = bill.payments?.some(
      (payment) => payment.status === 'pending' && payment.paymentMethod !== 'cash',
    )
    if (!hasPendingOnlinePayment) return

    syncAttempted.current = true

    confirmPayment(uuid)
      .unwrap()
      .then((result) => {
        if (result.success && result.data.status === 'Paid') {
          refetch()
        }
      })
      .catch(() => {
        // Gateway sync failed; bill detail still shows current state.
      })
  }, [bill, confirmPayment, refetch, uuid])

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading bill…</Typography>
  if (!bill) return <Alert severity="error">Bill not found.</Alert>

  const awaitingCashVerification = bill.payments?.some(
    (payment) => payment.paymentMethod === 'cash' && payment.status === 'awaiting_verification',
  )

  const handlePayOnline = async () => {
    const result = await initiatePayment({
      uuid,
      paymentMethod: 'any',
      returnBaseUrl: window.location.origin,
    }).unwrap()
    if (result.success && result.data.paymentUrl) {
      window.location.href = result.data.paymentUrl
      return
    }
    refetch()
  }

  const handlePayCash = async () => {
    if (!orNumber.trim()) return
    await initiatePayment({
      uuid,
      paymentMethod: 'cash',
      paymentReference: orNumber.trim(),
    }).unwrap()
    setCashSubmitted(true)
    refetch()
  }

  const handleSyncPayment = async () => {
    try {
      const result = await confirmPayment(uuid).unwrap()
      if (result.success && result.data.status === 'Paid') {
        refetch()
      }
    } catch {
      // Keep bill as pending; user can retry or pay again.
    }
  }

  return (
    <Box sx={{ maxWidth: 640 }}>
      <PortalPageHeader
        eyebrow="Payment"
        title={bill.billNumber}
        subtitle={bill.description}
        actions={
          <Button component={RouterLink} to="/client/bills" variant="outlined" sx={portalOutlinedButtonSx}>
            Back to bills
          </Button>
        }
      />
      <Chip size="small" label={bill.status} sx={{ mb: 2, ...portalStatusChipSx(bill.status) }} />
      <PortalPanel title="Bill Details">
        <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
          <Typography variant="body2">Amount: <strong>{formatCurrency(bill.amount)}</strong></Typography>
          <Typography variant="body2">Due: {bill.dueDate ? new Date(bill.dueDate).toLocaleDateString() : 'No due date'}</Typography>
          {bill.entryReferenceNo && (
            <Typography variant="body2">
              Entry:{' '}
              <Button size="small" component={RouterLink} to={`/client/entries/${bill.entryUuid}`}>
                {bill.entryReferenceNo}
              </Button>
            </Typography>
          )}

          {bill.status !== 'Paid' && !awaitingCashVerification && !cashSubmitted && (
            <Stack spacing={2} sx={{ mt: 1 }}>
              {options?.payMongoEnabled && (
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                  <Button variant="contained" sx={portalPrimaryButtonSx} disabled={paying} onClick={handlePayOnline}>
                    Pay Online (PayMongo)
                  </Button>
                  <Button variant="outlined" sx={portalOutlinedButtonSx} disabled={syncing} onClick={handleSyncPayment}>
                    {syncing ? 'Checking…' : 'Check Payment Status'}
                  </Button>
                </Stack>
              )}

              {options?.cashPaymentEnabled && (
                <Box sx={{ pt: options?.payMongoEnabled ? 1 : 0 }}>
                  <Typography variant="subtitle2" sx={{ mb: 1 }}>Pay at Agency Office (Cash)</Typography>
                  {options.cashPaymentInstructions && (
                    <Alert severity="info" sx={{ mb: 1.5, borderRadius: '0.65rem' }}>
                      {options.cashPaymentInstructions}
                    </Alert>
                  )}
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                    <TextField
                      label="Official Receipt (OR) Number"
                      value={orNumber}
                      onChange={(e) => setOrNumber(e.target.value)}
                      size="small"
                      fullWidth
                      placeholder="Enter OR number from cashier"
                    />
                    <Button
                      variant="outlined"
                      sx={portalOutlinedButtonSx}
                      disabled={paying || !orNumber.trim()}
                      onClick={handlePayCash}
                    >
                      Submit Cash Payment
                    </Button>
                  </Stack>
                </Box>
              )}
            </Stack>
          )}

          {(awaitingCashVerification || cashSubmitted) && bill.status !== 'Paid' && (
            <Alert severity="success" sx={{ mt: 1, borderRadius: '0.65rem' }}>
              Cash payment submitted. The agency cashier will verify your OR number. You will be notified once confirmed.
            </Alert>
          )}

          {bill.status === 'Paid' && (
            <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
              <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={() => navigate('/client/payment-history')}>
                View payment history
              </Button>
              <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={() => downloadAuthenticatedFile(`/client/bills/${uuid}/receipt`, `receipt-${bill.billNumber}.pdf`)}>
                Download receipt
              </Button>
            </Stack>
          )}
        </Stack>
      </PortalPanel>
    </Box>
  )
}
