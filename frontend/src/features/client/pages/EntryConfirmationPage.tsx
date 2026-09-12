import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import { useEffect, useRef, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetEntryQuery, useInitiateBillPaymentMutation } from '../api/clientApi'
import { redirectToBillPayment } from '../utils/billPayment'

export function EntryConfirmationPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading, refetch } = useGetEntryQuery(uuid, { skip: !uuid })
  const [initiatePayment] = useInitiateBillPaymentMutation()
  const [paymentState, setPaymentState] = useState<'idle' | 'redirecting' | 'completed' | 'failed'>('idle')
  const [paymentError, setPaymentError] = useState<string | null>(null)
  const paymentAttempted = useRef(false)
  const entry = data?.data
  const unpaidBill = entry?.bills.find((bill) => bill.status !== 'Paid')

  useEffect(() => {
    if (!entry || !unpaidBill || paymentAttempted.current) return

    paymentAttempted.current = true
    setPaymentState('redirecting')
    setPaymentError(null)

    redirectToBillPayment(initiatePayment, unpaidBill.uuid)
      .then((result) => {
        if (result === 'redirected') return
        if (result === 'completed') {
          setPaymentState('completed')
          refetch()
          return
        }
        setPaymentState('failed')
        setPaymentError('Unable to start PayMongo checkout. Use Pay Now to retry.')
      })
      .catch(() => {
        setPaymentState('failed')
        setPaymentError('Unable to start PayMongo checkout. Use Pay Now to retry.')
      })
  }, [entry, unpaidBill, initiatePayment, refetch])

  const handlePayNow = async () => {
    if (!unpaidBill) return
    setPaymentState('redirecting')
    setPaymentError(null)
    try {
      const result = await redirectToBillPayment(initiatePayment, unpaidBill.uuid)
      if (result === 'completed') {
        setPaymentState('completed')
        refetch()
        return
      }
      if (result === 'failed') {
        setPaymentState('failed')
        setPaymentError('Unable to start PayMongo checkout.')
      }
    } catch {
      setPaymentState('failed')
      setPaymentError('Unable to start PayMongo checkout.')
    }
  }

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading…</Typography>
  if (!entry) return <Alert severity="error">Entry not found.</Alert>

  const paymentComplete = paymentState === 'completed' || (!unpaidBill && entry.paymentStatus === 'Paid')

  return (
    <Box sx={{ maxWidth: 560, mx: 'auto' }}>
      <Stack spacing={1} sx={{ mb: 3, textAlign: 'center', alignItems: 'center' }}>
        <CheckCircleOutlinedIcon sx={{ fontSize: 56, color: '#15803d' }} />
        <Typography variant="h5" sx={{ fontWeight: 600 }}>Entry Submitted</Typography>
        <Typography sx={{ color: portalColors.textMuted }}>
          Your entry has been submitted for agency review.
        </Typography>
      </Stack>

      {paymentState === 'redirecting' && unpaidBill && (
        <Alert severity="info" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
            <CircularProgress size={18} />
            <Typography variant="body2">Redirecting to PayMongo to pay the processing fee…</Typography>
          </Stack>
        </Alert>
      )}

      {paymentError && (
        <Alert severity="warning" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          {paymentError}
        </Alert>
      )}

      {paymentComplete && (
        <Alert severity="success" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Processing fee payment completed.
        </Alert>
      )}

      <PortalPanel title={entry.referenceNo}>
        <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
          <Typography variant="body2">Agency: {entry.agencyName}</Typography>
          <Typography variant="body2">Status: {entry.status}</Typography>
          {unpaidBill && !paymentComplete && (
            <Alert severity="warning" sx={{ mt: 1 }}>
              Processing fee bill {unpaidBill.billNumber} (PHP {unpaidBill.amount.toLocaleString()}) must be paid before agency review proceeds.
            </Alert>
          )}
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ pt: 1 }}>
            <Button component={RouterLink} to={`/client/entries/${uuid}`} variant="contained" sx={portalPrimaryButtonSx}>
              View Entry
            </Button>
            {unpaidBill && !paymentComplete && (
              <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={handlePayNow} disabled={paymentState === 'redirecting'}>
                Pay Now
              </Button>
            )}
            <Button component={RouterLink} to="/client/entries" variant="outlined" sx={portalOutlinedButtonSx}>
              My Entries
            </Button>
          </Stack>
        </Stack>
      </PortalPanel>
    </Box>
  )
}
