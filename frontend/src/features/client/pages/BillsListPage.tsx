import { Box, Button, Chip, TableCell, TableRow } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { useGetBillsQuery } from '../api/clientApi'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(amount)
}

export function BillsListPage() {
  const { data, isLoading } = useGetBillsQuery()
  const bills = data?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Payments"
        title="Bills & Payments"
        subtitle="View and pay outstanding agency bills."
        actions={
          <Button component={RouterLink} to="/client/payment-history" variant="outlined">
            Payment History
          </Button>
        }
      />
      <PortalTablePanel
        title="All bills"
        columns={['Bill No.', 'Description', 'Amount', 'Due Date', 'Status', 'Action']}
        isLoading={isLoading}
        isEmpty={!isLoading && bills.length === 0}
        emptyMessage="No bills found."
      >
        {bills.map((bill) => (
          <TableRow key={bill.uuid} hover>
            <TableCell>{bill.billNumber}</TableCell>
            <TableCell>{bill.description}</TableCell>
            <TableCell>{formatCurrency(bill.amount)}</TableCell>
            <TableCell>{bill.dueDate ? new Date(bill.dueDate).toLocaleDateString() : '—'}</TableCell>
            <TableCell>
              <Chip size="small" label={bill.status} sx={portalStatusChipSx(bill.status)} />
            </TableCell>
            <TableCell>
              {bill.status !== 'Paid' ? (
                <Button size="small" component={RouterLink} to={`/client/bills/${bill.uuid}`}>
                  Pay
                </Button>
              ) : (
                '—'
              )}
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
