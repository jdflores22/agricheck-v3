export type SubmissionFileVersion = {
  versionNumber: number
  originalFileName: string
  fileSizeBytes: number
  createdAt: string
  isCurrent: boolean
}

export type SubmissionFileWithVersions = {
  uuid: string
  originalFileName: string
  fileSizeBytes: number
  createdAt: string
  reviewDecision?: string
  reviewComment?: string
  versions?: SubmissionFileVersion[]
}

export function resolveCurrentSubmissionFile(file?: SubmissionFileWithVersions | null) {
  if (!file) return null

  const currentVersion = file.versions?.find((version) => version.isCurrent) ?? file.versions?.[0]

  return {
    fileName: currentVersion?.originalFileName ?? file.originalFileName,
    fileSizeBytes: currentVersion?.fileSizeBytes ?? file.fileSizeBytes,
    uploadedAt: currentVersion?.createdAt ?? file.createdAt,
    versionNumber: currentVersion?.versionNumber ?? file.versions?.length ?? 1,
    previewKey: `${file.uuid}-${currentVersion?.versionNumber ?? 1}-${currentVersion?.fileSizeBytes ?? file.fileSizeBytes}`,
  }
}

export function resolveVersionComparePair(versions?: SubmissionFileVersion[]) {
  if (!versions?.length || versions.length < 2) return null

  const sorted = [...versions].sort((a, b) => a.versionNumber - b.versionNumber)
  const current = sorted.find((version) => version.isCurrent) ?? sorted[sorted.length - 1]
  const previous = sorted.find((version) => version.versionNumber === current.versionNumber - 1)

  if (!previous) return null

  return { previous, current }
}
