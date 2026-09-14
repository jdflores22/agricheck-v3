import AgricultureOutlinedIcon from '@mui/icons-material/AgricultureOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import MapOutlinedIcon from '@mui/icons-material/MapOutlined'
import PieChartOutlineOutlinedIcon from '@mui/icons-material/PieChartOutlineOutlined'
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
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import { DaAnalyticsKpiCard } from '../components/DaAgencyAnalyticsPanels'
import { useGetDaCommodityStockReportQuery, type DaCommodityStockRow } from '../api/daApi'

function formatMt(kg: number) {
  return `${(kg / 1000).toLocaleString(undefined, { maximumFractionDigits: 2 })} MT`
}

function formatKg(kg: number) {
  return `${kg.toLocaleString(undefined, { maximumFractionDigits: 0 })} kg`
}

function TrackSplitBar({ mavKg, regularKg }: { mavKg: number; regularKg: number }) {
  const total = mavKg + regularKg
  if (total <= 0) return <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>—</Typography>
  const mavPct = (mavKg / total) * 100
  return (
    <Stack spacing={0.5} sx={{ minWidth: 120 }}>
      <Box sx={{ height: 8, borderRadius: 99, overflow: 'hidden', bgcolor: portalAnalyticsColors.track, display: 'flex' }}>
        <Box sx={{ width: `${mavPct}%`, bgcolor: portalAnalyticsColors.darkest }} />
        <Box sx={{ width: `${100 - mavPct}%`, bgcolor: portalAnalyticsColors.pale }} />
      </Box>
      <Typography sx={{ fontSize: '0.7rem', color: portalColors.textMuted }}>
        MAV {mavPct.toFixed(0)}% · Reg {(100 - mavPct).toFixed(0)}%
      </Typography>
    </Stack>
  )
}

export function DaCommodityStockPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [search, setSearch] = useState(searchParams.get('q') ?? '')
  const hsCode = searchParams.get('hsCode') || undefined
  const commodityName = searchParams.get('commodityName') || undefined

  const { data, isLoading, isFetching } = useGetDaCommodityStockReportQuery({ hsCode, commodityName })
  const report = data?.data

  const filteredCommodities = useMemo(() => {
    if (!report) return []
    const term = search.trim().toLowerCase()
    if (!term) return report.commodities
    return report.commodities.filter(
      (row) =>
        row.commodityName.toLowerCase().includes(term)
        || row.hsCode.toLowerCase().includes(term),
    )
  }, [report, search])

  const selected = report?.commodities.find(
    (row) =>
      (hsCode && row.hsCode === hsCode)
      || (commodityName && row.commodityName === commodityName),
  )

  const agencyRows = useMemo(() => {
    if (!report || !selected) return []
    return report.byAgency.filter(
      (row) =>
        row.hsCode === selected.hsCode
        && row.commodityName === selected.commodityName,
    )
  }, [report, selected])

  const selectCommodity = (row: DaCommodityStockRow) => {
    const next = new URLSearchParams(searchParams)
    if (row.hsCode !== '—') next.set('hsCode', row.hsCode)
    next.set('commodityName', row.commodityName)
    setSearchParams(next, { replace: true })
  }

  const clearSelection = () => {
    const next = new URLSearchParams(searchParams)
    next.delete('hsCode')
    next.delete('commodityName')
    setSearchParams(next, { replace: true })
  }

  if (isLoading || !report) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  const mavShare = report.totalStockKg > 0 ? (report.totalMavStockKg / report.totalStockKg) * 100 : 0

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Presidential briefing"
        title="National Commodity Stock"
        subtitle="Actual physical stock nationwide by HS code and commodity — what is really in cold storage and bonded warehouses right now, split between MAV in-quota and regular out-quota imports."
      >
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mt: 1.5, alignItems: 'center' }}>
          <Chip
            size="small"
            label={`${report.commodityCount} commodities`}
            sx={{ bgcolor: portalAnalyticsColors.soft, color: portalAnalyticsColors.darkest, fontWeight: 700 }}
          />
          <Chip
            size="small"
            clickable
            component={RouterLink}
            to="/da/reports/import-pipeline"
            label="Expected vs actual"
            sx={{ fontWeight: 700 }}
          />
          <Chip
            size="small"
            clickable
            component={RouterLink}
            to="/da/reports/stock"
            icon={<MapOutlinedIcon />}
            label="Open stock map"
            sx={{ fontWeight: 700 }}
          />
          {selected ? (
            <Chip
              size="small"
              color="success"
              label={`${selected.commodityName}${selected.hsCode !== '—' ? ` · HS ${selected.hsCode}` : ''}`}
              onDelete={clearSelection}
            />
          ) : null}
          {isFetching ? <Chip size="small" label="Refreshing…" variant="outlined" /> : null}
        </Stack>
      </PortalPageHeader>

      <Grid container spacing={2} sx={{ mb: 4 }}>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Total physical stock"
            value={formatMt(report.totalStockKg)}
            meta={`${formatKg(report.totalStockKg)} in warehouses now`}
            icon={<Inventory2OutlinedIcon />}
            accent={portalAnalyticsColors.darkest}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="MAV in-quota stock"
            value={formatMt(report.totalMavStockKg)}
            meta={`${mavShare.toFixed(0)}% of stored volume`}
            icon={<PieChartOutlineOutlinedIcon />}
            accent={portalAnalyticsColors.dark}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Regular out-quota stock"
            value={formatMt(report.totalRegularStockKg)}
            meta="Does not reduce MAV remaining"
            icon={<AgricultureOutlinedIcon />}
            accent={portalAnalyticsColors.base}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Stored containers"
            value={report.storedContainers.toLocaleString()}
            meta={`${report.warehouseCount.toLocaleString()} active warehouses`}
            icon={<Inventory2OutlinedIcon />}
            accent={portalAnalyticsColors.mid}
          />
        </Grid>
      </Grid>

      <Box
        sx={{
          mb: 4,
          borderRadius: '0.875rem',
          border: `1px solid ${portalColors.border}`,
          bgcolor: portalColors.bgWhite,
          p: 2.5,
        }}
      >
        <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 1 }}>
          National stock composition
        </Typography>
        <LinearProgress
          variant="determinate"
          value={mavShare}
          sx={{
            height: 12,
            borderRadius: 99,
            bgcolor: portalAnalyticsColors.pale,
            '& .MuiLinearProgress-bar': { bgcolor: portalAnalyticsColors.darkest, borderRadius: 99 },
          }}
        />
        <Stack direction="row" spacing={1} sx={{ mt: 1.25, flexWrap: 'wrap', gap: 1 }}>
          <Chip size="small" label={`MAV ${formatMt(report.totalMavStockKg)}`} sx={{ bgcolor: portalAnalyticsColors.softStrong, fontWeight: 700 }} />
          <Chip size="small" label={`Regular ${formatMt(report.totalRegularStockKg)}`} sx={{ bgcolor: portalColors.bgMuted, fontWeight: 700 }} />
        </Stack>
      </Box>

      <Box sx={{ mb: 2 }}>
        <TextField
          size="small"
          label="Search commodity or HS code"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ maxWidth: 360, '& .MuiOutlinedInput-root': { borderRadius: '0.5rem' } }}
        />
      </Box>

      <PortalTablePanel
        title="By commodity"
        columns={['Commodity', 'HS Code', 'Physical stock', 'MAV', 'Regular', 'Track mix', 'Containers', 'Warehouses', 'Agencies']}
        isLoading={false}
        isEmpty={filteredCommodities.length === 0}
        emptyMessage="No stored commodity volume recorded yet."
      >
        {filteredCommodities.map((row) => (
          <TableRow
            key={`${row.hsCode}-${row.commodityName}`}
            hover
            selected={selected?.commodityName === row.commodityName && selected?.hsCode === row.hsCode}
            onClick={() => selectCommodity(row)}
            sx={{ cursor: 'pointer' }}
          >
            <TableCell sx={{ fontWeight: 600 }}>{row.commodityName}</TableCell>
            <TableCell>{row.hsCode}</TableCell>
            <TableCell sx={{ fontWeight: 700 }}>{formatMt(row.stockKg)}</TableCell>
            <TableCell>{formatMt(row.mavStockKg)}</TableCell>
            <TableCell>{formatMt(row.regularStockKg)}</TableCell>
            <TableCell><TrackSplitBar mavKg={row.mavStockKg} regularKg={row.regularStockKg} /></TableCell>
            <TableCell>{row.storedContainers}</TableCell>
            <TableCell>{row.warehouseCount}</TableCell>
            <TableCell>{row.agencyCount}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      {selected && agencyRows.length > 0 ? (
        <Box sx={{ mt: 3 }}>
          <PortalTablePanel
            title={`${selected.commodityName} — by supervising agency`}
            columns={['Agency', 'Physical stock', 'MAV', 'Regular', 'Containers', 'Warehouses']}
            isLoading={false}
            isEmpty={false}
          >
            {agencyRows.map((row) => (
              <TableRow key={`${row.agencyCode}-${row.hsCode}`} hover>
                <TableCell sx={{ fontWeight: 600 }}>{row.agencyCode}</TableCell>
                <TableCell>{formatMt(row.stockKg)}</TableCell>
                <TableCell>{formatMt(row.mavStockKg)}</TableCell>
                <TableCell>{formatMt(row.regularStockKg)}</TableCell>
                <TableCell>{row.storedContainers}</TableCell>
                <TableCell>{row.warehouseCount}</TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
        </Box>
      ) : null}
    </Box>
  )
}
