import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'

export interface MavImporterDashboard {
  openPeriods: number
  myApplications: number
  pendingApplications: number
  activeLicenses: number
  activeMics: number
  totalAvailableVolume: number
}

export interface MavAdminDashboard {
  openPeriods: number
  pendingApplications: number
  activeLicenses: number
  activeMics: number
  totalApplications: number
  complianceAlerts: number
}

export interface MavPeriodListItem {
  uuid: string
  mavYear: number
  poolType: string
  status: string
  openingDate: string
  closingDate: string
  applicationCount: number
  allocationCount: number
}

export interface MavCommodityAllocation {
  id: number
  hsCode: string
  commodityName: string
  totalVolume: number
  allocatedVolume: number
  availableVolume: number
  minimumImportVolume: number
}

export interface MavPeriodDetail extends MavPeriodListItem {
  allocations: MavCommodityAllocation[]
}

export interface MavApplicationListItem {
  uuid: string
  referenceNumber: string
  status: string
  mavYear: number
  poolType: string
  hsCode: string
  commodityName: string
  requestedVolume: number
  allocatedVolume?: number
  applicantName: string
  submittedAt?: string
}

export interface MavApplicationDetail {
  uuid: string
  referenceNumber: string
  status: string
  periodUuid: string
  mavYear: number
  poolType: string
  hsCode: string
  commodityName: string
  requestedVolume: number
  allocatedVolume?: number
  submittedAt?: string
  reviewedAt?: string
  rejectionReason?: string
  licenseUuid?: string
}

export interface MavLicenseListItem {
  uuid: string
  licenseNumber: string
  status: string
  mavYear: number
  poolType: string
  hsCode: string
  commodityName: string
  awardedVolume: number
  availableVolume: number
  issuedAt: string
  expiresAt: string
  holderName: string
}

export interface MavMicListItem {
  uuid: string
  certificateNumber: string
  status: string
  hsCode: string
  commodityName: string
  authorizedVolume: number
  utilizedVolume: number
  availableVolume: number
  issuedAt: string
  expiresAt: string
}

export interface MavLicenseDetail extends MavLicenseListItem {
  utilizedVolume: number
  importCertificates: MavMicListItem[]
  recentTransactions: Array<{
    id: number
    transactionType: string
    volume: number
    balanceBefore: number
    balanceAfter: number
    reference?: string
    createdAt: string
  }>
}

export interface MavComplianceDashboard {
  expiredLicenses: number
  expiredMics: number
  expiringLicenses: number
  expiringMics: number
  lowUtilizationAccounts: number
  overUtilizedAccounts: number
  pendingApplications: number
}

export interface MavComplianceAlerts {
  summary: MavComplianceDashboard
  expiringLicenses: Array<{ uuid: string; licenseNumber: string; holderName: string; hsCode: string; commodityName: string; availableVolume: number; expiresAt: string; daysRemaining: number }>
  expiringMics: Array<{ uuid: string; certificateNumber: string; holderName: string; hsCode: string; commodityName: string; availableVolume: number; expiresAt: string; daysRemaining: number }>
  lowUtilizationAccounts: Array<{ licenseUuid: string; licenseNumber: string; holderName: string; hsCode: string; commodityName: string; awardedVolume: number; utilizedVolume: number; utilizationPercent: number }>
  overUtilizedAccounts: Array<{ licenseUuid: string; licenseNumber: string; holderName: string; hsCode: string; commodityName: string; awardedVolume: number; utilizedVolume: number }>
  expiredLicenses: Array<{ uuid: string; licenseNumber: string; holderName: string; hsCode: string; commodityName: string; expiresAt: string }>
  expiredMics: Array<{ uuid: string; certificateNumber: string; holderName: string; hsCode: string; commodityName: string; expiresAt: string }>
}

export interface MavReportDetail {
  summary: MavReportSummary
  byCommodity: Array<{ hsCode: string; commodityName: string; applicationCount: number; activeLicenses: number; awardedVolume: number; utilizedVolume: number }>
  byPoolType: Array<{ poolType: string; applicationCount: number; activeLicenses: number; awardedVolume: number; utilizedVolume: number }>
}

export interface MavHsCategoryListItem {
  uuid: string
  hsCode: string
  description: string
  agencyCode?: string
  isActive: boolean
  headingCount: number
}

export interface MavHsDetailSummary {
  uuid: string
  description: string
  displayLabel: string
  isActive: boolean
}

export interface MavHsHeadingWithDetails {
  uuid: string
  headingNumber: string
  description: string
  isActive: boolean
  details: MavHsDetailSummary[]
}

export interface MavHsCategoryDetail extends MavHsCategoryListItem {
  notes?: string
  agencyId?: number
  headings: MavHsHeadingWithDetails[]
}

export interface MavHsDetailListItem {
  uuid: string
  description: string
  displayLabel: string
  hsCode: string
  headingNumber: string
  isActive: boolean
}

export interface MavReportSummary {
  totalApplications: number
  approvedApplications: number
  rejectedApplications: number
  activeLicenses: number
  issuedMics: number
  totalAwardedVolume: number
  totalUtilizedVolume: number
}

export const mavApi = createApi({
  reducerPath: 'mavApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['MavDashboard', 'MavPeriods', 'MavApplications', 'MavLicenses', 'MavCompliance', 'MavReports', 'MavHsLibrary'],
  endpoints: (builder) => ({
    getMavImporterDashboard: builder.query<ApiEnvelope<MavImporterDashboard>, void>({
      query: () => '/mav/dashboard',
      providesTags: ['MavDashboard'],
    }),
    getMavAdminDashboard: builder.query<ApiEnvelope<MavAdminDashboard>, void>({
      query: () => '/mav/admin/dashboard',
      providesTags: ['MavDashboard'],
    }),
    getOpenMavPeriods: builder.query<ApiEnvelope<MavPeriodListItem[]>, void>({
      query: () => '/mav/periods/open',
      providesTags: ['MavPeriods'],
    }),
    getMavAdminPeriods: builder.query<ApiEnvelope<MavPeriodListItem[]>, void>({
      query: () => '/mav/admin/periods',
      providesTags: ['MavPeriods'],
    }),
    getMavPeriod: builder.query<ApiEnvelope<MavPeriodDetail>, string>({
      query: (uuid) => `/mav/periods/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'MavPeriods', id: uuid }],
    }),
    createMavPeriod: builder.mutation<ApiEnvelope<MavPeriodDetail>, { mavYear: number; poolType: string; openingDate: string; closingDate: string }>({
      query: (body) => ({ url: '/mav/admin/periods', method: 'POST', body }),
      invalidatesTags: ['MavPeriods', 'MavDashboard'],
    }),
    openMavPeriod: builder.mutation<ApiEnvelope<MavPeriodDetail>, string>({
      query: (uuid) => ({ url: `/mav/admin/periods/${uuid}/open`, method: 'POST' }),
      invalidatesTags: ['MavPeriods', 'MavDashboard'],
    }),
    closeMavPeriod: builder.mutation<ApiEnvelope<MavPeriodDetail>, string>({
      query: (uuid) => ({ url: `/mav/admin/periods/${uuid}/close`, method: 'POST' }),
      invalidatesTags: ['MavPeriods', 'MavDashboard'],
    }),
    upsertMavAllocation: builder.mutation<ApiEnvelope<MavCommodityAllocation>, { periodUuid: string; commodityId: number; hsCode: string; commodityName: string; totalVolume: number; minimumImportVolume: number }>({
      query: ({ periodUuid, ...body }) => ({ url: `/mav/admin/periods/${periodUuid}/allocations`, method: 'POST', body }),
      invalidatesTags: ['MavPeriods'],
    }),
    getMyMavApplications: builder.query<ApiEnvelope<MavApplicationListItem[]>, void>({
      query: () => '/mav/applications',
      providesTags: ['MavApplications'],
    }),
    getMavAdminApplications: builder.query<ApiEnvelope<MavApplicationListItem[]>, { status?: string }>({
      query: ({ status }) => `/mav/admin/applications${status ? `?status=${status}` : ''}`,
      providesTags: ['MavApplications'],
    }),
    getMavApplication: builder.query<ApiEnvelope<MavApplicationDetail>, string>({
      query: (uuid) => `/mav/applications/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'MavApplications', id: uuid }],
    }),
    createMavApplication: builder.mutation<ApiEnvelope<MavApplicationDetail>, { periodUuid: string; hsCode: string; commodityName: string; requestedVolume: number; hsDetailUuid?: string }>({
      query: (body) => ({ url: '/mav/applications', method: 'POST', body }),
      invalidatesTags: ['MavApplications', 'MavDashboard'],
    }),
    submitMavApplication: builder.mutation<ApiEnvelope<MavApplicationDetail>, string>({
      query: (uuid) => ({ url: `/mav/applications/${uuid}/submit`, method: 'POST' }),
      invalidatesTags: ['MavApplications', 'MavDashboard'],
    }),
    updateMavApplication: builder.mutation<ApiEnvelope<MavApplicationDetail>, { uuid: string; hsCode: string; commodityName: string; requestedVolume: number; hsDetailUuid?: string }>({
      query: ({ uuid, ...body }) => ({ url: `/mav/applications/${uuid}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, { uuid }) => [{ type: 'MavApplications', id: uuid }, 'MavApplications'],
    }),
    approveMavApplication: builder.mutation<ApiEnvelope<MavApplicationDetail>, { uuid: string; allocatedVolume: number }>({
      query: ({ uuid, allocatedVolume }) => ({ url: `/mav/admin/applications/${uuid}/approve`, method: 'POST', body: { allocatedVolume } }),
      invalidatesTags: ['MavApplications', 'MavLicenses', 'MavDashboard', 'MavReports'],
    }),
    rejectMavApplication: builder.mutation<ApiEnvelope<MavApplicationDetail>, { uuid: string; reason: string }>({
      query: ({ uuid, reason }) => ({ url: `/mav/admin/applications/${uuid}/reject`, method: 'POST', body: { reason } }),
      invalidatesTags: ['MavApplications', 'MavDashboard'],
    }),
    getMyMavLicenses: builder.query<ApiEnvelope<MavLicenseListItem[]>, void>({
      query: () => '/mav/licenses',
      providesTags: ['MavLicenses'],
    }),
    getMavAdminLicenses: builder.query<ApiEnvelope<MavLicenseListItem[]>, void>({
      query: () => '/mav/admin/licenses',
      providesTags: ['MavLicenses'],
    }),
    getMavLicense: builder.query<ApiEnvelope<MavLicenseDetail>, string>({
      query: (uuid) => `/mav/licenses/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'MavLicenses', id: uuid }],
    }),
    issueMic: builder.mutation<ApiEnvelope<MavMicListItem>, { licenseUuid: string; volume: number }>({
      query: ({ licenseUuid, volume }) => ({ url: `/mav/licenses/${licenseUuid}/mic`, method: 'POST', body: { volume } }),
      invalidatesTags: ['MavLicenses', 'MavDashboard'],
    }),
    getAvailableMics: builder.query<ApiEnvelope<MavMicListItem[]>, void>({
      query: () => '/mav/mic/available',
      providesTags: ['MavLicenses'],
    }),
    revokeMavLicense: builder.mutation<ApiEnvelope<MavLicenseListItem>, { uuid: string; reason: string }>({
      query: ({ uuid, reason }) => ({ url: `/mav/admin/licenses/${uuid}/revoke`, method: 'POST', body: { reason } }),
      invalidatesTags: ['MavLicenses', 'MavDashboard'],
    }),
    getMavComplianceDashboard: builder.query<ApiEnvelope<MavComplianceDashboard>, void>({
      query: () => '/mav/compliance/dashboard',
      providesTags: ['MavCompliance'],
    }),
    getMavComplianceAlerts: builder.query<ApiEnvelope<MavComplianceAlerts>, void>({
      query: () => '/mav/compliance/alerts',
      providesTags: ['MavCompliance'],
    }),
    getMavReportSummary: builder.query<ApiEnvelope<MavReportSummary>, { mavYear?: number }>({
      query: ({ mavYear }) => `/mav/reports/summary${mavYear ? `?mavYear=${mavYear}` : ''}`,
      providesTags: ['MavReports'],
    }),
    getMavReportDetail: builder.query<ApiEnvelope<MavReportDetail>, { mavYear?: number }>({
      query: ({ mavYear }) => `/mav/reports/detail${mavYear ? `?mavYear=${mavYear}` : ''}`,
      providesTags: ['MavReports'],
    }),
    getMavAdminMicList: builder.query<ApiEnvelope<MavMicListItem[]>, void>({
      query: () => '/mav/admin/mic',
      providesTags: ['MavLicenses'],
    }),
    getMavHsCategories: builder.query<ApiEnvelope<MavHsCategoryListItem[]>, { agencyId?: number }>({
      query: ({ agencyId }) => `/mav/hs-categories${agencyId ? `?agencyId=${agencyId}` : ''}`,
      providesTags: ['MavHsLibrary'],
    }),
    getMavHsCategory: builder.query<ApiEnvelope<MavHsCategoryDetail>, string>({
      query: (uuid) => `/mav/hs-categories/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'MavHsLibrary', id: uuid }],
    }),
    createMavHsCategory: builder.mutation<ApiEnvelope<MavHsCategoryDetail>, { hsCode: string; description: string; notes?: string; agencyId?: number }>({
      query: (body) => ({ url: '/mav/hs-categories', method: 'POST', body }),
      invalidatesTags: ['MavHsLibrary'],
    }),
    updateMavHsCategory: builder.mutation<ApiEnvelope<MavHsCategoryDetail>, { uuid: string; hsCode: string; description: string; notes?: string; agencyId?: number; isActive: boolean }>({
      query: ({ uuid, ...body }) => ({ url: `/mav/hs-categories/${uuid}`, method: 'PUT', body }),
      invalidatesTags: ['MavHsLibrary'],
    }),
    createMavHsHeading: builder.mutation<ApiEnvelope<{ uuid: string; headingNumber: string; description: string; isActive: boolean; detailCount: number }>, { categoryUuid: string; headingNumber: string; description: string; notes?: string }>({
      query: ({ categoryUuid, ...body }) => ({ url: `/mav/hs-categories/${categoryUuid}/headings`, method: 'POST', body }),
      invalidatesTags: ['MavHsLibrary'],
    }),
    createMavHsDetail: builder.mutation<ApiEnvelope<MavHsDetailListItem>, { headingUuid: string; description: string; notes?: string }>({
      query: ({ headingUuid, ...body }) => ({ url: `/mav/hs-headings/${headingUuid}/details`, method: 'POST', body }),
      invalidatesTags: ['MavHsLibrary'],
    }),
    getMavHsPickerDetails: builder.query<ApiEnvelope<MavHsDetailListItem[]>, { agencyId?: number; categoryUuid?: string }>({
      query: ({ agencyId, categoryUuid }) => {
        const params = new URLSearchParams()
        if (agencyId) params.set('agencyId', String(agencyId))
        if (categoryUuid) params.set('categoryUuid', categoryUuid)
        const qs = params.toString()
        return `/mav/hs-details/picker${qs ? `?${qs}` : ''}`
      },
    }),
    runMavYearTransition: builder.mutation<ApiEnvelope<{ periodsCreated: number; allocationsCopied: number }>, { fromYear: number; toYear: number; poolType: string }>({
      query: (body) => ({ url: '/mav/admin/year-transition', method: 'POST', body }),
      invalidatesTags: ['MavPeriods', 'MavDashboard'],
    }),
  }),
})

export const {
  useGetMavImporterDashboardQuery,
  useGetMavAdminDashboardQuery,
  useGetOpenMavPeriodsQuery,
  useGetMavAdminPeriodsQuery,
  useGetMavPeriodQuery,
  useCreateMavPeriodMutation,
  useOpenMavPeriodMutation,
  useCloseMavPeriodMutation,
  useUpsertMavAllocationMutation,
  useGetMyMavApplicationsQuery,
  useGetMavAdminApplicationsQuery,
  useGetMavApplicationQuery,
  useCreateMavApplicationMutation,
  useSubmitMavApplicationMutation,
  useUpdateMavApplicationMutation,
  useApproveMavApplicationMutation,
  useRejectMavApplicationMutation,
  useGetMyMavLicensesQuery,
  useGetMavAdminLicensesQuery,
  useGetMavLicenseQuery,
  useIssueMicMutation,
  useGetAvailableMicsQuery,
  useRevokeMavLicenseMutation,
  useGetMavComplianceDashboardQuery,
  useGetMavComplianceAlertsQuery,
  useGetMavReportSummaryQuery,
  useGetMavReportDetailQuery,
  useGetMavAdminMicListQuery,
  useGetMavHsCategoriesQuery,
  useGetMavHsCategoryQuery,
  useCreateMavHsCategoryMutation,
  useUpdateMavHsCategoryMutation,
  useCreateMavHsHeadingMutation,
  useCreateMavHsDetailMutation,
  useGetMavHsPickerDetailsQuery,
  useRunMavYearTransitionMutation,
} = mavApi
