import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'
import type {
  ClientContainerInspectionPhoto,
  ClientContainerInspectionStatus,
  PagedResult,
} from '../../client/api/clientApi'

export interface AgencyDashboard {
  queueCount: number
  myAssignments: number
  pendingInspections: number
  containerInspectionQueue: number
  myContainerInspectionAssignments: number
  openBillings: number
  pendingAccreditation: number
  pendingCashPayments: number
  paidBillings: number
  awaitingBilling: number
  agencyCode: string
  agencyName: string
}

export interface AgencyContainerInspectionQueueItem {
  containerUuid: string
  containerNumber: string
  entryUuid: string
  entryReferenceNo: string
  applicantName: string
  entryType: string
  pendingPhotoCount: number
  isComplete: boolean
  isApproved: boolean
  submittedAt?: string
  isAssignedToMe: boolean
  assignedInspectorName?: string
}

export interface AgencyContainerInspectionEntryContext {
  uuid: string
  referenceNo: string
  entryType: string
  status: string
  applicantName: string
  companyName?: string
  commodityName?: string
  description?: string
  quantity?: number
  unit?: string
  originCountry?: string
  destinationCountry?: string
  portOfEntry?: string
  agencyCode: string
  certificateUuid?: string
  certificateNumber?: string
  certificateTitle?: string
}

export interface AgencyContainerInspectionHistoryItem {
  status: string
  comment?: string
  createdAt: string
  actorName?: string
}

export interface AgencyContainerInspectionDetail {
  containerUuid: string
  containerNumber: string
  containerType?: string
  containerStatus: string
  formDataJson?: string
  isComplete: boolean
  isApproved: boolean
  submittedAt?: string
  inspectionOutcome?: string
  inspectionOutcomeComment?: string
  inspectionCompletedAt?: string
  isAssignedToMe: boolean
  assignedInspectorName?: string
  photos: ClientContainerInspectionPhoto[]
  entry: AgencyContainerInspectionEntryContext
  history: AgencyContainerInspectionHistoryItem[]
}

export interface TransportTagQueueItem {
  containerUuid: string
  containerNumber: string
  entryUuid: string
  entryReference: string
  containerStatus: string
  hasTransportTag: boolean
  updatedAt: string
}

export interface TaggedTransportQueueItem {
  containerUuid: string
  containerNumber: string
  entryUuid: string
  entryReference: string
  containerStatus: string
  tagUuid: string
  scheduledWarehouseDate?: string | null
  taggedAt: string
  updatedAt: string
}

export interface TransportTagQueues {
  ready: TransportTagQueueItem[]
  tagged: TaggedTransportQueueItem[]
}

export interface AddTransportTagResult {
  tagUuid: string
  containerUuid: string
  containerNumber: string
  entryReference: string
  transportType: string
  scheduledWarehouseDate?: string | null
  status: string
  qrPayload: string
  qrCodeData?: string | null
  taggedAt: string
}

export interface TransportTagSummary {
  tagUuid: string
  transportType: string
  scheduledWarehouseDate?: string | null
  taggedAt: string
  qrCodeData?: string | null
}

export interface TransportTagContainerDetail {
  container: AgencyContainerInspectionDetail
  transportTag?: TransportTagSummary | null
  updatedAt: string
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
    importTrack?: string
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

export interface AgencyBillingCharge {
  uuid: string
  description: string
  amount: number
  sortOrder: number
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
  charges: AgencyBillingCharge[]
}

export interface AgencyBillingDetail extends AgencyBillingItem {
  entryUuid: string
  entry?: AgencyBillingEntryContext | null
}

export interface AgencyBillingMicUtilization {
  certificateNumber: string
  hsCode: string
  commodityName: string
  volume: number
  utilizedAt: string
}

export interface AgencyBillingCommodity {
  commodityId?: number | null
  commodityName?: string | null
  commodityCode?: string | null
  categoryName?: string | null
  description?: string | null
  quantity: number
  unit: string
  originCountry?: string | null
  destinationCountry?: string | null
  portOfEntry?: string | null
  hsCode?: string | null
  mavHsLabel?: string | null
}

export interface AgencyBillingEntryContext {
  uuid: string
  referenceNo: string
  entryType: string
  status: string
  applicantName: string
  companyName?: string | null
  paymentStatus: string
  submittedAt?: string
  mavNo?: string | null
  importTrack?: string | null
  commodity?: AgencyBillingCommodity | null
  micUtilizations: AgencyBillingMicUtilization[]
  suggestedProcessingFee?: number | null
  suggestedFeeCurrency?: string | null
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
  tagTypes: [
    'AgencyDashboard',
    'AgencyQueue',
    'AgencyAssignments',
    'AgencyEntry',
    'Inspections',
    'Billings',
    'BillingEntryContext',
    'Accreditation',
    'Reports',
    'ApprovedEntries',
    'AgencyPaymentConfig',
    'ContainerInspectionQueue',
    'TransportTags',
  ],
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
    getAgencyBillingDetail: builder.query<ApiEnvelope<AgencyBillingDetail>, string>({
      query: (uuid) => `/agency/billings/${uuid}`,
      providesTags: (_result, _error, uuid) => [{ type: 'Billings', id: uuid }],
    }),
    createAgencyBilling: builder.mutation<
      ApiEnvelope<AgencyBillingItem>,
      { entryUuid: string; title?: string; charges: Array<{ description: string; amount: number }> }
    >({
      query: (body) => ({ url: '/agency/billings', method: 'POST', body }),
      invalidatesTags: ['Billings', 'AgencyDashboard', 'ApprovedEntries'],
    }),
    updateAgencyBilling: builder.mutation<
      ApiEnvelope<AgencyBillingItem>,
      { uuid: string; title?: string; charges: Array<{ description: string; amount: number }> }
    >({
      query: ({ uuid, title, charges }) => ({
        url: `/agency/billings/${uuid}`,
        method: 'PUT',
        body: { title, charges },
      }),
      invalidatesTags: ['Billings', 'AgencyDashboard'],
    }),
    issueBilling: builder.mutation<ApiEnvelope<AgencyBillingItem>, string>({
      query: (uuid) => ({ url: `/agency/billings/${uuid}/issue`, method: 'POST' }),
      invalidatesTags: ['Billings', 'AgencyDashboard', 'ApprovedEntries'],
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
    getContainerInspectionQueue: builder.query<
      ApiEnvelope<PagedResult<AgencyContainerInspectionQueueItem>>,
      { page?: number; scope?: 'unclaimed' | 'mine' | 'all' }
    >({
      query: ({ page = 1, scope = 'unclaimed' }) =>
        `/agency/workflow/container-inspections?page=${page}&scope=${scope}`,
      providesTags: (_result, _error, arg) => [
        { type: 'ContainerInspectionQueue', id: arg.scope ?? 'unclaimed' },
      ],
    }),
    claimContainerInspection: builder.mutation<ApiEnvelope<AgencyContainerInspectionDetail>, string>({
      query: (containerUuid) => ({
        url: `/agency/workflow/containers/${containerUuid}/claim`,
        method: 'POST',
      }),
      invalidatesTags: [
        { type: 'ContainerInspectionQueue', id: 'unclaimed' },
        { type: 'ContainerInspectionQueue', id: 'mine' },
        'AgencyDashboard',
      ],
    }),
    getAgencyEntryContainerInspections: builder.query<ApiEnvelope<ClientContainerInspectionStatus[]>, string>({
      query: (entryUuid) => `/agency/workflow/entries/${entryUuid}/container-inspections`,
      providesTags: (_r, _e, entryUuid) => [{ type: 'ContainerInspectionQueue', id: entryUuid }],
    }),
    getContainerInspectionDetail: builder.query<ApiEnvelope<AgencyContainerInspectionDetail>, string>({
      query: (containerUuid) => `/agency/workflow/containers/${containerUuid}`,
      providesTags: (_r, _e, containerUuid) => [{ type: 'ContainerInspectionQueue', id: containerUuid }],
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
      invalidatesTags: (result) => [
        'AgencyEntry',
        'ContainerInspectionQueue',
        { type: 'ContainerInspectionQueue', id: 'unclaimed' },
        { type: 'ContainerInspectionQueue', id: 'mine' },
        ...(result?.data?.containerUuid
          ? [{ type: 'ContainerInspectionQueue' as const, id: result.data.containerUuid }]
          : []),
      ],
    }),
    completeContainerInspection: builder.mutation<
      ApiEnvelope<AgencyContainerInspectionDetail>,
      { containerUuid: string; decision: string; comment?: string }
    >({
      query: ({ containerUuid, decision, comment }) => ({
        url: `/agency/workflow/containers/${containerUuid}/complete`,
        method: 'POST',
        body: { decision, comment },
      }),
      invalidatesTags: (_result, _error, arg) => [
        'AgencyEntry',
        'ContainerInspectionQueue',
        { type: 'ContainerInspectionQueue', id: 'unclaimed' },
        { type: 'ContainerInspectionQueue', id: 'mine' },
        { type: 'ContainerInspectionQueue', id: arg.containerUuid },
      ],
    }),
    getTransportTagQueue: builder.query<ApiEnvelope<TransportTagQueues>, void>({
      query: () => '/agency/workflow/transport-tags/ready',
      providesTags: ['TransportTags'],
    }),
    getTransportTagContainerDetail: builder.query<ApiEnvelope<TransportTagContainerDetail>, string>({
      query: (containerUuid) => `/agency/workflow/transport-tags/containers/${containerUuid}`,
      providesTags: (_r, _e, containerUuid) => [{ type: 'TransportTags', id: containerUuid }],
    }),
    addTransportTag: builder.mutation<ApiEnvelope<AddTransportTagResult>, { containerUuid: string; scheduledWarehouseDate: string }>({
      query: (body) => ({
        url: '/agency/workflow/transport-tags',
        method: 'POST',
        body,
      }),
      invalidatesTags: (_r, _e, arg) => [
        'AgencyDashboard',
        'ApprovedEntries',
        'TransportTags',
        { type: 'TransportTags', id: arg.containerUuid },
      ],
    }),
    getTransportTag: builder.query<ApiEnvelope<AddTransportTagResult>, string>({
      query: (tagUuid) => `/agency/workflow/transport-tags/${tagUuid}`,
      providesTags: (_r, _e, tagUuid) => [{ type: 'TransportTags', id: tagUuid }],
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
    getEntriesAwaitingBilling: builder.query<ApiEnvelope<PagedResult<AgencyEntryListItem>>, { page?: number }>({
      query: ({ page = 1 }) => `/agency/billings/awaiting-entries?page=${page}`,
      providesTags: ['ApprovedEntries', 'Billings'],
    }),
    getBillingEntryContext: builder.query<ApiEnvelope<AgencyBillingEntryContext>, string>({
      query: (entryUuid) => `/agency/billings/awaiting-entries/${entryUuid}`,
      providesTags: (_result, _error, entryUuid) => [{ type: 'BillingEntryContext', id: entryUuid }],
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
    getAgencyBillingRevenueReport: builder.query<ApiEnvelope<AgencyBillingRevenueReport>, void>({
      query: () => '/agency/reports/billing-revenue',
      providesTags: ['Reports'],
    }),
    getAgencyPaymentSettings: builder.query<ApiEnvelope<AgencyPaymentSettings>, void>({
      query: () => '/agency/payment-config/settings',
      providesTags: ['AgencyPaymentConfig'],
    }),
    updateAgencyPaymentSettings: builder.mutation<ApiEnvelope<AgencyPaymentSettings>, UpdateAgencyPaymentSettingsRequest>({
      query: (body) => ({ url: '/agency/payment-config/settings', method: 'PUT', body }),
      invalidatesTags: ['AgencyPaymentConfig'],
    }),
    getPendingCashPayments: builder.query<ApiEnvelope<AgencyPendingCashPayment[]>, void>({
      query: () => '/agency/payment-config/pending-cash',
      providesTags: ['AgencyPaymentConfig'],
    }),
    verifyCashPayment: builder.mutation<ApiEnvelope<{ verified: boolean }>, { billUuid: string; approved: boolean; notes?: string }>({
      query: ({ billUuid, approved, notes }) => ({
        url: `/agency/payment-config/pending-cash/${billUuid}/verify`,
        method: 'POST',
        body: { approved, notes },
      }),
      invalidatesTags: ['AgencyPaymentConfig', 'Billings'],
    }),
  }),
})

export interface AgencyPaymentSettings {
  agencyId: number
  agencyCode: string
  agencyName: string
  gateway: {
    enabled: boolean
    mode: string
    hasApiKey: boolean
    apiKeyMasked: string
    hasWebhookSecret: boolean
    publicKey?: string | null
  }
  cashPaymentEnabled: boolean
  cashPaymentInstructions?: string | null
  usesGlobalPayMongoFallback: boolean
  processingFees: Array<{ entryType: string; amount: number; currency: string }>
}

export interface UpdateAgencyPaymentSettingsRequest {
  payMongoEnabled: boolean
  cashPaymentEnabled: boolean
  payMongoApiKey?: string
  payMongoWebhookSecret?: string
  payMongoPublicKey?: string
  cashPaymentInstructions?: string
  importFeeAmount: number
  exportFeeAmount: number
  currency: string
}

export interface AgencyBillingRevenueReport {
  agencyCode: string
  agencyName: string
  collected: number
  pending: number
  paidCount: number
  pendingCashCount: number
  openBillings: number
  byMonth: Array<{ month: string; amount: number; paymentCount: number }>
}

export interface AgencyPendingCashPayment {
  billUuid: string
  billNumber: string
  entryReferenceNo?: string | null
  clientName: string
  amount: number
  paymentMethod: string
  externalReference?: string | null
  submittedAt: string
}

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
  useGetContainerInspectionQueueQuery,
  useClaimContainerInspectionMutation,
  useGetAgencyEntryContainerInspectionsQuery,
  useGetContainerInspectionDetailQuery,
  useGetAgencyBillingsQuery,
  useGetAgencyBillingDetailQuery,
  useCreateAgencyBillingMutation,
  useUpdateAgencyBillingMutation,
  useIssueBillingMutation,
  useMarkBillingPaidMutation,
  useVerifyBillingPaymentMutation,
  useReviewInspectionPhotoMutation,
  useCompleteContainerInspectionMutation,
  useGetTransportTagQueueQuery,
  useGetTransportTagContainerDetailQuery,
  useAddTransportTagMutation,
  useGetTransportTagQuery,
  useGetAgencyAccreditationQuery,
  useGetAgencyAccreditationDetailQuery,
  useReviewAccreditationFileMutation,
  useCompleteAccreditationReviewMutation,
  useGetApprovedEntriesQuery,
  useGetEntriesAwaitingBillingQuery,
  useGetBillingEntryContextQuery,
  useAddEvaluatorNoteMutation,
  useGetSecretaryReportQuery,
  useGetAgencyBillingRevenueReportQuery,
  useGetAgencyPaymentSettingsQuery,
  useUpdateAgencyPaymentSettingsMutation,
  useGetPendingCashPaymentsQuery,
  useVerifyCashPaymentMutation,
} = agencyApi
