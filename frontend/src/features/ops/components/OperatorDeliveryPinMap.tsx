import { Box, Typography } from '@mui/material'
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

export type RouteWizardStep = 0 | 1 | 2

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

function MapClickHandler({ onClick, enabled }: { onClick: (lat: number, lng: number) => void; enabled: boolean }) {
  useMapEvents({
    click(e) {
      if (enabled) onClick(e.latlng.lat, e.latlng.lng)
    },
  })
  return null
}

function FitBounds({
  pickup,
  warehouse,
  wizardStep,
}: {
  pickup: DeliveryPinCoords
  warehouse: DeliveryPinCoords
  wizardStep: RouteWizardStep
}) {
  const map = useMap()
  useEffect(() => {
    if (wizardStep === 0) {
      map.setView([pickup.lat, pickup.lng], 14)
      return
    }
    const bounds = L.latLngBounds([pickup.lat, pickup.lng], [warehouse.lat, warehouse.lng])
    map.fitBounds(bounds.pad(0.2), { maxZoom: 14 })
  }, [pickup.lat, pickup.lng, warehouse.lat, warehouse.lng, wizardStep, map])
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
  wizardStep: RouteWizardStep
  pickupPlaced: boolean
  warehousePlaced: boolean
  onPickupChange: (coords: DeliveryPinCoords) => void
  onWarehouseChange: (coords: DeliveryPinCoords) => void
  hint: string
  height?: number
}

export function OperatorDeliveryPinMap({
  pickup,
  warehouse,
  wizardStep,
  pickupPlaced,
  warehousePlaced,
  onPickupChange,
  onWarehouseChange,
  hint,
  height = 360,
}: OperatorDeliveryPinMapProps) {
  const [routeLine, setRouteLine] = useState<[number, number][] | null>(null)

  useEffect(() => {
    if (wizardStep !== 2) {
      setRouteLine(null)
      return
    }
    let cancelled = false
    void fetchRouteLine(pickup, warehouse).then((line) => {
      if (!cancelled) setRouteLine(line)
    })
    return () => {
      cancelled = true
    }
  }, [wizardStep, pickup.lat, pickup.lng, warehouse.lat, warehouse.lng])

  const fallbackLine = useMemo(
    () => [[pickup.lat, pickup.lng], [warehouse.lat, warehouse.lng]] as [number, number][],
    [pickup, warehouse],
  )

  const centerLat = wizardStep === 0 ? pickup.lat : (pickup.lat + warehouse.lat) / 2
  const centerLng = wizardStep === 0 ? pickup.lng : (pickup.lng + warehouse.lng) / 2

  const showPickup = pickupPlaced || wizardStep >= 1
  const showWarehouse = wizardStep >= 1 && (warehousePlaced || wizardStep === 2)
  const mapClickEnabled = wizardStep === 0 || wizardStep === 1

  return (
    <Box>
      <Typography variant="caption" sx={{ display: 'block', mb: 1, color: portalColors.textMuted }}>
        {hint}
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
          zoom={wizardStep === 0 ? 14 : 11}
          style={{ height: '100%', width: '100%' }}
          scrollWheelZoom
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <FitBounds pickup={pickup} warehouse={warehouse} wizardStep={wizardStep} />
          <MapClickHandler
            enabled={mapClickEnabled}
            onClick={(lat, lng) => {
              if (wizardStep === 0) onPickupChange({ lat, lng })
              else if (wizardStep === 1) onWarehouseChange({ lat, lng })
            }}
          />
          {showPickup && <Marker position={[pickup.lat, pickup.lng]} icon={pickupIcon} />}
          {showWarehouse && <Marker position={[warehouse.lat, warehouse.lng]} icon={warehouseIcon} />}
          {wizardStep === 2 && (
            <Polyline
              positions={routeLine ?? fallbackLine}
              pathOptions={{ color: '#2563EB', weight: 5, opacity: 0.85 }}
            />
          )}
        </MapContainer>
      </Box>
    </Box>
  )
}
