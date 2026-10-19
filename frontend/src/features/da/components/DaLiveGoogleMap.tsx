import { Box, CircularProgress, Typography } from '@mui/material'
import { useEffect, useMemo, useRef, useState } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaLiveDelivery } from '../api/daApi'
import { createDaStopMapIcon, createDaTruckMapIcon } from '../utils/daTruckMapIcon'
import { getGoogleMapsApiKey, loadGoogleMapsScript } from '../utils/googleMapsRuntime'
import { resolveDriverGpsDisplay } from '../utils/daDriverGpsStatus'
import {
  isUsablePhilippinesCoordinate,
  PHILIPPINES_MAP_CENTER,
} from '../utils/philippinesMap'

interface DaLiveGoogleMapProps {
  deliveries: DaLiveDelivery[]
  selectedUuid: string | null
  serverTime?: string | null
  height?: number | string
  className?: string
}

export function DaLiveGoogleMap({
  deliveries,
  selectedUuid,
  serverTime,
  height = 520,
  className,
}: DaLiveGoogleMapProps) {
  const containerRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<google.maps.Map | null>(null)
  const overlaysRef = useRef<{
    markers: google.maps.Marker[]
    polylines: google.maps.Polyline[]
  }>({ markers: [], polylines: [] })

  const [mapState, setMapState] = useState<'loading' | 'ready' | 'missing-key' | 'error'>('loading')

  const visible = useMemo(() => {
    if (!selectedUuid) return deliveries
    return deliveries.filter((d) => d.containerUuid === selectedUuid)
  }, [deliveries, selectedUuid])

  useEffect(() => {
    const key = getGoogleMapsApiKey()
    if (!key) {
      setMapState('missing-key')
      return
    }
    let cancelled = false
    loadGoogleMapsScript()
      .then(() => {
        if (!cancelled) setMapState('ready')
      })
      .catch(() => {
        if (!cancelled) setMapState('error')
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (mapState !== 'ready' || !containerRef.current || typeof google === 'undefined') return

    if (!mapRef.current) {
      mapRef.current = new google.maps.Map(containerRef.current, {
        center: { lat: PHILIPPINES_MAP_CENTER[0], lng: PHILIPPINES_MAP_CENTER[1] },
        zoom: 6,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: true,
        gestureHandling: 'greedy',
      })
    }
  }, [mapState])

  useEffect(() => {
    const map = mapRef.current
    if (!map || mapState !== 'ready') return

    for (const m of overlaysRef.current.markers) m.setMap(null)
    for (const p of overlaysRef.current.polylines) p.setMap(null)
    overlaysRef.current = { markers: [], polylines: [] }

    const bounds = new google.maps.LatLngBounds()
    let hasBounds = false

    for (const d of visible) {
      const trail = d.recentTrail ?? []
      const path: google.maps.LatLngLiteral[] = []
      for (const p of trail) {
        const lat = Number(p.latitude)
        const lng = Number(p.longitude)
        if (isUsablePhilippinesCoordinate(lat, lng)) {
          path.push({ lat, lng })
          bounds.extend({ lat, lng })
          hasBounds = true
        }
      }

      if (path.length >= 2) {
        const polyline = new google.maps.Polyline({
          path,
          geodesic: true,
          strokeColor: '#2563EB',
          strokeOpacity: 0.85,
          strokeWeight: 4,
          map,
        })
        overlaysRef.current.polylines.push(polyline)
      }

      const pLat = Number(d.pickup.latitude)
      const pLng = Number(d.pickup.longitude)
      if (isUsablePhilippinesCoordinate(pLat, pLng)) {
        const marker = new google.maps.Marker({
          map,
          position: { lat: pLat, lng: pLng },
          title: `Pickup · ${d.pickup.label}`,
          icon: createDaStopMapIcon(google.maps, 'P', '#15803D'),
        })
        overlaysRef.current.markers.push(marker)
        bounds.extend({ lat: pLat, lng: pLng })
        hasBounds = true
      }

      const dLat = Number(d.destination.latitude)
      const dLng = Number(d.destination.longitude)
      if (isUsablePhilippinesCoordinate(dLat, dLng)) {
        const marker = new google.maps.Marker({
          map,
          position: { lat: dLat, lng: dLng },
          title: `Destination · ${d.destination.label}`,
          icon: createDaStopMapIcon(google.maps, 'D', '#F97316'),
        })
        overlaysRef.current.markers.push(marker)
        bounds.extend({ lat: dLat, lng: dLng })
        hasBounds = true
      }

      const lat = d.currentLatitude != null ? Number(d.currentLatitude) : null
      const lng = d.currentLongitude != null ? Number(d.currentLongitude) : null
      if (lat != null && lng != null && isUsablePhilippinesCoordinate(lat, lng)) {
        const gps = resolveDriverGpsDisplay(d, serverTime)
        const marker = new google.maps.Marker({
          map,
          position: { lat, lng },
          title: `${d.containerNumber} · ${d.driverName ?? 'Driver'}`,
          icon: createDaTruckMapIcon(google.maps, gps.offline),
        })
        overlaysRef.current.markers.push(marker)
        bounds.extend({ lat, lng })
        hasBounds = true
      }
    }

    if (hasBounds) {
      map.fitBounds(bounds, selectedUuid ? 48 : 64)
    } else {
      map.setCenter({ lat: PHILIPPINES_MAP_CENTER[0], lng: PHILIPPINES_MAP_CENTER[1] })
      map.setZoom(6)
    }
  }, [visible, selectedUuid, serverTime, mapState])

  if (mapState === 'missing-key') {
    return (
      <Box
        sx={{
          height,
          borderRadius: 2,
          border: `1px dashed ${portalColors.border}`,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          p: 3,
          bgcolor: portalColors.bgWhite,
        }}
      >
        <Typography sx={{ fontSize: '0.9rem', color: portalColors.textMuted, textAlign: 'center' }}>
          Google Maps API key is not configured. Set <code>window.__AGRICHECK_GOOGLE_MAPS_API_KEY__</code> in{' '}
          <code>public/config.js</code> (or <code>VITE_GOOGLE_MAPS_API_KEY</code> for local dev).
        </Typography>
      </Box>
    )
  }

  if (mapState === 'error') {
    return (
      <Box sx={{ height, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
        <Typography color="error">Failed to load Google Maps.</Typography>
      </Box>
    )
  }

  return (
    <Box sx={{ position: 'relative', height, borderRadius: 2, overflow: 'hidden', border: `1px solid ${portalColors.border}` }} className={className}>
      {mapState === 'loading' && (
        <Box
          sx={{
            position: 'absolute',
            inset: 0,
            zIndex: 2,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: 'rgba(255,255,255,0.75)',
          }}
        >
          <CircularProgress size={28} sx={{ color: portalColors.primary }} />
        </Box>
      )}
      <Box ref={containerRef} sx={{ width: '100%', height: '100%' }} />
      <Box
        sx={{
          position: 'absolute',
          bottom: 10,
          left: 10,
          zIndex: 1,
          px: 1.25,
          py: 0.75,
          borderRadius: 1,
          bgcolor: 'rgba(255,255,255,0.92)',
          border: `1px solid ${portalColors.border}`,
          fontSize: '0.7rem',
        }}
      >
        <Typography component="span" sx={{ fontSize: 'inherit', fontWeight: 700 }}>
          Live leg: pickup → destination
        </Typography>
        <Typography component="span" sx={{ fontSize: 'inherit', ml: 1 }}>
          Truck = driver position
        </Typography>
      </Box>
    </Box>
  )
}
