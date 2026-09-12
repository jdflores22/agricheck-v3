import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import { useEffect, useRef, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useConfirmBillPaymentByTokenMutation,
  useConfirmBillPaymentMutation,
} from '../api/clientApi'

type ReturnState = 'confirming' | 'success' | 'cancelled' | 'pending' | 'error'

export function BillPaymentReturnPage() {
  const { billUuid = '' } = useParams()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const status = searchParams.get('status')
  const token = searchParams.get('token')
  const [returnState, setReturnState] = useState<ReturnState>(() =>
    status === 'cancelled' ? 'cancelled' : 'confirming',
  )
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [bill, setBill] = useState<{
    uuid: string
    billNumber: string
    status: string
    entryUuid?: string
    entryReferenceNo?: string
  } | null>(null)
  const [confirmPayment] = useConfirmBillPaymentMutation()
  const [confirmPaymentByToken] = useConfirmBillPaymentByTokenMutation()
  const attempted = useRef(false)

  useEffect(() => {
    if (!billUuid || attempted.current) return
    if (status === 'cancelled') return

    attempted.current = true

    const confirm = token
      ? confirmPaymentByToken(token).unwrap()
      : confirmPayment(billUuid).unwrap()

    confirm
      .then((result) => {
        if (!result.success || !result.data) {
          setReturnState('error')
          setErrorMessage('Unable to confirm payment.')
          return
        }

        setBill(result.data)
        if (result.data.status === 'Paid') {
          setReturnState('success')
          return
        }

        setReturnState('pending')
      })
      .catch((error: { data?: { message?: string; error?: { message?: string } } }) => {
        const message =
          error?.data?.message ??
          error?.data?.error?.message ??
          'Payment confirmation failed. If you already paid, please wait a moment and try again.'

        if (message.toLowerCase().includes('not been completed')) {
          setReturnState('pending')
          setErrorMessage(message)
          return
        }

        setReturnState('error')
        setErrorMessage(message)
      })
  }, [billUuid, confirmPayment, confirmPaymentByToken, status, token])

  const handleRetryConfirm = async () => {
    setReturnState('confirming')
    setErrorMessage(null)
    attempted.current = false

    try {
      const result = token
        ? await confirmPaymentByToken(token).unwrap()
        : await confirmPayment(billUuid).unwrap()

      if (result.success && result.data) {
        setBill(result.data)
        setReturnState(result.data.status === 'Paid' ? 'success' : 'pending')
        return
      }

      setReturnState('error')
      setErrorMessage('Unable to confirm payment.')
    } catch (error: unknown) {
      const message =
        (error as { data?: { message?: string } })?.data?.message ??
        'Payment confirmation failed.'
      setReturnState('pending')
      setErrorMessage(message)
    }
  }

  const entryUuid = bill?.entryUuid
  const successTarget = entryUuid
    ? `/client/entries/${entryUuid}?payment=success`
    : `/client/bills/${billUuid}`

  return (
    <Box sx={{ maxWidth: 560, mx: 'auto', py: 4, px: 2 }}>
      <Stack spacing={1} sx={{ mb: 3, textAlign: 'center', alignItems: 'center' }}>
        {returnState === 'success' ? (
          <CheckCircleOutlinedIcon sx={{ fontSize: 56, color: '#15803d' }} />
        ) : returnState === 'cancelled' || returnState === 'error' ? (
          <ErrorOutlineOutlinedIcon sx={{ fontSize: 56, color: '#b45309' }} />
        ) : (
          <CircularProgress size={48} />
        )}
        <Typography variant="h5" sx={{ fontWeight: 600 }}>
          {returnState === 'success' && 'Payment Successful'}
          {returnState === 'cancelled' && 'Payment Cancelled'}
          {returnState === 'confirming' && 'Confirming Payment…'}
          {returnState === 'pending' && 'Payment Processing'}
          {returnState === 'error' && 'Payment Confirmation Failed'}
        </Typography>
        <Typography sx={{ color: portalColors.textMuted }}>
          {returnState === 'success' && 'Your processing fee has been received.'}
          {returnState === 'cancelled' && 'You cancelled checkout before completing payment.'}
          {returnState === 'confirming' && 'Verifying your PayMongo payment…'}
          {returnState === 'pending' && 'PayMongo may still be processing your payment.'}
          {returnState === 'error' && 'We could not verify your payment automatically.'}
        </Typography>
      </Stack>

      {errorMessage && (
        <Alert severity={returnState === 'pending' ? 'info' : 'warning'} sx={{ mb: 2, borderRadius: '0.75rem' }}>
          {errorMessage}
        </Alert>
      )}

      {bill && returnState === 'success' && (
        <PortalPanel title={bill.billNumber}>
          <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
            {bill.entryReferenceNo && (
              <Typography variant="body2">Entry: {bill.entryReferenceNo}</Typography>
            )}
            <Typography variant="body2">Status: Paid</Typography>
          </Stack>
        </PortalPanel>
      )}

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ mt: 3, justifyContent: 'center' }}>
        {returnState === 'success' && (
          <Button
            variant="contained"
            sx={portalPrimaryButtonSx}
            onClick={() => navigate(successTarget)}
          >
            {entryUuid ? 'View Entry' : 'View Bill'}
          </Button>
        )}
        {(returnState === 'pending' || returnState === 'error') && (
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={handleRetryConfirm}>
            Check Again
          </Button>
        )}
        {returnState === 'cancelled' && (
          <Button
            variant="contained"
            sx={portalPrimaryButtonSx}
            component={RouterLink}
            to={token ? `/client/payment/link/${token}` : `/client/bills/${billUuid}`}
          >
            Try Payment Again
          </Button>
        )}
        <Button
          variant="outlined"
          sx={portalOutlinedButtonSx}
          component={RouterLink}
          to={entryUuid ? `/client/entries/${entryUuid}` : '/client/bills'}
        >
          Back to Portal
        </Button>
      </Stack>
    </Box>
  )
}
