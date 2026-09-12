import { createApi } from '@reduxjs/toolkit/query/react'
import { baseQueryWithReauth } from './baseQuery'
import type { ApiEnvelope, AuthResponse, LoginPayload, RegisterPayload, UserSummary } from '../types'

export const authApi = createApi({
  reducerPath: 'authApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Me'],
  endpoints: (builder) => ({
    login: builder.mutation<ApiEnvelope<AuthResponse>, LoginPayload>({
      query: (body) => ({ url: '/auth/login', method: 'POST', body }),
    }),
    register: builder.mutation<ApiEnvelope<AuthResponse>, RegisterPayload>({
      query: (body) => ({ url: '/auth/register', method: 'POST', body }),
    }),
    logout: builder.mutation<ApiEnvelope<{ message: string }>, { refreshToken: string }>({
      query: (body) => ({ url: '/auth/logout', method: 'POST', body }),
    }),
    me: builder.query<ApiEnvelope<{ user: UserSummary; redirectPath: string }>, void>({
      query: () => '/auth/me',
      providesTags: ['Me'],
    }),
    forgotPassword: builder.mutation<ApiEnvelope<{ message: string }>, { email: string }>({
      query: (body) => ({ url: '/auth/forgot-password', method: 'POST', body }),
    }),
    resendVerification: builder.mutation<ApiEnvelope<{ message: string }>, { email: string }>({
      query: (body) => ({ url: '/auth/resend-verification', method: 'POST', body }),
    }),
    resetPassword: builder.mutation<ApiEnvelope<{ message: string }>, { token: string; newPassword: string }>({
      query: (body) => ({ url: '/auth/reset-password', method: 'POST', body }),
    }),
    verifyEmail: builder.mutation<ApiEnvelope<{ message: string }>, { token: string }>({
      query: (body) => ({ url: '/auth/verify-email', method: 'POST', body }),
    }),
    changePassword: builder.mutation<
      ApiEnvelope<{ message: string }>,
      { currentPassword: string; newPassword: string }
    >({
      query: (body) => ({ url: '/auth/change-password', method: 'POST', body }),
      invalidatesTags: ['Me'],
    }),
  }),
})

export const {
  useLoginMutation,
  useRegisterMutation,
  useLogoutMutation,
  useMeQuery,
  useForgotPasswordMutation,
  useResendVerificationMutation,
  useResetPasswordMutation,
  useVerifyEmailMutation,
  useChangePasswordMutation,
} = authApi
