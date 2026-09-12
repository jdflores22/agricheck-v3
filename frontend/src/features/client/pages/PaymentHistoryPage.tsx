import { Box, Button, TableCell, TableRow } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { useGetPaymentHistoryQuery } from '../api/clientApi'
import { downloadAuthenticatedFile } from '../utils/downloadFile'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(amount)
}

export function PaymentHistoryPage() {
  const { data, isLoading } = useGetPaymentHistoryQuery()
  const payments = data?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Payments"
        title="Payment History"
        subtitle="Review your completed bill payments."
      />
      <PortalTablePanel
        title="All payments"
        columns={['Date', 'Bill No.', 'Entry', 'Amount', 'Method', 'Reference', 'Status', 'Receipt']}
        isLoading={isLoading}
        isEmpty={!isLoading && payments.length === 0}
        emptyMessage="No payment history yet."
      >
        {payments.map((payment) => (
          <TableRow key={`${payment.billUuid}-${payment.paidAt}`} hover>
            <TableCell>{new Date(payment.paidAt).toLocaleString()}</TableCell>
            <TableCell>{payment.billNumber}</TableCell>
            <TableCell>{payment.entryReferenceNo ?? '—'}</TableCell>
            <TableCell>{formatCurrency(payment.amount)}</TableCell>
            <TableCell>{payment.paymentMethod}</TableCell>
            <TableCell>{payment.externalReference ?? '—'}</TableCell>
            <TableCell>{payment.status}</TableCell>
            <TableCell>
              {payment.status === 'completed' && (
                <Button size="small" sx={portalOutlinedButtonSx} onClick={() => downloadAuthenticatedFile(`/client/bills/${payment.billUuid}/receipt`, `receipt-${payment.billNumber}.pdf`)}>
                  PDF
                </Button>
              )}
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
