import { Box, TableCell, TableRow, Typography } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { useGetAdminRevenueSummaryQuery } from '../api/adminApi'

export function AdminRevenuePage() {
  const { data, isLoading } = useGetAdminRevenueSummaryQuery()
  const summary = data?.data

  const displayMoney = (value?: number) =>
    isLoading ? '…' : `PHP ${(value ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2 })}`

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Payments"
        title="Platform Revenue"
        subtitle="Entry processing service fees collected by the system. Agency DA billings are not included."
        action={{ label: 'View Transactions', to: '/admin/entry-payments' }}
      />

      <Box
        sx={{
          mb: 3,
          display: 'grid',
          gap: 2,
          gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', xl: 'repeat(4, 1fr)' },
        }}
      >
        <PortalStatCard
          label="Total Collected"
          value={displayMoney(summary?.totalCollected)}
          caption={`${summary?.paidCount ?? 0} completed payments`}
        />
        <PortalStatCard
          label="Pending Amount"
          value={displayMoney(summary?.pendingAmount)}
          caption={`${summary?.pendingCount ?? 0} pending attempts`}
        />
        <PortalStatCard
          label="Completed"
          value={isLoading ? '…' : String(summary?.paidCount ?? 0)}
          caption="Successful entry payments"
        />
        <PortalStatCard
          label="Failed"
          value={isLoading ? '…' : String(summary?.failedCount ?? 0)}
          caption="Failed payment attempts"
        />
      </Box>

      <Box
        sx={{
          display: 'grid',
          gap: 3,
          gridTemplateColumns: { xs: '1fr', lg: '1fr 1fr' },
        }}
      >
        <PortalTablePanel
          title="Revenue by agency"
          columns={['Agency', 'Payments', 'Amount']}
          isLoading={isLoading}
          isEmpty={!isLoading && (summary?.byAgency.length ?? 0) === 0}
          emptyMessage="No collected payments yet."
        >
          {(summary?.byAgency ?? []).map((row) => (
            <TableRow key={row.agencyCode} hover>
              <TableCell>
                <Typography sx={{ fontWeight: 600 }}>{row.agencyCode}</Typography>
                <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{row.agencyName}</Typography>
              </TableCell>
              <TableCell>{row.paymentCount}</TableCell>
              <TableCell>PHP {row.amount.toLocaleString()}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>

        <PortalTablePanel
          title="Monthly collections"
          columns={['Month', 'Payments', 'Amount']}
          isLoading={isLoading}
          isEmpty={!isLoading && (summary?.byMonth.length ?? 0) === 0}
          emptyMessage="No monthly revenue data yet."
        >
          {(summary?.byMonth ?? []).map((row) => (
            <TableRow key={row.month} hover>
              <TableCell>{row.month}</TableCell>
              <TableCell>{row.paymentCount}</TableCell>
              <TableCell>PHP {row.amount.toLocaleString()}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
