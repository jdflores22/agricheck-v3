import { Box } from '@mui/material'
import L from 'leaflet'
import { useEffect, useMemo } from 'react'
import { MapContainer, Marker, Polyline, Popup, TileLayer, useMap } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaContainerActualRoute, DaDeliveryProofPhoto } from '../api/daApi'
import {
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

function FitRoute({ route }: { route: DaContainerActualRoute }) {
  const map = useMap()
  useEffect(() => {
    const points: L.LatLngExpression[] = [
      [Number(route.pickup.latitude), Number(route.pickup.longitude)],
      [Number(route.destination.latitude), Number(route.destination.longitude)],
    ]
    for (const p of route.trail) {
      points.push([Number(p.latitude), Number(p.longitude)])
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
  const trail = useMemo(
    () => route.trail.map((p) => [Number(p.latitude), Number(p.longitude)] as [number, number]),
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
        {trail.length >= 2 && (
          <Polyline positions={trail} pathOptions={{ color: '#2563EB', weight: 5, opacity: 0.9 }} />
        )}
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
