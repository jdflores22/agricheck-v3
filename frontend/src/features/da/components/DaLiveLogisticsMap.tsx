import { Box } from '@mui/material'
import L from 'leaflet'
import { Fragment, useEffect, useMemo, useState } from 'react'
import { CircleMarker, MapContainer, Marker, Polyline, Popup, TileLayer, useMap } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaContainerTrailPoint, DaLiveDelivery } from '../api/daApi'
import { resolveDriverGpsDisplay } from '../utils/daDriverGpsStatus'
import { fetchOsrmDrivingRoute, type LatLng } from '../utils/daOsrmRoute'
import {
  PHILIPPINES_BOUNDS,
  PHILIPPINES_MAP_CENTER,
  PHILIPPINES_MAP_MIN_ZOOM,
} from '../utils/philippinesMap'

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

type DeliveryRoutes = {
  navigation: LatLng[] | null
  overview: LatLng[] | null
}

function FitAllDeliveries({
  deliveries,
  selectedUuid,
  routes,
  fullTrailByUuid,
}: {
  deliveries: DaLiveDelivery[]
  selectedUuid: string | null
  routes: Record<string, DeliveryRoutes>
  fullTrailByUuid?: Record<string, DaContainerTrailPoint[]>
}) {
  const map = useMap()
  useEffect(() => {
    const points: L.LatLngExpression[] = []
    for (const d of deliveries) {
      if (d.currentLatitude != null && d.currentLongitude != null) {
        points.push([Number(d.currentLatitude), Number(d.currentLongitude)])
      }
      const full = fullTrailByUuid?.[d.containerUuid]
      const trailPoints = full && full.length > 0 ? full : d.recentTrail
      for (const p of trailPoints) {
        points.push([Number(p.latitude), Number(p.longitude)])
      }
      points.push([Number(d.pickup.latitude), Number(d.pickup.longitude)])
      points.push([Number(d.destination.latitude), Number(d.destination.longitude)])
      const r = routes[d.containerUuid]
      r?.navigation?.forEach(([lat, lng]) => points.push([lat, lng]))
      r?.overview?.forEach(([lat, lng]) => points.push([lat, lng]))
    }
    if (points.length === 0) {
      map.setView(PHILIPPINES_MAP_CENTER, 6)
      return
    }
    const bounds = L.latLngBounds(points)
    map.fitBounds(bounds.pad(0.15), { maxZoom: selectedUuid ? 15 : 12, animate: true })
  }, [deliveries, map, selectedUuid, routes, fullTrailByUuid])
  return null
}

interface DaLiveLogisticsMapProps {
  deliveries: DaLiveDelivery[]
  selectedUuid: string | null
  serverTime?: string | null
  /** Full GPS history for selected delivery (from actual-route API). */
  fullTrailByUuid?: Record<string, DaContainerTrailPoint[]>
  height?: number
}

export function DaLiveLogisticsMap({
  deliveries,
  selectedUuid,
  serverTime,
  fullTrailByUuid,
  height = 520,
}: DaLiveLogisticsMapProps) {
  const visible = useMemo(() => {
    if (!selectedUuid) return deliveries
    return deliveries.filter((d) => d.containerUuid === selectedUuid)
  }, [deliveries, selectedUuid])

  const [routes, setRoutes] = useState<Record<string, DeliveryRoutes>>({})

  useEffect(() => {
    let cancelled = false
    async function loadRoutes() {
      const next: Record<string, DeliveryRoutes> = {}
      await Promise.all(
        visible.map(async (d) => {
          const pLat = Number(d.pickup.latitude)
          const pLng = Number(d.pickup.longitude)
          const wLat = Number(d.destination.latitude)
          const wLng = Number(d.destination.longitude)
          const hasDriver = d.currentLatitude != null && d.currentLongitude != null
          const fromLat = hasDriver ? Number(d.currentLatitude) : pLat
          const fromLng = hasDriver ? Number(d.currentLongitude) : pLng
          const [navigation, overview] = await Promise.all([
            fetchOsrmDrivingRoute(fromLat, fromLng, wLat, wLng),
            fetchOsrmDrivingRoute(pLat, pLng, wLat, wLng),
          ])
          next[d.containerUuid] = { navigation, overview }
        }),
      )
      if (!cancelled) setRoutes(next)
    }
    loadRoutes()
    return () => {
      cancelled = true
    }
  }, [visible])

  const center = useMemo((): [number, number] => {
    const first = visible.find((d) => d.currentLatitude != null && d.currentLongitude != null)
    if (first?.currentLatitude != null && first.currentLongitude != null) {
      return [Number(first.currentLatitude), Number(first.currentLongitude)]
    }
    const p = visible[0]?.pickup
    if (p) return [Number(p.latitude), Number(p.longitude)]
    return PHILIPPINES_MAP_CENTER
  }, [visible])

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
        <FitAllDeliveries
          deliveries={visible}
          selectedUuid={selectedUuid}
          routes={routes}
          fullTrailByUuid={fullTrailByUuid}
        />
        {visible.map((d) => {
          const full = fullTrailByUuid?.[d.containerUuid]
          const source = full && full.length > 0 ? full : d.recentTrail
          const trail = source.map((p) => [Number(p.latitude), Number(p.longitude)] as [number, number])
          const lat = d.currentLatitude != null ? Number(d.currentLatitude) : null
          const lng = d.currentLongitude != null ? Number(d.currentLongitude) : null
          const gps = resolveDriverGpsDisplay(d, serverTime)
          const route = routes[d.containerUuid]
          return (
            <Fragment key={d.containerUuid}>
              {route?.overview && route.overview.length >= 2 && (
                <Polyline
                  positions={route.overview}
                  pathOptions={{ color: '#86EFAC', weight: 5, opacity: 0.55, dashArray: '10 8' }}
                />
              )}
              {route?.navigation && route.navigation.length >= 2 && (
                <Polyline
                  positions={route.navigation}
                  pathOptions={{ color: '#15803D', weight: 6, opacity: 0.9 }}
                />
              )}
              {trail.length >= 2 && (
                <Polyline positions={trail} pathOptions={{ color: '#2563EB', weight: 4, opacity: 0.85 }} />
              )}
              <Marker position={[Number(d.pickup.latitude), Number(d.pickup.longitude)]} icon={pickupIcon}>
                <Popup>Pickup · {d.pickup.label}</Popup>
              </Marker>
              <Marker
                position={[Number(d.destination.latitude), Number(d.destination.longitude)]}
                icon={warehouseIcon}
              >
                <Popup>Warehouse · {d.destination.label}</Popup>
              </Marker>
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
  )
}

export type { DaContainerTrailPoint }
