import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'

export interface CertificateVerifyResult {
  certificateNumber: string
  title: string
  status: string
  effectiveStatus: string
  issuedAt: string
  expiresAt?: string
  isValid: boolean
  holderName?: string
  processType?: string
  companyName?: string
  companyType?: string
  accreditationNumber?: string
  qrCodeData?: string
  revokedAt?: string
  revocationReason?: string
}

export const publicApi = createApi({
  reducerPath: 'publicApi',
  baseQuery: fetchBaseQuery({ baseUrl: '/api/v1' }),
  endpoints: (builder) => ({
    verifyCertificate: builder.query<ApiEnvelope<CertificateVerifyResult>, string>({
      query: (code) => `/certificates/verify/${encodeURIComponent(code)}`,
    }),
  }),
})

export const { useVerifyCertificateQuery } = publicApi
