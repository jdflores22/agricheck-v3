import FullscreenOutlinedIcon from '@mui/icons-material/FullscreenOutlined'
import NotificationsActiveOutlinedIcon from '@mui/icons-material/NotificationsActiveOutlined'
import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined'
import RefreshOutlinedIcon from '@mui/icons-material/RefreshOutlined'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogContent,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  Typography,
} from '@mui/material'
import CloseIcon from '@mui/icons-material/Close'
import { Link as RouterLink } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import { useGetDaContainerActualRouteQuery, useGetDaLogisticsLiveQuery, type DaLiveDelivery } from '../api/daApi'
import { DaLiveGoogleMap } from '../components/DaLiveGoogleMap'
import { resolveDriverGpsDisplay } from '../utils/daDriverGpsStatus'

function canShowDeliveryReport(delivery: DaLiveDelivery) {
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
  const inTransit = snapshot?.inTransitDeliveries ?? []

  const [selectedUuid, setSelectedUuid] = useState<string | null>(null)
  const [fullMapOpen, setFullMapOpen] = useState(false)
  const selected = inTransit.find((d) => d.containerUuid === selectedUuid) ?? null
  const reportReady = selected != null && canShowDeliveryReport(selected)

  const { data: selectedRouteEnvelope } = useGetDaContainerActualRouteQuery(selectedUuid ?? '', {
    skip: !selectedUuid,
    pollingInterval: 15_000,
  })
  const selectedRoute = selectedRouteEnvelope?.data

  const mapDeliveries = useMemo(() => {
    if (!selectedUuid || !selected) return inTransit
    return [selected]
  }, [inTransit, selected, selectedUuid])

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
        subtitle="Google Maps view ng mga delivery na Picked up / In transit lang. Pickup (P) → destination (D), truck icon = driver. Compliance alerts at warehouse check-in nasa hiwalay na page."
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button
              size="small"
              variant="outlined"
              component={RouterLink}
              to="/da/logistics/alerts"
              startIcon={<NotificationsActiveOutlinedIcon />}
            >
              Alerts & holds
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

      <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2}>
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Stack direction="row" spacing={1} sx={{ mb: 1, justifyContent: 'flex-end' }}>
            <Button
              size="small"
              variant="contained"
              startIcon={<FullscreenOutlinedIcon />}
              onClick={() => setFullMapOpen(true)}
              disabled={inTransit.length === 0}
            >
              Full map
            </Button>
          </Stack>
          <DaLiveGoogleMap
            deliveries={mapDeliveries}
            selectedUuid={selectedUuid}
            serverTime={snapshot?.serverTime}
            height={520}
          />
          {selected && (
            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1}
              sx={{
                mt: 2,
                p: 1.5,
                borderRadius: 2,
                bgcolor: reportReady ? '#ECFDF5' : portalColors.bgWhite,
                border: `1px solid ${reportReady ? portalColors.primary : portalColors.border}`,
                alignItems: { sm: 'center' },
              }}
            >
              <Typography sx={{ flex: 1, fontWeight: 700, fontSize: '0.9rem' }}>
                {selected.containerNumber}
                {selectedRoute?.departureTime
                  ? ` · live leg since ${new Date(selectedRoute.departureTime).toLocaleString()}`
                  : ''}
              </Typography>
              <Button
                variant="contained"
                color="primary"
                component={RouterLink}
                to={`/da/logistics/containers/${selected.containerUuid}/report`}
                target="_blank"
                rel="noopener noreferrer"
                startIcon={<OpenInNewOutlinedIcon />}
                disabled={!reportReady}
              >
                Delivery report
              </Button>
            </Stack>
          )}
        </Box>
        <Box sx={{ width: { xs: '100%', lg: 340 }, flexShrink: 0 }}>
          <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: portalColors.textMuted, mb: 1 }}>
            In transit ({inTransit.length})
          </Typography>
          <List dense disablePadding sx={{ maxHeight: 560, overflow: 'auto' }}>
            {inTransit.length === 0 && (
              <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted, py: 2 }}>
                Walang live navigation ngayon. Kapag naka-pick up o in transit ang driver, lalabas dito.
              </Typography>
            )}
            {inTransit.map((d) => (
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

      <Dialog fullScreen open={fullMapOpen} onClose={() => setFullMapOpen(false)}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', px: 2, py: 1.5 }}>
          <Typography sx={{ fontWeight: 800 }}>Live delivery map — monitoring</Typography>
          <IconButton onClick={() => setFullMapOpen(false)} aria-label="Close full map">
            <CloseIcon />
          </IconButton>
        </Box>
        <DialogContent sx={{ p: 0, height: 'calc(100vh - 56px)' }}>
          <DaLiveGoogleMap
            deliveries={inTransit}
            selectedUuid={selectedUuid}
            serverTime={snapshot?.serverTime}
            height="100%"
          />
        </DialogContent>
      </Dialog>
    </Box>
  )
}
