import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'
import type { PagedResult } from '../../client/api/clientApi'

export interface AccreditationOfficerDashboard {
  unclaimedCount: number
  myApplicationsCount: number
  approvedCount: number
  rejectedCount: number
  unclaimed: AccreditationOfficerListItem[]
  myApplications: AccreditationOfficerListItem[]
}

export interface AccreditationOfficerListItem {
  uuid: string
  companyName: string
  submissionType: string
  status: string
  displayStatus: string
  applicantName: string
  submittedAt?: string
  assignedOfficerName?: string
  claimedAt?: string
  isAssignedToMe: boolean
}

export interface AccreditationOfficerDetail extends AccreditationOfficerListItem {
  formDataJson?: string
  reviewComments?: string
  canClaim: boolean
  submittedAt?: string
  accreditationNumber?: string
  certificateUuid?: string
  certificateNumber?: string
  formSchemaJson?: string
  formName?: string
  files: Array<{
    uuid: string
    originalFileName: string
    reviewDecision?: string
    reviewComment?: string
    fileSizeBytes: number
    createdAt: string
    versions?: Array<{
      versionNumber: number
      originalFileName: string
      fileSizeBytes: number
      createdAt: string
      isCurrent: boolean
    }>
  }>
  history: Array<{ status: string; comment?: string; createdAt: string; actorName?: string }>
}

export interface CompleteAccreditationReviewResult {
  status: string
  accreditationNumber?: string
  certificateIssued: boolean
  certificateUuid?: string
  certificateNumber?: string
  certificateMessage?: string
}

export const accreditationOfficerApi = createApi({
  reducerPath: 'accreditationOfficerApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['AccreditationOfficerDashboard', 'AccreditationOfficer'],
  endpoints: (builder) => ({
    getAccreditationOfficerDashboard: builder.query<ApiEnvelope<AccreditationOfficerDashboard>, void>({
      query: () => '/accreditation-officer/dashboard',
      providesTags: ['AccreditationOfficerDashboard', 'AccreditationOfficer'],
    }),
    getAccreditationOfficerSubmissions: builder.query<
      ApiEnvelope<PagedResult<AccreditationOfficerListItem>>,
      { page?: number; filter?: string }
    >({
      query: ({ page = 1, filter }) => {
        const params = new URLSearchParams({ page: String(page) })
        if (filter) params.set('filter', filter)
        return `/accreditation-officer/submissions?${params.toString()}`
      },
      providesTags: ['AccreditationOfficer'],
    }),
    getAccreditationOfficerDetail: builder.query<ApiEnvelope<AccreditationOfficerDetail>, string>({
      query: (uuid) => `/accreditation-officer/submissions/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'AccreditationOfficer', id: uuid }],
    }),
    claimAccreditationSubmission: builder.mutation<ApiEnvelope<{ claimed: boolean }>, string>({
      query: (uuid) => ({ url: `/accreditation-officer/submissions/${uuid}/claim`, method: 'POST' }),
      invalidatesTags: ['AccreditationOfficerDashboard', 'AccreditationOfficer'],
    }),
    releaseAccreditationSubmission: builder.mutation<ApiEnvelope<{ released: boolean }>, string>({
      query: (uuid) => ({ url: `/accreditation-officer/submissions/${uuid}/release`, method: 'POST' }),
      invalidatesTags: ['AccreditationOfficerDashboard', 'AccreditationOfficer'],
    }),
    reviewAccreditationOfficerFile: builder.mutation<
      ApiEnvelope<{ saved: boolean }>,
      { fileUuid: string; decision: string; comment?: string }
    >({
      query: ({ fileUuid, decision, comment }) => ({
        url: `/accreditation-officer/submissions/files/${fileUuid}/review`,
        method: 'POST',
        body: { decision, comment },
      }),
      invalidatesTags: ['AccreditationOfficer'],
    }),
    completeAccreditationOfficerReview: builder.mutation<
      ApiEnvelope<CompleteAccreditationReviewResult>,
      { uuid: string; decision: string; comment?: string; accreditationNumber?: string }
    >({
      query: ({ uuid, decision, comment, accreditationNumber }) => ({
        url: `/accreditation-officer/submissions/${uuid}/complete`,
        method: 'POST',
        body: { decision, comment, accreditationNumber },
      }),
      invalidatesTags: ['AccreditationOfficerDashboard', 'AccreditationOfficer'],
    }),
  }),
})

export const {
  useGetAccreditationOfficerDashboardQuery,
  useGetAccreditationOfficerSubmissionsQuery,
  useGetAccreditationOfficerDetailQuery,
  useClaimAccreditationSubmissionMutation,
  useReleaseAccreditationSubmissionMutation,
  useReviewAccreditationOfficerFileMutation,
  useCompleteAccreditationOfficerReviewMutation,
} = accreditationOfficerApi
