import { Box, Typography } from '@mui/material'
import L from 'leaflet'
import { useEffect } from 'react'
import { MapContainer, Marker, TileLayer, useMap } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaWarehouse } from '../api/daApi'
import { hasWarehouseCoordinates } from '../utils/daWarehouseUtils'

interface DaWarehouseLocationMapProps {
  warehouse: DaWarehouse
  height?: number
}

function createWarehouseIcon(isActive: boolean) {
  const color = isActive ? portalColors.primary : portalColors.textMuted
  return L.divIcon({
    className: '',
    html: `
      <div style="
        width: 32px;
        height: 32px;
        border-radius: 999px;
        background: ${color};
        border: 3px solid #ffffff;
        box-shadow: 0 4px 12px rgba(28, 25, 23, 0.28);
        display: flex;
        align-items: center;
        justify-content: center;
      ">
        <div style="
          width: 10px;
          height: 10px;
          border-radius: 999px;
          background: #ffffff;
        "></div>
      </div>
    `,
    iconSize: [32, 32],
    iconAnchor: [16, 16],
  })
}

function MapFocus({ latitude, longitude }: { latitude: number; longitude: number }) {
  const map = useMap()

  useEffect(() => {
    map.setView([latitude, longitude], 14)
  }, [latitude, longitude, map])

  return null
}

export function DaWarehouseLocationMap({ warehouse, height = 280 }: DaWarehouseLocationMapProps) {
  if (!hasWarehouseCoordinates(warehouse)) {
    return (
      <Box
        sx={{
          height,
          borderRadius: '0.75rem',
          border: `1px dashed ${portalColors.border}`,
          bgcolor: portalColors.bgMuted,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          px: 2,
        }}
      >
        <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, textAlign: 'center' }}>
          No coordinates on file for this warehouse.
        </Typography>
      </Box>
    )
  }

  const latitude = Number(warehouse.latitude)
  const longitude = Number(warehouse.longitude)

  return (
    <Box
      sx={{
        height,
        borderRadius: '0.75rem',
        overflow: 'hidden',
        border: `1px solid ${portalColors.border}`,
      }}
    >
      <MapContainer
        center={[latitude, longitude]}
        zoom={14}
        style={{ height: '100%', width: '100%' }}
        scrollWheelZoom={false}
        dragging
        zoomControl
      >
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <MapFocus latitude={latitude} longitude={longitude} />
        <Marker position={[latitude, longitude]} icon={createWarehouseIcon(warehouse.isActive)} />
      </MapContainer>
    </Box>
  )
}
