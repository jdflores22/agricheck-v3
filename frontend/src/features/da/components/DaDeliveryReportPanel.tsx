import { Box, CircularProgress, Stack, Typography } from '@mui/material'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaContainerActualRoute } from '../api/daApi'
import { DaActualRouteMap } from './DaActualRouteMap'
import { DaDeliveryProofGallery } from './DaDeliveryProofGallery'
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
      </Typography>
      <DaActualRouteMap route={route} height={420} />
      <Typography sx={{ fontWeight: 700, fontSize: '0.9rem' }}>GPS coordinates (navigation start → warehouse)</Typography>
      <DaGpsTrailTable trail={route.trail} containerNumber={route.containerNumber} />
      <Typography sx={{ fontWeight: 700, fontSize: '0.9rem' }}>Arrival proof (geotagged photos from AgriTrack)</Typography>
      <DaDeliveryProofGallery photos={route.proofPhotos ?? []} />
    </Stack>
  )
}
