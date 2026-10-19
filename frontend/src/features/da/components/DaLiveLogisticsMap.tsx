import { Box, Stack, Typography } from '@mui/material'
import L from 'leaflet'
import { Fragment, useEffect, useMemo } from 'react'
import { CircleMarker, MapContainer, Marker, Popup, TileLayer, useMap } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaContainerTrailPoint, DaDeliveryProofPhoto, DaLiveDelivery } from '../api/daApi'
import { DaGpsTrailMapLayers } from './DaGpsTrailMapLayers'
import { resolveDriverGpsDisplay } from '../utils/daDriverGpsStatus'
import {
  isUsablePhilippinesCoordinate,
  PHILIPPINES_BOUNDS,
  PHILIPPINES_MAP_CENTER,
  PHILIPPINES_MAP_MIN_ZOOM,
} from '../utils/philippinesMap'

function PhilippinesDefaultView() {
  const map = useMap()
  useEffect(() => {
    map.setView(PHILIPPINES_MAP_CENTER, 6)
  }, [map])
  return null
}

function pinIcon(color: string, label: string) {
  return L.divIcon({
    className: '',
    html: `
      <div style="
        min-width: 28px;
        height: 28px;
        padding: 0 6px;
        border-radius: 999px;
        background: ${color};
        border: 3px solid #ffffff;
        box-shadow: 0 4px 12px rgba(28, 25, 23, 0.28);
        color: #fff;
        font-size: 10px;
        font-weight: 800;
        display: flex;
        align-items: center;
        justify-content: center;
      ">${label}</div>
    `,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
  })
}

const pickupIcon = pinIcon('#15803D', 'P')
const warehouseIcon = pinIcon('#F97316', 'W')
const driverLiveIcon = pinIcon('#2563EB', '●')
const driverOfflineIcon = pinIcon('#94A3B8', '●')
const proofIcon = pinIcon('#7C3AED', '📷')

function FitAllDeliveries({
  deliveries,
  selectedUuid,
  fullTrailByUuid,
  proofPhotosByUuid,
}: {
  deliveries: DaLiveDelivery[]
  selectedUuid: string | null
  fullTrailByUuid?: Record<string, DaContainerTrailPoint[]>
  proofPhotosByUuid?: Record<string, DaDeliveryProofPhoto[]>
}) {
  const map = useMap()
  useEffect(() => {
    const points: L.LatLngExpression[] = []
    for (const d of deliveries) {
      if (d.currentLatitude != null && d.currentLongitude != null) {
        const cLat = Number(d.currentLatitude)
        const cLng = Number(d.currentLongitude)
        if (isUsablePhilippinesCoordinate(cLat, cLng)) points.push([cLat, cLng])
      }
      const full = fullTrailByUuid?.[d.containerUuid]
      const trailPoints = full && full.length > 0 ? full : d.recentTrail
      for (const p of trailPoints) {
        const lat = Number(p.latitude)
        const lng = Number(p.longitude)
        if (isUsablePhilippinesCoordinate(lat, lng)) points.push([lat, lng])
      }
      const pLat = Number(d.pickup.latitude)
      const pLng = Number(d.pickup.longitude)
      if (isUsablePhilippinesCoordinate(pLat, pLng)) points.push([pLat, pLng])
      const dLat = Number(d.destination.latitude)
      const dLng = Number(d.destination.longitude)
      if (isUsablePhilippinesCoordinate(dLat, dLng)) points.push([dLat, dLng])
      for (const photo of proofPhotosByUuid?.[d.containerUuid] ?? []) {
        if (photo.captureLatitude != null && photo.captureLongitude != null) {
          points.push([Number(photo.captureLatitude), Number(photo.captureLongitude)])
        }
      }
    }
    if (points.length === 0) {
      map.setView(PHILIPPINES_MAP_CENTER, 6)
      return
    }
    const bounds = L.latLngBounds(points)
    map.fitBounds(bounds.pad(0.15), { maxZoom: selectedUuid ? 15 : 12, animate: true })
  }, [deliveries, map, selectedUuid, fullTrailByUuid, proofPhotosByUuid])
  return null
}

interface DaLiveLogisticsMapProps {
  deliveries: DaLiveDelivery[]
  selectedUuid: string | null
  serverTime?: string | null
  /** Full GPS history for selected delivery (from actual-route API). */
  fullTrailByUuid?: Record<string, DaContainerTrailPoint[]>
  proofPhotosByUuid?: Record<string, DaDeliveryProofPhoto[]>
  height?: number
}

export function DaLiveLogisticsMap({
  deliveries,
  selectedUuid,
  serverTime,
  fullTrailByUuid,
  proofPhotosByUuid,
  height = 520,
}: DaLiveLogisticsMapProps) {
  const visible = useMemo(() => {
    if (!selectedUuid) return deliveries
    return deliveries.filter((d) => d.containerUuid === selectedUuid)
  }, [deliveries, selectedUuid])

  const center = useMemo((): [number, number] => {
    const first = visible.find((d) => d.currentLatitude != null && d.currentLongitude != null)
    if (first?.currentLatitude != null && first.currentLongitude != null) {
      return [Number(first.currentLatitude), Number(first.currentLongitude)]
    }
    const p = visible[0]?.pickup
    if (p) return [Number(p.latitude), Number(p.longitude)]
    return PHILIPPINES_MAP_CENTER
  }, [visible])

  const usingFullTrail = Boolean(selectedUuid && fullTrailByUuid?.[selectedUuid]?.length)

  return (
    <Box sx={{ position: 'relative' }}>
      <Box
        sx={{
          borderRadius: 2,
          overflow: 'hidden',
          border: `1px solid ${portalColors.border}`,
          height,
        }}
      >
        <MapContainer
          center={center}
          zoom={6}
          minZoom={PHILIPPINES_MAP_MIN_ZOOM}
          maxBounds={PHILIPPINES_BOUNDS}
          maxBoundsViscosity={1}
          style={{ height: '100%', width: '100%' }}
          scrollWheelZoom
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <PhilippinesDefaultView />
          <FitAllDeliveries
            deliveries={visible}
            selectedUuid={selectedUuid}
            fullTrailByUuid={fullTrailByUuid}
            proofPhotosByUuid={proofPhotosByUuid}
          />
          {visible.map((d) => {
            const full = fullTrailByUuid?.[d.containerUuid]
            const trail = full && full.length > 0 ? full : d.recentTrail
            const lat = d.currentLatitude != null ? Number(d.currentLatitude) : null
            const lng = d.currentLongitude != null ? Number(d.currentLongitude) : null
            const gps = resolveDriverGpsDisplay(d, serverTime)
            const proofs = proofPhotosByUuid?.[d.containerUuid] ?? []
            return (
              <Fragment key={d.containerUuid}>
                {trail.length >= 1 && (
                  <DaGpsTrailMapLayers
                    trail={trail}
                    layerKey={d.containerUuid}
                    showGapMarkers={selectedUuid === d.containerUuid}
                    gapMarkerLimit={selectedUuid === d.containerUuid ? 12 : 4}
                  />
                )}
                {isUsablePhilippinesCoordinate(Number(d.pickup.latitude), Number(d.pickup.longitude)) && (
                  <Marker position={[Number(d.pickup.latitude), Number(d.pickup.longitude)]} icon={pickupIcon}>
                    <Popup>Pickup · {d.pickup.label}</Popup>
                  </Marker>
                )}
                {isUsablePhilippinesCoordinate(
                  Number(d.destination.latitude),
                  Number(d.destination.longitude),
                ) && (
                  <Marker
                    position={[Number(d.destination.latitude), Number(d.destination.longitude)]}
                    icon={warehouseIcon}
                  >
                    <Popup>Warehouse · {d.destination.label}</Popup>
                  </Marker>
                )}
                {proofs.map((photo) => {
                  if (photo.captureLatitude == null || photo.captureLongitude == null) return null
                  return (
                    <Marker
                      key={photo.photoUuid}
                      position={[Number(photo.captureLatitude), Number(photo.captureLongitude)]}
                      icon={proofIcon}
                    >
                      <Popup>{photo.label}</Popup>
                    </Marker>
                  )
                })}
                {lat != null && lng != null && (
                  <Marker position={[lat, lng]} icon={gps.offline ? driverOfflineIcon : driverLiveIcon}>
                    <Popup>
                      {d.containerNumber} · {d.driverName ?? 'Driver'}
                      <br />
                      {gps.label}
                      <br />
                      {d.status}
                    </Popup>
                  </Marker>
                )}
                {lat != null && lng != null && (
                  <CircleMarker
                    center={[lat, lng]}
                    radius={10}
                    pathOptions={{
                      color: gps.offline ? '#94A3B8' : '#2563EB',
                      fillColor: gps.offline ? '#CBD5E1' : '#3B82F6',
                      fillOpacity: 0.35,
                      weight: 2,
                    }}
                  />
                )}
              </Fragment>
            )
          })}
        </MapContainer>
      </Box>
      <Stack
        direction="row"
        spacing={1.5}
        sx={{
          position: 'absolute',
          bottom: 10,
          left: 10,
          zIndex: 1000,
          px: 1.25,
          py: 0.75,
          borderRadius: 1,
          bgcolor: 'rgba(255,255,255,0.92)',
          border: `1px solid ${portalColors.border}`,
          fontSize: '0.7rem',
          flexWrap: 'wrap',
        }}
      >
        <Typography component="span" sx={{ fontSize: 'inherit', fontWeight: 700 }}>
          {usingFullTrail ? 'Actual GPS trail' : 'Recent GPS pings'}
        </Typography>
        <Typography component="span" sx={{ fontSize: 'inherit' }}>
          <span style={{ color: '#2563EB' }}>━</span> live sync
        </Typography>
        <Typography component="span" sx={{ fontSize: 'inherit' }}>
          <span style={{ color: '#EA580C' }}>┄</span> queued offline
        </Typography>
        <Typography component="span" sx={{ fontSize: 'inherit' }}>
          <span style={{ color: '#DC2626' }}>┄</span> no signal gap
        </Typography>
        <Typography component="span" sx={{ fontSize: 'inherit' }}>
          <span style={{ color: '#7C3AED' }}>📷</span> geotag proof
        </Typography>
      </Stack>
    </Box>
  )
}

export type { DaContainerTrailPoint }
