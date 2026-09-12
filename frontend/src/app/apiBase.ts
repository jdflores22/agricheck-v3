declare global {
  interface Window {
    __AGRICHECK_API_BASE__?: string
  }
}

function normalize(value: string | undefined): string {
  if (!value) return ''
  return value.trim().replace(/\/$/, '')
}

export function getApiOrigin(): string {
  const runtime = typeof window !== 'undefined' ? window.__AGRICHECK_API_BASE__ : undefined
  return normalize(runtime || import.meta.env.VITE_API_BASE_URL)
}

export function getApiV1Base(): string {
  return `${getApiOrigin()}/api/v1`
}

export function apiUrl(path: string): string {
  const suffix = path.startsWith('/') ? path : `/${path}`
  return `${getApiV1Base()}${suffix}`
}

export function resolveUploadUrl(path?: string | null): string | null {
  if (!path) return null
  const trimmed = path.trim()
  if (!trimmed) return null
  if (/^(https?:|data:|blob:)/i.test(trimmed)) return trimmed

  const origin = getApiOrigin()
  let suffix = trimmed.startsWith('/') ? trimmed : `/${trimmed}`

  if (suffix.startsWith('/certificate-templates/')) {
    suffix = `/uploads/certificates/${suffix.slice('/certificate-templates/'.length)}`
  } else if (/^\/[a-f0-9]{32}\.[a-z0-9]+$/i.test(suffix)) {
    suffix = `/uploads/agency-logos${suffix}`
  } else if (!suffix.startsWith('/uploads/')) {
    return suffix
  }

  return origin ? `${origin}${suffix}` : suffix
}
