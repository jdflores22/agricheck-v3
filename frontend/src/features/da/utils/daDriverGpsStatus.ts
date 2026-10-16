import type { DaLiveDelivery } from '../api/daApi'

const OFFLINE_THRESHOLD_MINUTES = 3

/** Recompute offline minutes using API flag + client clock between polls. */
export function resolveDriverGpsDisplay(
  delivery: DaLiveDelivery,
  serverTimeIso?: string | null,
): { offline: boolean; offlineMinutes: number | null; label: string } {
  const serverMs = serverTimeIso ? Date.parse(serverTimeIso) : Date.now()
  const skewMs = Date.now() - (Number.isFinite(serverMs) ? serverMs : Date.now())

  if (delivery.driverGpsOffline) {
    const mins =
      delivery.driverOfflineMinutes ??
      (delivery.lastLocationAt
        ? Math.max(
            OFFLINE_THRESHOLD_MINUTES,
            Math.floor(
              (Date.now() - skewMs - Date.parse(delivery.lastLocationAt)) / 60_000,
            ),
          )
        : null)
    const label =
      mins != null && mins > 0 ? `Offline · ${mins} min` : 'Offline · no GPS yet'
    return { offline: true, offlineMinutes: mins, label }
  }

  if (delivery.lastLocationAt) {
    const ageMin = (Date.now() - skewMs - Date.parse(delivery.lastLocationAt)) / 60_000
    if (ageMin >= OFFLINE_THRESHOLD_MINUTES) {
      const mins = Math.floor(ageMin)
      return { offline: true, offlineMinutes: mins, label: `Offline · ${mins} min` }
    }
  }

  return { offline: false, offlineMinutes: null, label: 'Live GPS' }
}
