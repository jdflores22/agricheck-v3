export interface ContainerTrackDestination {
  name: string
  latitude: number
  longitude: number
}

export interface ContainerLocationPoint {
  latitude: number
  longitude: number
  recordedAt: string
}

export interface ContainerTrack {
  containerUuid: string
  containerNumber: string
  entryReference: string
  status: string
  currentLatitude?: number
  currentLongitude?: number
  destination: ContainerTrackDestination
  trail: ContainerLocationPoint[]
}

export interface OsrmRoute {
  coordinates: Array<{ latitude: number; longitude: number }>
  distanceMeters: number
  durationSeconds: number
}
