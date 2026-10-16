import MapOutlinedIcon from '@mui/icons-material/MapOutlined'
import RefreshOutlinedIcon from '@mui/icons-material/RefreshOutlined'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  Typography,
} from '@mui/material'
import { useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import {
  useClearDaDeliveryComplianceHoldMutation,
  useGetDaContainerActualRouteQuery,
  useGetDaLogisticsLiveQuery,
  type DaLiveDelivery,
} from '../api/daApi'
import { DaLiveLogisticsMap } from '../components/DaLiveLogisticsMap'

function formatHoldReason(reason: string) {
  if (reason === 'off_route_blocked') return 'misroute'
  return reason.replace(/_/g, ' ')
}

function formatKm(km: number | null | undefined) {
  if (km == null) return '—'
  return `${km.toFixed(1)} km`
}

function DeliveryListItem({
  delivery,
  selected,
  onSelect,
}: {
  delivery: DaLiveDelivery
  selected: boolean
  onSelect: () => void
}) {
  return (
    <ListItemButton selected={selected} onClick={onSelect} sx={{ borderRadius: 1, mb: 0.5 }}>
      <ListItemText
        primary={delivery.containerNumber}
        secondary={
          <>
            {delivery.entryReference} · {delivery.status}
            {delivery.driverName ? ` · ${delivery.driverName}` : ''}
          </>
        }
        slotProps={{
          primary: { sx: { fontWeight: 700, fontSize: '0.9rem' } },
          secondary: { sx: { fontSize: '0.75rem' } },
        }}
      />
    </ListItemButton>
  )
}

export function DaLiveLogisticsPage() {
  const { data, isLoading, isFetching, refetch } = useGetDaLogisticsLiveQuery(undefined, {
    pollingInterval: 15_000,
  })
  const snapshot = data?.data
  const active = snapshot?.activeDeliveries ?? []
  const arrived = snapshot?.recentlyArrived ?? []
  const complianceAlerts = snapshot?.complianceAlerts ?? []
  const activeHolds = snapshot?.activeComplianceHolds ?? []
  const [clearHold] = useClearDaDeliveryComplianceHoldMutation()
  const allOnMap = useMemo(() => [...active, ...arrived], [active, arrived])

  const [selectedUuid, setSelectedUuid] = useState<string | null>(null)
  const selected = allOnMap.find((d) => d.containerUuid === selectedUuid) ?? null
  const showActualRoute =
    selected != null && (selected.status === 'AtWarehouse' || arrived.some((a) => a.containerUuid === selectedUuid))

  const { data: routeData, isLoading: routeLoading } = useGetDaContainerActualRouteQuery(selectedUuid ?? '', {
    skip: !selectedUuid || !showActualRoute,
  })
  const actualRoute = routeData?.data

  if (isLoading && !snapshot) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="DA Secretary · Logistics"
        title="Live delivery map"
        subtitle="Bird’s-eye view of drivers with the app active — actual GPS trail from AgriTrack. Not available to operators."
        actions={
          <Button
            size="small"
            variant="outlined"
            startIcon={<RefreshOutlinedIcon />}
            onClick={() => refetch()}
            disabled={isFetching}
          >
            Refresh
          </Button>
        }
      />

      {activeHolds.length > 0 && (
        <Box sx={{ mb: 2, p: 2, borderRadius: 2, border: `2px solid #DC2626`, bgcolor: '#FEF2F2' }}>
          <Typography sx={{ fontWeight: 800, mb: 1 }}>Active compliance holds — trip cannot end</Typography>
          <Typography sx={{ fontSize: '0.8rem', color: portalColors.textMuted, mb: 1.5 }}>
            NMIS/Doctor handoff is blocked until DA clears misroute or wrong-warehouse flags.
          </Typography>
          {activeHolds.map((h) => (
            <Stack
              key={h.containerUuid}
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1}
              sx={{ mb: 1, alignItems: { sm: 'center' } }}
            >
              <Typography sx={{ flex: 1, fontSize: '0.85rem' }}>
                <strong>{h.containerNumber}</strong> · {formatHoldReason(h.holdReason)} · {new Date(h.holdAt).toLocaleString()}
                {h.driverName ? ` · ${h.driverName}` : ''}
              </Typography>
              <Button
                size="small"
                variant="contained"
                color="primary"
                onClick={() =>
                  clearHold({
                    uuid: h.containerUuid,
                    clearanceNotes: 'DA authority reviewed route/warehouse compliance.',
                  })
                }
              >
                Clear hold
              </Button>
            </Stack>
          ))}
        </Box>
      )}

      {complianceAlerts.length > 0 && (
        <Box sx={{ mb: 2, p: 2, borderRadius: 2, border: `1px solid ${portalColors.border}`, bgcolor: '#FEF2F2' }}>
          <Typography sx={{ fontWeight: 800, mb: 1, fontSize: '0.9rem' }}>Compliance alerts (24h)</Typography>
          {complianceAlerts.slice(0, 8).map((a) => (
            <Typography key={`${a.containerUuid}-${a.recordedAt}`} sx={{ fontSize: '0.8rem', mb: 0.5 }}>
              {a.containerNumber} · {a.eventType}
              {a.details ? ` — ${a.details}` : ''} · {new Date(a.recordedAt).toLocaleString()}
            </Typography>
          ))}
        </Box>
      )}

      <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2} sx={{ mb: 2 }}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <DaLiveLogisticsMap deliveries={allOnMap} selectedUuid={selectedUuid} height={560} />
        </Box>
        <Box sx={{ width: { xs: '100%', lg: 340 }, flexShrink: 0 }}>
          <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: portalColors.textMuted, mb: 1 }}>
            In progress ({active.length})
          </Typography>
          <List dense disablePadding sx={{ mb: 2, maxHeight: 240, overflow: 'auto' }}>
            {active.length === 0 && (
              <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted, py: 2 }}>
                No active deliveries with an assigned driver right now.
              </Typography>
            )}
            {active.map((d) => (
              <DeliveryListItem
                key={d.containerUuid}
                delivery={d}
                selected={selectedUuid === d.containerUuid}
                onSelect={() => setSelectedUuid(d.containerUuid)}
              />
            ))}
          </List>

          <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: portalColors.textMuted, mb: 1 }}>
            Arrived (48h) — actual path report
          </Typography>
          <List dense disablePadding sx={{ maxHeight: 200, overflow: 'auto' }}>
            {arrived.map((d) => (
              <DeliveryListItem
                key={d.containerUuid}
                delivery={d}
                selected={selectedUuid === d.containerUuid}
                onSelect={() => setSelectedUuid(d.containerUuid)}
              />
            ))}
          </List>
        </Box>
      </Stack>

      {selected && (
        <Box
          sx={{
            p: 2,
            borderRadius: 2,
            border: `1px solid ${portalColors.border}`,
            bgcolor: portalColors.bgWhite,
          }}
        >
          <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center' }}>
            <MapOutlinedIcon fontSize="small" color="primary" />
            <Typography sx={{ fontWeight: 800 }}>{selected.containerNumber}</Typography>
            <Chip size="small" label={selected.status} />
          </Stack>
          <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted, mb: 1 }}>
            {selected.pickup.label} → {selected.destination.label}
          </Typography>
          {showActualRoute && (
            <>
              {routeLoading && <CircularProgress size={20} />}
              {actualRoute && (
                <Typography sx={{ fontSize: '0.85rem' }}>
                  Actual route: <strong>{formatKm(actualRoute.distanceKm)}</strong> ·{' '}
                  <strong>{actualRoute.pointCount}</strong> GPS points
                  {actualRoute.arrivalTime ? ` · arrived ${new Date(actualRoute.arrivalTime).toLocaleString()}` : ''}
                </Typography>
              )}
            </>
          )}
          {!showActualRoute && (
            <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted }}>
              Trail updates every ~15s while the delivery is active. Select an arrived container for the full actual-path
              report.
            </Typography>
          )}
        </Box>
      )}
    </Box>
  )
}
