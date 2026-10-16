import { Box, Chip, CircularProgress, Stack, Typography } from '@mui/material'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaContainerActualRoute } from '../api/daApi'
import { DaActualRouteMap } from './DaActualRouteMap'
import { DaDeliveryProofGallery } from './DaDeliveryProofGallery'
import { findGpsTrailOfflineGaps } from '../utils/daGpsTrailOfflineGaps'
import { resolveTrailPointCapturedOffline } from '../utils/daTrailPointStatus'
import { DaGpsTrailTable } from './DaGpsTrailTable'

function formatKm(km: number | null | undefined) {
  if (km == null) return '—'
  return `${km.toFixed(1)} km`
}

interface DaDeliveryReportPanelProps {
  route: DaContainerActualRoute | undefined
  loading?: boolean
}

export function DaDeliveryReportPanel({ route, loading }: DaDeliveryReportPanelProps) {
  if (loading && !route) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  if (!route) {
    return (
      <Typography sx={{ color: portalColors.textMuted }}>
        Delivery report not available for this container.
      </Typography>
    )
  }

  const proofCount = route.proofPhotos?.length ?? 0
  const offlineGaps = findGpsTrailOfflineGaps(route.trail)
  const queuedOfflineCount = route.trail.filter((p) => resolveTrailPointCapturedOffline(p)).length
  const onlineCount = route.trail.length - queuedOfflineCount

  return (
    <Stack spacing={2}>
      <Typography sx={{ fontSize: '0.9rem', color: portalColors.textMuted }}>
        {route.pickup.label} → {route.destination.label}
        {route.driverName ? ` · ${route.driverName}` : ''}
        {route.vehiclePlate ? ` · ${route.vehiclePlate}` : ''}
      </Typography>
      <Typography sx={{ fontSize: '0.85rem' }}>
        GPS trail: <strong>{formatKm(route.distanceKm)}</strong> · <strong>{route.pointCount}</strong> points · status{' '}
        <strong>{route.status}</strong>
        {route.warehouseHandoffEligibleAt
          ? ` · warehouse check-in ${new Date(route.warehouseHandoffEligibleAt).toLocaleString()}`
          : ''}
        {route.arrivalTime ? ` · received ${new Date(route.arrivalTime).toLocaleString()}` : ''}
        {proofCount > 0 ? ` · ${proofCount} geotag photo(s) on file` : ' · no geotag photos saved yet'}
        {route.trail.length > 0
          ? ` · coordinates: ${onlineCount} online, ${queuedOfflineCount} offline (queued on device)`
          : ''}
      </Typography>
      <DaActualRouteMap route={route} height={420} />
      <Stack direction="row" spacing={1.5} sx={{ flexWrap: 'wrap', alignItems: 'center', gap: 1 }}>
        <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
          <Box sx={{ width: 28, height: 4, borderRadius: 1, bgcolor: '#2563EB' }} />
          <Typography sx={{ fontSize: '0.8rem', color: portalColors.textMuted }}>Online (live sync)</Typography>
        </Stack>
        <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
          <Box sx={{ width: 28, height: 0, borderTop: '4px dashed #EA580C' }} />
          <Typography sx={{ fontSize: '0.8rem', color: portalColors.textMuted }}>Offline queued (no network)</Typography>
        </Stack>
        <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
          <Box sx={{ width: 28, height: 0, borderTop: '4px dashed #DC2626' }} />
          <Typography sx={{ fontSize: '0.8rem', color: portalColors.textMuted }}>No GPS fix (gap ≥ 3 min)</Typography>
        </Stack>
      </Stack>
      {offlineGaps.length > 0 && (
        <Box
          sx={{
            p: 1.5,
            borderRadius: 2,
            border: `1px solid #FECACA`,
            bgcolor: '#FEF2F2',
          }}
        >
          <Typography sx={{ fontWeight: 700, fontSize: '0.9rem', mb: 1 }}>
            GPS offline periods ({offlineGaps.length})
          </Typography>
          <Stack spacing={1}>
            {offlineGaps.map((gap) => (
              <Typography key={gap.gapIndex} sx={{ fontSize: '0.85rem' }}>
                <Chip
                  size="small"
                  label={`#${gap.gapIndex}`}
                  sx={{ mr: 1, height: 22, bgcolor: '#FEE2E2', color: '#991B1B', fontWeight: 700 }}
                />
                Offline ~<strong>{gap.durationMinutes} min</strong> · last ping{' '}
                <strong>{new Date(gap.offlineStartedAt).toLocaleString()}</strong> → back online{' '}
                <strong>{new Date(gap.backOnlineAt).toLocaleString()}</strong>
              </Typography>
            ))}
          </Stack>
        </Box>
      )}
      <Typography sx={{ fontWeight: 700, fontSize: '0.9rem' }}>GPS coordinates (navigation start → warehouse)</Typography>
      <DaGpsTrailTable trail={route.trail} containerNumber={route.containerNumber} offlineGaps={offlineGaps} />
      <Typography sx={{ fontWeight: 700, fontSize: '0.9rem' }}>Arrival proof (geotagged photos from AgriTrack)</Typography>
      <DaDeliveryProofGallery photos={route.proofPhotos ?? []} />
    </Stack>
  )
}
