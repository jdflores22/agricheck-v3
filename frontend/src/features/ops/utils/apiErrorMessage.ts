import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import type { ApiEnvelope } from '../../auth/types'

export function getApiErrorMessage(error: unknown): string | null {
  if (!error || typeof error !== 'object' || !('data' in error)) return null
  const data = (error as FetchBaseQueryError).data as ApiEnvelope<unknown> | undefined
  return data?.errors?.[0]?.message ?? null
}
