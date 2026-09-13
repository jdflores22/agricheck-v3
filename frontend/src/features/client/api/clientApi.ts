import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'
import { clearOptimisticAccreditation, writeOptimisticAccreditation } from '../optimisticAccreditation'

function currentUserUuid(getState: () => unknown) {
  return (getState() as { auth?: { user?: { uuid?: string } } }).auth?.user?.uuid
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface ClientDashboardProfile {
  firstName: string
  lastName: string
  email: string
}

export interface ClientDashboardAccreditation {
  status?: string
  displayStatus?: string
  submissionType?: string
  accreditationNumber?: string
  companyName?: string
  reviewComments?: string
  isAccredited: boolean
}

export interface ClientDashboardEntryStats {
  total: number
  pending: number
  approved: number
  forCompliance: number
  daIssueBilling?: number
  forInspection?: number
  readyForTransport?: number
  inTransit?: number
}

export interface ClientDashboardLogisticsStats {
  pendingPayments: number
  unpaidBills: number
  overdueBills: number
  totalAmountDue: number
  approvedContainers: number
  assignedContainers: number
  pendingInspections: number
}

export interface ClientDashboardAgency {
  id: number
  code: string
  name: string
  logoUrl?: string
  hasEntryForm: boolean
}

export interface ClientDashboardRecentBill {
  uuid: string
  entryUuid?: string
  billNumber: string
  entryReferenceNo?: string
  agencyName?: string
  description: string
  amount: number
  status: string
  dueDate?: string
  isOverdue: boolean
  paymentLinkToken?: string
}

export interface ClientDashboardWorkflowStats {
  daIssueBilling: number
  forInspection: number
  readyForTransport: number
  awaitingTransport: number
  inTransit: number
  daBillingPaymentPending: number
}

export interface ClientDashboard {
  profile: ClientDashboardProfile
  accreditation: ClientDashboardAccreditation
  entries: ClientDashboardEntryStats
  workflow: ClientDashboardWorkflowStats
  logistics: ClientDashboardLogisticsStats
  agencies: ClientDashboardAgency[]
  recentBills: ClientDashboardRecentBill[]
  recentEntries: Array<{ uuid: string; referenceNo: string; status: string; agencyCode: string; createdAt: string }>
  recentCertificates: Array<{ uuid: string; certificateNumber: string; title: string; status: string; issuedAt: string }>
}

export interface EntryFileVersion {
  versionNumber: number
  originalFileName: string
  fileSizeBytes: number
  createdAt: string
  isCurrent: boolean
}

export interface ClientFormListItem {
  uuid: string
  name: string
  formType: string
  versionNumber: number
}

export interface ClientFormDetail {
  uuid: string
  name: string
  formType: string
  versionNumber: number
  schemaJson: string
}

export interface EntryListItem {
  uuid: string
  referenceNo: string
  entryType: string
  status: string
  agencyCode: string
  commodityName?: string
  createdAt: string
  updatedAt: string
  submittedAt?: string
  paymentStatus: string
  paymentAmount?: number
  complianceDeadlineAt?: string
}

export interface EntryDetail {
  commodityId?: number
  commodityName?: string
  description?: string
  quantity: number
  unit: string
  originCountry?: string
  destinationCountry?: string
  portOfEntry?: string
}

export interface EntryContainer {
  uuid: string
  sequenceNumber: number
  containerNumber: string
  containerType?: string
  formDataJson?: string
}

export interface ClientContainerProcessStep {
  key: string
  label: string
  description: string
  state: string
  completedAt?: string
}

export interface ClientContainerBookingSummary {
  uuid: string
  bookingNumber: string
  warehouseName: string
  status: string
  scheduledDate: string
}

export interface ClientContainerWarehouseInfo {
  facilityName?: string
  locationCode?: string
  receivedAt?: string
  status?: string
}

export interface ClientContainerTransportTag {
  tagUuid: string
  scheduledWarehouseDate?: string | null
  taggedAt: string
  qrCodeData?: string | null
}

export interface ClientContainerDetail {
  uuid: string
  sequenceNumber: number
  containerNumber: string
  containerType?: string
  formDataJson?: string
  status: string
  entryUuid: string
  entryReferenceNo: string
  entryStatus: string
  agencyCode: string
  agencyName: string
  departureTime?: string
  arrivalTime?: string
  createdAt: string
  updatedAt: string
  canBookWarehouse: boolean
  warehouseBookingBlockedReason?: string
  processSteps: ClientContainerProcessStep[]
  warehouseInfo?: ClientContainerWarehouseInfo | null
  bookings: ClientContainerBookingSummary[]
  transportTag?: ClientContainerTransportTag | null
}

export interface ClientDaBillingCharge {
  description: string
  amount: number
  sortOrder: number
}

export interface ClientDaBilling {
  uuid: string
  entryUuid: string
  entryReferenceNo: string
  billNumber: string
  description: string
  amount: number
  status: string
  displayStatus: string
  issuedAt?: string
  paidAt?: string
  paymentReference?: string
  paymentProofOriginalFileName?: string
  paymentUploadedAt?: string
  verificationNotes?: string
  charges?: ClientDaBillingCharge[]
}

export type ContainerInspectionPhotoType =
  | 'ActualItem'
  | 'LabelImage'
  | 'XrayImage'
  | 'ExaminationImage'
  | 'ReportImage'
  | 'RequestForInspection'

export interface ClientContainerInspectionPhoto {
  uuid: string
  containerUuid: string
  photoType: string
  originalFileName: string
  reviewDecision: string
  reviewComment?: string
  createdAt: string
  reviewedAt?: string
  reviewedByName?: string
}

export interface ClientContainerInspectionStatus {
  containerUuid: string
  containerNumber: string
  photos: ClientContainerInspectionPhoto[]
  isComplete: boolean
  isApproved: boolean
}

export const CONTAINER_INSPECTION_PHOTO_TYPES: ContainerInspectionPhotoType[] = [
  'ActualItem',
  'LabelImage',
  'XrayImage',
  'ExaminationImage',
  'ReportImage',
  'RequestForInspection',
]

export const CONTAINER_INSPECTION_PHOTO_LABELS: Record<ContainerInspectionPhotoType, string> = {
  ActualItem: 'Actual Item',
  LabelImage: 'Label Image',
  XrayImage: 'X-ray Image',
  ExaminationImage: 'Examination Image',
  ReportImage: 'Report Image',
  RequestForInspection: 'Request for Inspection',
}

export interface Entry {
  uuid: string
  referenceNo: string
  entryType: string
  status: string
  agencyId: number
  agencyCode: string
  agencyName: string
  notes?: string
  submittedAt?: string
  paymentStatus: string
  paymentAmount?: number
  complianceDeadlineAt?: string
  formDataJson?: string
  detail?: EntryDetail
  containers?: EntryContainer[]
  files: Array<{ uuid: string; originalFileName: string; contentType: string; fileSizeBytes: number; documentType?: string; createdAt: string; evaluationDecision?: string; evaluationComment?: string; versions?: EntryFileVersion[] }>
  statusHistory: Array<{ fromStatus: string; toStatus: string; comment?: string; createdAt: string }>
  timeline: Array<{ eventType: string; title: string; description?: string; createdAt: string }>
  bills: Array<{ uuid: string; billNumber: string; description: string; amount: number; status: string; dueDate?: string; paidAt?: string }>
  mav?: EntryMavInfo
}

export interface EntryMicUtilization {
  micUuid: string
  certificateNumber: string
  volume: number
  utilizedAt: string
  hsCode: string
  commodityName: string
}

export interface EntryMavInfo {
  mavNo?: string
  importTrack?: string
  mavDocumentStatus: string
  mavRemarks?: string
  mavCertificateFileUuid?: string
  mavCertificateFileName?: string
  micUtilizations: EntryMicUtilization[]
  totalUtilizedVolume: number
  requiredVolume?: number
}

export interface CheckMavNoResult {
  isAvailable: boolean
  message?: string
}

export const clientApi = createApi({
  reducerPath: 'clientApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Dashboard', 'Entries', 'Accreditation', 'Certificates', 'Bookings', 'Bills', 'Profile', 'Containers', 'Inspections', 'DaBillings', 'ContainerInspections'],
  endpoints: (builder) => ({
    getDashboard: builder.query<ApiEnvelope<ClientDashboard>, void>({
      query: () => '/client/dashboard',
      providesTags: ['Dashboard'],
      keepUnusedDataFor: 120,
    }),
    getAgencies: builder.query<ApiEnvelope<Array<{ id: number; code: string; name: string }>>, void>({
      query: () => '/commodities/agencies',
    }),
    getCommodities: builder.query<ApiEnvelope<Array<{ id: number; code: string; name: string; categoryName: string }>>, void>({
      query: () => '/commodities',
    }),
    getEntries: builder.query<ApiEnvelope<PagedResult<EntryListItem>>, { page?: number; status?: string }>({
      query: ({ page = 1, status }) => ({ url: '/entries', params: { page, pageSize: 20, status } }),
      providesTags: ['Entries'],
    }),
    getEntry: builder.query<ApiEnvelope<Entry>, string>({
      query: (uuid) => `/entries/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'Entries', id: uuid }],
    }),
    createEntry: builder.mutation<ApiEnvelope<Entry>, Record<string, unknown>>({
      query: (body) => ({ url: '/entries', method: 'POST', body }),
      invalidatesTags: ['Entries', 'Dashboard'],
    }),
    updateEntry: builder.mutation<ApiEnvelope<Entry>, { uuid: string; body: Record<string, unknown> }>({
      query: ({ uuid, body }) => ({ url: `/entries/${uuid}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, arg) => [{ type: 'Entries', id: arg.uuid }, 'Entries'],
    }),
    submitEntry: builder.mutation<ApiEnvelope<Entry>, string>({
      query: (uuid) => ({ url: `/entries/${uuid}/submit`, method: 'POST' }),
      invalidatesTags: ['Entries', 'Dashboard', 'Bills'],
    }),
    checkMavNo: builder.query<ApiEnvelope<CheckMavNoResult>, { mavNo: string; excludeUuid?: string }>({
      query: ({ mavNo, excludeUuid }) => ({
        url: '/entries/check-mav-no',
        params: { mavNo, excludeUuid },
      }),
    }),
    updateEntryMav: builder.mutation<ApiEnvelope<Entry>, { uuid: string; mavNo: string }>({
      query: ({ uuid, mavNo }) => ({ url: `/entries/${uuid}/mav`, method: 'PUT', body: { mavNo } }),
      invalidatesTags: (_r, _e, arg) => [{ type: 'Entries', id: arg.uuid }],
    }),
    utilizeEntryMic: builder.mutation<ApiEnvelope<Entry>, { uuid: string; micUuid: string; volume: number }>({
      query: ({ uuid, micUuid, volume }) => ({
        url: `/entries/${uuid}/mav/utilize`,
        method: 'POST',
        body: { micUuid, volume },
      }),
      invalidatesTags: (_r, _e, arg) => [{ type: 'Entries', id: arg.uuid }],
    }),
    getAccreditationSubmissions: builder.query<ApiEnvelope<PagedResult<{ uuid: string; companyName: string; submissionType: string; status: string; displayStatus: string; submittedAt?: string; createdAt: string }>>, number | void>({
      query: (page = 1) => ({ url: '/accreditation/submissions', params: { page, pageSize: 20 } }),
      providesTags: ['Accreditation'],
    }),
    getAccreditationSubmission: builder.query<ApiEnvelope<{
      uuid: string
      companyName: string
      submissionType: string
      status: string
      displayStatus: string
      formDataJson?: string
      reviewComments?: string
      accreditationNumber?: string
      certificateUuid?: string
      certificateNumber?: string
      submittedAt?: string
      files: Array<{ uuid: string; originalFileName: string; fileSizeBytes: number; createdAt: string; reviewDecision?: string; reviewComment?: string; versions?: Array<{ versionNumber: number; originalFileName: string; fileSizeBytes: number; createdAt: string; isCurrent: boolean }> }>
      history: Array<{ status: string; comment?: string; createdAt: string }>
    }>, string>({
      query: (uuid) => `/accreditation/submissions/${uuid}`,
      providesTags: ['Accreditation'],
    }),
    createAccreditation: builder.mutation<ApiEnvelope<unknown>, { companyName: string; submissionType: string; formDataJson?: string }>({
      query: (body) => ({ url: '/accreditation/submissions', method: 'POST', body }),
      async onQueryStarted(body, { dispatch, getState, queryFulfilled }) {
        const next: ClientDashboardAccreditation = {
          status: 'Draft',
          displayStatus: 'Draft',
          companyName: body.companyName,
          submissionType: body.submissionType,
          isAccredited: false,
        }
        writeOptimisticAccreditation(next, currentUserUuid(getState))
        const patch = dispatch(
          clientApi.util.updateQueryData('getDashboard', undefined, (draft) => {
            if (!draft.data) return
            draft.data.accreditation = { ...draft.data.accreditation, ...next }
          }),
        )
        try {
          await queryFulfilled
        } catch {
          patch.undo()
          clearOptimisticAccreditation()
        }
      },
      invalidatesTags: ['Accreditation', 'Dashboard'],
    }),
    submitAccreditation: builder.mutation<ApiEnvelope<unknown>, string>({
      query: (uuid) => ({ url: `/accreditation/submissions/${uuid}/submit`, method: 'POST' }),
      async onQueryStarted(_uuid, { dispatch, getState, queryFulfilled }) {
        const next: ClientDashboardAccreditation = {
          status: 'Submitted',
          displayStatus: 'Submitted',
          isAccredited: false,
        }
        writeOptimisticAccreditation(next, currentUserUuid(getState))
        const patch = dispatch(
          clientApi.util.updateQueryData('getDashboard', undefined, (draft) => {
            if (!draft.data) return
            draft.data.accreditation = { ...draft.data.accreditation, ...next }
          }),
        )
        try {
          await queryFulfilled
        } catch {
          patch.undo()
          clearOptimisticAccreditation()
        }
      },
      invalidatesTags: ['Accreditation', 'Dashboard'],
    }),
    getCertificates: builder.query<ApiEnvelope<PagedResult<{ uuid: string; certificateNumber: string; title: string; status: string; issuedAt: string; expiresAt?: string; entryReferenceNo?: string }>>, number | void>({
      query: (page = 1) => ({ url: '/certificates', params: { page, pageSize: 20 } }),
      providesTags: ['Certificates'],
    }),
    getWarehouseFacilities: builder.query<ApiEnvelope<Array<{ id: number; code: string; name: string; location?: string; capacity: number }>>, void>({
      query: () => '/warehouse/facilities',
    }),
    getBookings: builder.query<ApiEnvelope<PagedResult<{ uuid: string; bookingNumber: string; warehouseName: string; containerReference: string; status: string; scheduledDate: string; amount?: number }>>, number | void>({
      query: (page = 1) => ({ url: '/warehouse/bookings', params: { page, pageSize: 20 } }),
      providesTags: ['Bookings'],
    }),
    createBooking: builder.mutation<ApiEnvelope<unknown>, { warehouseFacilityId: number; containerReference: string; scheduledDate: string; notes?: string }>({
      query: (body) => ({ url: '/warehouse/bookings', method: 'POST', body }),
      invalidatesTags: ['Bookings', 'Dashboard'],
    }),
    cancelBooking: builder.mutation<ApiEnvelope<unknown>, string>({
      query: (uuid) => ({ url: `/warehouse/bookings/${uuid}/cancel`, method: 'POST' }),
      invalidatesTags: ['Bookings', 'Dashboard'],
    }),
    payBill: builder.mutation<ApiEnvelope<unknown>, { uuid: string; paymentMethod: string }>({
      query: ({ uuid, paymentMethod }) => ({ url: `/client/bills/${uuid}/pay`, method: 'POST', body: { paymentMethod } }),
      invalidatesTags: ['Bills', 'Entries', 'Dashboard'],
    }),
    payBillByToken: builder.mutation<ApiEnvelope<unknown>, { token: string; paymentMethod: string }>({
      query: ({ token, paymentMethod }) => ({ url: `/client/bills/pay/${token}/complete`, method: 'POST', body: { paymentMethod } }),
      invalidatesTags: ['Bills', 'Entries', 'Dashboard'],
    }),
    getBillPaymentOptions: builder.query<ApiEnvelope<{
      payMongoEnabled: boolean
      cashPaymentEnabled: boolean
      gatewayMode: string
      cashPaymentInstructions?: string | null
    }>, string>({
      query: (uuid) => `/client/bills/${uuid}/payment-options`,
    }),
    initiateBillPayment: builder.mutation<ApiEnvelope<{
      mode: string
      paymentReference: string
      paymentUrl?: string
      bill: {
        uuid: string
        billNumber: string
        description: string
        amount: number
        status: string
      }
    }>, { uuid: string; paymentMethod: string; returnBaseUrl?: string; paymentReference?: string }>({
      query: ({ uuid, paymentMethod, returnBaseUrl, paymentReference }) => ({
        url: `/client/bills/${uuid}/initiate`,
        method: 'POST',
        body: { paymentMethod, returnBaseUrl, paymentReference },
      }),
      invalidatesTags: ['Bills', 'Entries', 'Dashboard'],
    }),
    confirmBillPayment: builder.mutation<ApiEnvelope<{
      uuid: string
      billNumber: string
      description: string
      amount: number
      status: string
      entryUuid?: string
      entryReferenceNo?: string
    }>, string>({
      query: (uuid) => ({ url: `/client/bills/${uuid}/confirm-payment`, method: 'POST' }),
      invalidatesTags: ['Bills', 'Entries', 'Dashboard'],
    }),
    confirmBillPaymentByToken: builder.mutation<ApiEnvelope<{
      uuid: string
      billNumber: string
      description: string
      amount: number
      status: string
      entryUuid?: string
      entryReferenceNo?: string
    }>, string>({
      query: (token) => ({ url: `/client/bills/pay/${token}/confirm-payment`, method: 'POST' }),
      invalidatesTags: ['Bills', 'Entries', 'Dashboard'],
    }),
    getBills: builder.query<ApiEnvelope<Array<{ uuid: string; billNumber: string; description: string; amount: number; status: string; dueDate?: string; paidAt?: string }>>, void>({
      query: () => '/client/bills',
      providesTags: ['Bills'],
    }),
    getBill: builder.query<ApiEnvelope<{
      uuid: string
      billNumber: string
      description: string
      amount: number
      status: string
      dueDate?: string
      paymentLinkToken?: string
      entryUuid?: string
      entryReferenceNo?: string
      payments: Array<{ amount: number; paymentMethod: string; status: string; createdAt: string }>
    }>, string>({
      query: (uuid) => `/client/bills/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'Bills', id: uuid }],
    }),
    getBillByToken: builder.query<ApiEnvelope<{
      uuid: string
      billNumber: string
      description: string
      amount: number
      status: string
      dueDate?: string
      entryReferenceNo?: string
    }>, string>({
      query: (token) => `/client/bills/pay/${token}`,
    }),
    getPaymentHistory: builder.query<ApiEnvelope<Array<{
      billUuid: string
      billNumber: string
      entryReferenceNo?: string
      amount: number
      paymentMethod: string
      status: string
      externalReference?: string
      paidAt: string
    }>>, void>({
      query: () => '/client/bills/history/payments',
      providesTags: ['Bills'],
    }),
    getCertificate: builder.query<ApiEnvelope<{
      uuid: string
      certificateNumber: string
      verificationCode: string
      title: string
      status: string
      issuedAt: string
      expiresAt?: string
      entryReferenceNo?: string
      qrCodeData?: string
      summaryJson?: string
    }>, string>({
      query: (uuid) => `/certificates/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'Certificates', id: uuid }],
    }),
    getBooking: builder.query<ApiEnvelope<{
      uuid: string
      bookingNumber: string
      warehouseName: string
      containerReference: string
      status: string
      scheduledDate: string
      notes?: string
      amount?: number
    }>, string>({
      query: (uuid) => `/warehouse/bookings/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'Bookings', id: uuid }],
    }),
    getClientProfile: builder.query<ApiEnvelope<{
      firstName: string
      lastName: string
      email: string
      phone?: string
      companyName?: string
      address?: string
    }>, void>({
      query: () => '/client/profile',
      providesTags: ['Profile'],
    }),
    updateClientProfile: builder.mutation<ApiEnvelope<unknown>, {
      firstName: string
      lastName: string
      phone?: string
      companyName?: string
      address?: string
    }>({
      query: (body) => ({ url: '/client/profile', method: 'PUT', body }),
      invalidatesTags: ['Profile', 'Dashboard'],
    }),
    getContainers: builder.query<ApiEnvelope<Array<{
      uuid: string
      containerNumber: string
      status: string
      entryReferenceNo: string
      agencyCode: string
      updatedAt: string
    }>>, string | void>({
      query: (status) => ({ url: '/client/containers', params: status ? { status } : undefined }),
      providesTags: ['Containers'],
    }),
    getContainer: builder.query<ApiEnvelope<ClientContainerDetail>, string>({
      query: (uuid) => `/client/containers/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'Containers', id: uuid }],
    }),
    getDaBillings: builder.query<ApiEnvelope<ClientDaBilling[]>, string>({
      query: (entryUuid) => `/client/entries/${entryUuid}/da-billings`,
      providesTags: (_r, _e, entryUuid) => [{ type: 'DaBillings', id: entryUuid }],
    }),
    uploadDaBillingPaymentProof: builder.mutation<
      ApiEnvelope<ClientDaBilling>,
      { entryUuid: string; billingUuid: string; file: File; paymentReference: string; notes?: string }
    >({
      query: ({ entryUuid, billingUuid, file, paymentReference, notes }) => {
        const formData = new FormData()
        formData.append('file', file)
        formData.append('paymentReference', paymentReference)
        if (notes) formData.append('notes', notes)
        return {
          url: `/client/entries/${entryUuid}/da-billings/${billingUuid}/payment-proof`,
          method: 'POST',
          body: formData,
        }
      },
      invalidatesTags: (_r, _e, arg) => [{ type: 'DaBillings', id: arg.entryUuid }, 'Entries', 'Dashboard'],
    }),
    getContainerInspections: builder.query<ApiEnvelope<ClientContainerInspectionStatus[]>, string>({
      query: (entryUuid) => `/client/entries/${entryUuid}/container-inspections`,
      providesTags: (_r, _e, entryUuid) => [{ type: 'ContainerInspections', id: entryUuid }],
    }),
    uploadContainerInspectionPhoto: builder.mutation<
      ApiEnvelope<ClientContainerInspectionPhoto>,
      { entryUuid: string; containerUuid: string; photoType: ContainerInspectionPhotoType; file: File }
    >({
      query: ({ entryUuid, containerUuid, photoType, file }) => {
        const formData = new FormData()
        formData.append('file', file)
        return {
          url: `/client/entries/${entryUuid}/container-inspections/containers/${containerUuid}/photos/${photoType}`,
          method: 'POST',
          body: formData,
        }
      },
      invalidatesTags: (_r, _e, arg) => [{ type: 'ContainerInspections', id: arg.entryUuid }, 'Entries'],
    }),
    getInspections: builder.query<ApiEnvelope<Array<{
      uuid: string
      entryUuid: string
      entryReferenceNo: string
      agencyCode: string
      status: string
      scheduledAt?: string
      completedAt?: string
    }>>, string | { status?: string; entryUuid?: string } | void>({
      query: (arg) => {
        if (typeof arg === 'string') return { url: '/client/inspections', params: arg ? { status: arg } : undefined }
        if (arg && typeof arg === 'object') return { url: '/client/inspections', params: arg }
        return '/client/inspections'
      },
      providesTags: ['Inspections'],
    }),
    getInspection: builder.query<ApiEnvelope<{
      uuid: string
      entryUuid: string
      entryReferenceNo: string
      agencyCode: string
      agencyName: string
      status: string
      scheduledAt?: string
      completedAt?: string
      findings?: string
      inspectorName?: string
    }>, string>({
      query: (uuid) => `/client/inspections/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'Inspections', id: uuid }],
    }),
    updateAccreditation: builder.mutation<ApiEnvelope<unknown>, { uuid: string; body: { companyName: string; formDataJson?: string } }>({
      query: ({ uuid, body }) => ({ url: `/accreditation/submissions/${uuid}`, method: 'PUT', body }),
      invalidatesTags: ['Accreditation', 'Dashboard'],
    }),
    uploadEntryFile: builder.mutation<ApiEnvelope<unknown>, { uuid: string; file: File; documentType?: string }>({
      query: ({ uuid, file, documentType }) => {
        const formData = new FormData()
        formData.append('file', file)
        if (documentType) formData.append('documentType', documentType)
        return { url: `/entries/${uuid}/files`, method: 'POST', body: formData }
      },
      invalidatesTags: (_r, _e, arg) => [{ type: 'Entries', id: arg.uuid }],
    }),
    uploadAccreditationFile: builder.mutation<ApiEnvelope<unknown>, { uuid: string; file: File }>({
      query: ({ uuid, file }) => {
        const formData = new FormData()
        formData.append('file', file)
        return { url: `/accreditation/submissions/${uuid}/files`, method: 'POST', body: formData }
      },
      invalidatesTags: ['Accreditation'],
    }),
    uploadComplianceFile: builder.mutation<ApiEnvelope<unknown>, { uuid: string; fileUuid: string; file: File }>({
      query: ({ uuid, fileUuid, file }) => {
        const formData = new FormData()
        formData.append('file', file)
        return { url: `/entries/${uuid}/files/${fileUuid}/compliance`, method: 'POST', body: formData }
      },
      invalidatesTags: (_r, _e, arg) => [{ type: 'Entries', id: arg.uuid }],
    }),
    initiateBillPaymentByToken: builder.mutation<ApiEnvelope<{
      mode: string
      paymentReference: string
      paymentUrl?: string
      bill: { uuid: string; billNumber: string; description: string; amount: number; status: string }
    }>, { token: string; paymentMethod: string; returnBaseUrl?: string }>({
      query: ({ token, paymentMethod, returnBaseUrl }) => ({
        url: `/client/bills/pay/${token}/initiate`,
        method: 'POST',
        body: { paymentMethod, returnBaseUrl },
      }),
      invalidatesTags: ['Bills', 'Entries', 'Dashboard'],
    }),
    getClientForms: builder.query<ApiEnvelope<ClientFormListItem[]>, { agencyId?: number; formType?: string }>({
      query: ({ agencyId, formType = 'ENTRY' }) => ({
        url: '/client/forms',
        params: {
          formType,
          ...(agencyId ? { agencyId } : {}),
        },
      }),
    }),
    getClientForm: builder.query<ApiEnvelope<ClientFormDetail>, string>({
      query: (uuid) => `/client/forms/${uuid}`,
    }),
    uploadAccreditationComplianceFile: builder.mutation<ApiEnvelope<unknown>, { uuid: string; fileUuid: string; file: File }>({
      query: ({ uuid, fileUuid, file }) => {
        const formData = new FormData()
        formData.append('file', file)
        return { url: `/accreditation/submissions/${uuid}/files/${fileUuid}/compliance`, method: 'POST', body: formData }
      },
      invalidatesTags: ['Accreditation'],
    }),
    resubmitAccreditationCompliance: builder.mutation<ApiEnvelope<unknown>, string>({
      query: (uuid) => ({ url: `/accreditation/submissions/${uuid}/compliance/resubmit`, method: 'POST' }),
      async onQueryStarted(_uuid, { dispatch, getState, queryFulfilled }) {
        const next: ClientDashboardAccreditation = {
          status: 'Submitted',
          displayStatus: 'Resubmitted for Review',
          isAccredited: false,
        }
        writeOptimisticAccreditation(next, currentUserUuid(getState))
        const patch = dispatch(
          clientApi.util.updateQueryData('getDashboard', undefined, (draft) => {
            if (!draft.data) return
            draft.data.accreditation = { ...draft.data.accreditation, ...next }
          }),
        )
        try {
          await queryFulfilled
        } catch {
          patch.undo()
          clearOptimisticAccreditation()
        }
      },
      invalidatesTags: ['Accreditation', 'Dashboard'],
    }),
    resubmitCompliance: builder.mutation<ApiEnvelope<Entry>, string>({
      query: (uuid) => ({ url: `/entries/${uuid}/compliance/resubmit`, method: 'POST' }),
      invalidatesTags: (_r, _e, uuid) => [{ type: 'Entries', id: uuid }, 'Entries', 'Dashboard'],
    }),
  }),
})

export const {
  useGetDashboardQuery,
  useGetAgenciesQuery,
  useGetCommoditiesQuery,
  useGetEntriesQuery,
  useGetEntryQuery,
  useCreateEntryMutation,
  useUpdateEntryMutation,
  useSubmitEntryMutation,
  useCheckMavNoQuery,
  useUpdateEntryMavMutation,
  useUtilizeEntryMicMutation,
  useGetAccreditationSubmissionsQuery,
  useGetAccreditationSubmissionQuery,
  useCreateAccreditationMutation,
  useSubmitAccreditationMutation,
  useGetCertificatesQuery,
  useGetWarehouseFacilitiesQuery,
  useGetBookingsQuery,
  useCreateBookingMutation,
  useCancelBookingMutation,
  usePayBillMutation,
  usePayBillByTokenMutation,
  useGetBillPaymentOptionsQuery,
  useInitiateBillPaymentMutation,
  useInitiateBillPaymentByTokenMutation,
  useConfirmBillPaymentMutation,
  useConfirmBillPaymentByTokenMutation,
  useGetClientFormsQuery,
  useGetClientFormQuery,
  useGetBillsQuery,
  useGetBillQuery,
  useGetBillByTokenQuery,
  useGetPaymentHistoryQuery,
  useGetCertificateQuery,
  useGetBookingQuery,
  useGetClientProfileQuery,
  useUpdateClientProfileMutation,
  useGetContainersQuery,
  useGetContainerQuery,
  useGetDaBillingsQuery,
  useUploadDaBillingPaymentProofMutation,
  useGetContainerInspectionsQuery,
  useUploadContainerInspectionPhotoMutation,
  useGetInspectionsQuery,
  useGetInspectionQuery,
  useUpdateAccreditationMutation,
  useUploadEntryFileMutation,
  useUploadAccreditationFileMutation,
  useUploadAccreditationComplianceFileMutation,
  useResubmitAccreditationComplianceMutation,
  useUploadComplianceFileMutation,
  useResubmitComplianceMutation,
} = clientApi
