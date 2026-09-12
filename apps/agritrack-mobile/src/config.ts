import Constants from 'expo-constants'
import { Platform } from 'react-native'

const extra = Constants.expoConfig?.extra as { apiBaseUrl?: string } | undefined

export function defaultApiBaseUrl() {
  if (extra?.apiBaseUrl) return extra.apiBaseUrl.replace(/\/$/, '')
  if (Platform.OS === 'android') return 'http://10.0.2.2:5000'
  return 'http://localhost:5000'
}

export const STORAGE_KEYS = {
  apiBaseUrl: 'agritrack.apiBaseUrl',
  accessToken: 'agritrack.accessToken',
  refreshToken: 'agritrack.refreshToken',
  userJson: 'agritrack.user',
} as const
