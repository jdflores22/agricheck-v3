import type { OsrmRoute } from '../types/tracking'

interface OsrmResponse {
  routes?: Array<{
    distance: number
    duration: number
    geometry?: { coordinates?: Array<[number, number]> }
  }>
}

export async function fetchDrivingRoute(
  fromLat: number,
  fromLng: number,
  toLat: number,
  toLng: number,
): Promise<OsrmRoute | null> {
  const url =
    `https://router.project-osrm.org/route/v1/driving/${fromLng},${fromLat};${toLng},${toLat}` +
    '?overview=full&geometries=geojson'

  const response = await fetch(url)
  if (!response.ok) {
    return null
  }

  const payload = (await response.json()) as OsrmResponse
  const route = payload.routes?.[0]
  const coords = route?.geometry?.coordinates
  if (!route || !coords?.length) {
    return null
  }

  return {
    coordinates: coords.map(([longitude, latitude]) => ({ latitude, longitude })),
    distanceMeters: route.distance,
    durationSeconds: route.duration,
  }
}

export function formatDistance(meters: number) {
  if (meters >= 1000) {
    return `${(meters / 1000).toFixed(1)} km`
  }
  return `${Math.round(meters)} m`
}

export function formatDuration(seconds: number) {
  const minutes = Math.round(seconds / 60)
  if (minutes < 60) {
    return `${minutes} min`
  }
  const hours = Math.floor(minutes / 60)
  const remainder = minutes % 60
  return `${hours}h ${remainder}m`
}
