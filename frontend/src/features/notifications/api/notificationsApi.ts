import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'

export interface NotificationItem {
  uuid: string
  type: string
  title: string
  message: string
  relatedEntityType?: string
  relatedEntityUuid?: string
  isRead: boolean
  createdAt: string
}

export const notificationsApi = createApi({
  reducerPath: 'notificationsApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Notifications'],
  endpoints: (builder) => ({
    getNotifications: builder.query<ApiEnvelope<NotificationItem[]>, { limit?: number; unreadOnly?: boolean } | void>({
      query: (args) => ({
        url: '/notifications',
        params: args ? { limit: args.limit ?? 20, unreadOnly: args.unreadOnly ?? false } : undefined,
      }),
      providesTags: ['Notifications'],
    }),
    getUnreadCount: builder.query<ApiEnvelope<{ count: number }>, void>({
      query: () => '/notifications/unread-count',
      providesTags: ['Notifications'],
    }),
    markNotificationRead: builder.mutation<ApiEnvelope<unknown>, string>({
      query: (uuid) => ({ url: `/notifications/${uuid}/read`, method: 'POST' }),
      invalidatesTags: ['Notifications'],
    }),
    markAllNotificationsRead: builder.mutation<ApiEnvelope<unknown>, void>({
      query: () => ({ url: '/notifications/read-all', method: 'POST' }),
      invalidatesTags: ['Notifications'],
    }),
    getNotificationPreferences: builder.query<ApiEnvelope<{ emailEnabled: boolean; inAppEnabled: boolean }>, void>({
      query: () => '/notifications/preferences',
    }),
    updateNotificationPreferences: builder.mutation<ApiEnvelope<{ emailEnabled: boolean; inAppEnabled: boolean }>, { emailEnabled: boolean; inAppEnabled: boolean }>({
      query: (body) => ({ url: '/notifications/preferences', method: 'PUT', body }),
    }),
  }),
})

export const {
  useGetNotificationsQuery,
  useGetUnreadCountQuery,
  useMarkNotificationReadMutation,
  useMarkAllNotificationsReadMutation,
  useGetNotificationPreferencesQuery,
  useUpdateNotificationPreferencesMutation,
} = notificationsApi
