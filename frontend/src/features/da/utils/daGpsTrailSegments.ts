import type { DaContainerTrailPoint } from '../api/daApi'
import { findGpsTrailOfflineGaps } from './daGpsTrailOfflineGaps'
import { connectionBetweenPoints, type GpsTrailConnectionKind } from './daTrailPointStatus'
import { isUsablePhilippinesCoordinate } from './philippinesMap'

export interface GpsTrailColoredSegment {
  kind: GpsTrailConnectionKind
  positions: [number, number][]
  gapIndex?: number
  offlineStartedAt?: string
  backOnlineAt?: string
  durationMinutes?: number
}

function toPosition(p: DaContainerTrailPoint): [number, number] | null {
  const lat = Number(p.latitude)
  const lng = Number(p.longitude)
  if (!isUsablePhilippinesCoordinate(lat, lng)) return null
  return [lat, lng]
}

function pushMerged(
  segments: GpsTrailColoredSegment[],
  kind: GpsTrailConnectionKind,
  a: [number, number],
  b: [number, number],
  meta?: Partial<GpsTrailColoredSegment>,
) {
  const last = segments[segments.length - 1]
  if (last && last.kind === kind && last.positions.length > 0) {
    const tail = last.positions[last.positions.length - 1]
    if (tail[0] === a[0] && tail[1] === a[1]) {
      last.positions.push(b)
      return
    }
  }
  segments.push({ kind, positions: [a, b], ...meta })
}

export function buildGpsTrailMapSegments(trail: DaContainerTrailPoint[]) {
  const sorted = [...trail].sort((a, b) => Date.parse(a.recordedAt) - Date.parse(b.recordedAt))
  const gaps = findGpsTrailOfflineGaps(sorted)
  const segments: GpsTrailColoredSegment[] = []

  for (let i = 0; i < sorted.length - 1; i++) {
    const from = sorted[i]
    const to = sorted[i + 1]
    const a = toPosition(from)
    const b = toPosition(to)
    if (!a || !b) continue

    const kind = connectionBetweenPoints(from, to)
    const gap = gaps.find((g) => g.offlineStartedAt === from.recordedAt)
    pushMerged(segments, kind, a, b, gap
      ? {
          gapIndex: gap.gapIndex,
          offlineStartedAt: gap.offlineStartedAt,
          backOnlineAt: gap.backOnlineAt,
          durationMinutes: gap.durationMinutes,
        }
      : undefined)
  }

  const online = segments.filter((s) => s.kind === 'online')
  const offlineBuffered = segments.filter((s) => s.kind === 'offline-buffered')
  const noSignal = segments.filter((s) => s.kind === 'no-signal-gap')

  return { segments, online, offlineBuffered, noSignal, gaps }
}
