import {
  Box,
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
import { useGetAdminEntryPaymentsQuery } from '../api/adminApi'

const statusOptions = [
  { value: '', label: 'All statuses' },
  { value: 'completed', label: 'Completed' },
  { value: 'pending', label: 'Pending' },
  { value: 'failed', label: 'Failed' },
]

export function AdminEntryPaymentsPage() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const { data, isLoading } = useGetAdminEntryPaymentsQuery({ page, status: status || undefined })
  const payments = data?.data?.items ?? []
  const totalCount = data?.data?.totalCount ?? 0

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Payments"
        title="Entry Payments"
        subtitle="Track processing fee payments for submitted entries across all agencies."
        action={{ label: 'Revenue Report', to: '/admin/revenue' }}
      />

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
        title={`Entry payment transactions (${totalCount})`}
        columns={['Bill', 'Entry', 'Agency', 'Client', 'Amount', 'Method', 'Status', 'Paid', 'Reference']}
        isLoading={isLoading}
        isEmpty={!isLoading && payments.length === 0}
        emptyMessage="No entry payments recorded yet."
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
