import {
  BaseQueryFn,
  FetchArgs,
  FetchBaseQueryError,
  fetchBaseQuery,
} from '@reduxjs/toolkit/query/react'
import type { RootState } from '../../../app/store'
import { logout, setCredentials } from '../authSlice'
import type { ApiEnvelope, AuthResponse } from '../types'

export const rawBaseQuery = fetchBaseQuery({
  baseUrl: '/api/v1',
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.accessToken
    if (token) headers.set('Authorization', `Bearer ${token}`)
    return headers
  },
})

export const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions,
) => {
  let result = await rawBaseQuery(args, api, extraOptions)

  if (result.error?.status === 401) {
    const refreshToken = (api.getState() as RootState).auth.refreshToken
    if (refreshToken) {
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
        result = await rawBaseQuery(args, api, extraOptions)
      } else {
        api.dispatch(logout())
      }
    }
  }

  return result
}
