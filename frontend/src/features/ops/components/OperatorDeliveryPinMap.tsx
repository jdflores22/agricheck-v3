import { Box, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material'
import L from 'leaflet'
import { useEffect, useMemo, useState } from 'react'
import { MapContainer, Marker, Polyline, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { portalColors } from '../../../components/portal/portalTheme'

export type DeliveryPinRole = 'pickup' | 'warehouse'

export interface DeliveryPinCoords {
  lat: number
  lng: number
}

function pinIcon(color: string) {
  return L.divIcon({
    className: '',
    html: `
      <div style="
        width: 28px;
        height: 28px;
        border-radius: 999px;
        background: ${color};
        border: 3px solid #ffffff;
        box-shadow: 0 4px 12px rgba(28, 25, 23, 0.28);
      "></div>
    `,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
  })
}

const pickupIcon = pinIcon('#15803D')
const warehouseIcon = pinIcon('#F97316')

function MapClickHandler({ onClick }: { onClick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click(e) {
      onClick(e.latlng.lat, e.latlng.lng)
    },
  })
  return null
}

function FitBounds({
  pickup,
  warehouse,
}: {
  pickup: DeliveryPinCoords
  warehouse: DeliveryPinCoords
}) {
  const map = useMap()
  useEffect(() => {
    const bounds = L.latLngBounds([pickup.lat, pickup.lng], [warehouse.lat, warehouse.lng])
    map.fitBounds(bounds.pad(0.2), { maxZoom: 14 })
  }, [pickup.lat, pickup.lng, warehouse.lat, warehouse.lng, map])
  return null
}

async function fetchRouteLine(
  pickup: DeliveryPinCoords,
  warehouse: DeliveryPinCoords,
): Promise<[number, number][] | null> {
  try {
    const url =
      `https://router.project-osrm.org/route/v1/driving/` +
      `${pickup.lng},${pickup.lat};${warehouse.lng},${warehouse.lat}?overview=full&geometries=geojson`
    const res = await fetch(url)
    if (!res.ok) return null
    const data = (await res.json()) as {
      routes?: { geometry?: { coordinates?: [number, number][] } }[]
    }
    const coords = data.routes?.[0]?.geometry?.coordinates
    if (!coords?.length) return null
    return coords.map(([lng, lat]) => [lat, lng])
  } catch {
    return null
  }
}

interface OperatorDeliveryPinMapProps {
  pickup: DeliveryPinCoords
  warehouse: DeliveryPinCoords
  activePin: DeliveryPinRole
  onActivePinChange: (role: DeliveryPinRole) => void
  onPickupChange: (coords: DeliveryPinCoords) => void
  onWarehouseChange: (coords: DeliveryPinCoords) => void
  height?: number
}

export function OperatorDeliveryPinMap({
  pickup,
  warehouse,
  activePin,
  onActivePinChange,
  onPickupChange,
  onWarehouseChange,
  height = 360,
}: OperatorDeliveryPinMapProps) {
  const [routeLine, setRouteLine] = useState<[number, number][] | null>(null)

  useEffect(() => {
    let cancelled = false
    void fetchRouteLine(pickup, warehouse).then((line) => {
      if (!cancelled) setRouteLine(line)
    })
    return () => {
      cancelled = true
    }
  }, [pickup.lat, pickup.lng, warehouse.lat, warehouse.lng])

  const fallbackLine = useMemo(
    () => [[pickup.lat, pickup.lng], [warehouse.lat, warehouse.lng]] as [number, number][],
    [pickup, warehouse],
  )

  const centerLat = (pickup.lat + warehouse.lat) / 2
  const centerLng = (pickup.lng + warehouse.lng) / 2

  return (
    <Box>
      <ToggleButtonGroup
        exclusive
        size="small"
        value={activePin}
        onChange={(_, value: DeliveryPinRole | null) => value && onActivePinChange(value)}
        sx={{ mb: 1.5 }}
      >
        <ToggleButton value="pickup">Tap map: pickup</ToggleButton>
        <ToggleButton value="warehouse">Tap map: warehouse</ToggleButton>
      </ToggleButtonGroup>
      <Typography variant="caption" sx={{ display: 'block', mb: 1, color: portalColors.textMuted }}>
        Click the map to move the selected pin. Route preview uses OpenStreetMap routing (approximate).
      </Typography>
      <Box
        sx={{
          height,
          borderRadius: '0.75rem',
          overflow: 'hidden',
          border: `1px solid ${portalColors.border}`,
        }}
      >
        <MapContainer
          center={[centerLat, centerLng]}
          zoom={11}
          style={{ height: '100%', width: '100%' }}
          scrollWheelZoom
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <FitBounds pickup={pickup} warehouse={warehouse} />
          <MapClickHandler
            onClick={(lat, lng) => {
              if (activePin === 'pickup') onPickupChange({ lat, lng })
              else onWarehouseChange({ lat, lng })
            }}
          />
          <Marker position={[pickup.lat, pickup.lng]} icon={pickupIcon} />
          <Marker position={[warehouse.lat, warehouse.lng]} icon={warehouseIcon} />
          <Polyline
            positions={routeLine ?? fallbackLine}
            pathOptions={{ color: '#2563EB', weight: 5, opacity: 0.85 }}
          />
        </MapContainer>
      </Box>
    </Box>
  )
}
