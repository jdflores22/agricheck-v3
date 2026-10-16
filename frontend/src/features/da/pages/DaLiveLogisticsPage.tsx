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
import { DaActualRouteMap } from '../components/DaActualRouteMap'
import { DaDeliveryProofGallery } from '../components/DaDeliveryProofGallery'
import { DaLiveLogisticsMap } from '../components/DaLiveLogisticsMap'
import { resolveDriverGpsDisplay } from '../utils/daDriverGpsStatus'

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
  serverTime,
}: {
  delivery: DaLiveDelivery
  selected: boolean
  onSelect: () => void
  serverTime?: string | null
}) {
  const gps = resolveDriverGpsDisplay(delivery, serverTime)
  return (
    <ListItemButton selected={selected} onClick={onSelect} sx={{ borderRadius: 1, mb: 0.5 }}>
      <ListItemText
        primary={
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center', flexWrap: 'wrap' }}>
            <span>{delivery.containerNumber}</span>
            {gps.offline && (
              <Chip size="small" label={gps.label} color="default" sx={{ height: 20, fontSize: '0.65rem' }} />
            )}
          </Stack>
        }
        secondary={
          <>
            {delivery.entryReference} · {delivery.status}
            {delivery.driverName ? ` · ${delivery.driverName}` : ''}
            {!gps.offline && delivery.lastLocationAt ? ' · Live GPS' : ''}
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
  const fetchDeliveryReport =
    selected != null &&
    !['Assigned', 'Pending', 'ReadyForTransport', 'Released'].includes(selected.status)

  const { data: routeData, isLoading: routeLoading } = useGetDaContainerActualRouteQuery(selectedUuid ?? '', {
    skip: !selectedUuid || !fetchDeliveryReport,
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
        subtitle="Philippines map only. Live: green route, blue GPS trail, gray dot = offline 3+ min. Delivery report below: full GPS path + geotagged pickup/warehouse photos from AgriTrack."
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
          <DaLiveLogisticsMap
            deliveries={allOnMap}
            selectedUuid={selectedUuid}
            serverTime={snapshot?.serverTime}
            height={560}
          />
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
                serverTime={snapshot?.serverTime}
              />
            ))}
          </List>

          <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: portalColors.textMuted, mb: 1 }}>
            Warehouse check-in (48h) — route & proof
          </Typography>
          <List dense disablePadding sx={{ maxHeight: 200, overflow: 'auto' }}>
            {arrived.map((d) => (
              <DeliveryListItem
                key={d.containerUuid}
                delivery={d}
                selected={selectedUuid === d.containerUuid}
                onSelect={() => setSelectedUuid(d.containerUuid)}
                serverTime={snapshot?.serverTime}
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
          <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center', flexWrap: 'wrap' }}>
            <MapOutlinedIcon fontSize="small" color="primary" />
            <Typography sx={{ fontWeight: 800 }}>{selected.containerNumber}</Typography>
            <Chip size="small" label={selected.status} />
            {(() => {
              const gps = resolveDriverGpsDisplay(selected, snapshot?.serverTime)
              return (
                <Chip
                  size="small"
                  label={gps.label}
                  color={gps.offline ? 'warning' : 'success'}
                  variant={gps.offline ? 'filled' : 'outlined'}
                />
              )
            })()}
          </Stack>
          <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted, mb: 1 }}>
            {selected.pickup.label} → {selected.destination.label}
          </Typography>
          {!fetchDeliveryReport && (
            <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted }}>
              Delivery report (GPS path + geotag photos) appears after the driver confirms pickup in AgriTrack.
            </Typography>
          )}
          {fetchDeliveryReport && (
            <Box sx={{ mt: 2 }}>
              <Typography sx={{ fontWeight: 800, fontSize: '0.95rem', mb: 1 }}>Delivery report</Typography>
              {routeLoading && <CircularProgress size={22} sx={{ mb: 2 }} />}
              {actualRoute && (
                <>
                  <Typography sx={{ fontSize: '0.85rem', mb: 1.5 }}>
                    GPS trail: <strong>{formatKm(actualRoute.distanceKm)}</strong> ·{' '}
                    <strong>{actualRoute.pointCount}</strong> points
                    {actualRoute.warehouseHandoffEligibleAt
                      ? ` · warehouse check-in ${new Date(actualRoute.warehouseHandoffEligibleAt).toLocaleString()}`
                      : ''}
                    {actualRoute.arrivalTime
                      ? ` · warehouse received ${new Date(actualRoute.arrivalTime).toLocaleString()}`
                      : ''}
                  </Typography>
                  <DaActualRouteMap route={actualRoute} height={380} />
                  <Typography sx={{ fontWeight: 700, fontSize: '0.85rem', mt: 2, mb: 1 }}>
                    Arrival proof (geotagged photos)
                  </Typography>
                  <DaDeliveryProofGallery photos={actualRoute.proofPhotos ?? []} />
                </>
              )}
            </Box>
          )}
        </Box>
      )}
    </Box>
  )
}
