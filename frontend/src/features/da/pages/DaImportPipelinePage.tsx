import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import PendingActionsOutlinedIcon from '@mui/icons-material/PendingActionsOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import {
  Box,
  Chip,
  CircularProgress,
  Grid,
  LinearProgress,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { DaAnalyticsKpiCard } from '../components/DaAgencyAnalyticsPanels'
import { useGetDaImportPipelineReportQuery } from '../api/daApi'

function formatMt(kg: number) {
  return `${(kg / 1000).toLocaleString(undefined, { maximumFractionDigits: 2 })} MT`
}

function formatKg(kg: number) {
  return `${kg.toLocaleString(undefined, { maximumFractionDigits: 0 })} kg`
}

export function DaImportPipelinePage() {
  const [search, setSearch] = useState('')
  const { data, isLoading, isFetching } = useGetDaImportPipelineReportQuery({})
  const report = data?.data

  const filteredCommodities = useMemo(() => {
    if (!report) return []
    const term = search.trim().toLowerCase()
    if (!term) return report.byCommodity
    return report.byCommodity.filter(
      (row) =>
        row.commodityName.toLowerCase().includes(term)
        || row.hsCode.toLowerCase().includes(term),
    )
  }, [report, search])

  if (isLoading || !report) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  const fulfillmentPct = report.totalExpectedKg + report.totalActualKg > 0
    ? Math.round((report.totalActualKg / (report.totalExpectedKg + report.totalActualKg)) * 100)
    : 0

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Import oversight"
        title="Expected vs actual imports"
        subtitle="Compare import entries still in the pipeline against commodities already received into warehouse inventory."
      >
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mt: 1.5 }}>
          <Chip
            size="small"
            clickable
            component={RouterLink}
            to="/da/reports/commodities"
            label="Warehouse stock (actual)"
            sx={{ fontWeight: 700 }}
          />
          <Chip size="small" clickable component={RouterLink} to="/da/reports/stock" label="Stock map" sx={{ fontWeight: 700 }} />
        </Stack>
      </PortalPageHeader>

      {isFetching && <LinearProgress sx={{ mb: 2 }} />}

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, md: 3 }}>
          <DaAnalyticsKpiCard
            label="Expected (in pipeline)"
            value={formatMt(report.totalExpectedKg)}
            meta={`${report.expectedContainers} container(s) not yet in warehouse`}
            icon={<LocalShippingOutlinedIcon />}
          />
        </Grid>
        <Grid size={{ xs: 12, md: 3 }}>
          <DaAnalyticsKpiCard
            label="Actual (in warehouse)"
            value={formatMt(report.totalActualKg)}
            meta={`${report.actualContainers} container(s) in stored inventory`}
            icon={<WarehouseOutlinedIcon />}
          />
        </Grid>
        <Grid size={{ xs: 12, md: 3 }}>
          <DaAnalyticsKpiCard
            label="Active import entries"
            value={String(report.pipelineEntryCount)}
            meta="Submitted entries with goods not fully warehoused"
            icon={<PendingActionsOutlinedIcon />}
          />
        </Grid>
        <Grid size={{ xs: 12, md: 3 }}>
          <DaAnalyticsKpiCard
            label="Warehoused share"
            value={`${fulfillmentPct}%`}
            meta="Actual volume as share of expected + actual"
            icon={<WarehouseOutlinedIcon />}
          />
        </Grid>
      </Grid>

      <PortalTablePanel
        title="Pipeline stage breakdown"
        columns={['Stage', 'Volume', 'Containers', 'Entries']}
        isEmpty={report.byStage.length === 0}
        emptyMessage="No expected import volume in the pipeline."
      >
        {report.byStage.map((row) => (
          <TableRow key={row.stage} hover>
            <TableCell>{row.label}</TableCell>
            <TableCell>{formatMt(row.expectedKg)}</TableCell>
            <TableCell>{row.containerCount}</TableCell>
            <TableCell>{row.entryCount}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Box sx={{ mt: 3 }}>
        <TextField
          label="Search commodity or HS code"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          size="small"
          sx={{ mb: 2, maxWidth: 360 }}
        />
        <PortalTablePanel
          title="By commodity — expected vs actual"
          columns={['Commodity', 'HS', 'Expected', 'Actual', 'Processing', 'In transit', 'Awaiting storage']}
          isEmpty={filteredCommodities.length === 0}
          emptyMessage="No commodity rows match your search."
        >
          {filteredCommodities.map((row) => (
            <TableRow key={`${row.hsCode}-${row.commodityName}`} hover>
              <TableCell>{row.commodityName}</TableCell>
              <TableCell>{row.hsCode}</TableCell>
              <TableCell>{formatMt(row.expectedKg)}</TableCell>
              <TableCell>{formatMt(row.actualKg)}</TableCell>
              <TableCell>{formatKg(row.processingKg)}</TableCell>
              <TableCell>{formatKg(row.inTransitKg)}</TableCell>
              <TableCell>{formatKg(row.awaitingStorageKg)}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="Import entries in pipeline"
          columns={['Reference', 'Agency', 'Stage', 'Commodity', 'Expected', 'Containers', 'Submitted']}
          isEmpty={report.entries.length === 0}
          emptyMessage="No active import entries awaiting warehouse intake."
        >
          {report.entries.map((entry) => (
            <TableRow key={entry.entryUuid} hover>
              <TableCell>{entry.referenceNo}</TableCell>
              <TableCell>{entry.agencyCode}</TableCell>
              <TableCell>
                <Chip size="small" label={entry.pipelineLabel} sx={{ fontWeight: 600 }} />
              </TableCell>
              <TableCell>{entry.commodityName}</TableCell>
              <TableCell>{formatMt(entry.expectedKg)}</TableCell>
              <TableCell>
                {entry.storedContainers}/{entry.totalContainers} stored · {entry.pendingContainers} pending
              </TableCell>
              <TableCell>
                {entry.submittedAt ? new Date(entry.submittedAt).toLocaleDateString() : '—'}
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
