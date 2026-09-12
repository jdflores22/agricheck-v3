import { useRef, useState } from 'react'
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
  TextField,
  Typography,
} from '@mui/material'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx, portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetDaBillingsQuery,
  useUploadDaBillingPaymentProofMutation,
  type ClientDaBilling,
} from '../api/clientApi'

function DaBillingCard({
  entryUuid,
  billing,
}: {
  entryUuid: string
  billing: ClientDaBilling
}) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [paymentReference, setPaymentReference] = useState(billing.paymentReference ?? '')
  const [notes, setNotes] = useState('')
  const [pendingFile, setPendingFile] = useState<File | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [successOpen, setSuccessOpen] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')
  const [showReplaceForm, setShowReplaceForm] = useState(false)
  const [uploadProof, { isLoading }] = useUploadDaBillingPaymentProofMutation()

  const proofSubmitted = billing.status === 'PaymentPending' && Boolean(billing.paymentUploadedAt)
  const canUpload = billing.status === 'Issued' || (billing.status === 'PaymentPending' && showReplaceForm)
  const statusLabel =
    billing.displayStatus ??
    (proofSubmitted ? 'Payment Submitted' : billing.status === 'Issued' ? 'Awaiting Payment Proof' : billing.status)

  const handlePickFile = () => {
    if (!paymentReference.trim()) {
      setErrorMessage('Enter your payment reference before choosing a proof file.')
      return
    }
    setErrorMessage('')
    fileInputRef.current?.click()
  }

  const handleFileSelected = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    e.target.value = ''
    if (!file) return
    if (!paymentReference.trim()) {
      setErrorMessage('Payment reference is required.')
      return
    }
    setPendingFile(file)
    setConfirmOpen(true)
  }

  const handleConfirmUpload = async () => {
    if (!pendingFile || !paymentReference.trim()) return

    try {
      await uploadProof({
        entryUuid,
        billingUuid: billing.uuid,
        file: pendingFile,
        paymentReference: paymentReference.trim(),
        notes: notes.trim() || undefined,
      }).unwrap()
      setConfirmOpen(false)
      setSuccessOpen(true)
      setShowReplaceForm(false)
      setPendingFile(null)
      setErrorMessage('')
    } catch {
      setConfirmOpen(false)
      setErrorMessage('Unable to upload payment proof. Please try again.')
    }
  }

  return (
    <>
      <PortalPanel title={billing.billNumber}>
        <Box sx={{ px: 2.5, py: 2 }}>
          <Stack spacing={1.25}>
            <Typography variant="body2">{billing.description}</Typography>
            <Typography variant="body2">Amount: PHP {billing.amount.toLocaleString()}</Typography>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
              <Typography variant="body2">Status:</Typography>
              <Chip
                size="small"
                label={statusLabel}
                sx={portalStatusChipSx(
                  billing.status === 'PaymentPending' && billing.paymentUploadedAt
                    ? 'PaymentSubmitted'
                    : billing.status,
                )}
              />
            </Stack>

            {proofSubmitted && !showReplaceForm && (
              <Alert
                severity="success"
                icon={<CheckCircleOutlinedIcon sx={{ fontSize: 'inherit' }} />}
                sx={{ borderRadius: '0.65rem' }}
              >
                <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>
                  Payment proof submitted
                </Typography>
                <Typography sx={{ fontSize: '0.8125rem', mt: 0.5 }}>
                  Reference: <strong>{billing.paymentReference}</strong>
                </Typography>
                {billing.paymentProofOriginalFileName && (
                  <Typography sx={{ fontSize: '0.8125rem' }}>
                    File: {billing.paymentProofOriginalFileName}
                  </Typography>
                )}
                {billing.paymentUploadedAt && (
                  <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                    Uploaded {new Date(billing.paymentUploadedAt).toLocaleString()}
                  </Typography>
                )}
                <Typography sx={{ fontSize: '0.8125rem', mt: 0.5 }}>
                  Waiting for agency payment verification before inspection can proceed.
                </Typography>
              </Alert>
            )}

            {proofSubmitted && !showReplaceForm && (
              <Button
                variant="outlined"
                size="small"
                sx={{ alignSelf: 'flex-start', ...portalOutlinedButtonSx }}
                onClick={() => setShowReplaceForm(true)}
              >
                Replace payment proof
              </Button>
            )}

            {billing.verificationNotes && (
              <Alert severity="info" sx={{ fontSize: '0.8125rem' }}>{billing.verificationNotes}</Alert>
            )}

            {canUpload && (
              <Box sx={{ pt: 1 }}>
                <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, mb: 1.5 }}>
                  {proofSubmitted ? 'Upload replacement proof' : 'Upload payment proof'}
                </Typography>
                <Stack spacing={1.5}>
                  <TextField
                    label="Payment reference"
                    value={paymentReference}
                    onChange={(e) => setPaymentReference(e.target.value)}
                    size="small"
                    fullWidth
                    required
                  />
                  <TextField
                    label="Notes (optional)"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    size="small"
                    fullWidth
                    multiline
                    minRows={2}
                  />
                  <input ref={fileInputRef} type="file" hidden accept="image/*,.pdf" onChange={handleFileSelected} />
                  <Button
                    variant="contained"
                    sx={portalPrimaryButtonSx}
                    disabled={isLoading || !paymentReference.trim()}
                    onClick={handlePickFile}
                  >
                    {isLoading ? 'Uploading…' : 'Choose proof file'}
                  </Button>
                </Stack>
              </Box>
            )}

            {billing.status === 'Paid' && billing.paidAt && (
              <Alert severity="success" sx={{ fontSize: '0.8125rem' }}>
                Payment verified on {new Date(billing.paidAt).toLocaleString()}.
              </Alert>
            )}

            {errorMessage && (
              <Alert severity="error" onClose={() => setErrorMessage('')}>
                {errorMessage}
              </Alert>
            )}
          </Stack>
        </Box>
      </PortalPanel>

      <Dialog open={confirmOpen} onClose={() => !isLoading && setConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Confirm payment proof upload</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted, mb: 1.5 }}>
            You are about to submit proof of payment for <strong>{billing.billNumber}</strong>.
          </Typography>
          <Stack spacing={0.75} sx={{ fontSize: '0.875rem' }}>
            <Typography>Amount: PHP {billing.amount.toLocaleString()}</Typography>
            <Typography>Reference: {paymentReference.trim()}</Typography>
            <Typography>File: {pendingFile?.name ?? '—'}</Typography>
            {notes.trim() && <Typography>Notes: {notes.trim()}</Typography>}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setConfirmOpen(false)} disabled={isLoading} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleConfirmUpload} variant="contained" disabled={isLoading} sx={portalPrimaryButtonSx}>
            {isLoading ? 'Uploading…' : 'Yes, Upload Proof'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={successOpen} onClose={() => setSuccessOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Payment proof uploaded</DialogTitle>
        <DialogContent>
          <Alert severity="success" sx={{ mb: 1.5 }}>
            Your DA payment proof was uploaded successfully.
          </Alert>
          <Typography sx={{ color: portalColors.textMuted, fontSize: '0.875rem' }}>
            The agency will verify your payment. You will be notified when your entry moves to{' '}
            <strong>For Inspection</strong>.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setSuccessOpen(false)} variant="contained" sx={portalPrimaryButtonSx}>
            OK
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}

export function EntryDaBillingPanel({ entryUuid }: { entryUuid: string }) {
  const { data, isLoading, isFetching, refetch } = useGetDaBillingsQuery(entryUuid)
  const billings = data?.data ?? []

  if (isLoading) {
    return <Typography sx={{ color: portalColors.textMuted }}>Loading DA billing…</Typography>
  }

  if (billings.length === 0) {
    return (
      <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
        DA billing has not been issued yet. You will be notified when a bill is available for payment.
      </Alert>
    )
  }

  const pendingVerification = billings.some((b) => b.status === 'PaymentPending')

  return (
    <Stack spacing={2}>
      <Alert severity="warning" sx={{ borderRadius: '0.75rem' }}>
        {pendingVerification
          ? 'Your payment proof is under agency review. No further action is required unless you need to replace the uploaded file.'
          : 'Upload your DA payment proof after settling the bill. The agency will verify payment before inspection can proceed.'}
      </Alert>
      {isFetching && (
        <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>Refreshing billing status…</Typography>
      )}
      {billings.map((billing) => (
        <DaBillingCard
          key={`${billing.uuid}-${billing.paymentUploadedAt ?? billing.status}`}
          entryUuid={entryUuid}
          billing={billing}
        />
      ))}
      <Button size="small" sx={{ alignSelf: 'flex-start', ...portalOutlinedButtonSx }} onClick={() => refetch()}>
        Refresh status
      </Button>
    </Stack>
  )
}
