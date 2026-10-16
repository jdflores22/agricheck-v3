import L from 'leaflet'

/** Approximate bounding box for the Philippine archipelago (map pan/zoom clamp). */
export const PHILIPPINES_BOUNDS = L.latLngBounds([4.2, 116.0], [21.3, 127.5])

export const PHILIPPINES_MAP_CENTER: [number, number] = [12.8797, 121.774]

export const PHILIPPINES_MAP_MIN_ZOOM = 5

/** Reject null island / missing coords that zoom the map to the whole world. */
export function isUsablePhilippinesCoordinate(lat: number, lng: number): boolean {
  if (!Number.isFinite(lat) || !Number.isFinite(lng)) return false
  if (Math.abs(lat) < 0.0001 && Math.abs(lng) < 0.0001) return false
  return PHILIPPINES_BOUNDS.contains(L.latLng(lat, lng))
}
