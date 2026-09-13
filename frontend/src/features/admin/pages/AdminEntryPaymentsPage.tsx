import {
  Box,
  Button,
  Chip,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import { useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetAdminEntryPaymentsQuery,
  useGetAdminPendingEntryCashPaymentsQuery,
  useVerifyAdminEntryCashPaymentMutation,
} from '../api/adminApi'

const statusOptions = [
  { value: '', label: 'All statuses' },
  { value: 'completed', label: 'Completed' },
  { value: 'pending', label: 'Pending' },
  { value: 'awaiting_verification', label: 'Awaiting verification' },
  { value: 'failed', label: 'Failed' },
]

export function AdminEntryPaymentsPage() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const { data, isLoading } = useGetAdminEntryPaymentsQuery({ page, status: status || undefined })
  const { data: pendingCashData, isLoading: pendingCashLoading } = useGetAdminPendingEntryCashPaymentsQuery()
  const [verifyCash, { isLoading: verifying }] = useVerifyAdminEntryCashPaymentMutation()
  const payments = data?.data?.items ?? []
  const totalCount = data?.data?.totalCount ?? 0
  const pendingCash = pendingCashData?.data ?? []

  const handleVerify = async (billUuid: string, approved: boolean) => {
    await verifyCash({ billUuid, approved }).unwrap()
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Payments"
        title="Entry Processing Fees"
        subtitle="Platform service fees for entry submissions. Agency DA billings are managed separately by each agency."
        action={{ label: 'Revenue Report', to: '/admin/revenue' }}
      />

      <Box sx={{ mb: 3 }}>
      <PortalTablePanel
        title={`Pending cash (OR) verification (${pendingCash.length})`}
        columns={['Bill', 'Entry', 'Agency', 'Client', 'Amount', 'OR #', 'Submitted', 'Actions']}
        isLoading={pendingCashLoading}
        isEmpty={!pendingCashLoading && pendingCash.length === 0}
        emptyMessage="No entry processing fee cash payments awaiting verification."
      >
        {pendingCash.map((payment) => (
          <TableRow key={payment.billUuid} hover>
            <TableCell>{payment.billNumber}</TableCell>
            <TableCell>{payment.entryReferenceNo ?? '—'}</TableCell>
            <TableCell>{payment.agencyCode ?? '—'}</TableCell>
            <TableCell>{payment.clientName}</TableCell>
            <TableCell>PHP {payment.amount.toLocaleString()}</TableCell>
            <TableCell>{payment.externalReference ?? '—'}</TableCell>
            <TableCell>{new Date(payment.submittedAt).toLocaleString()}</TableCell>
            <TableCell>
              <Stack direction="row" spacing={1}>
                <Button
                  size="small"
                  color="error"
                  disabled={verifying}
                  onClick={() => handleVerify(payment.billUuid, false)}
                >
                  Reject
                </Button>
                <Button
                  size="small"
                  variant="contained"
                  sx={portalPrimaryButtonSx}
                  disabled={verifying}
                  onClick={() => handleVerify(payment.billUuid, true)}
                >
                  Approve
                </Button>
              </Stack>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
      </Box>

      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <TextField
          select
          label="Status"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value)
            setPage(1)
          }}
          size="small"
          sx={{ minWidth: 180 }}
        >
          {statusOptions.map((option) => (
            <MenuItem key={option.value || 'all'} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </TextField>
      </Stack>

      <PortalTablePanel
        title={`Entry fee transactions (${totalCount})`}
        columns={['Bill', 'Entry', 'Agency', 'Client', 'Amount', 'Method', 'Status', 'Paid', 'Reference']}
        isLoading={isLoading}
        isEmpty={!isLoading && payments.length === 0}
        emptyMessage="No entry processing fee payments recorded yet."
      >
        {payments.map((payment) => (
          <TableRow key={payment.id} hover>
            <TableCell>{payment.billNumber}</TableCell>
            <TableCell>{payment.entryReferenceNo ?? '—'}</TableCell>
            <TableCell>{payment.agencyCode ?? '—'}</TableCell>
            <TableCell>{payment.clientName}</TableCell>
            <TableCell>PHP {payment.amount.toLocaleString()}</TableCell>
            <TableCell>{payment.paymentMethod}</TableCell>
            <TableCell>
              <Chip size="small" label={payment.status} sx={portalStatusChipSx(payment.status)} />
            </TableCell>
            <TableCell>
              {payment.paidAt ? new Date(payment.paidAt).toLocaleString() : new Date(payment.createdAt).toLocaleString()}
            </TableCell>
            <TableCell>{payment.externalReference ?? payment.gatewayTransactionId ?? '—'}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
