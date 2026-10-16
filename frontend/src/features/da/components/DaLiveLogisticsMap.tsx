import { Box } from '@mui/material'
import L from 'leaflet'
import { Fragment, useEffect, useMemo } from 'react'
import { CircleMarker, MapContainer, Marker, Polyline, Popup, TileLayer, useMap } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaContainerTrailPoint, DaLiveDelivery } from '../api/daApi'

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
const driverIcon = pinIcon('#2563EB', '●')

function FitAllDeliveries({
  deliveries,
  selectedUuid,
}: {
  deliveries: DaLiveDelivery[]
  selectedUuid: string | null
}) {
  const map = useMap()
  useEffect(() => {
    const points: L.LatLngExpression[] = []
    for (const d of deliveries) {
      if (d.currentLatitude != null && d.currentLongitude != null) {
        points.push([Number(d.currentLatitude), Number(d.currentLongitude)])
      }
      for (const p of d.recentTrail) {
        points.push([Number(p.latitude), Number(p.longitude)])
      }
      points.push([Number(d.pickup.latitude), Number(d.pickup.longitude)])
      points.push([Number(d.destination.latitude), Number(d.destination.longitude)])
    }
    if (points.length === 0) return
    const bounds = L.latLngBounds(points)
    map.fitBounds(bounds.pad(0.15), { maxZoom: selectedUuid ? 15 : 12, animate: true })
  }, [deliveries, map, selectedUuid])
  return null
}

interface DaLiveLogisticsMapProps {
  deliveries: DaLiveDelivery[]
  selectedUuid: string | null
  height?: number
}

export function DaLiveLogisticsMap({ deliveries, selectedUuid, height = 520 }: DaLiveLogisticsMapProps) {
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
    return [14.5995, 120.9842]
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
      <MapContainer center={center} zoom={11} style={{ height: '100%', width: '100%' }} scrollWheelZoom>
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <FitAllDeliveries deliveries={visible} selectedUuid={selectedUuid} />
        {visible.map((d) => {
          const trail = d.recentTrail.map((p) => [Number(p.latitude), Number(p.longitude)] as [number, number])
          const lat = d.currentLatitude != null ? Number(d.currentLatitude) : null
          const lng = d.currentLongitude != null ? Number(d.currentLongitude) : null
          return (
            <Fragment key={d.containerUuid}>
              {trail.length >= 2 && (
                <Polyline positions={trail} pathOptions={{ color: '#2563EB', weight: 4, opacity: 0.85 }} />
              )}
              <Marker
                position={[Number(d.pickup.latitude), Number(d.pickup.longitude)]}
                icon={pickupIcon}
              >
                <Popup>Pickup · {d.pickup.label}</Popup>
              </Marker>
              <Marker
                position={[Number(d.destination.latitude), Number(d.destination.longitude)]}
                icon={warehouseIcon}
              >
                <Popup>Warehouse · {d.destination.label}</Popup>
              </Marker>
              {lat != null && lng != null && (
                <Marker position={[lat, lng]} icon={driverIcon}>
                  <Popup>
                    {d.containerNumber} · {d.driverName ?? 'Driver'}
                    <br />
                    {d.status}
                  </Popup>
                </Marker>
              )}
              {lat != null && lng != null && (
                <CircleMarker
                  center={[lat, lng]}
                  radius={10}
                  pathOptions={{ color: '#2563EB', fillColor: '#3B82F6', fillOpacity: 0.35, weight: 2 }}
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
