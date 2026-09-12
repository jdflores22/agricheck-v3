import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'
import type { ClientContainerInspectionPhoto, PagedResult } from '../../client/api/clientApi'
import type { OpsContainerListItem } from '../../ops/api/opsApi'

export interface AgencyDashboard {
  queueCount: number
  myAssignments: number
  pendingInspections: number
  openBillings: number
  pendingAccreditation: number
  agencyCode: string
  agencyName: string
}

export interface AgencyEntryListItem {
  uuid: string
  referenceNo: string
  entryType: string
  status: string
  applicantName: string
  companyName?: string
  commodityName?: string
  paymentStatus: string
  submittedAt?: string
  isAssignedToMe: boolean
}

export interface AgencyEntryEvaluation {
  uuid: string
  referenceNo: string
  entryType: string
  status: string
  agencyCode: string
  applicantName: string
  companyName?: string
  paymentStatus: string
  submittedAt?: string
  notes?: string
  formDataJson?: string
  formSchemaJson?: string
  formName?: string
  isAssignedToMe: boolean
  detail?: {
    commodityName?: string
    description?: string
    quantity: number
    unit: string
    originCountry?: string
    destinationCountry?: string
    portOfEntry?: string
  }
  files: Array<{
    uuid: string
    originalFileName: string
    documentType?: string
    fileSizeBytes?: number
    createdAt?: string
    evaluationDecision?: string
    evaluationComment?: string
    versions?: Array<{
      versionNumber: number
      originalFileName: string
      fileSizeBytes: number
      createdAt: string
      isCurrent: boolean
    }>
  }>
  compliance: Array<{ itemId: number; label: string; isRequired: boolean; status: string; notes?: string }>
  notesList: Array<{ note: string; isInternal: boolean; authorName: string; createdAt: string }>
  statusHistory: Array<{ fromStatus: string; toStatus: string; comment?: string; createdAt: string }>
  timeline: Array<{ eventType: string; title: string; description?: string; createdAt: string }>
  mav?: {
    mavNo?: string
    mavDocumentStatus: string
    mavRemarks?: string
    mavCertificateFileUuid?: string
    mavCertificateFileName?: string
    micUtilizations: Array<{
      micUuid: string
      certificateNumber: string
      volume: number
      utilizedAt: string
      hsCode: string
      commodityName: string
    }>
    totalUtilizedVolume: number
    requiredVolume?: number
  }
}

export interface InspectionListItem {
  uuid: string
  entryUuid: string
  entryReferenceNo: string
  status: string
  applicantName: string
  scheduledAt?: string
  completedAt?: string
}

export interface InspectionDetail extends InspectionListItem {
  findings?: string
  photos: Array<{ uuid: string; originalFileName: string; caption?: string; createdAt: string }>
}

export interface AgencyBillingItem {
  uuid: string
  billNumber: string
  description: string
  amount: number
  status: string
  entryReferenceNo?: string
  issuedAt?: string
  paidAt?: string
}

export interface AgencyAccreditationItem {
  uuid: string
  companyName: string
  submissionType: string
  status: string
  applicantName: string
  submittedAt?: string
  assignedOfficerName?: string
  claimedAt?: string
  isAssignedToMe: boolean
}

export interface AgencyAccreditationDetail extends AgencyAccreditationItem {
  formDataJson?: string
  reviewComments?: string
  canClaim: boolean
  files: Array<{ uuid: string; originalFileName: string; reviewDecision?: string; reviewComment?: string }>
  history: Array<{ status: string; comment?: string; createdAt: string }>
}

export interface SecretaryReport {
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

export const agencyApi = createApi({
  reducerPath: 'agencyApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['AgencyDashboard', 'AgencyQueue', 'AgencyAssignments', 'AgencyEntry', 'Inspections', 'Billings', 'Accreditation', 'Reports', 'ApprovedEntries'],
  endpoints: (builder) => ({
    getAgencyDashboard: builder.query<ApiEnvelope<AgencyDashboard>, void>({
      query: () => '/agency/dashboard',
      providesTags: ['AgencyDashboard'],
    }),
    getEvaluatorQueue: builder.query<ApiEnvelope<PagedResult<AgencyEntryListItem>>, { page?: number }>({
      query: ({ page = 1 }) => `/agency/evaluator/queue?page=${page}`,
      providesTags: ['AgencyQueue'],
    }),
    getMyAssignments: builder.query<ApiEnvelope<PagedResult<AgencyEntryListItem>>, { page?: number }>({
      query: ({ page = 1 }) => `/agency/evaluator/assignments?page=${page}`,
      providesTags: ['AgencyAssignments'],
    }),
    assignEntry: builder.mutation<ApiEnvelope<{ assigned: boolean }>, string>({
      query: (uuid) => ({ url: `/agency/evaluator/entries/${uuid}/assign`, method: 'POST' }),
      invalidatesTags: ['AgencyQueue', 'AgencyAssignments', 'AgencyDashboard', 'AgencyEntry'],
    }),
    getAgencyEntry: builder.query<ApiEnvelope<AgencyEntryEvaluation>, string>({
      query: (uuid) => `/agency/evaluator/entries/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'AgencyEntry', id: uuid }],
    }),
    evaluateFile: builder.mutation<ApiEnvelope<{ saved: boolean }>, { fileUuid: string; decision: string; comment?: string }>({
      query: ({ fileUuid, decision, comment }) => ({
        url: `/agency/evaluator/files/${fileUuid}/evaluate`,
        method: 'POST',
        body: { decision, comment },
      }),
      invalidatesTags: ['AgencyEntry'],
    }),
    evaluateEntryMav: builder.mutation<ApiEnvelope<{ saved: boolean }>, { entryUuid: string; decision: string; remarks?: string }>({
      query: ({ entryUuid, decision, remarks }) => ({
        url: `/agency/evaluator/entries/${entryUuid}/mav/evaluate`,
        method: 'POST',
        body: { decision, remarks },
      }),
      invalidatesTags: ['AgencyEntry'],
    }),
    updateCompliance: builder.mutation<ApiEnvelope<{ saved: boolean }>, { entryUuid: string; items: Array<{ itemId: number; status: string; notes?: string }> }>({
      query: ({ entryUuid, items }) => ({
        url: `/agency/evaluator/entries/${entryUuid}/compliance`,
        method: 'PUT',
        body: { items },
      }),
      invalidatesTags: ['AgencyEntry'],
    }),
    completeEvaluation: builder.mutation<ApiEnvelope<{ completed: boolean }>, { entryUuid: string; decision: string; comment?: string }>({
      query: ({ entryUuid, decision, comment }) => ({
        url: `/agency/evaluator/entries/${entryUuid}/complete`,
        method: 'POST',
        body: { decision, comment },
      }),
      invalidatesTags: ['AgencyQueue', 'AgencyAssignments', 'AgencyDashboard', 'AgencyEntry'],
    }),
    getInspections: builder.query<ApiEnvelope<PagedResult<InspectionListItem>>, { page?: number; status?: string }>({
      query: ({ page = 1, status }) => {
        const params = new URLSearchParams({ page: String(page) })
        if (status) params.set('status', status)
        return `/agency/inspections?${params}`
      },
      providesTags: ['Inspections'],
    }),
    getInspection: builder.query<ApiEnvelope<InspectionDetail>, string>({
      query: (uuid) => `/agency/inspections/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'Inspections', id: uuid }],
    }),
    createInspection: builder.mutation<ApiEnvelope<InspectionDetail>, { entryUuid: string; scheduledAt?: string }>({
      query: (body) => ({ url: '/agency/inspections', method: 'POST', body }),
      invalidatesTags: ['Inspections', 'AgencyDashboard'],
    }),
    completeInspection: builder.mutation<ApiEnvelope<InspectionDetail>, { uuid: string; result: string; findings?: string }>({
      query: ({ uuid, result, findings }) => ({
        url: `/agency/inspections/${uuid}/complete`,
        method: 'POST',
        body: { result, findings },
      }),
      invalidatesTags: ['Inspections', 'AgencyDashboard'],
    }),
    getAgencyBillings: builder.query<ApiEnvelope<PagedResult<AgencyBillingItem>>, { page?: number }>({
      query: ({ page = 1 }) => `/agency/billings?page=${page}`,
      providesTags: ['Billings'],
    }),
    createAgencyBilling: builder.mutation<ApiEnvelope<AgencyBillingItem>, { entryUuid: string; amount: number; description: string }>({
      query: (body) => ({ url: '/agency/billings', method: 'POST', body }),
      invalidatesTags: ['Billings', 'AgencyDashboard'],
    }),
    issueBilling: builder.mutation<ApiEnvelope<AgencyBillingItem>, string>({
      query: (uuid) => ({ url: `/agency/billings/${uuid}/issue`, method: 'POST' }),
      invalidatesTags: ['Billings'],
    }),
    markBillingPaid: builder.mutation<ApiEnvelope<AgencyBillingItem>, string>({
      query: (uuid) => ({ url: `/agency/billings/${uuid}/pay`, method: 'POST' }),
      invalidatesTags: ['Billings', 'AgencyDashboard'],
    }),
    verifyBillingPayment: builder.mutation<ApiEnvelope<AgencyBillingItem>, { uuid: string; approved: boolean; notes?: string }>({
      query: ({ uuid, approved, notes }) => ({
        url: `/agency/billings/${uuid}/verify-payment`,
        method: 'POST',
        body: { approved, notes },
      }),
      invalidatesTags: ['Billings', 'AgencyDashboard'],
    }),
    reviewInspectionPhoto: builder.mutation<
      ApiEnvelope<ClientContainerInspectionPhoto>,
      { photoUuid: string; decision: 'Approved' | 'Rejected'; comment?: string }
    >({
      query: ({ photoUuid, decision, comment }) => ({
        url: `/agency/workflow/inspection-photos/${photoUuid}/review`,
        method: 'POST',
        body: { decision, comment },
      }),
      invalidatesTags: ['AgencyEntry'],
    }),
    addTransportTag: builder.mutation<ApiEnvelope<OpsContainerListItem>, { containerUuid: string; transportType?: string }>({
      query: (body) => ({
        url: '/agency/workflow/transport-tags',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['AgencyDashboard', 'ApprovedEntries'],
    }),
    getAgencyAccreditation: builder.query<ApiEnvelope<PagedResult<AgencyAccreditationItem>>, { page?: number; filter?: string }>({
      query: ({ page = 1, filter }) => {
        const params = new URLSearchParams({ page: String(page) })
        if (filter) params.set('filter', filter)
        return `/agency/accreditation/submissions?${params.toString()}`
      },
      providesTags: ['Accreditation'],
    }),
    getAgencyAccreditationDetail: builder.query<ApiEnvelope<AgencyAccreditationDetail>, string>({
      query: (uuid) => `/agency/accreditation/submissions/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'Accreditation', id: uuid }],
    }),
    reviewAccreditationFile: builder.mutation<ApiEnvelope<{ saved: boolean }>, { fileUuid: string; decision: string; comment?: string }>({
      query: ({ fileUuid, decision, comment }) => ({
        url: `/agency/accreditation/submissions/files/${fileUuid}/review`,
        method: 'POST',
        body: { decision, comment },
      }),
      invalidatesTags: ['Accreditation'],
    }),
    completeAccreditationReview: builder.mutation<ApiEnvelope<{ completed: boolean }>, { uuid: string; decision: string; comment?: string; accreditationNumber?: string }>({
      query: ({ uuid, decision, comment, accreditationNumber }) => ({
        url: `/agency/accreditation/submissions/${uuid}/complete`,
        method: 'POST',
        body: { decision, comment, accreditationNumber },
      }),
      invalidatesTags: ['Accreditation', 'AgencyDashboard'],
    }),
    getApprovedEntries: builder.query<ApiEnvelope<PagedResult<AgencyEntryListItem>>, { page?: number }>({
      query: ({ page = 1 }) => `/agency/entries/approved?page=${page}`,
      providesTags: ['ApprovedEntries'],
    }),
    addEvaluatorNote: builder.mutation<ApiEnvelope<{ saved: boolean }>, { entryUuid: string; note: string; isInternal?: boolean }>({
      query: ({ entryUuid, note, isInternal = true }) => ({
        url: `/agency/evaluator/entries/${entryUuid}/notes`,
        method: 'POST',
        body: { note, isInternal },
      }),
      invalidatesTags: ['AgencyEntry'],
    }),
    getSecretaryReport: builder.query<ApiEnvelope<SecretaryReport>, void>({
      query: () => '/agency/reports/summary',
      providesTags: ['Reports'],
    }),
  }),
})

export const {
  useGetAgencyDashboardQuery,
  useGetEvaluatorQueueQuery,
  useGetMyAssignmentsQuery,
  useAssignEntryMutation,
  useGetAgencyEntryQuery,
  useEvaluateFileMutation,
  useEvaluateEntryMavMutation,
  useUpdateComplianceMutation,
  useCompleteEvaluationMutation,
  useGetInspectionsQuery,
  useGetInspectionQuery,
  useCreateInspectionMutation,
  useCompleteInspectionMutation,
  useGetAgencyBillingsQuery,
  useCreateAgencyBillingMutation,
  useIssueBillingMutation,
  useMarkBillingPaidMutation,
  useVerifyBillingPaymentMutation,
  useReviewInspectionPhotoMutation,
  useAddTransportTagMutation,
  useGetAgencyAccreditationQuery,
  useGetAgencyAccreditationDetailQuery,
  useReviewAccreditationFileMutation,
  useCompleteAccreditationReviewMutation,
  useGetApprovedEntriesQuery,
  useAddEvaluatorNoteMutation,
  useGetSecretaryReportQuery,
} = agencyApi
