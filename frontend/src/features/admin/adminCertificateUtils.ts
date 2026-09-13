import { apiUrl } from '../../app/apiBase'

export function isCertificateTemplateLive(template: { isActive: boolean; hasPublishedVersion: boolean }) {
  return template.isActive && template.hasPublishedVersion
}

export const CERTIFICATE_DELETE_LIVE_TOOLTIP = 'Cannot delete a live template. Deactivate it first.'

export function getCertificateActivateTooltip(template: {
  isActive: boolean
  elementCount?: number
  hasPublishedVersion?: boolean
}) {
  if (template.isActive) return 'Deactivate this certificate template'
  if ((template.elementCount ?? 0) === 0) return 'Add elements in the builder before activating.'
  if (!template.hasPublishedVersion) {
    return 'Activates this template and publishes the latest version.'
  }
  return 'Make this template available for certificate generation'
}

export function formatCertificateProcessType(type: string) {
  if (type === 'ImportEntry') return 'Import Entry'
  if (type === 'ExportEntry') return 'Export Entry'
  return type.replace(/([A-Z])/g, ' $1').trim()
}

export function formatCertificateDate(iso: string) {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export async function downloadAdminCertificateExport(uuid: string, fileName: string, accessToken?: string | null) {
  const response = await fetch(apiUrl(`/admin/certificate-templates/${uuid}/export`), {
    headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
  })

  if (!response.ok) {
    throw new Error('Export failed')
  }

  const blob = await response.blob()
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
