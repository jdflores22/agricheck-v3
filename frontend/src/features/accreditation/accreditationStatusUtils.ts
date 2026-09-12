export type AccreditationHistoryItem = {
  status: string
  comment?: string
  createdAt: string
}

export type AccreditationReviewPhase =
  | 'initial_review'
  | 'awaiting_client_revision'
  | 'resubmitted_for_review'
  | 'approved'
  | 'rejected'
  | 'submitted'
  | 'other'

export function getAccreditationReviewPhase(
  status: string,
  history: AccreditationHistoryItem[],
): AccreditationReviewPhase {
  if (status === 'RevisionRequired') return 'awaiting_client_revision'
  if (status === 'Approved') return 'approved'
  if (status === 'Rejected') return 'rejected'
  if (status === 'Submitted') return 'submitted'

  if (status === 'UnderReview') {
    const sorted = [...history].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
    )
    const latest = sorted[0]
    const hadRevisionRequired = sorted.some((item) => item.status === 'RevisionRequired')
    const latestIsClientResubmit =
      latest?.status === 'UnderReview' &&
      (latest.comment?.toLowerCase().includes('resubmitted') ?? false)

    if (hadRevisionRequired && latestIsClientResubmit) {
      return 'resubmitted_for_review'
    }

    return 'initial_review'
  }

  return 'other'
}

export function formatAccreditationStatusLabel(status: string, phase: AccreditationReviewPhase) {
  if (phase === 'awaiting_client_revision') return 'Revision Required'
  if (phase === 'resubmitted_for_review') return 'Resubmitted for Review'
  if (phase === 'initial_review') return 'Under Review'

  return status.replace(/([A-Z])/g, ' $1').trim()
}

export function getAccreditationPhaseAlert(phase: AccreditationReviewPhase) {
  switch (phase) {
    case 'awaiting_client_revision':
      return {
        severity: 'warning' as const,
        message:
          'This application is waiting for the applicant to upload revised documents. Review controls will unlock after they resubmit.',
      }
    case 'resubmitted_for_review':
      return {
        severity: 'info' as const,
        message:
          'The applicant has resubmitted revised documents. Please review the updated files and submit a new application outcome.',
      }
    case 'initial_review':
      return {
        severity: 'info' as const,
        message: 'Review each required document, then submit the application outcome when finished.',
      }
    default:
      return null
  }
}

export function canOfficerReviewAccreditation(status: string, phase: AccreditationReviewPhase, isAssignedToMe: boolean, canClaim: boolean) {
  if (!isAssignedToMe || canClaim) return false
  if (status === 'UnderReview' && (phase === 'initial_review' || phase === 'resubmitted_for_review')) {
    return true
  }
  return false
}

export function getAccreditationStatusChipKey(displayStatus: string, fallbackStatus?: string) {
  if (displayStatus === 'Resubmitted for Review') return 'ResubmittedForReview'
  if (displayStatus === 'Revision Required') return 'RevisionRequired'
  return fallbackStatus ?? displayStatus
}

export function resolveAccreditationListItemStatus(item: { status: string; displayStatus?: string; history?: AccreditationHistoryItem[] }) {
  const label =
    item.displayStatus ??
    formatAccreditationStatusLabel(item.status, getAccreditationReviewPhase(item.status, item.history ?? []))
  const chipKey = getAccreditationStatusChipKey(label, item.status)
  return { label, chipKey }
}

export function isResubmittedForReview(item: { status: string; displayStatus?: string; history?: AccreditationHistoryItem[] }) {
  if (item.displayStatus === 'Resubmitted for Review') return true
  return getAccreditationReviewPhase(item.status, item.history ?? []) === 'resubmitted_for_review'
}
