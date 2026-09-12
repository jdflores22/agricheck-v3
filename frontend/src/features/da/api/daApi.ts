import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'

export interface DaDashboard {
  activeAgencies: number
  totalAgencies: number
  totalEntries: number
  pendingEntries: number
  approvedEntries: number
  pendingAccreditation: number
  openBillings: number
  completedInspections: number
}

export interface DaAgencyLeadershipUser {
  userUuid: string
  fullName: string
  email: string
}

export interface DaAgencySummary {
  id: number
  code: string
  name: string
  isActive: boolean
  parentId?: number | null
  logoUrl?: string | null
  totalEntries: number
  pendingEntries: number
  pendingAccreditation: number
  openBillings: number
  secretary?: DaAgencyLeadershipUser | null
  undersecretaries: DaAgencyLeadershipUser[]
}

export interface DaOversightReport {
  totalEntries: number
  submittedEntries: number
  underReviewEntries: number
  approvedEntries: number
  rejectedEntries: number
  completedInspections: number
  pendingAccreditation: number
  issuedBillings: number
  paidBillings: number
}

export interface DaAgencyRecentEntry {
  uuid: string
  referenceNo: string
  status: string
  createdAt: string
  submitterName?: string | null
}

export interface DaAgencyOversight {
  agency: DaAgencySummary
  report: DaOversightReport
  isParentAgency: boolean
  parentAgency?: DaAgencySummary | null
  childAgencies: DaAgencySummary[]
  activeUsers: number
  openBillings: number
  recentEntries: DaAgencyRecentEntry[]
}

export interface DaWarehouse {
  id: number
  code: string
  name: string
  isActive: boolean
  capacity: number
  regionId?: number | null
  regionName?: string | null
  provinceId?: number | null
  provinceName?: string | null
  cityId?: number | null
  cityName?: string | null
  barangayId?: number | null
  barangayName?: string | null
  streetAddress?: string | null
  zipCode?: string | null
  formattedAddress: string
  latitude?: number | null
  longitude?: number | null
}

export interface SaveDaWarehouseRequest {
  code: string
  name: string
  capacity: number
  regionId: number
  provinceId: number
  cityId: number
  barangayId: number
  streetAddress?: string
  zipCode?: string
  latitude?: number
  longitude?: number
  isActive: boolean
}

export interface DaWarehouseInventoryItem {
  uuid: string
  containerNumber: string
  containerType?: string | null
  entryReference: string
  locationCode?: string | null
  status: string
  receivedAt: string
  receivedByName?: string | null
}

export interface DaWarehouseDetail {
  warehouse: DaWarehouse
  storedContainers: number
  releasedContainers: number
  pendingBookings: number
  capacity: number
  utilizationPercent: number
  inventory: DaWarehouseInventoryItem[]
}

export const daApi = createApi({
  reducerPath: 'daApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['DaDashboard', 'DaAgencies', 'DaReports', 'DaWarehouses'],
  endpoints: (builder) => ({
    getDaDashboard: builder.query<ApiEnvelope<DaDashboard>, void>({
      query: () => '/da/dashboard',
      providesTags: ['DaDashboard'],
    }),
    getDaAgencies: builder.query<ApiEnvelope<DaAgencySummary[]>, void>({
      query: () => '/da/agencies',
      providesTags: ['DaAgencies'],
    }),
    getDaAgencyOversight: builder.query<ApiEnvelope<DaAgencyOversight>, number>({
      query: (id) => `/da/agencies/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'DaAgencies', id }],
    }),
    getDaReport: builder.query<ApiEnvelope<DaOversightReport>, void>({
      query: () => '/da/reports/summary',
      providesTags: ['DaReports'],
    }),
    getDaWarehouses: builder.query<ApiEnvelope<DaWarehouse[]>, void>({
      query: () => '/da/warehouses',
      providesTags: ['DaWarehouses'],
    }),
    getDaWarehouseDetail: builder.query<ApiEnvelope<DaWarehouseDetail>, number>({
      query: (id) => `/da/warehouses/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'DaWarehouses', id }],
    }),
    createDaWarehouse: builder.mutation<ApiEnvelope<DaWarehouse>, SaveDaWarehouseRequest>({
      query: (body) => ({ url: '/da/warehouses', method: 'POST', body }),
      invalidatesTags: ['DaWarehouses'],
    }),
    updateDaWarehouse: builder.mutation<ApiEnvelope<DaWarehouse>, { id: number; body: SaveDaWarehouseRequest }>({
      query: ({ id, body }) => ({ url: `/da/warehouses/${id}`, method: 'PUT', body }),
      invalidatesTags: ['DaWarehouses'],
    }),
  }),
})

export const {
  useGetDaDashboardQuery,
  useGetDaAgenciesQuery,
  useGetDaAgencyOversightQuery,
  useGetDaReportQuery,
  useGetDaWarehousesQuery,
  useGetDaWarehouseDetailQuery,
  useCreateDaWarehouseMutation,
  useUpdateDaWarehouseMutation,
} = daApi
