import { useCallback, useEffect, useState } from 'react'
import {
  ActivityIndicator,
  Pressable,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native'
import MapView, { Marker, Polyline, PROVIDER_DEFAULT } from 'react-native-maps'
import { getContainerTrack } from '../api/agritrackApi'
import { fetchDrivingRoute, formatDistance, formatDuration } from '../services/osrm'
import type { ContainerTrack } from '../types/tracking'
import { colors } from '../theme'

interface MapScreenProps {
  containerUuid: string
  containerNumber: string
}

export function MapScreen({ containerUuid, containerNumber }: MapScreenProps) {
  const [track, setTrack] = useState<ContainerTrack | null>(null)
  const [routeCoords, setRouteCoords] = useState<Array<{ latitude: number; longitude: number }>>([])
  const [routeMeta, setRouteMeta] = useState<{ distanceMeters: number; durationSeconds: number } | null>(null)
  const [initialRegion, setInitialRegion] = useState<{
    latitude: number
    longitude: number
    latitudeDelta: number
    longitudeDelta: number
  } | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    setError('')
    setRouteCoords([])
    setRouteMeta(null)
    try {
      const data = await getContainerTrack(containerUuid)
      setTrack(data)
      if (!initialRegion) {
        setInitialRegion(computeRegion(data))
      }

      const fromLat = data.currentLatitude ?? data.trail.at(-1)?.latitude
      const fromLng = data.currentLongitude ?? data.trail.at(-1)?.longitude
      if (fromLat != null && fromLng != null) {
        const route = await fetchDrivingRoute(
          fromLat,
          fromLng,
          data.destination.latitude,
          data.destination.longitude,
        )
        if (route) {
          setRouteCoords(route.coordinates)
          setRouteMeta({ distanceMeters: route.distanceMeters, durationSeconds: route.durationSeconds })
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load map data')
    } finally {
      setLoading(false)
    }
  }, [containerUuid, initialRegion])

  useEffect(() => {
    setInitialRegion(null)
    setTrack(null)
    setLoading(true)
  }, [containerUuid])

  useEffect(() => {
    void load()
    const timer = setInterval(() => {
      void load()
    }, 30_000)
    return () => clearInterval(timer)
  }, [load])

  const region = initialRegion ?? {
    latitude: 14.5995,
    longitude: 120.9842,
    latitudeDelta: 0.35,
    longitudeDelta: 0.35,
  }

  const driverCoord =
    track?.currentLatitude != null && track.currentLongitude != null
      ? { latitude: track.currentLatitude, longitude: track.currentLongitude }
      : track?.trail.at(-1)
        ? { latitude: track.trail.at(-1)!.latitude, longitude: track.trail.at(-1)!.longitude }
        : null

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>{containerNumber}</Text>
        <Text style={styles.subtitle}>{track?.destination.name ?? 'Loading destination...'}</Text>
        {routeMeta ? (
          <Text style={styles.meta}>
            OSRM route: {formatDistance(routeMeta.distanceMeters)} · {formatDuration(routeMeta.durationSeconds)}
          </Text>
        ) : null}
        {error ? <Text style={styles.error}>{error}</Text> : null}
      </View>

      {loading && !track ? (
        <ActivityIndicator color={colors.primary} style={{ marginTop: 24 }} />
      ) : (
        <MapView provider={PROVIDER_DEFAULT} style={styles.map} initialRegion={region}>
          {driverCoord ? (
            <Marker coordinate={driverCoord} title="Current position" pinColor={colors.primary} />
          ) : null}
          <Marker
            coordinate={{
              latitude: track?.destination.latitude ?? region.latitude,
              longitude: track?.destination.longitude ?? region.longitude,
            }}
            title={track?.destination.name ?? 'Warehouse'}
            pinColor="#b45309"
          />
          {track?.trail.length ? (
            <Polyline
              coordinates={track.trail.map((point) => ({
                latitude: point.latitude,
                longitude: point.longitude,
              }))}
              strokeColor="#2563eb"
              strokeWidth={3}
            />
          ) : null}
          {routeCoords.length ? (
            <Polyline coordinates={routeCoords} strokeColor="#166534" strokeWidth={4} />
          ) : null}
        </MapView>
      )}

      <View style={styles.footer}>
        <Pressable style={styles.refreshBtn} onPress={() => { setLoading(true); void load() }}>
          <Text style={styles.refreshText}>Refresh map</Text>
        </Pressable>
        <ScrollView horizontal showsHorizontalScrollIndicator={false}>
          <Legend color="#2563eb" label="GPS trail" />
          <Legend color="#166534" label="OSRM route" />
          <Legend color={colors.primary} label="You" />
          <Legend color="#b45309" label="Warehouse" />
        </ScrollView>
      </View>
    </View>
  )
}

function computeRegion(track: ContainerTrack) {
  const points = [
    ...(track.currentLatitude != null && track.currentLongitude != null
      ? [{ latitude: track.currentLatitude, longitude: track.currentLongitude }]
      : []),
    ...track.trail.map((point) => ({ latitude: point.latitude, longitude: point.longitude })),
    { latitude: track.destination.latitude, longitude: track.destination.longitude },
  ]

  const latitudes = points.map((point) => point.latitude)
  const longitudes = points.map((point) => point.longitude)
  const minLat = Math.min(...latitudes)
  const maxLat = Math.max(...latitudes)
  const minLng = Math.min(...longitudes)
  const maxLng = Math.max(...longitudes)

  return {
    latitude: (minLat + maxLat) / 2,
    longitude: (minLng + maxLng) / 2,
    latitudeDelta: Math.max(0.05, (maxLat - minLat) * 1.6),
    longitudeDelta: Math.max(0.05, (maxLng - minLng) * 1.6),
  }
}

function Legend({ color, label }: { color: string; label: string }) {
  return (
    <View style={styles.legendItem}>
      <View style={[styles.legendDot, { backgroundColor: color }]} />
      <Text style={styles.legendText}>{label}</Text>
    </View>
  )
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: colors.bg },
  header: { padding: 16, paddingBottom: 8 },
  title: { fontSize: 20, fontWeight: '800', color: colors.text },
  subtitle: { color: colors.muted, marginTop: 4 },
  meta: { color: colors.text, marginTop: 6, fontWeight: '600' },
  error: { color: colors.danger, marginTop: 6 },
  map: { flex: 1 },
  footer: { padding: 12, gap: 10 },
  refreshBtn: {
    alignSelf: 'flex-start',
    backgroundColor: colors.primary,
    paddingHorizontal: 14,
    paddingVertical: 8,
    borderRadius: 8,
  },
  refreshText: { color: '#fff', fontWeight: '700' },
  legendItem: { flexDirection: 'row', alignItems: 'center', marginRight: 16 },
  legendDot: { width: 10, height: 10, borderRadius: 5, marginRight: 6 },
  legendText: { color: colors.muted, fontSize: 12 },
})
