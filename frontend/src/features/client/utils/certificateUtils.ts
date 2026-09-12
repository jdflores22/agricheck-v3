export interface CertificateSummary {
  processType?: string
  accreditationSubmissionUuid?: string
  accreditationNumber?: string
  companyName?: string
}

export function parseCertificateSummary(summaryJson?: string | null): CertificateSummary | null {
  if (!summaryJson) return null
  try {
    return JSON.parse(summaryJson) as CertificateSummary
  } catch {
    return null
  }
}

export function formatCertificateDate(value?: string | null) {
  if (!value) return null
  return new Date(value).toLocaleDateString('en-US', {
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  })
}

export function formatProcessTypeLabel(processType?: string | null, title?: string) {
  if (processType) {
    return processType.replace(/_/g, ' ').replace(/\b\w/g, (char) => char.toUpperCase())
  }
  if (title?.toLowerCase().includes('accreditation')) return 'Accreditation'
  if (title?.toLowerCase().includes('import')) return 'Import Entry'
  if (title?.toLowerCase().includes('export')) return 'Export Entry'
  return 'Certificate'
}

export function getEffectiveCertificateStatus(status: string, expiresAt?: string | null) {
  const normalized = status.toUpperCase()
  if (normalized === 'ACTIVE' && expiresAt && new Date(expiresAt) < new Date()) {
    return 'EXPIRED'
  }
  return normalized
}

export function getCertificateStatusLabel(status: string) {
  switch (status.toUpperCase()) {
    case 'ACTIVE':
      return 'Active'
    case 'EXPIRED':
      return 'Expired'
    case 'REVOKED':
      return 'Revoked'
    default:
      return status
  }
}
