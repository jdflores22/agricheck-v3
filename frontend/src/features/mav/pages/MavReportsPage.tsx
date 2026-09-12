import { Box, Button, Stack, TableCell, TableRow, TextField } from '@mui/material'
import { useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { downloadAuthenticatedFile } from '../../client/utils/downloadFile'
import { useGetMavReportDetailQuery } from '../api/mavApi'

const reportStatItems = [
  { key: 'totalApplications', label: 'Total Applications' },
  { key: 'approvedApplications', label: 'Approved' },
  { key: 'rejectedApplications', label: 'Rejected' },
  { key: 'activeLicenses', label: 'Active Licenses' },
  { key: 'issuedMics', label: 'Issued MICs' },
  { key: 'totalAwardedVolume', label: 'Total Awarded (MT)' },
  { key: 'totalUtilizedVolume', label: 'Total Utilized (MT)' },
]

export function MavReportsPage() {
  const [mavYear, setMavYear] = useState<number | undefined>(2026)
  const { data, isLoading } = useGetMavReportDetailQuery({ mavYear })
  const report = data?.data
  const summary = report?.summary

  const exportCsv = async () => {
    const qs = mavYear ? `?mavYear=${mavYear}` : ''
    await downloadAuthenticatedFile(`/mav/reports/export.csv${qs}`, `mav-report-${mavYear ?? 'all'}.csv`)
  }

  const exportPdf = async () => {
    const qs = mavYear ? `?mavYear=${mavYear}` : ''
    await downloadAuthenticatedFile(`/mav/reports/export.pdf${qs}`, `mav-report-${mavYear ?? 'all'}.pdf`)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Reports"
        title="MAV Reports"
        subtitle="Summary statistics and breakdowns by MAV year."
        actions={
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={exportCsv}>
              Export CSV
            </Button>
            <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={exportPdf}>
              Export PDF
            </Button>
          </Stack>
        }
      />

      <PortalPanel title="Filter">
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ px: 2.5, py: 2 }}>
          <TextField
            label="MAV Year"
            type="number"
            value={mavYear ?? ''}
            onChange={(e) => setMavYear(e.target.value ? Number(e.target.value) : undefined)}
            sx={{ width: 200 }}
          />
        </Stack>
      </PortalPanel>

      <Box sx={{ mt: 3 }}>
        <PortalStatGrid
          items={reportStatItems}
          stats={summary as unknown as Record<string, number | undefined>}
          isLoading={isLoading}
          columns={{ xs: '1fr 1fr', md: 'repeat(3, 1fr)' }}
        />
      </Box>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="By Commodity"
          columns={['HS Code', 'Commodity', 'Applications', 'Active Licenses', 'Awarded (MT)', 'Utilized (MT)']}
          isLoading={isLoading}
          isEmpty={!isLoading && (report?.byCommodity.length ?? 0) === 0}
          emptyMessage="No commodity data for this year."
        >
          {(report?.byCommodity ?? []).map((row) => (
            <TableRow key={row.hsCode} hover>
              <TableCell>{row.hsCode}</TableCell>
              <TableCell>{row.commodityName}</TableCell>
              <TableCell>{row.applicationCount}</TableCell>
              <TableCell>{row.activeLicenses}</TableCell>
              <TableCell>{row.awardedVolume}</TableCell>
              <TableCell>{row.utilizedVolume}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="By Pool Type"
          columns={['Pool', 'Applications', 'Active Licenses', 'Awarded (MT)', 'Utilized (MT)']}
          isLoading={isLoading}
          isEmpty={!isLoading && (report?.byPoolType.length ?? 0) === 0}
          emptyMessage="No pool data for this year."
        >
          {(report?.byPoolType ?? []).map((row) => (
            <TableRow key={row.poolType} hover>
              <TableCell>{row.poolType}</TableCell>
              <TableCell>{row.applicationCount}</TableCell>
              <TableCell>{row.activeLicenses}</TableCell>
              <TableCell>{row.awardedVolume}</TableCell>
              <TableCell>{row.utilizedVolume}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
