import type { FetchBaseQueryError } from '@reduxjs/toolkit/query/react'
import { resolveUploadUrl } from '../../../app/apiBase'
import type { ApiEnvelope } from '../../auth/types'

export function resolveAgencyLogoUrl(logoUrl?: string | null): string | null {
  return resolveUploadUrl(logoUrl)
}

export const AGENCY_LOGO_ACCEPT = 'image/png,image/jpeg,image/jpg,image/gif,image/svg+xml,image/webp'
export const AGENCY_LOGO_MAX_BYTES = 2 * 1024 * 1024

const allowedLogoExtensions = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.svg']
const allowedLogoMimeTypes = ['image/png', 'image/jpeg', 'image/gif', 'image/webp', 'image/svg+xml']

export function validateAgencyLogoFile(file: File): string | null {
  if (file.size === 0) return 'Please choose a logo file to upload.'
  if (file.size > AGENCY_LOGO_MAX_BYTES) return 'Logo file must be 2MB or smaller.'

  const extension = file.name.includes('.') ? file.name.slice(file.name.lastIndexOf('.')).toLowerCase() : ''
  const mimeOk = allowedLogoMimeTypes.includes(file.type)
  const extensionOk = allowedLogoExtensions.includes(extension)

  if (!mimeOk && !extensionOk) {
    return 'Logo must be PNG, JPG, GIF, WEBP, or SVG.'
  }

  return null
}

export function getAdminApiErrorMessage(error: unknown, fallback: string): string {
  if (!error || typeof error !== 'object' || !('data' in error)) {
    return fallback
  }

  const data = (error as FetchBaseQueryError).data as ApiEnvelope<unknown> | undefined
  return data?.errors?.[0]?.message ?? fallback
}

export const emptyAgencyForm = {
  code: '',
  name: '',
  parentId: '' as number | '',
  description: '',
  address: '',
  contactNumber: '',
  email: '',
  isActive: true,
}
