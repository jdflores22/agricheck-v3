import { Box, TableCell, TableRow } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { useGetMavComplianceAlertsQuery } from '../api/mavApi'

const statCards = [
  { key: 'expiredLicenses', label: 'Expired Licenses' },
  { key: 'expiredMics', label: 'Expired MICs' },
  { key: 'expiringLicenses', label: 'Expiring Licenses (30d)' },
  { key: 'expiringMics', label: 'Expiring MICs (30d)' },
  { key: 'lowUtilizationAccounts', label: 'Low Utilization' },
  { key: 'overUtilizedAccounts', label: 'Over-Utilized' },
  { key: 'pendingApplications', label: 'Pending Applications' },
]

export function MavCompliancePage() {
  const { data, isLoading } = useGetMavComplianceAlertsQuery()
  const alerts = data?.data
  const stats = alerts?.summary

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Compliance"
        title="MAV Compliance"
        subtitle="Monitor compliance alerts and expired records."
      />

      <PortalStatGrid
        items={statCards}
        stats={stats as unknown as Record<string, number | undefined>}
        isLoading={isLoading}
        columns={{ xs: '1fr 1fr', md: 'repeat(3, 1fr)' }}
      />

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="Expiring Licenses (30 days)"
          columns={['License', 'Holder', 'Commodity', 'Available (MT)', 'Expires', 'Days Left']}
          isLoading={isLoading}
          isEmpty={!isLoading && (alerts?.expiringLicenses.length ?? 0) === 0}
          emptyMessage="No licenses expiring soon."
        >
          {(alerts?.expiringLicenses ?? []).map((row) => (
            <TableRow key={row.uuid} hover>
              <TableCell>{row.licenseNumber}</TableCell>
              <TableCell>{row.holderName}</TableCell>
              <TableCell>{row.commodityName} ({row.hsCode})</TableCell>
              <TableCell>{row.availableVolume}</TableCell>
              <TableCell>{new Date(row.expiresAt).toLocaleDateString()}</TableCell>
              <TableCell>{row.daysRemaining}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="Low Utilization (&lt;20%)"
          columns={['License', 'Holder', 'Commodity', 'Awarded', 'Utilized', 'Utilization %']}
          isLoading={isLoading}
          isEmpty={!isLoading && (alerts?.lowUtilizationAccounts.length ?? 0) === 0}
          emptyMessage="No low-utilization accounts."
        >
          {(alerts?.lowUtilizationAccounts ?? []).map((row) => (
            <TableRow key={row.licenseUuid} hover>
              <TableCell>{row.licenseNumber}</TableCell>
              <TableCell>{row.holderName}</TableCell>
              <TableCell>{row.commodityName} ({row.hsCode})</TableCell>
              <TableCell>{row.awardedVolume}</TableCell>
              <TableCell>{row.utilizedVolume}</TableCell>
              <TableCell>{row.utilizationPercent.toFixed(1)}%</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="Over-Utilized Accounts"
          columns={['License', 'Holder', 'Commodity', 'Awarded', 'Utilized']}
          isLoading={isLoading}
          isEmpty={!isLoading && (alerts?.overUtilizedAccounts.length ?? 0) === 0}
          emptyMessage="No over-utilized accounts."
        >
          {(alerts?.overUtilizedAccounts ?? []).map((row) => (
            <TableRow key={row.licenseUuid} hover>
              <TableCell>{row.licenseNumber}</TableCell>
              <TableCell>{row.holderName}</TableCell>
              <TableCell>{row.commodityName} ({row.hsCode})</TableCell>
              <TableCell>{row.awardedVolume}</TableCell>
              <TableCell>{row.utilizedVolume}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
