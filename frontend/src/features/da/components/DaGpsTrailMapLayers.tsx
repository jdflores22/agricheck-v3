import { Marker, Polyline, Popup } from 'react-leaflet'
import L from 'leaflet'
import type { DaContainerTrailPoint } from '../api/daApi'
import { buildGpsTrailMapSegments } from '../utils/daGpsTrailSegments'
import { useMemo } from 'react'

const noSignalIcon = L.divIcon({
  className: '',
  html: `<div style="min-width:28px;height:28px;padding:0 6px;border-radius:999px;background:#DC2626;border:3px solid #fff;box-shadow:0 4px 12px rgba(28,25,23,.28);color:#fff;font-size:10px;font-weight:800;display:flex;align-items:center;justify-content:center;">GAP</div>`,
  iconSize: [28, 28],
  iconAnchor: [14, 14],
})

const queuedOfflineIcon = L.divIcon({
  className: '',
  html: `<div style="min-width:28px;height:28px;padding:0 6px;border-radius:999px;background:#EA580C;border:3px solid #fff;box-shadow:0 4px 12px rgba(28,25,23,.28);color:#fff;font-size:10px;font-weight:800;display:flex;align-items:center;justify-content:center;">Q</div>`,
  iconSize: [28, 28],
  iconAnchor: [14, 14],
})

interface DaGpsTrailMapLayersProps {
  trail: DaContainerTrailPoint[]
  layerKey: string
  showGapMarkers?: boolean
  gapMarkerLimit?: number
}

export function DaGpsTrailMapLayers({
  trail,
  layerKey,
  showGapMarkers = true,
  gapMarkerLimit = 12,
}: DaGpsTrailMapLayersProps) {
  const { online, offlineBuffered, noSignal } = useMemo(
    () => buildGpsTrailMapSegments(trail),
    [trail],
  )

  return (
    <>
      {online.map((segment, i) =>
        segment.positions.length >= 2 ? (
          <Polyline
            key={`${layerKey}-online-${i}`}
            positions={segment.positions}
            pathOptions={{ color: '#2563EB', weight: 5, opacity: 0.9 }}
          />
        ) : null,
      )}
      {offlineBuffered.map((segment, i) => (
        <Polyline
          key={`${layerKey}-queued-${i}`}
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
          key={`${layerKey}-gap-${segment.gapIndex ?? i}`}
          positions={segment.positions}
          pathOptions={{
            color: '#DC2626',
            weight: 5,
            opacity: 0.85,
            dashArray: '10, 14',
          }}
        />
      ))}
      {showGapMarkers &&
        noSignal.map((segment, i) => {
          if (!segment.offlineStartedAt || !segment.backOnlineAt) return null
          const [lat, lng] = segment.positions[0]
          return (
            <Marker
              key={`${layerKey}-gap-m-${segment.gapIndex ?? i}`}
              position={[lat, lng]}
              icon={noSignalIcon}
            >
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
      {showGapMarkers &&
        offlineBuffered.slice(0, gapMarkerLimit).map((segment, i) => {
          const [lat, lng] = segment.positions[0]
          return (
            <Marker key={`${layerKey}-queued-m-${i}`} position={[lat, lng]} icon={queuedOfflineIcon}>
              <Popup>Coordinate queued on device (no network when captured)</Popup>
            </Marker>
          )
        })}
    </>
  )
}
