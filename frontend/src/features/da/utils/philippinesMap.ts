import L from 'leaflet'

/** Approximate bounding box for the Philippine archipelago (map pan/zoom clamp). */
export const PHILIPPINES_BOUNDS = L.latLngBounds([4.2, 116.0], [21.3, 127.5])

export const PHILIPPINES_MAP_CENTER: [number, number] = [12.8797, 121.774]

export const PHILIPPINES_MAP_MIN_ZOOM = 5
