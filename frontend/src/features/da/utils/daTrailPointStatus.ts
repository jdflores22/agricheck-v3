import type { DaContainerTrailPoint } from '../api/daApi'
import { DRIVER_GPS_OFFLINE_GAP_MINUTES } from './daGpsTrailOfflineGaps'

/** Matches backend ContainerLocationTrailHelper.BufferedSyncDelaySeconds */
export const BUFFERED_SYNC_DELAY_SECONDS = 4 * 60 + 30

export type GpsTrailConnectionKind = 'online' | 'offline-buffered' | 'no-signal-gap'

export function resolveSyncDelaySeconds(point: DaContainerTrailPoint): number | null {
  if (point.syncDelaySeconds != null && Number.isFinite(point.syncDelaySeconds)) {
    return Math.max(0, Math.round(point.syncDelaySeconds))
  }
  if (!point.syncedAtUtc) return null
  const delayMs = Date.parse(point.syncedAtUtc) - Date.parse(point.recordedAt)
  if (!Number.isFinite(delayMs)) return null
  return Math.max(0, Math.round(delayMs / 1000))
}

export function resolveTrailPointCapturedOffline(point: DaContainerTrailPoint): boolean {
  if (point.capturedOffline) return true
  const delay = resolveSyncDelaySeconds(point)
  return delay != null && delay >= BUFFERED_SYNC_DELAY_SECONDS
}

/** True when API trail lacks sync metadata (pre-deploy or error). */
export function trailLacksSyncMetadata(trail: DaContainerTrailPoint[]): boolean {
  if (trail.length === 0) return false
  return trail.every((p) => p.syncDelaySeconds == null && !p.syncedAtUtc)
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
