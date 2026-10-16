import { Box } from '@mui/material'
import L from 'leaflet'
import { useEffect, useMemo } from 'react'
import { MapContainer, Marker, Polyline, Popup, TileLayer, useMap } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaContainerActualRoute, DaDeliveryProofPhoto } from '../api/daApi'
import { buildGpsTrailMapSegments } from '../utils/daGpsTrailSegments'
import {
  isUsablePhilippinesCoordinate,
  PHILIPPINES_BOUNDS,
  PHILIPPINES_MAP_CENTER,
  PHILIPPINES_MAP_MIN_ZOOM,
} from '../utils/philippinesMap'

function pinIcon(color: string, label: string) {
  return L.divIcon({
    className: '',
    html: `<div style="min-width:28px;height:28px;padding:0 6px;border-radius:999px;background:${color};border:3px solid #fff;box-shadow:0 4px 12px rgba(28,25,23,.28);color:#fff;font-size:10px;font-weight:800;display:flex;align-items:center;justify-content:center;">${label}</div>`,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
  })
}

const pickupIcon = pinIcon('#15803D', 'P')
const warehouseIcon = pinIcon('#F97316', 'W')
const proofIcon = pinIcon('#7C3AED', '📷')
const noSignalIcon = pinIcon('#DC2626', 'GAP')
const queuedOfflineIcon = pinIcon('#EA580C', 'Q')

function FitRoute({ route }: { route: DaContainerActualRoute }) {
  const map = useMap()
  useEffect(() => {
    const points: L.LatLngExpression[] = []
    const pLat = Number(route.pickup.latitude)
    const pLng = Number(route.pickup.longitude)
    const dLat = Number(route.destination.latitude)
    const dLng = Number(route.destination.longitude)
    if (isUsablePhilippinesCoordinate(pLat, pLng)) points.push([pLat, pLng])
    if (isUsablePhilippinesCoordinate(dLat, dLng)) points.push([dLat, dLng])
    for (const p of route.trail) {
      const lat = Number(p.latitude)
      const lng = Number(p.longitude)
      if (isUsablePhilippinesCoordinate(lat, lng)) points.push([lat, lng])
    }
    for (const photo of route.proofPhotos) {
      if (photo.captureLatitude != null && photo.captureLongitude != null) {
        points.push([Number(photo.captureLatitude), Number(photo.captureLongitude)])
      }
    }
    if (points.length === 0) {
      map.setView(PHILIPPINES_MAP_CENTER, 6)
      return
    }
    const bounds = L.latLngBounds(points)
    map.fitBounds(bounds.pad(0.12), { maxZoom: 15, animate: true })
  }, [map, route])
  return null
}

interface DaActualRouteMapProps {
  route: DaContainerActualRoute
  height?: number
}

export function DaActualRouteMap({ route, height = 360 }: DaActualRouteMapProps) {
  const { online, offlineBuffered, noSignal } = useMemo(
    () => buildGpsTrailMapSegments(route.trail),
    [route.trail],
  )

  return (
    <Box
      sx={{
        borderRadius: 2,
        overflow: 'hidden',
        border: `1px solid ${portalColors.border}`,
        height,
      }}
    >
      <MapContainer
        center={PHILIPPINES_MAP_CENTER}
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
        <FitRoute route={route} />
        {online.map((segment, i) =>
          segment.positions.length >= 2 ? (
            <Polyline
              key={`online-${i}`}
              positions={segment.positions}
              pathOptions={{ color: '#2563EB', weight: 5, opacity: 0.9 }}
            />
          ) : null,
        )}
        {offlineBuffered.map((segment, i) => (
          <Polyline
            key={`queued-${i}`}
            positions={segment.positions}
            pathOptions={{
              color: '#EA580C',
              weight: 5,
              opacity: 0.9,
              dashArray: '8, 10',
            }}
          />
        ))}
        {noSignal.map((segment, i) => (
          <Polyline
            key={`gap-${segment.gapIndex ?? i}`}
            positions={segment.positions}
            pathOptions={{
              color: '#DC2626',
              weight: 5,
              opacity: 0.85,
              dashArray: '10, 14',
            }}
          />
        ))}
        {noSignal.map((segment, i) => {
          if (!segment.offlineStartedAt || !segment.backOnlineAt) return null
          const [lat, lng] = segment.positions[0]
          return (
            <Marker key={`gap-m-${segment.gapIndex ?? i}`} position={[lat, lng]} icon={noSignalIcon}>
              <Popup>
                No GPS fix · {segment.durationMinutes} min
                <br />
                Last ping {new Date(segment.offlineStartedAt).toLocaleString()}
                <br />
                Next ping {new Date(segment.backOnlineAt).toLocaleString()}
              </Popup>
            </Marker>
          )
        })}
        {offlineBuffered.slice(0, 12).map((segment, i) => {
          const [lat, lng] = segment.positions[0]
          return (
            <Marker key={`queued-m-${i}`} position={[lat, lng]} icon={queuedOfflineIcon}>
              <Popup>Coordinate queued on device (no network when captured)</Popup>
            </Marker>
          )
        })}
        <Marker position={[Number(route.pickup.latitude), Number(route.pickup.longitude)]} icon={pickupIcon}>
          <Popup>Pickup · {route.pickup.label}</Popup>
        </Marker>
        <Marker
          position={[Number(route.destination.latitude), Number(route.destination.longitude)]}
          icon={warehouseIcon}
        >
          <Popup>Warehouse · {route.destination.label}</Popup>
        </Marker>
        {route.proofPhotos.map((photo: DaDeliveryProofPhoto) => {
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
      </MapContainer>
    </Box>
  )
}
