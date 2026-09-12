import AsyncStorage from '@react-native-async-storage/async-storage'
import Constants from 'expo-constants'
import * as Notifications from 'expo-notifications'
import { Platform } from 'react-native'
import { registerPushDevice, unregisterPushDevice } from '../api/agritrackApi'

const KNOWN_ASSIGNMENTS_KEY = 'agritrack.knownAssignmentUuids'
const PUSH_TOKEN_KEY = 'agritrack.expoPushToken'

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowAlert: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
    shouldShowBanner: true,
    shouldShowList: true,
  }),
})

export async function ensureNotificationPermissions() {
  const settings = await Notifications.getPermissionsAsync()
  if (settings.granted || settings.ios?.status === Notifications.IosAuthorizationStatus.PROVISIONAL) {
    return true
  }

  const requested = await Notifications.requestPermissionsAsync()
  return requested.granted || requested.ios?.status === Notifications.IosAuthorizationStatus.PROVISIONAL
}

export async function registerPushNotifications() {
  const allowed = await ensureNotificationPermissions()
  if (!allowed) {
    return null
  }

  if (Platform.OS === 'android') {
    await Notifications.setNotificationChannelAsync('assignments', {
      name: 'Container assignments',
      importance: Notifications.AndroidImportance.HIGH,
      vibrationPattern: [0, 250, 250, 250],
    })
  }

  const projectId =
    Constants.expoConfig?.extra?.eas?.projectId ??
    Constants.easConfig?.projectId

  const tokenResult = projectId
    ? await Notifications.getExpoPushTokenAsync({ projectId })
    : await Notifications.getExpoPushTokenAsync()

  const expoPushToken = tokenResult.data
  await AsyncStorage.setItem(PUSH_TOKEN_KEY, expoPushToken)
  await registerPushDevice(expoPushToken, Platform.OS)
  return expoPushToken
}

export async function unregisterPushNotifications() {
  const token = await AsyncStorage.getItem(PUSH_TOKEN_KEY)
  if (token) {
    try {
      await unregisterPushDevice(token)
    } catch {
      // Ignore logout cleanup failures.
    }
    await AsyncStorage.removeItem(PUSH_TOKEN_KEY)
  }
}

export async function showLocalAssignmentAlert(containerNumber: string) {
  await Notifications.scheduleNotificationAsync({
    content: {
      title: 'New container assignment',
      body: `You were assigned container ${containerNumber}.`,
      data: { type: 'agritrack_assignment' },
    },
    trigger: null,
  })
}

export async function loadKnownAssignmentUuids(): Promise<Set<string>> {
  const raw = await AsyncStorage.getItem(KNOWN_ASSIGNMENTS_KEY)
  if (!raw) {
    return new Set()
  }
  try {
    return new Set(JSON.parse(raw) as string[])
  } catch {
    return new Set()
  }
}

export async function saveKnownAssignmentUuids(uuids: Set<string>) {
  await AsyncStorage.setItem(KNOWN_ASSIGNMENTS_KEY, JSON.stringify([...uuids]))
}

export async function detectNewAssignments(
  containers: Array<{ uuid: string; containerNumber: string }>,
  isInitialLoad: boolean,
) {
  const known = await loadKnownAssignmentUuids()
  const current = new Set(containers.map((c) => c.uuid))

  if (!isInitialLoad) {
    for (const container of containers) {
      if (!known.has(container.uuid)) {
        await showLocalAssignmentAlert(container.containerNumber)
      }
    }
  }

  await saveKnownAssignmentUuids(current)
}

export function addNotificationListeners(
  onAssignment?: (containerUuid?: string) => void,
) {
  const received = Notifications.addNotificationReceivedListener((event) => {
    const type = event.request.content.data?.type
    if (type === 'agritrack_assignment') {
      onAssignment?.(event.request.content.data?.containerUuid as string | undefined)
    }
  })

  const response = Notifications.addNotificationResponseReceivedListener((event) => {
    const type = event.notification.request.content.data?.type
    if (type === 'agritrack_assignment') {
      onAssignment?.(event.notification.request.content.data?.containerUuid as string | undefined)
    }
  })

  return () => {
    received.remove()
    response.remove()
  }
}
