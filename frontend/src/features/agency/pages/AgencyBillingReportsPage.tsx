import { Box, TableCell, TableRow, Typography } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatCard } from '../../../components/portal/PortalStatCard'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { useGetAgencyBillingRevenueReportQuery } from '../api/agencyApi'

export function AgencyBillingReportsPage() {
  const { data, isLoading } = useGetAgencyBillingRevenueReportQuery()
  const report = data?.data

  const displayMoney = (value?: number) =>
    isLoading ? '…' : `PHP ${(value ?? 0).toLocaleString(undefined, { minimumFractionDigits: 2 })}`

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Financial Reports"
        title="DA Billing Revenue"
        subtitle={
          report
            ? `${report.agencyName} (${report.agencyCode}) — agency service billing collections only`
            : 'Agency DA billing collections and outstanding amounts'
        }
        action={{ label: 'Manage Billing', to: '/agency/billing' }}
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
          value={displayMoney(report?.collected)}
          caption="Paid DA / agency billings"
        />
        <PortalStatCard
          label="Outstanding"
          value={displayMoney(report?.pending)}
          caption={`${report?.openBillings ?? 0} open billings`}
        />
        <PortalStatCard
          label="Pending Cash (OR)"
          value={isLoading ? '…' : String(report?.pendingCashCount ?? 0)}
          caption="Awaiting cashier verification"
        />
        <PortalStatCard
          label="Paid Billings"
          value={isLoading ? '…' : String(report?.paidCount ?? 0)}
          caption="Completed agency billings"
        />
      </Box>

      <PortalTablePanel
        title="Monthly collections"
        columns={['Month', 'Billings', 'Amount']}
        isLoading={isLoading}
        isEmpty={!isLoading && (report?.byMonth.length ?? 0) === 0}
        emptyMessage="No DA billing revenue recorded yet."
      >
        {(report?.byMonth ?? []).map((row) => (
          <TableRow key={row.month} hover>
            <TableCell>
              <Typography sx={{ fontWeight: 600 }}>{row.month}</Typography>
            </TableCell>
            <TableCell>{row.paymentCount}</TableCell>
            <TableCell sx={{ color: portalColors.textDark }}>
              PHP {row.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
