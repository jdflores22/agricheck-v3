import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
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
import { Link as RouterLink } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import {
  useClearDaDeliveryComplianceHoldMutation,
  useGetDaLogisticsLiveQuery,
  type DaLiveDelivery,
} from '../api/daApi'
import { DaLiveLogisticsMap } from '../components/DaLiveLogisticsMap'
import { resolveDriverGpsDisplay } from '../utils/daDriverGpsStatus'

function formatHoldReason(reason: string) {
  if (reason === 'off_route_blocked') return 'misroute'
  return reason.replace(/_/g, ' ')
}

function canOpenDeliveryReport(delivery: DaLiveDelivery) {
  return (
    !['Pending', 'ReadyForTransport', 'Released'].includes(delivery.status) &&
    (delivery.status !== 'Assigned' ||
      Boolean(delivery.lastLocationAt) ||
      (delivery.recentTrail?.length ?? 0) > 0)
  )
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
  const reportHref = `/da/logistics/containers/${delivery.containerUuid}/report`
  return (
    <ListItemButton selected={selected} onClick={onSelect} sx={{ borderRadius: 1, mb: 0.5, flexDirection: 'column', alignItems: 'stretch' }}>
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
      {canOpenDeliveryReport(delivery) && (
        <Button
          size="small"
          variant="text"
          component={RouterLink}
          to={reportHref}
          startIcon={<AssessmentOutlinedIcon />}
          sx={{ alignSelf: 'flex-start', mt: 0.5, fontSize: '0.75rem' }}
          onClick={(e) => e.stopPropagation()}
        >
          Open delivery report
        </Button>
      )}
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
        subtitle="Philippines map only. Blue = GPS trail. Open delivery report for full coordinates and geotag photos."
        actions={
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="contained" component={RouterLink} to="/da/logistics/reports">
              All delivery reports
            </Button>
            <Button
              size="small"
              variant="outlined"
              startIcon={<RefreshOutlinedIcon />}
              onClick={() => refetch()}
              disabled={isFetching}
            >
              Refresh
            </Button>
          </Stack>
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
          <List dense disablePadding sx={{ mb: 2, maxHeight: 280, overflow: 'auto' }}>
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
            Warehouse check-in (48h)
          </Typography>
          <List dense disablePadding sx={{ maxHeight: 220, overflow: 'auto' }}>
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
          <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted, mb: 1.5 }}>
            {selected.pickup.label} → {selected.destination.label}
          </Typography>
          {canOpenDeliveryReport(selected) ? (
            <Button
              variant="contained"
              component={RouterLink}
              to={`/da/logistics/containers/${selected.containerUuid}/report`}
              startIcon={<AssessmentOutlinedIcon />}
            >
              Open full delivery report (GPS points & geotag photos)
            </Button>
          ) : (
            <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted }}>
              GPS report unlocks after the driver starts navigation in AgriTrack.
            </Typography>
          )}
        </Box>
      )}
    </Box>
  )
}
