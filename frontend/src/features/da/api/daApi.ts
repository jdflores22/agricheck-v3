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
  hsCode?: string | null
  commodityName?: string | null
  volumeKg?: number | null
}

export interface DaMavAgencyRow {
  agencyCode: string
  activeLicenses: number
  awardedVolume: number
  utilizedVolume: number
  remainingVolume: number
  regularVolume?: number
  combinedVolume?: number
}

export interface DaMavCommodityRow {
  hsCode: string
  commodityName: string
  agencyCode?: string | null
  applicationCount: number
  activeLicenses: number
  awardedVolume: number
  utilizedVolume: number
  remainingVolume: number
  regularVolume?: number
  combinedVolume?: number
}

export interface DaMavNationalReport {
  mavYear: number
  openPeriods: number
  totalApplications: number
  approvedApplications: number
  activeLicenses: number
  issuedMics: number
  totalAwardedVolume: number
  totalUtilizedVolume: number
  totalRemainingVolume: number
  regularImportCount?: number
  totalRegularVolume?: number
  totalCombinedVolume?: number
  byAgency: DaMavAgencyRow[]
  byCommodity: DaMavCommodityRow[]
}

export interface DaGeoStockQuery {
  regionId?: number
  provinceId?: number
  cityId?: number
  barangayId?: number
  hsCode?: string
  commodityName?: string
}

export interface DaGeoStockBreadcrumb {
  level: string
  id?: number | null
  name: string
}

export interface DaGeoStockCommodity {
  hsCode: string
  commodityName: string
  volumeKg: number
  storedContainers: number
  warehouseCount: number
}

export interface DaGeoStockLocation {
  level: string
  id?: number | null
  name: string
  volumeKg: number
  warehouseCount: number
  storedContainers: number
  capacity: number
  utilizationPercent: number
  topCommodities: DaGeoStockCommodity[]
}

export interface DaGeoStockWarehouse {
  id: number
  code: string
  name: string
  volumeKg: number
  storedContainers: number
  capacity: number
  utilizationPercent: number
  barangayName?: string | null
  topCommodityName?: string | null
  topCommodityHsCode?: string | null
}

export interface DaCommodityStockQuery {
  hsCode?: string
  commodityName?: string
  agencyCode?: string
}

export interface DaCommodityStockRow {
  hsCode: string
  commodityName: string
  stockKg: number
  mavStockKg: number
  regularStockKg: number
  storedContainers: number
  warehouseCount: number
  agencyCount: number
  linkedImportEntries: number
}

export interface DaCommodityStockAgencyRow {
  agencyCode: string
  hsCode: string
  commodityName: string
  stockKg: number
  mavStockKg: number
  regularStockKg: number
  storedContainers: number
  warehouseCount: number
}

export interface DaImportPipelineStageRow {
  stage: string
  label: string
  expectedKg: number
  containerCount: number
  entryCount: number
}

export interface DaImportPipelineCommodityRow {
  hsCode: string
  commodityName: string
  expectedKg: number
  actualKg: number
  processingKg: number
  inTransitKg: number
  awaitingStorageKg: number
  expectedContainers: number
  actualContainers: number
}

export interface DaImportPipelineEntryRow {
  entryUuid: string
  referenceNo: string
  agencyCode: string
  entryStatus: string
  pipelineStage: string
  pipelineLabel: string
  commodityName: string
  hsCode: string
  expectedKg: number
  totalContainers: number
  storedContainers: number
  pendingContainers: number
  submittedAt?: string | null
}

export interface DaImportPipelineReport {
  totalExpectedKg: number
  totalActualKg: number
  expectedContainers: number
  actualContainers: number
  pipelineEntryCount: number
  byStage: DaImportPipelineStageRow[]
  byCommodity: DaImportPipelineCommodityRow[]
  entries: DaImportPipelineEntryRow[]
}

export interface DaImportPipelineQuery {
  hsCode?: string
  commodityName?: string
  agencyCode?: string
}

export interface DaCommodityStockReport {
  totalStockKg: number
  totalMavStockKg: number
  totalRegularStockKg: number
  commodityCount: number
  storedContainers: number
  warehouseCount: number
  commodities: DaCommodityStockRow[]
  byAgency: DaCommodityStockAgencyRow[]
}

export interface DaGeoStockReport {
  level: string
  scopeLabel: string
  totalVolumeKg: number
  warehouseCount: number
  storedContainers: number
  capacity: number
  utilizationPercent: number
  path: DaGeoStockBreadcrumb[]
  commodities: DaGeoStockCommodity[]
  locations: DaGeoStockLocation[]
  warehouses: DaGeoStockWarehouse[]
}

export interface DaWarehouseDetail {
  warehouse: DaWarehouse
  storedContainers: number
  releasedContainers: number
  pendingBookings: number
  capacity: number
  utilizationPercent: number
  totalVolumeKg: number
  commodities: DaGeoStockCommodity[]
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
    getDaMavNationalReport: builder.query<ApiEnvelope<DaMavNationalReport>, { mavYear?: number }>({
      query: ({ mavYear }) => `/da/reports/mav${mavYear ? `?mavYear=${mavYear}` : ''}`,
      providesTags: ['DaReports'],
    }),
    getDaCommodityStockReport: builder.query<ApiEnvelope<DaCommodityStockReport>, DaCommodityStockQuery>({
      query: (params) => {
        const search = new URLSearchParams()
        if (params.hsCode) search.set('hsCode', params.hsCode)
        if (params.commodityName) search.set('commodityName', params.commodityName)
        if (params.agencyCode) search.set('agencyCode', params.agencyCode)
        const qs = search.toString()
        return `/da/reports/commodity-stock${qs ? `?${qs}` : ''}`
      },
      providesTags: ['DaReports'],
    }),
    getDaImportPipelineReport: builder.query<ApiEnvelope<DaImportPipelineReport>, DaImportPipelineQuery | void>({
      query: (params) => {
        const search = new URLSearchParams()
        if (params?.hsCode) search.set('hsCode', params.hsCode)
        if (params?.commodityName) search.set('commodityName', params.commodityName)
        if (params?.agencyCode) search.set('agencyCode', params.agencyCode)
        const qs = search.toString()
        return `/da/reports/import-pipeline${qs ? `?${qs}` : ''}`
      },
      providesTags: ['DaReports'],
    }),
    getDaGeoStockReport: builder.query<ApiEnvelope<DaGeoStockReport>, DaGeoStockQuery>({
      query: (params) => {
        const search = new URLSearchParams()
        if (params.regionId) search.set('regionId', String(params.regionId))
        if (params.provinceId) search.set('provinceId', String(params.provinceId))
        if (params.cityId) search.set('cityId', String(params.cityId))
        if (params.barangayId) search.set('barangayId', String(params.barangayId))
        if (params.hsCode) search.set('hsCode', params.hsCode)
        if (params.commodityName) search.set('commodityName', params.commodityName)
        const qs = search.toString()
        return `/da/reports/geo-stock${qs ? `?${qs}` : ''}`
      },
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
  useGetDaMavNationalReportQuery,
  useGetDaCommodityStockReportQuery,
  useGetDaImportPipelineReportQuery,
  useGetDaGeoStockReportQuery,
  useGetDaWarehousesQuery,
  useGetDaWarehouseDetailQuery,
  useCreateDaWarehouseMutation,
  useUpdateDaWarehouseMutation,
} = daApi
