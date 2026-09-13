import type { ClientContainerInspectionPhoto } from '../../client/api/clientApi'
import {
  CONTAINER_INSPECTION_PHOTO_LABELS,
  CONTAINER_INSPECTION_PHOTO_TYPES,
} from '../../client/api/clientApi'

export type ContainerPhotoReview = {
  photoType: string
  label: string
  photoUuid: string
  fileName: string
  reviewDecision?: string
  reviewComment?: string
}

export function listContainerPhotoReviews(photos: ClientContainerInspectionPhoto[]): ContainerPhotoReview[] {
  const photosByType = new Map(photos.map((photo) => [photo.photoType, photo]))

  return CONTAINER_INSPECTION_PHOTO_TYPES.flatMap((photoType) => {
    const photo = photosByType.get(photoType)
    if (!photo) return []

    return [{
      photoType,
      label: CONTAINER_INSPECTION_PHOTO_LABELS[photoType],
      photoUuid: photo.uuid,
      fileName: photo.originalFileName,
      reviewDecision: photo.reviewDecision === 'Pending' ? undefined : photo.reviewDecision,
      reviewComment: photo.reviewComment,
    }]
  })
}

export function getContainerPhotoReviewStats(photos: ContainerPhotoReview[]) {
  const approved = photos.filter((photo) => photo.reviewDecision === 'Approved').length
  const rejected = photos.filter((photo) => photo.reviewDecision === 'Rejected').length
  const pending = photos.filter((photo) => !photo.reviewDecision).length
  const evaluated = photos.length - pending
  const allEvaluated = photos.length > 0 && pending === 0
  const allApproved = photos.length > 0 && approved === photos.length
  const requiresRevisionOutcome = !allApproved

  return {
    total: photos.length,
    evaluated,
    approved,
    rejected,
    pending,
    allEvaluated,
    allApproved,
    requiresRevisionOutcome,
  }
}

export function resolveContainerFinalDecision(
  stats: Pick<ReturnType<typeof getContainerPhotoReviewStats>, 'requiresRevisionOutcome' | 'allApproved'>,
  current: string,
) {
  if (stats.requiresRevisionOutcome) return 'RevisionRequired'
  if (stats.allApproved) return current === 'Rejected' ? 'Rejected' : 'Approved'
  return 'RevisionRequired'
}

export function canSelectContainerFinalDecision(
  stats: Pick<
    ReturnType<typeof getContainerPhotoReviewStats>,
    'allEvaluated' | 'allApproved' | 'requiresRevisionOutcome'
  >,
  decision: string,
) {
  if (!stats.allEvaluated) return false
  if (decision === 'Approved') return stats.allApproved
  if (decision === 'RevisionRequired') return stats.requiresRevisionOutcome
  if (decision === 'Rejected') return stats.allApproved
  return false
}

export function buildAutoContainerComment(
  photos: ContainerPhotoReview[],
  outcome: string,
): string {
  if (outcome === 'Approved') {
    return 'All required container photos have been reviewed and approved. This container may proceed to transport tagging.'
  }

  if (outcome === 'Rejected') {
    return 'This container inspection was rejected after review.'
  }

  const rejected = photos.filter((photo) => photo.reviewDecision === 'Rejected')
  if (rejected.length === 0) {
    return 'Please revise and re-upload the required container photos.'
  }

  const bullets = rejected.map(
    (photo) => `• ${photo.label}: ${photo.reviewComment?.trim() || 'Photo rejected.'}`,
  )
  return ['Please revise and re-upload the following container photos:', '', ...bullets].join('\n')
}

export function getContainerSubmissionBlockers(input: {
  photos: ContainerPhotoReview[]
  outcome: string
}): string[] {
  const stats = getContainerPhotoReviewStats(input.photos)
  const blockers: string[] = []

  if (stats.total === 0) {
    blockers.push('No required container photos were uploaded.')
  } else if (!stats.allEvaluated) {
    blockers.push(`${stats.pending} photo${stats.pending === 1 ? '' : 's'} still need a review decision.`)
  } else if (!canSelectContainerFinalDecision(stats, input.outcome)) {
    blockers.push('Choose a container outcome that matches the photo review results.')
  }

  return blockers
}

export function validateContainerOutcomeSubmission(input: {
  photos: ContainerPhotoReview[]
  outcome: string
}): string | null {
  const blockers = getContainerSubmissionBlockers(input)
  return blockers.length > 0 ? blockers[0] : null
}

export function getContainerOutcomeConfirmationMessage(input: {
  containerNumber: string
  entryReferenceNo: string
  outcome: string
  photos: ContainerPhotoReview[]
}) {
  const stats = getContainerPhotoReviewStats(input.photos)

  if (input.outcome === 'Approved') {
    return {
      title: 'Approve this container?',
      body: `Confirm approval for container ${input.containerNumber} on entry ${input.entryReferenceNo}. All ${stats.approved} photos are approved and the container will be marked ready for transport.`,
    }
  }

  if (input.outcome === 'Rejected') {
    return {
      title: 'Reject this container?',
      body: `Confirm rejection for container ${input.containerNumber} on entry ${input.entryReferenceNo}.`,
    }
  }

  return {
    title: 'Send container back for revision?',
    body: `Confirm revision required for container ${input.containerNumber}. The importer will be asked to re-upload ${stats.rejected} rejected photo${stats.rejected === 1 ? '' : 's'}.`,
  }
}
