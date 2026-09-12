import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { useEffect, useRef } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useConfirmBillPaymentMutation, useGetBillQuery, useInitiateBillPaymentMutation } from '../api/clientApi'
import { downloadAuthenticatedFile } from '../utils/downloadFile'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(amount)
}

export function BillDetailPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const { data, isLoading, refetch } = useGetBillQuery(uuid, { skip: !uuid })
  const [initiatePayment, { isLoading: paying }] = useInitiateBillPaymentMutation()
  const [confirmPayment, { isLoading: syncing }] = useConfirmBillPaymentMutation()
  const syncAttempted = useRef(false)
  const bill = data?.data

  useBreadcrumbLabel(bill?.billNumber)

  useEffect(() => {
    if (!bill || bill.status === 'Paid' || syncAttempted.current) return
    syncAttempted.current = true

    confirmPayment(uuid)
      .unwrap()
      .then((result) => {
        if (result.success && result.data.status === 'Paid') {
          refetch()
        }
      })
      .catch(() => {
        // Ignore when PayMongo payment is still pending.
      })
  }, [bill, confirmPayment, refetch, uuid])

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading bill…</Typography>
  if (!bill) return <Alert severity="error">Bill not found.</Alert>

  const handlePay = async () => {
    const result = await initiatePayment({ uuid, paymentMethod: 'card' }).unwrap()
    if (result.success && result.data.paymentUrl) {
      window.location.href = result.data.paymentUrl
      return
    }
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
          {bill.status !== 'Paid' && (
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ mt: 1, alignSelf: 'flex-start' }}>
              <Button variant="contained" sx={portalPrimaryButtonSx} disabled={paying} onClick={handlePay}>
                Pay Now
              </Button>
              <Button variant="outlined" sx={portalOutlinedButtonSx} disabled={syncing} onClick={handleSyncPayment}>
                {syncing ? 'Checking…' : 'Check Payment Status'}
              </Button>
            </Stack>
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
