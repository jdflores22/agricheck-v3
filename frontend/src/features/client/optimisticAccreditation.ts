import type { ClientDashboardAccreditation } from './api/clientApi'

const STORAGE_KEY = 'agricheck.optimisticAccreditation'

const STATUS_RANK: Record<string, number> = {
  Draft: 1,
  Submitted: 2,
  RevisionRequired: 2,
  UnderReview: 3,
  Rejected: 2,
  Approved: 4,
}

type StoredOptimisticAccreditation = {
  userUuid?: string
  accreditation: ClientDashboardAccreditation
}

export function writeOptimisticAccreditation(accreditation: ClientDashboardAccreditation, userUuid?: string) {
  try {
    const payload: StoredOptimisticAccreditation = { userUuid, accreditation }
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(payload))
  } catch {
    // Ignore quota / private-mode failures; RTK cache still covers the same tab.
  }
}

export function clearOptimisticAccreditation() {
  try {
    sessionStorage.removeItem(STORAGE_KEY)
  } catch {
    // no-op
  }
}

function readStored(): StoredOptimisticAccreditation | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as StoredOptimisticAccreditation | ClientDashboardAccreditation
    if (parsed && typeof parsed === 'object' && 'accreditation' in parsed) {
      return parsed
    }
    return { accreditation: parsed as ClientDashboardAccreditation }
  } catch {
    return null
  }
}

export function resolveDashboardAccreditation(
  server?: ClientDashboardAccreditation,
  userUuid?: string,
): ClientDashboardAccreditation | undefined {
  const stored = readStored()
  const optimistic = stored?.accreditation
  if (!optimistic) return server
  if (stored?.userUuid && userUuid && stored.userUuid !== userUuid) {
    return server
  }
  if (server?.isAccredited) {
    clearOptimisticAccreditation()
    return server
  }

  const serverRank = STATUS_RANK[server?.status ?? ''] ?? 0
  const optimisticRank = STATUS_RANK[optimistic.status ?? ''] ?? 0
  if (server?.status && serverRank >= optimisticRank) {
    clearOptimisticAccreditation()
    return server
  }

  return {
    ...server,
    ...optimistic,
    isAccredited: server?.isAccredited ?? optimistic.isAccredited,
  }
}
