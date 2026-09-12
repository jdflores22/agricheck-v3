import { recordContainerLocation } from '../api/agritrackApi'
import AsyncStorage from '@react-native-async-storage/async-storage'
import * as Location from 'expo-location'
import * as TaskManager from 'expo-task-manager'

export const BACKGROUND_LOCATION_TASK = 'agritrack-background-location'
const ACTIVE_CONTAINER_KEY = 'agritrack.activeContainerUuid'

TaskManager.defineTask(BACKGROUND_LOCATION_TASK, async ({ data, error }) => {
  if (error) {
    return
  }

  const containerUuid = await AsyncStorage.getItem(ACTIVE_CONTAINER_KEY)
  const locations = (data as { locations?: Location.LocationObject[] } | undefined)?.locations
  if (!containerUuid || !locations?.length) {
    return
  }

  const latest = locations[locations.length - 1]
  try {
    await recordContainerLocation(containerUuid, latest.coords.latitude, latest.coords.longitude)
  } catch {
    // Background uploads may fail when offline or token expired; next interval retries.
  }
})

export async function startBackgroundTracking(containerUuid: string) {
  const foreground = await Location.requestForegroundPermissionsAsync()
  if (!foreground.granted) {
    throw new Error('Foreground location permission is required for GPS tracking.')
  }

  const background = await Location.requestBackgroundPermissionsAsync()
  if (!background.granted) {
    throw new Error('Background location permission is required during In Transit.')
  }

  await AsyncStorage.setItem(ACTIVE_CONTAINER_KEY, containerUuid)

  const started = await Location.hasStartedLocationUpdatesAsync(BACKGROUND_LOCATION_TASK)
  if (started) {
    await Location.stopLocationUpdatesAsync(BACKGROUND_LOCATION_TASK)
  }

  await Location.startLocationUpdatesAsync(BACKGROUND_LOCATION_TASK, {
    accuracy: Location.Accuracy.Balanced,
    timeInterval: 60_000,
    distanceInterval: 50,
    showsBackgroundLocationIndicator: true,
    foregroundService: {
      notificationTitle: 'AgriTrack GPS',
      notificationBody: 'Recording container location while in transit.',
      notificationColor: '#166534',
    },
    pausesUpdatesAutomatically: false,
  })
}

export async function stopBackgroundTracking() {
  await AsyncStorage.removeItem(ACTIVE_CONTAINER_KEY)
  const started = await Location.hasStartedLocationUpdatesAsync(BACKGROUND_LOCATION_TASK)
  if (started) {
    await Location.stopLocationUpdatesAsync(BACKGROUND_LOCATION_TASK)
  }
}

export async function getActiveTrackingContainerUuid() {
  return AsyncStorage.getItem(ACTIVE_CONTAINER_KEY)
}
