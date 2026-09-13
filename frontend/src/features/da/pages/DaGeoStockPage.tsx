import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import MapOutlinedIcon from '@mui/icons-material/MapOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import {
  Box,
  Breadcrumbs,
  Chip,
  CircularProgress,
  Grid,
  LinearProgress,
  Link,
  Stack,
  TableCell,
  TableRow,
  Typography,
} from '@mui/material'
import { useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import { DaAnalyticsKpiCard } from '../components/DaAgencyAnalyticsPanels'
import {
  useGetDaGeoStockReportQuery,
  type DaGeoStockCommodity,
  type DaGeoStockLocation,
  type DaGeoStockQuery,
} from '../api/daApi'

function formatKg(value: number) {
  return `${value.toLocaleString(undefined, { maximumFractionDigits: 0 })} KG`
}

function optionalId(value: string | null) {
  if (!value) return undefined
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : undefined
}

function childLabel(level: string) {
  if (level === 'nation') return 'Regions'
  if (level === 'region') return 'Provinces'
  if (level === 'province') return 'Cities / Municipalities'
  if (level === 'city') return 'Barangays'
  return 'Locations'
}

function queryFromSearch(searchParams: URLSearchParams): DaGeoStockQuery {
  return {
    regionId: optionalId(searchParams.get('regionId')),
    provinceId: optionalId(searchParams.get('provinceId')),
    cityId: optionalId(searchParams.get('cityId')),
    barangayId: optionalId(searchParams.get('barangayId')),
    hsCode: searchParams.get('hsCode') || undefined,
    commodityName: searchParams.get('commodityName') || undefined,
  }
}

function toSearch(query: DaGeoStockQuery) {
  const next = new URLSearchParams()
  if (query.regionId) next.set('regionId', String(query.regionId))
  if (query.provinceId) next.set('provinceId', String(query.provinceId))
  if (query.cityId) next.set('cityId', String(query.cityId))
  if (query.barangayId) next.set('barangayId', String(query.barangayId))
  if (query.hsCode) next.set('hsCode', query.hsCode)
  if (query.commodityName) next.set('commodityName', query.commodityName)
  return next
}

function UtilizationBar({ value }: { value: number }) {
  return (
    <Stack spacing={0.5} sx={{ minWidth: 88 }}>
      <LinearProgress
        variant="determinate"
        value={Math.max(0, Math.min(100, value))}
        sx={{
          height: 6,
          borderRadius: 99,
          bgcolor: portalAnalyticsColors.track,
          '& .MuiLinearProgress-bar': { bgcolor: portalAnalyticsColors.dark, borderRadius: 99 },
        }}
      />
      <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{value.toFixed(1)}%</Typography>
    </Stack>
  )
}

export function DaGeoStockPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const query = useMemo(() => queryFromSearch(searchParams), [searchParams])
  const { data, isLoading, isFetching } = useGetDaGeoStockReportQuery(query)
  const report = data?.data

  const setQuery = (next: DaGeoStockQuery) => setSearchParams(toSearch(next), { replace: true })

  const drillLocation = (location: DaGeoStockLocation) => {
    if (location.id == null) return
    if (location.level === 'region') {
      setQuery({ regionId: location.id, hsCode: query.hsCode, commodityName: query.commodityName })
      return
    }
    if (location.level === 'province') {
      setQuery({
        regionId: query.regionId,
        provinceId: location.id,
        hsCode: query.hsCode,
        commodityName: query.commodityName,
      })
      return
    }
    if (location.level === 'city') {
      setQuery({
        regionId: query.regionId,
        provinceId: query.provinceId,
        cityId: location.id,
        hsCode: query.hsCode,
        commodityName: query.commodityName,
      })
      return
    }
    setQuery({
      regionId: query.regionId,
      provinceId: query.provinceId,
      cityId: query.cityId,
      barangayId: location.id,
      hsCode: query.hsCode,
      commodityName: query.commodityName,
    })
  }

  const selectCommodity = (commodity: DaGeoStockCommodity) => {
    setQuery({
      ...query,
      hsCode: commodity.hsCode === '—' ? undefined : commodity.hsCode,
      commodityName: commodity.commodityName,
    })
  }

  if (isLoading || !report) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  const filteredCommodity = report.commodities.find(
    (item) =>
      (query.hsCode && item.hsCode === query.hsCode)
      || (query.commodityName && item.commodityName === query.commodityName),
  )

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Presidential briefing"
        title="Nationwide Stock Map"
        subtitle="First view is the whole Philippines by region. Open a region to see where commodity volume sits — province, city/municipality, then barangay — using HS code and warehouse utilization."
      >
        <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1, mt: 1.5, alignItems: 'center' }}>
          <Chip size="small" label={report.scopeLabel} sx={{ bgcolor: portalAnalyticsColors.soft, color: portalAnalyticsColors.darkest, fontWeight: 700 }} />
          {filteredCommodity ? (
            <Chip
              size="small"
              color="success"
              label={`${filteredCommodity.commodityName}${filteredCommodity.hsCode !== '—' ? ` · HS ${filteredCommodity.hsCode}` : ''} · ${formatKg(filteredCommodity.volumeKg)}`}
              onDelete={() => setQuery({ ...query, hsCode: undefined, commodityName: undefined })}
            />
          ) : null}
        </Stack>
      </PortalPageHeader>

      <Breadcrumbs sx={{ mb: 3 }}>
        {report.path.map((crumb, index) => {
          const isLast = index === report.path.length - 1
          if (isLast) {
            return (
              <Typography key={`${crumb.level}-${crumb.id ?? 'root'}`} sx={{ fontWeight: 700, color: portalColors.textDark }}>
                {crumb.name}
              </Typography>
            )
          }

          return (
            <Link
              key={`${crumb.level}-${crumb.id ?? 'root'}`}
              component="button"
              underline="hover"
              onClick={() => {
                if (crumb.level === 'nation') {
                  setQuery({ hsCode: query.hsCode, commodityName: query.commodityName })
                  return
                }
                if (crumb.level === 'region') {
                  setQuery({ regionId: crumb.id ?? undefined, hsCode: query.hsCode, commodityName: query.commodityName })
                  return
                }
                if (crumb.level === 'province') {
                  setQuery({
                    regionId: query.regionId,
                    provinceId: crumb.id ?? undefined,
                    hsCode: query.hsCode,
                    commodityName: query.commodityName,
                  })
                  return
                }
                setQuery({
                  regionId: query.regionId,
                  provinceId: query.provinceId,
                  cityId: crumb.id ?? undefined,
                  hsCode: query.hsCode,
                  commodityName: query.commodityName,
                })
              }}
              sx={{ color: portalAnalyticsColors.dark, fontWeight: 600 }}
            >
              {crumb.name}
            </Link>
          )
        })}
      </Breadcrumbs>

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label={`Stock in ${report.scopeLabel}`}
            value={formatKg(report.totalVolumeKg)}
            meta={filteredCommodity ? `${filteredCommodity.commodityName} in this area` : 'All commodities currently stored'}
            icon={<Inventory2OutlinedIcon />}
            accent={portalAnalyticsColors.darkest}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Warehouse utilization"
            value={`${report.utilizationPercent.toFixed(1)}%`}
            meta={`${report.storedContainers.toLocaleString()} of ${report.capacity.toLocaleString()} container slots`}
            icon={<WarehouseOutlinedIcon />}
            accent={portalAnalyticsColors.dark}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Warehouses"
            value={report.warehouseCount.toLocaleString()}
            meta={`Active facilities in ${report.scopeLabel}`}
            icon={<MapOutlinedIcon />}
            accent={portalAnalyticsColors.base}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, xl: 3 }}>
          <DaAnalyticsKpiCard
            label="Commodities on hand"
            value={report.commodities.length.toLocaleString()}
            meta="Grouped by HS code and commodity"
            icon={<Inventory2OutlinedIcon />}
            accent={portalAnalyticsColors.mid}
          />
        </Grid>
      </Grid>

      {isFetching ? <LinearProgress sx={{ mb: 2, borderRadius: 99 }} /> : null}

      <Grid container spacing={3}>
        <Grid size={{ xs: 12, lg: 8 }}>
          {report.level === 'nation' ? (
            <Box>
              <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, letterSpacing: '0.12em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 1.5 }}>
                Regions of the Philippines
              </Typography>
              <Grid container spacing={1.5}>
                {report.locations.map((location) => (
                  <Grid key={`${location.level}-${location.id ?? location.name}`} size={{ xs: 12, sm: 6, xl: 4 }}>
                    <Box
                      component="button"
                      onClick={() => drillLocation(location)}
                      sx={{
                        display: 'block',
                        width: '100%',
                        textAlign: 'left',
                        p: 2,
                        borderRadius: '0.875rem',
                        border: `1px solid ${portalColors.border}`,
                        bgcolor: portalColors.bgWhite,
                        cursor: location.id == null ? 'default' : 'pointer',
                        transition: 'border-color 0.15s ease, box-shadow 0.15s ease',
                        '&:hover': location.id == null
                          ? undefined
                          : {
                              borderColor: portalAnalyticsColors.softStrong,
                              boxShadow: '0 8px 20px rgba(22, 101, 52, 0.08)',
                            },
                      }}
                    >
                      <Typography sx={{ fontWeight: 700, color: portalAnalyticsColors.darkest }}>{location.name}</Typography>
                      <Typography sx={{ mt: 0.75, fontSize: '1.35rem', fontWeight: 800, color: portalColors.textDark }}>
                        {formatKg(location.volumeKg)}
                      </Typography>
                      <Typography sx={{ mt: 0.25, fontSize: '0.8125rem', color: portalColors.textMuted }}>
                        {location.topCommodities[0]
                          ? `${location.topCommodities[0].commodityName}${location.topCommodities[0].hsCode !== '—' ? ` · HS ${location.topCommodities[0].hsCode}` : ''}`
                          : 'No stored commodity yet'}
                      </Typography>
                      <Stack direction="row" sx={{ mt: 1.25, justifyContent: 'space-between', alignItems: 'center', gap: 1 }}>
                        <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
                          {location.warehouseCount} warehouse{location.warehouseCount === 1 ? '' : 's'}
                        </Typography>
                        <UtilizationBar value={location.utilizationPercent} />
                      </Stack>
                    </Box>
                  </Grid>
                ))}
              </Grid>
            </Box>
          ) : (
            <PortalTablePanel
              title={childLabel(report.level)}
              columns={['Location', 'Stock', 'Top commodity', 'Warehouses', 'Utilization']}
              isLoading={false}
              isEmpty={report.locations.length === 0 && report.warehouses.length === 0}
              emptyMessage="No warehouse stock in this area yet."
            >
              {report.locations.map((location) => (
                <TableRow
                  key={`${location.level}-${location.id ?? location.name}`}
                  hover
                  onClick={() => drillLocation(location)}
                  sx={{ cursor: location.id == null ? 'default' : 'pointer' }}
                >
                  <TableCell sx={{ fontWeight: 700 }}>{location.name}</TableCell>
                  <TableCell>{formatKg(location.volumeKg)}</TableCell>
                  <TableCell>
                    {location.topCommodities[0]
                      ? `${location.topCommodities[0].commodityName}${location.topCommodities[0].hsCode !== '—' ? ` (HS ${location.topCommodities[0].hsCode})` : ''}`
                      : '—'}
                  </TableCell>
                  <TableCell>{location.warehouseCount}</TableCell>
                  <TableCell><UtilizationBar value={location.utilizationPercent} /></TableCell>
                </TableRow>
              ))}
            </PortalTablePanel>
          )}

          {report.warehouses.length > 0 ? (
            <Box sx={{ mt: 3 }}>
              <PortalTablePanel
                title="Warehouses in this area"
                columns={['Warehouse', 'Barangay', 'Commodity', 'Stock', 'Slots', 'Utilization']}
                isLoading={false}
                isEmpty={false}
                emptyMessage="No warehouses."
              >
                {report.warehouses.map((warehouse) => (
                  <TableRow key={warehouse.id} hover onClick={() => navigate(`/da/warehouses/${warehouse.id}`)} sx={{ cursor: 'pointer' }}>
                    <TableCell sx={{ fontWeight: 700 }}>{warehouse.code} — {warehouse.name}</TableCell>
                    <TableCell>{warehouse.barangayName ?? '—'}</TableCell>
                    <TableCell>
                      {warehouse.topCommodityName
                        ? `${warehouse.topCommodityName}${warehouse.topCommodityHsCode ? ` · HS ${warehouse.topCommodityHsCode}` : ''}`
                        : '—'}
                    </TableCell>
                    <TableCell>{formatKg(warehouse.volumeKg)}</TableCell>
                    <TableCell>{warehouse.storedContainers} / {warehouse.capacity}</TableCell>
                    <TableCell><UtilizationBar value={warehouse.utilizationPercent} /></TableCell>
                  </TableRow>
                ))}
              </PortalTablePanel>
            </Box>
          ) : null}
        </Grid>

        <Grid size={{ xs: 12, lg: 4 }}>
          <Box
            sx={{
              borderRadius: '0.875rem',
              border: `1px solid ${portalColors.border}`,
              bgcolor: portalColors.bgWhite,
              p: 2.5,
            }}
          >
            <Typography sx={{ fontWeight: 700, color: portalAnalyticsColors.darkest, mb: 0.5 }}>
              Commodities in {report.scopeLabel}
            </Typography>
            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mb: 2, lineHeight: 1.5 }}>
              Select a commodity to see where that volume is sitting. Example: NCR Pork 150,000 KG, then open the cities and barangays that hold it.
            </Typography>
            {report.commodities.length === 0 ? (
              <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>No stored commodity volume yet.</Typography>
            ) : (
              <Stack spacing={1.25}>
                {report.commodities.map((commodity) => {
                  const selected = query.hsCode === commodity.hsCode || query.commodityName === commodity.commodityName
                  return (
                    <Box
                      key={`${commodity.hsCode}-${commodity.commodityName}`}
                      component="button"
                      onClick={() => selectCommodity(commodity)}
                      sx={{
                        display: 'block',
                        width: '100%',
                        textAlign: 'left',
                        p: 1.5,
                        borderRadius: '0.75rem',
                        border: `1px solid ${selected ? portalAnalyticsColors.softStrong : portalColors.border}`,
                        bgcolor: selected ? portalAnalyticsColors.soft : portalColors.bgWhite,
                        cursor: 'pointer',
                      }}
                    >
                      <Typography sx={{ fontWeight: 700, color: portalColors.textDark }}>{commodity.commodityName}</Typography>
                      <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
                        {commodity.hsCode === '—' ? 'No HS code' : `HS ${commodity.hsCode}`} · {commodity.warehouseCount} warehouse{commodity.warehouseCount === 1 ? '' : 's'}
                      </Typography>
                      <Typography sx={{ mt: 0.5, fontWeight: 800, color: portalAnalyticsColors.darkest }}>
                        {formatKg(commodity.volumeKg)}
                      </Typography>
                    </Box>
                  )
                })}
              </Stack>
            )}
          </Box>
        </Grid>
      </Grid>
    </Box>
  )
}
