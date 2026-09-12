import MapOutlinedIcon from '@mui/icons-material/MapOutlined'
import { Box, Button, Chip, Stack, Typography } from '@mui/material'
import L from 'leaflet'
import { useEffect } from 'react'
import { MapContainer, Marker, Popup, TileLayer, useMap } from 'react-leaflet'
import 'leaflet/dist/leaflet.css'
import { portalColors } from '../../../components/portal/portalTheme'
import { Link as RouterLink } from 'react-router-dom'
import type { DaWarehouse } from '../api/daApi'
import {
  getWarehousesWithCoordinates,
  PHILIPPINES_MAP_CENTER,
  PHILIPPINES_MAP_ZOOM,
} from '../utils/daWarehouseUtils'

interface DaWarehouseMapProps {
  warehouses: DaWarehouse[]
}

function createWarehouseIcon(isActive: boolean) {
  const color = isActive ? portalColors.primary : portalColors.textMuted
  return L.divIcon({
    className: '',
    html: `
      <div style="
        width: 28px;
        height: 28px;
        border-radius: 999px;
        background: ${color};
        border: 3px solid #ffffff;
        box-shadow: 0 2px 8px rgba(28, 25, 23, 0.25);
        display: flex;
        align-items: center;
        justify-content: center;
      ">
        <div style="
          width: 8px;
          height: 8px;
          border-radius: 999px;
          background: #ffffff;
        "></div>
      </div>
    `,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
    popupAnchor: [0, -12],
  })
}

function MapViewport({ warehouses }: { warehouses: DaWarehouse[] }) {
  const map = useMap()

  useEffect(() => {
    const mapped = getWarehousesWithCoordinates(warehouses)

    if (mapped.length === 0) {
      map.setView(PHILIPPINES_MAP_CENTER, PHILIPPINES_MAP_ZOOM)
      return
    }

    if (mapped.length === 1) {
      map.setView([Number(mapped[0].latitude), Number(mapped[0].longitude)], 11)
      return
    }

    const bounds = L.latLngBounds(
      mapped.map((warehouse) => [Number(warehouse.latitude), Number(warehouse.longitude)] as [number, number]),
    )
    map.fitBounds(bounds, { padding: [48, 48], maxZoom: 11 })
  }, [warehouses, map])

  return null
}

export function DaWarehouseMap({ warehouses }: DaWarehouseMapProps) {
  const mappedWarehouses = getWarehousesWithCoordinates(warehouses)
  const missingCoordinates = warehouses.length - mappedWarehouses.length

  return (
    <Box
      sx={{
        mt: 3,
        borderRadius: '0.75rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
        overflow: 'hidden',
      }}
    >
      <Box sx={{ p: 2.5, borderBottom: `1px solid ${portalColors.border}` }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} sx={{ alignItems: { md: 'center' } }}>
          <MapOutlinedIcon sx={{ color: portalColors.primary }} />
          <Box sx={{ flex: 1 }}>
            <Typography sx={{ fontWeight: 700, color: portalColors.textDark }}>
              National Warehouse Map
            </Typography>
            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
              Geographic overview of registered warehouses across the Philippines.
            </Typography>
          </Box>
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
            <Chip
              size="small"
              label={`${mappedWarehouses.length} mapped`}
              sx={{ bgcolor: portalColors.successSoft, color: portalColors.successText, fontWeight: 600 }}
            />
            {missingCoordinates > 0 ? (
              <Chip
                size="small"
                label={`${missingCoordinates} missing coordinates`}
                sx={{ bgcolor: portalColors.bgMuted, color: portalColors.textMuted, fontWeight: 600 }}
              />
            ) : null}
          </Stack>
        </Stack>
      </Box>

      <Box sx={{ position: 'relative', height: { xs: 320, md: 480 }, bgcolor: portalColors.bgMuted }}>
        <MapContainer
          center={PHILIPPINES_MAP_CENTER}
          zoom={PHILIPPINES_MAP_ZOOM}
          style={{ height: '100%', width: '100%' }}
          scrollWheelZoom
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <MapViewport warehouses={warehouses} />
          {mappedWarehouses.map((warehouse) => (
            <Marker
              key={warehouse.id}
              position={[Number(warehouse.latitude), Number(warehouse.longitude)]}
              icon={createWarehouseIcon(warehouse.isActive)}
            >
              <Popup>
                <Box sx={{ minWidth: 220 }}>
                  <Typography sx={{ fontWeight: 700, fontSize: '0.875rem', mb: 0.5 }}>
                    {warehouse.name}
                  </Typography>
                  <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 1 }}>
                    {warehouse.code}
                  </Typography>
                  <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textDark, mb: 1 }}>
                    {warehouse.formattedAddress}
                  </Typography>
                  <Chip
                    size="small"
                    label={warehouse.isActive ? 'Active' : 'Inactive'}
                    sx={{
                      bgcolor: warehouse.isActive ? portalColors.successSoft : portalColors.bgMuted,
                      color: warehouse.isActive ? portalColors.successText : portalColors.textMuted,
                      fontWeight: 600,
                      mb: 1.5,
                    }}
                  />
                  <Button
                    component={RouterLink}
                    to={`/da/warehouses/${warehouse.id}`}
                    size="small"
                    variant="contained"
                    sx={{
                      bgcolor: portalColors.primary,
                      color: '#fff',
                      textTransform: 'none',
                      fontWeight: 600,
                      boxShadow: 'none',
                      '&:hover': { bgcolor: portalColors.primaryDark, boxShadow: 'none' },
                    }}
                  >
                    View details
                  </Button>
                </Box>
              </Popup>
            </Marker>
          ))}
        </MapContainer>

        <Stack
          direction="row"
          spacing={1}
          sx={{
            position: 'absolute',
            left: 12,
            bottom: 12,
            zIndex: 1000,
            bgcolor: 'rgba(255,255,255,0.92)',
            borderRadius: '0.5rem',
            border: `1px solid ${portalColors.border}`,
            px: 1.25,
            py: 0.75,
          }}
        >
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: portalColors.primary }} />
            <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>Active</Typography>
          </Stack>
          <Stack direction="row" spacing={0.75} sx={{ alignItems: 'center' }}>
            <Box sx={{ width: 12, height: 12, borderRadius: '50%', bgcolor: portalColors.textMuted }} />
            <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>Inactive</Typography>
          </Stack>
        </Stack>
      </Box>
    </Box>
  )
}
