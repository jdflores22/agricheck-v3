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
