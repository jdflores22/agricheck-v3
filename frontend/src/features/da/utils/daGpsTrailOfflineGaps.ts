import type { DaContainerTrailPoint } from '../api/daApi'

/** Same threshold as live map / DaLogisticsLiveService (no ping within this window = offline). */
export const DRIVER_GPS_OFFLINE_GAP_MINUTES = 3

export interface GpsTrailOfflineGap {
  gapIndex: number
  lastPingBeforeOffline: DaContainerTrailPoint
  firstPingAfterOffline: DaContainerTrailPoint
  offlineStartedAt: string
  backOnlineAt: string
  durationMinutes: number
}

export function findGpsTrailOfflineGaps(
  trail: DaContainerTrailPoint[],
  thresholdMinutes = DRIVER_GPS_OFFLINE_GAP_MINUTES,
): GpsTrailOfflineGap[] {
  if (trail.length < 2) return []

  const sorted = [...trail].sort((a, b) => Date.parse(a.recordedAt) - Date.parse(b.recordedAt))
  const thresholdMs = thresholdMinutes * 60_000
  const gaps: GpsTrailOfflineGap[] = []

  for (let i = 1; i < sorted.length; i++) {
    const prev = sorted[i - 1]
    const cur = sorted[i]
    const deltaMs = Date.parse(cur.recordedAt) - Date.parse(prev.recordedAt)
    if (!Number.isFinite(deltaMs) || deltaMs < thresholdMs) continue

    const durationMinutes = Math.round(deltaMs / 60_000)
    gaps.push({
      gapIndex: gaps.length + 1,
      lastPingBeforeOffline: prev,
      firstPingAfterOffline: cur,
      offlineStartedAt: prev.recordedAt,
      backOnlineAt: cur.recordedAt,
      durationMinutes,
    })
  }

  return gaps
}
