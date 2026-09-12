import type { FormFieldSchema } from '../../forms/formSchema'

export type ComplianceDocumentFile = {
  uuid: string
  originalFileName: string
  reviewDecision?: string
  reviewComment?: string
  versions?: Array<{ versionNumber: number; isCurrent: boolean }>
}

export type ComplianceDocumentItem = ComplianceDocumentFile & {
  label: string
  needsUpload: boolean
  revisionUploaded: boolean
}

function humanizeFieldName(fieldName: string) {
  return fieldName
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase())
}

export function buildFileLabelMap(
  formValues: Record<string, string>,
  schemaFields: FormFieldSchema[],
): Map<string, string> {
  const labelByFieldName = new Map(schemaFields.map((field) => [field.name, field.label]))

  const map = new Map<string, string>()
  for (const [key, value] of Object.entries(formValues)) {
    if (!key.endsWith('_file_uuid')) continue
    const fileUuid = value.trim()
    if (!fileUuid) continue

    const fieldName = key.slice(0, -'_file_uuid'.length)
    map.set(fileUuid, labelByFieldName.get(fieldName) ?? humanizeFieldName(fieldName))
  }

  return map
}

export function listComplianceDocuments(
  files: ComplianceDocumentFile[],
  formValues: Record<string, string>,
  schemaFields: FormFieldSchema[],
): ComplianceDocumentItem[] {
  const labelMap = buildFileLabelMap(formValues, schemaFields)

  return files
    .filter((file) => {
      if (file.reviewDecision === 'RevisionRequired') return true
      if (file.reviewDecision === 'Pending' && (file.versions?.length ?? 0) > 1) return true
      return false
    })
    .map((file) => ({
      ...file,
      label: labelMap.get(file.uuid) ?? file.originalFileName,
      needsUpload: file.reviewDecision === 'RevisionRequired',
      revisionUploaded: file.reviewDecision === 'Pending' && (file.versions?.length ?? 0) > 1,
    }))
}

export function canResubmitCompliance(files: ComplianceDocumentFile[]) {
  const pendingRevision = files.some((file) => file.reviewDecision === 'RevisionRequired')
  const hasRevisedUpload = files.some(
    (file) => file.reviewDecision === 'Pending' && (file.versions?.length ?? 0) > 1,
  )

  return !pendingRevision && hasRevisedUpload
}

export function parseReviewerCommentBullets(comment?: string | null) {
  if (!comment?.trim()) return []

  const lines = comment
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean)

  const bulletLines = lines
    .filter((line) => line.startsWith('•') || line.startsWith('-'))
    .map((line) => line.replace(/^[•-]\s*/, '').trim())

  if (bulletLines.length > 0) return bulletLines

  return lines.length > 1 ? lines.slice(1) : lines
}

export function getReviewerCommentIntro(comment?: string | null) {
  if (!comment?.trim()) return ''

  const firstLine = comment.split('\n').map((line) => line.trim()).find(Boolean) ?? ''
  if (firstLine.startsWith('•') || firstLine.startsWith('-')) return ''
  return firstLine
}
