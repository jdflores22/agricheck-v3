import type { FormFieldSchema } from '../../forms/formSchema'
import { visibleFormFields } from '../../forms/formSchema'

export type AccreditationOfficerFile = {
  uuid: string
  originalFileName: string
  reviewDecision?: string
  reviewComment?: string
}

export type RequiredDocumentReview = {
  fieldLabel: string
  fieldName: string
  fileUuid: string
  fileName: string
  reviewDecision?: string
  reviewComment?: string
}

export function listRequiredDocumentReviews(
  schemaFields: FormFieldSchema[],
  formValues: Record<string, string>,
  files: AccreditationOfficerFile[],
): RequiredDocumentReview[] {
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
        reviewDecision: matched?.reviewDecision,
        reviewComment: matched?.reviewComment,
      },
    ]
  })
}

export function getRequiredDocumentReviewStats(documents: RequiredDocumentReview[]) {
  const approved = documents.filter((d) => d.reviewDecision === 'Approved').length
  const revisionRequired = documents.filter((d) => d.reviewDecision === 'RevisionRequired').length
  const rejected = documents.filter((d) => d.reviewDecision === 'Rejected').length
  const pending = documents.filter((d) => !d.reviewDecision).length
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

export function getAccreditationFileReviewStats(files: AccreditationOfficerFile[]) {
  const approved = files.filter((f) => f.reviewDecision === 'Approved').length
  const revisionRequired = files.filter((f) => f.reviewDecision === 'RevisionRequired').length
  const rejected = files.filter((f) => f.reviewDecision === 'Rejected').length
  const pending = files.filter((f) => !f.reviewDecision).length
  const allFilesApproved = files.length > 0 && approved === files.length
  const requiresRevisionDecision = !allFilesApproved

  return {
    total: files.length,
    approved,
    revisionRequired,
    rejected,
    pending,
    allFilesApproved,
    requiresRevisionDecision,
  }
}

export function resolveFinalDecision(
  stats: Pick<ReturnType<typeof getRequiredDocumentReviewStats>, 'requiresRevisionOutcome' | 'allApproved'>,
  current: string,
) {
  if (stats.requiresRevisionOutcome) return 'RevisionRequired'
  if (stats.allApproved) return current === 'Rejected' ? 'Rejected' : 'Approved'
  return 'RevisionRequired'
}

export function canSelectFinalDecision(
  stats: Pick<
    ReturnType<typeof getRequiredDocumentReviewStats>,
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

export function formatOutcomeLabel(outcome: string) {
  switch (outcome) {
    case 'Approved':
      return 'Approved'
    case 'Rejected':
      return 'Rejected'
    case 'RevisionRequired':
      return 'Revision Required'
    default:
      return outcome.replace(/([A-Z])/g, ' $1').trim()
  }
}

export function buildAutoApplicationComment(
  documents: RequiredDocumentReview[],
  outcome: string,
): string {
  if (outcome === 'Approved') {
    return 'All required documents have been reviewed and approved. Your accreditation application is approved.'
  }

  if (outcome === 'Rejected') {
    const rejected = documents.filter((d) => d.reviewDecision === 'Rejected')
    if (rejected.length === 0) {
      return 'Your accreditation application has been rejected.'
    }

    const bullets = rejected.map(
      (doc) => `• ${doc.fieldLabel}: ${doc.reviewComment?.trim() || 'Document rejected.'}`,
    )
    return ['Your accreditation application has been rejected.', '', 'Remarks:', ...bullets].join('\n')
  }

  const needsRevision = documents.filter(
    (d) => d.reviewDecision === 'RevisionRequired' || d.reviewDecision === 'Rejected' || !d.reviewDecision,
  )

  if (needsRevision.length === 0) {
    return 'Please revise and resubmit the required documents for your accreditation application.'
  }

  const bullets = needsRevision.map((doc) => {
    if (!doc.reviewDecision) {
      return `• ${doc.fieldLabel}: Please submit and ensure this document is complete.`
    }
    if (doc.reviewDecision === 'Rejected') {
      return `• ${doc.fieldLabel}: ${doc.reviewComment?.trim() || 'Document rejected. Please replace and resubmit.'}`
    }
    return `• ${doc.fieldLabel}: ${doc.reviewComment?.trim() || 'Please revise and resubmit this document.'}`
  })

  return [
    'Please address the following document issues and resubmit your accreditation application:',
    '',
    ...bullets,
  ].join('\n')
}

export function validateApplicationOutcomeSubmission(input: {
  documents: RequiredDocumentReview[]
  outcome: string
}): string | null {
  const stats = getRequiredDocumentReviewStats(input.documents)

  if (stats.total === 0) {
    return 'No required documents were found for this application.'
  }

  if (!stats.allEvaluated) {
    return `Review all required documents first (${stats.evaluated}/${stats.total} completed).`
  }

  if (!canSelectFinalDecision(stats, input.outcome)) {
    return 'This application outcome is not allowed based on the current document reviews.'
  }

  return null
}

export function getOutcomeConfirmationMessage(input: {
  companyName: string
  outcome: string
  documents: RequiredDocumentReview[]
}) {
  const stats = getRequiredDocumentReviewStats(input.documents)

  if (input.outcome === 'Approved') {
    return {
      title: 'Approve Application?',
      body: `You are about to approve ${input.companyName}. All ${stats.total} required documents have been approved. An accreditation number and certificate will be generated automatically.`,
    }
  }

  if (input.outcome === 'Rejected') {
    return {
      title: 'Reject Application?',
      body: `You are about to reject the accreditation application for ${input.companyName}. The applicant will be notified with your comments.`,
    }
  }

  return {
    title: 'Send for Revision?',
    body: `You are about to mark ${input.companyName} as Revision Required. The applicant will receive your document remarks and must resubmit corrected files.`,
  }
}
