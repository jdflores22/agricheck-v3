import {
  BaseQueryFn,
  FetchArgs,
  FetchBaseQueryError,
  fetchBaseQuery,
} from '@reduxjs/toolkit/query/react'
import { getApiV1Base } from '../../../app/apiBase'
import type { RootState } from '../../../app/store'
import { logout, setCredentials } from '../authSlice'
import { shouldRefreshAccessToken } from '../authTokenUtils'
import type { ApiEnvelope, AuthResponse } from '../types'

export const rawBaseQuery = fetchBaseQuery({
  baseUrl: getApiV1Base(),
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.accessToken
    if (token) headers.set('Authorization', `Bearer ${token}`)
    return headers
  },
})

function getRequestUrl(args: string | FetchArgs): string {
  return typeof args === 'string' ? args : args.url
}

async function tryRefreshSession(
  api: Parameters<BaseQueryFn>[1],
  extraOptions: Parameters<BaseQueryFn>[2],
): Promise<boolean> {
  const state = api.getState() as RootState
  const refreshToken = state.auth.refreshToken
  if (!refreshToken) return false

  const refreshResult = await rawBaseQuery(
    { url: '/auth/refresh', method: 'POST', body: { refreshToken } },
    api,
    extraOptions,
  )
  const envelope = refreshResult.data as ApiEnvelope<AuthResponse> | undefined
  if (envelope?.success && envelope.data) {
    api.dispatch(setCredentials({
      tokens: envelope.data.tokens,
      user: envelope.data.user,
      redirectPath: envelope.data.redirectPath,
    }))
    return true
  }

  api.dispatch(logout())
  return false
}

export const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions,
) => {
  const url = getRequestUrl(args)
  const isAuthBootstrapRequest = url === '/auth/refresh' || url === '/auth/login' || url === '/auth/register'

  if (!isAuthBootstrapRequest && shouldRefreshAccessToken((api.getState() as RootState).auth)) {
    await tryRefreshSession(api, extraOptions)
  }

  let result = await rawBaseQuery(args, api, extraOptions)

  if (result.error?.status === 401 && !isAuthBootstrapRequest) {
    const refreshed = await tryRefreshSession(api, extraOptions)
    if (refreshed) {
      result = await rawBaseQuery(args, api, extraOptions)
    }
  }

  return result
}
