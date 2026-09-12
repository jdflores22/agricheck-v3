import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import { getApiV1Base } from '../../../app/apiBase'

export interface HealthResponse {
  success: boolean
  data: {
    status: string
    service: string
    version: string
    database: string
    timestamp: string
  }
  errors: Array<{ code: string; message: string }> | null
}

export const healthApi = createApi({
  reducerPath: 'healthApi',
  baseQuery: fetchBaseQuery({ baseUrl: getApiV1Base() }),
  endpoints: (builder) => ({
    getHealth: builder.query<HealthResponse, void>({
      query: () => '/health',
    }),
  }),
})

export const { useGetHealthQuery } = healthApi
