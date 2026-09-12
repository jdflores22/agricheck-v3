import AsyncStorage from '@react-native-async-storage/async-storage'
import { STORAGE_KEYS } from '../config'
import type { AuthTokens, AuthUser } from '../types/api'

export async function getApiBaseUrl() {
  const stored = await AsyncStorage.getItem(STORAGE_KEYS.apiBaseUrl)
  return stored ?? null
}

export async function setApiBaseUrl(url: string) {
  await AsyncStorage.setItem(STORAGE_KEYS.apiBaseUrl, url.replace(/\/$/, ''))
}

export async function loadSession(): Promise<{
  accessToken: string | null
  refreshToken: string | null
  user: AuthUser | null
}> {
  const [accessToken, refreshToken, userJson] = await Promise.all([
    AsyncStorage.getItem(STORAGE_KEYS.accessToken),
    AsyncStorage.getItem(STORAGE_KEYS.refreshToken),
    AsyncStorage.getItem(STORAGE_KEYS.userJson),
  ])

  return {
    accessToken,
    refreshToken,
    user: userJson ? (JSON.parse(userJson) as AuthUser) : null,
  }
}

export async function saveSession(tokens: AuthTokens, user: AuthUser) {
  await Promise.all([
    AsyncStorage.setItem(STORAGE_KEYS.accessToken, tokens.accessToken),
    AsyncStorage.setItem(STORAGE_KEYS.refreshToken, tokens.refreshToken),
    AsyncStorage.setItem(STORAGE_KEYS.userJson, JSON.stringify(user)),
  ])
}

export async function clearSession() {
  await Promise.all([
    AsyncStorage.removeItem(STORAGE_KEYS.accessToken),
    AsyncStorage.removeItem(STORAGE_KEYS.refreshToken),
    AsyncStorage.removeItem(STORAGE_KEYS.userJson),
  ])
}
