import type {
  AuthResponse,
  ContainerListItem,
  DriverDashboard,
  DriverProfile,
  MobileSyncPullResult,
} from '../types/api'
import type { ContainerTrack } from '../types/tracking'
import { apiRequest, resolveBaseUrl } from './client'

export async function login(email: string, password: string) {
  const url = await resolveBaseUrl()
  const response = await fetch(`${url}/api/mobile/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  const envelope = await response.json()
  if (!response.ok || !envelope.success) {
    throw new Error(envelope.errors?.[0]?.message ?? 'Login failed')
  }
  return envelope.data as AuthResponse
}

export function getAssignedContainers() {
  return apiRequest<ContainerListItem[]>('/api/mobile/containers/assigned')
}

export function syncPull() {
  return apiRequest<MobileSyncPullResult>('/api/mobile/sync/pull')
}

export function updateContainerStatus(uuid: string, status: string) {
  return apiRequest<ContainerListItem>(`/api/mobile/containers/${uuid}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status }),
  })
}

export function recordContainerLocation(uuid: string, latitude: number, longitude: number) {
  return apiRequest<{ recorded: boolean }>(`/api/mobile/containers/${uuid}/location`, {
    method: 'POST',
    body: JSON.stringify({ latitude, longitude }),
  })
}

export function getContainerTrack(uuid: string) {
  return apiRequest<ContainerTrack>(`/api/mobile/containers/${uuid}/track`)
}

export function registerPushDevice(expoPushToken: string, platform: string) {
  return apiRequest<{ registered: boolean }>('/api/mobile/push/register', {
    method: 'POST',
    body: JSON.stringify({ expoPushToken, platform }),
  })
}

export function unregisterPushDevice(expoPushToken: string) {
  return apiRequest<{ removed: boolean }>(`/api/mobile/push/register?expoPushToken=${encodeURIComponent(expoPushToken)}`, {
    method: 'DELETE',
  })
}

export function getDriverDashboard() {
  return apiRequest<DriverDashboard>('/api/v1/ops/driver/dashboard')
}

export function getDriverProfile() {
  return apiRequest<DriverProfile>('/api/v1/ops/driver/profile')
}

export function updateDriverProfile(body: Partial<DriverProfile>) {
  return apiRequest<DriverProfile>('/api/v1/ops/driver/profile', {
    method: 'PUT',
    body: JSON.stringify(body),
  })
}

export function getClaimableContainers() {
  return apiRequest<ContainerListItem[]>('/api/mobile/operator/containers/claimable')
}

export function claimContainer(uuid: string) {
  return apiRequest<ContainerListItem>(`/api/mobile/operator/containers/${uuid}/claim`, {
    method: 'POST',
    body: JSON.stringify({}),
  })
}

export function assignDriver(uuid: string, driverUserUuid: string) {
  return apiRequest<ContainerListItem>(`/api/mobile/operator/containers/${uuid}/assign-driver`, {
    method: 'POST',
    body: JSON.stringify({ driverUserUuid }),
  })
}
