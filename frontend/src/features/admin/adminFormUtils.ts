import { portalColors } from '../../components/portal/portalTheme'
import type { FormType } from '../forms/formSchema'

export const FORM_TYPE_TABS: Array<{ value: 'ALL' | FormType; label: string }> = [
  { value: 'ALL', label: 'All' },
  { value: 'ACCREDITATION', label: 'Accreditation' },
  { value: 'ENTRY', label: 'Entry' },
  { value: 'CONTAINER', label: 'Container' },
  { value: 'INSPECTION', label: 'Inspection' },
  { value: 'MAV', label: 'MAV' },
]

export function parseFormTypeTab(value: string | null): 'ALL' | FormType {
  if (!value) return 'ALL'
  const match = FORM_TYPE_TABS.find((tab) => tab.value === value)
  return match ? (match.value as 'ALL' | FormType) : 'ALL'
}

export function getFormTypeLabel(formType: string) {
  switch (formType) {
    case 'ENTRY':
      return 'Entry'
    case 'CONTAINER':
      return 'Container'
    case 'INSPECTION':
      return 'Inspection'
    case 'MAV':
      return 'MAV'
    case 'ACCREDITATION':
      return 'Accreditation'
    default:
      return formType
  }
}

export function formatFormDate(iso: string) {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export async function downloadAdminFormExport(uuid: string, fileName: string, accessToken?: string | null) {
  const response = await fetch(`/api/v1/admin/forms/${uuid}/export`, {
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

export const adminFormActionIconSx = {
  border: `1px solid ${portalColors.borderStrong}`,
  color: portalColors.textDark,
  borderRadius: '0.375rem',
  '&:hover': {
    borderColor: portalColors.primary,
    bgcolor: portalColors.bgMuted,
    color: portalColors.primary,
  },
} as const

export const adminFormDeleteIconSx = {
  border: '1px solid #fecaca',
  color: '#b91c1c',
  borderRadius: '0.375rem',
  '&:hover': {
    borderColor: '#f87171',
    bgcolor: '#fef2f2',
  },
} as const
