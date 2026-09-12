import { apiUrl } from '../../../app/apiBase'
import { store } from '../../../app/store'

export type AuthenticatedFile = {
  blob: Blob
  contentType: string
  objectUrl: string
}

export async function fetchAuthenticatedFile(path: string): Promise<AuthenticatedFile> {
  const token = store.getState().auth.accessToken
  const response = await fetch(apiUrl(path), {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  })
  if (!response.ok) {
    throw new Error('File request failed')
  }

  const contentType = response.headers.get('Content-Type') ?? 'application/octet-stream'
  const blob = await response.blob()
  const objectUrl = URL.createObjectURL(blob)
  return { blob, contentType, objectUrl }
}

export function revokeAuthenticatedFileUrl(objectUrl: string) {
  URL.revokeObjectURL(objectUrl)
}

export async function downloadAuthenticatedFile(path: string, filename: string) {
  const file = await fetchAuthenticatedFile(path)
  const link = document.createElement('a')
  link.href = file.objectUrl
  link.download = filename
  link.click()
  revokeAuthenticatedFileUrl(file.objectUrl)
}

export function isPdfFile(contentType: string, fileName: string) {
  return contentType.includes('pdf') || fileName.toLowerCase().endsWith('.pdf')
}

export function isImageFile(contentType: string, fileName: string) {
  if (contentType.startsWith('image/')) return true
  return /\.(png|jpe?g|gif|webp|bmp)$/i.test(fileName)
}
