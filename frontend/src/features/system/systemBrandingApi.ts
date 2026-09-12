import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import { getApiV1Base } from '../../app/apiBase'
import type { ApiEnvelope } from '../auth/types'

export interface SystemBranding {
  systemName: string
  systemLogoUrl?: string | null
  spinnerLogoUrl?: string | null
  faviconUrl?: string | null
  spinnerColor: string
  primaryColor: string
  footerText: string
}

export const systemBrandingApi = createApi({
  reducerPath: 'systemBrandingApi',
  baseQuery: fetchBaseQuery({ baseUrl: getApiV1Base() }),
  tagTypes: ['SystemBranding'],
  endpoints: (builder) => ({
    getSystemBranding: builder.query<ApiEnvelope<SystemBranding>, void>({
      query: () => '/system/branding',
      providesTags: ['SystemBranding'],
    }),
  }),
})

export const { useGetSystemBrandingQuery } = systemBrandingApi

export function resolveBrandingAssetUrl(path?: string | null): string | null {
  if (!path) return null
  if (path.startsWith('http://') || path.startsWith('https://')) return path
  return path.startsWith('/') ? path : `/${path}`
}
