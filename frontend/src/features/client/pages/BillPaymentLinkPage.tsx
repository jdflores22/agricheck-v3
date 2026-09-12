import { Alert, Box, Button, Stack, Typography } from '@mui/material'
import { useParams } from 'react-router-dom'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetBillByTokenQuery, useInitiateBillPaymentByTokenMutation } from '../api/clientApi'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(amount)
}

export function BillPaymentLinkPage() {
  const { token = '' } = useParams()
  const { data, isLoading, refetch } = useGetBillByTokenQuery(token, { skip: !token })
  const [initiatePayment, { isLoading: paying, isSuccess }] = useInitiateBillPaymentByTokenMutation()
  const bill = data?.data

  if (isLoading) return <Typography sx={{ py: 6, textAlign: 'center', color: portalColors.textMuted }}>Loading payment link…</Typography>
  if (!bill) return <Alert severity="error" sx={{ maxWidth: 480, mx: 'auto', mt: 4 }}>Payment link is invalid or expired.</Alert>

  const handlePay = async () => {
    const result = await initiatePayment({ token, paymentMethod: 'card' }).unwrap()
    if (result.success && result.data.paymentUrl) {
      window.location.href = result.data.paymentUrl
      return
    }
    refetch()
  }

  const paid = bill.status === 'Paid' || isSuccess

  return (
    <Box sx={{ maxWidth: 480, mx: 'auto', py: 4, px: 2 }}>
      <Typography sx={{ fontSize: '1.25rem', fontWeight: 600, mb: 2, textAlign: 'center' }}>AgriCheck Payment</Typography>
      <PortalPanel title={bill.billNumber}>
        <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
          <Typography variant="body2">{bill.description}</Typography>
          {bill.entryReferenceNo && <Typography variant="body2">Entry: {bill.entryReferenceNo}</Typography>}
          <Typography variant="h6">{formatCurrency(bill.amount)}</Typography>
          <Typography variant="body2" color="text.secondary">Status: {paid ? 'Paid' : bill.status}</Typography>
          {paid ? (
            <Alert severity="success">Payment completed successfully.</Alert>
          ) : (
            <Button variant="contained" sx={portalPrimaryButtonSx} disabled={paying} onClick={handlePay}>
              Pay Now
            </Button>
          )}
        </Stack>
      </PortalPanel>
    </Box>
  )
}
