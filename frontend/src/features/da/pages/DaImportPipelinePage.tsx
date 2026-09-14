import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
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
  Typography,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { DaAnalyticsKpiCard } from '../components/DaAgencyAnalyticsPanels'
import {
  useGetDaImportPipelineReportQuery,
  type DaImportPipelineCommodityRow,
} from '../api/daApi'

function formatMt(kg: number) {
  return `${(kg / 1000).toLocaleString(undefined, { maximumFractionDigits: 2 })} MT`
}

function formatKg(kg: number) {
  return `${kg.toLocaleString(undefined, { maximumFractionDigits: 0 })} kg`
}

function importerLabel(name: string, companyName?: string | null) {
  return companyName?.trim() ? companyName.trim() : name
}

export function DaImportPipelinePage() {
  const [search, setSearch] = useState('')
  const [selectedCommodity, setSelectedCommodity] = useState<DaImportPipelineCommodityRow | null>(null)
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

  const commodityImporters = useMemo(() => {
    if (!report || !selectedCommodity) return []
    return report.byCommodityImporter.filter(
      (row) =>
        row.hsCode === selectedCommodity.hsCode
        && row.commodityName === selectedCommodity.commodityName,
    )
  }, [report, selectedCommodity])

  const commodityWarehouses = useMemo(() => {
    if (!report || !selectedCommodity) return []
    return report.byCommodityWarehouse.filter(
      (row) =>
        row.hsCode === selectedCommodity.hsCode
        && row.commodityName === selectedCommodity.commodityName,
    )
  }, [report, selectedCommodity])

  const filteredImporters = useMemo(() => {
    if (!report) return []
    const term = search.trim().toLowerCase()
    if (!term) return report.byImporter
    return report.byImporter.filter(
      (row) =>
        row.importerName.toLowerCase().includes(term)
        || (row.companyName?.toLowerCase().includes(term) ?? false),
    )
  }, [report, search])

  if (isLoading || !report) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Import oversight"
        title="Expected vs actual imports"
        subtitle="Compare import entries still in the pipeline against commodities already received into warehouse inventory — classified by commodity, importer, and warehouse."
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
          {selectedCommodity ? (
            <Chip
              size="small"
              color="success"
              label={`${selectedCommodity.commodityName}${selectedCommodity.hsCode !== '—' ? ` · HS ${selectedCommodity.hsCode}` : ''}`}
              onDelete={() => setSelectedCommodity(null)}
            />
          ) : null}
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
            label="Registered importers"
            value={String(report.byImporter.length)}
            meta="Importers with expected or actual volume"
            icon={<BusinessOutlinedIcon />}
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
          label="Search commodity, HS code, or importer"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          size="small"
          sx={{ mb: 2, maxWidth: 420 }}
        />
        <PortalTablePanel
          title="By commodity — expected vs actual"
          columns={['Commodity', 'HS', 'Expected', 'Actual', 'Processing', 'In transit', 'Awaiting storage', 'Importers', 'Warehouses']}
          isEmpty={filteredCommodities.length === 0}
          emptyMessage="No commodity rows match your search."
        >
          {filteredCommodities.map((row) => (
            <TableRow
              key={`${row.hsCode}-${row.commodityName}`}
              hover
              selected={selectedCommodity?.commodityName === row.commodityName && selectedCommodity?.hsCode === row.hsCode}
              onClick={() => setSelectedCommodity(row)}
              sx={{ cursor: 'pointer' }}
            >
              <TableCell sx={{ fontWeight: 600 }}>{row.commodityName}</TableCell>
              <TableCell>{row.hsCode}</TableCell>
              <TableCell>{formatMt(row.expectedKg)}</TableCell>
              <TableCell>{formatMt(row.actualKg)}</TableCell>
              <TableCell>{formatKg(row.processingKg)}</TableCell>
              <TableCell>{formatKg(row.inTransitKg)}</TableCell>
              <TableCell>{formatKg(row.awaitingStorageKg)}</TableCell>
              <TableCell>{row.importerCount}</TableCell>
              <TableCell>{row.warehouseCount}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
        <Typography sx={{ mt: 1, fontSize: '0.75rem', color: portalColors.textMuted }}>
          Click a commodity row to see which importers brought it in and which warehouses currently hold it.
        </Typography>
      </Box>

      {selectedCommodity ? (
        <Box sx={{ mt: 3 }}>
          <Grid container spacing={2}>
            <Grid size={{ xs: 12, lg: 6 }}>
              <PortalTablePanel
                title={`${selectedCommodity.commodityName} — importers`}
                columns={['Importer', 'Contact', 'Expected', 'Actual', 'Containers']}
                isEmpty={commodityImporters.length === 0}
                emptyMessage="No importer volume recorded for this commodity."
              >
                {commodityImporters.map((row) => (
                  <TableRow key={row.importerUuid} hover>
                    <TableCell sx={{ fontWeight: 600 }}>
                      {importerLabel(row.importerName, row.companyName)}
                    </TableCell>
                    <TableCell>
                      {row.companyName?.trim() ? row.importerName : '—'}
                    </TableCell>
                    <TableCell>{formatMt(row.expectedKg)}</TableCell>
                    <TableCell>{formatMt(row.actualKg)}</TableCell>
                    <TableCell>{row.containerCount}</TableCell>
                  </TableRow>
                ))}
              </PortalTablePanel>
            </Grid>
            <Grid size={{ xs: 12, lg: 6 }}>
              <PortalTablePanel
                title={`${selectedCommodity.commodityName} — warehouses (actual stock)`}
                columns={['Warehouse', 'Region', 'Volume', 'Containers']}
                isEmpty={commodityWarehouses.length === 0}
                emptyMessage="No warehouse stock recorded for this commodity yet."
              >
                {commodityWarehouses.map((row) => (
                  <TableRow key={row.warehouseId} hover>
                    <TableCell sx={{ fontWeight: 600 }}>
                      {row.warehouseName}
                      <Typography sx={{ fontSize: '0.7rem', color: portalColors.textMuted }}>
                        {row.warehouseCode}
                      </Typography>
                    </TableCell>
                    <TableCell>{row.regionName ?? '—'}</TableCell>
                    <TableCell>{formatMt(row.actualKg)}</TableCell>
                    <TableCell>{row.containerCount}</TableCell>
                  </TableRow>
                ))}
              </PortalTablePanel>
            </Grid>
          </Grid>
        </Box>
      ) : null}

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="By importer — all commodities"
          columns={['Importer', 'Contact person', 'Expected', 'Actual', 'Entries', 'Commodities']}
          isEmpty={filteredImporters.length === 0}
          emptyMessage="No importer rows match your search."
        >
          {filteredImporters.map((row) => (
            <TableRow key={row.importerUuid} hover>
              <TableCell sx={{ fontWeight: 600 }}>
                {importerLabel(row.importerName, row.companyName)}
              </TableCell>
              <TableCell>{row.companyName?.trim() ? row.importerName : '—'}</TableCell>
              <TableCell>{formatMt(row.expectedKg)}</TableCell>
              <TableCell>{formatMt(row.actualKg)}</TableCell>
              <TableCell>{row.entryCount}</TableCell>
              <TableCell>{row.commodityCount}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="Import entries in pipeline"
          columns={['Reference', 'Importer', 'Agency', 'Stage', 'Commodity', 'Expected', 'Containers', 'Submitted']}
          isEmpty={report.entries.length === 0}
          emptyMessage="No active import entries awaiting warehouse intake."
        >
          {report.entries.map((entry) => (
            <TableRow key={entry.entryUuid} hover>
              <TableCell>{entry.referenceNo}</TableCell>
              <TableCell>
                <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>
                  {importerLabel(entry.importerName, entry.companyName)}
                </Typography>
                {entry.companyName?.trim() ? (
                  <Typography sx={{ fontSize: '0.7rem', color: portalColors.textMuted }}>
                    {entry.importerName}
                  </Typography>
                ) : null}
              </TableCell>
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
