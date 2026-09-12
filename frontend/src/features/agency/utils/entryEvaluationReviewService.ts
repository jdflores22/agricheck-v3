import type { FormFieldSchema } from '../../forms/formSchema'
import { visibleFormFields } from '../../forms/formSchema'

export type EntryEvaluationFile = {
  uuid: string
  originalFileName: string
  evaluationDecision?: string
  evaluationComment?: string
}

export type RequiredEntryDocumentReview = {
  fieldLabel: string
  fieldName: string
  fileUuid: string
  fileName: string
  evaluationDecision?: string
  evaluationComment?: string
}

export function listRequiredEntryDocumentReviews(
  schemaFields: FormFieldSchema[],
  formValues: Record<string, string>,
  files: EntryEvaluationFile[],
): RequiredEntryDocumentReview[] {
  const filesByUuid = new Map(files.map((file) => [file.uuid, file]))
  const fileFields = visibleFormFields(schemaFields, formValues).filter(
    (field) => field.type === 'file' || field.type === 'geotag_photo',
  )

  return fileFields.flatMap((field) => {
    const fileName = formValues[field.name]?.trim()
    const fileUuid = formValues[`${field.name}_file_uuid`]?.trim()
    if (!fileName || !fileUuid) return []

    const matched = filesByUuid.get(fileUuid)
    return [
      {
        fieldLabel: field.label,
        fieldName: field.name,
        fileUuid,
        fileName,
        evaluationDecision: matched?.evaluationDecision,
        evaluationComment: matched?.evaluationComment,
      },
    ]
  })
}

export function getRequiredEntryDocumentReviewStats(documents: RequiredEntryDocumentReview[]) {
  const approved = documents.filter((d) => d.evaluationDecision === 'Approved').length
  const revisionRequired = documents.filter((d) => d.evaluationDecision === 'RevisionRequired').length
  const rejected = documents.filter((d) => d.evaluationDecision === 'Rejected').length
  const pending = documents.filter((d) => !d.evaluationDecision).length
  const evaluated = documents.length - pending
  const allEvaluated = documents.length > 0 && pending === 0
  const allApproved = documents.length > 0 && approved === documents.length
  const requiresRevisionOutcome = !allApproved

  return {
    total: documents.length,
    evaluated,
    approved,
    revisionRequired,
    rejected,
    pending,
    allEvaluated,
    allApproved,
    requiresRevisionOutcome,
  }
}

export function resolveEntryFinalDecision(
  stats: Pick<ReturnType<typeof getRequiredEntryDocumentReviewStats>, 'requiresRevisionOutcome' | 'allApproved'>,
  current: string,
) {
  if (stats.requiresRevisionOutcome) return 'RevisionRequired'
  if (stats.allApproved) return current === 'Rejected' ? 'Rejected' : 'Approved'
  return 'RevisionRequired'
}

export function canSelectEntryFinalDecision(
  stats: Pick<
    ReturnType<typeof getRequiredEntryDocumentReviewStats>,
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

export function buildAutoEntryComment(
  documents: RequiredEntryDocumentReview[],
  outcome: string,
): string {
  if (outcome === 'Approved') {
    return 'All required documents have been reviewed and approved. Your entry may proceed to the next stage.'
  }

  if (outcome === 'Rejected') {
    const rejected = documents.filter((d) => d.evaluationDecision === 'Rejected')
    if (rejected.length === 0) {
      return 'Your entry has been rejected.'
    }

    const bullets = rejected.map(
      (doc) => `• ${doc.fieldLabel}: ${doc.evaluationComment?.trim() || 'Document rejected.'}`,
    )
    return ['Your entry has been rejected.', '', 'Remarks:', ...bullets].join('\n')
  }

  const needsRevision = documents.filter(
    (d) => d.evaluationDecision === 'RevisionRequired' || d.evaluationDecision === 'Rejected' || !d.evaluationDecision,
  )

  if (needsRevision.length === 0) {
    return 'Please revise and resubmit the required documents for your entry.'
  }

  const bullets = needsRevision.map((doc) => {
    if (!doc.evaluationDecision) {
      return `• ${doc.fieldLabel}: Please submit and ensure this document is complete.`
    }
    if (doc.evaluationDecision === 'Rejected') {
      return `• ${doc.fieldLabel}: ${doc.evaluationComment?.trim() || 'Document rejected. Please replace and resubmit.'}`
    }
    return `• ${doc.fieldLabel}: ${doc.evaluationComment?.trim() || 'Please revise and resubmit this document.'}`
  })

  return [
    'Please address the following document issues and resubmit your entry:',
    '',
    ...bullets,
  ].join('\n')
}

export function validateEntryOutcomeSubmission(input: {
  documents: RequiredEntryDocumentReview[]
  outcome: string
}): string | null {
  const stats = getRequiredEntryDocumentReviewStats(input.documents)

  if (stats.total === 0) {
    return 'No required documents were found for this entry.'
  }

  if (!stats.allEvaluated) {
    return `Review all required documents first (${stats.evaluated}/${stats.total} completed).`
  }

  if (!canSelectEntryFinalDecision(stats, input.outcome)) {
    return 'This entry outcome is not allowed based on the current document reviews.'
  }

  return null
}

export function getEntryOutcomeConfirmationMessage(input: {
  referenceNo: string
  companyName?: string
  outcome: string
  documents: RequiredEntryDocumentReview[]
}) {
  const label = input.companyName ? `${input.referenceNo} (${input.companyName})` : input.referenceNo

  if (input.outcome === 'Approved') {
    return {
      title: 'Approve Entry?',
      body: `You are about to approve entry ${label}. All required documents have been approved.`,
    }
  }

  if (input.outcome === 'Rejected') {
    return {
      title: 'Reject Entry?',
      body: `You are about to reject entry ${label}. The applicant will be notified with your comments.`,
    }
  }

  return {
    title: 'Mark Entry for Revision?',
    body: `You are about to mark entry ${label} as Revision Required. The applicant must upload corrected documents before review can continue.`,
  }
}
