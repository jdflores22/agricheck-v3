import type { DaContainerTrailPoint } from '../api/daApi'
import { DRIVER_GPS_OFFLINE_GAP_MINUTES } from './daGpsTrailOfflineGaps'

/** Matches backend ContainerLocationTrailHelper.BufferedSyncDelayMinutes */
export const BUFFERED_SYNC_DELAY_MINUTES = 5

export type GpsTrailConnectionKind = 'online' | 'offline-buffered' | 'no-signal-gap'

export function resolveTrailPointCapturedOffline(point: DaContainerTrailPoint): boolean {
  if (point.capturedOffline) return true
  if (!point.syncedAtUtc) return false
  const delayMs = Date.parse(point.syncedAtUtc) - Date.parse(point.recordedAt)
  return Number.isFinite(delayMs) && delayMs >= BUFFERED_SYNC_DELAY_MINUTES * 60_000
}

export function connectionBetweenPoints(
  from: DaContainerTrailPoint,
  to: DaContainerTrailPoint,
): GpsTrailConnectionKind {
  const deltaMs = Date.parse(to.recordedAt) - Date.parse(from.recordedAt)
  if (Number.isFinite(deltaMs) && deltaMs >= DRIVER_GPS_OFFLINE_GAP_MINUTES * 60_000) {
    return 'no-signal-gap'
  }
  if (resolveTrailPointCapturedOffline(from) || resolveTrailPointCapturedOffline(to)) {
    return 'offline-buffered'
  }
  return 'online'
}

export function trailPointStatusLabel(point: DaContainerTrailPoint): 'Online' | 'Offline (queued)' {
  return resolveTrailPointCapturedOffline(point) ? 'Offline (queued)' : 'Online'
}
