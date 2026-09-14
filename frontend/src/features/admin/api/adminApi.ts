import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'
import type { PagedResult } from '../../client/api/clientApi'
import { systemBrandingApi } from '../../system/systemBrandingApi'

export interface AdminDashboard {
  totalUsers: number
  activeUsers: number
  totalAgencies: number
  totalEntries: number
  totalCertificates: number
  activeCertificates: number
  formTemplates: number
  certificateTemplates: number
  recentAuditLogs: number
}

export interface AdminUserListItem {
  uuid: string
  email: string
  status: string
  fullName: string
  roles: string[]
  createdAt: string
  lastLoginAt?: string
}

export interface AdminUserDetail {
  uuid: string
  email: string
  status: string
  firstName: string
  lastName: string
  phone?: string
  companyName?: string
  roles: string[]
  agencyIds: number[]
  roleAgencies: Record<string, number[]>
  createdAt: string
  lastLoginAt?: string
}

export interface AdminAgency {
  id: number
  code: string
  name: string
  description?: string | null
  parentId?: number | null
  parentName?: string | null
  isActive: boolean
  memberCount: number
}

export interface AdminAgencyUser {
  userUuid: string
  fullName: string
  email: string
  roles: string[]
  assignedAt: string
}

export interface AdminAgencyLeadershipUser {
  userUuid: string
  fullName: string
  email: string
  assignedAt: string
}

export interface AdminAgencyDetail {
  id: number
  code: string
  name: string
  description?: string | null
  address?: string | null
  contactNumber?: string | null
  email?: string | null
  logoUrl?: string | null
  parentId?: number | null
  parentName?: string | null
  isActive: boolean
  totalUsers: number
  totalEntries: number
  secretary?: AdminAgencyLeadershipUser | null
  undersecretaries: AdminAgencyLeadershipUser[]
  users: AdminAgencyUser[]
}

export interface SaveAdminAgencyRequest {
  code: string
  name: string
  parentId?: number | null
  description?: string | null
  address?: string | null
  contactNumber?: string | null
  email?: string | null
  isActive?: boolean
}

export interface AdminCommodity {
  id: number
  code: string
  name: string
  categoryName: string
  isActive: boolean
}

export interface ProcessingFeeConfig {
  id: number
  agencyId: number
  agencyCode: string
  entryType: string
  amount: number
  currency: string
  isActive: boolean
}

export interface PaymentGatewaySettings {
  enabled: boolean
  mode: string
  hasApiKey: boolean
  apiKeyMasked: string
  hasWebhookSecret: boolean
  publicKey?: string | null
}

export interface GlobalEntryProcessingFee {
  entryType: string
  amount: number
  currency: string
}

export interface AdminPaymentSettings {
  processingFees: GlobalEntryProcessingFee[]
}

export interface UpdateAdminPaymentSettingsRequest {
  importFeeAmount: number
  exportFeeAmount: number
  currency: string
}

export interface AdminEntryPaymentListItem {
  id: number
  billUuid: string
  billNumber: string
  entryReferenceNo?: string | null
  agencyCode?: string | null
  clientName: string
  amount: number
  paymentMethod: string
  status: string
  externalReference?: string | null
  gatewayTransactionId?: string | null
  createdAt: string
  paidAt?: string | null
}

export interface AdminPendingEntryCashPayment {
  billUuid: string
  billNumber: string
  entryReferenceNo?: string | null
  agencyCode?: string | null
  clientName: string
  amount: number
  externalReference?: string | null
  submittedAt: string
}

export interface AdminRevenueAgencyBreakdown {
  agencyCode: string
  agencyName: string
  amount: number
  paymentCount: number
}

export interface AdminRevenueMonthlyBreakdown {
  month: string
  amount: number
  paymentCount: number
}

export interface AdminRevenueSummary {
  totalCollected: number
  pendingAmount: number
  paidCount: number
  pendingCount: number
  failedCount: number
  byAgency: AdminRevenueAgencyBreakdown[]
  byMonth: AdminRevenueMonthlyBreakdown[]
}

export interface FormTemplateListItem {
  uuid: string
  name: string
  formType: string
  status: string
  isActive: boolean
  latestVersion: number
  hasPublishedVersion: boolean
  agencyIds: number[]
  fieldCount: number
  createdAt: string
  submissionCount: number
}

export interface FormTemplateVersionSummary {
  versionNumber: number
  isPublished: boolean
  createdAt: string
}

export interface FormTemplateDetail extends FormTemplateListItem {
  schemaJson: string
  versionNumber: number
  agencyIds: number[]
  updatedAt: string
  versions: FormTemplateVersionSummary[]
}

export interface CertificateTemplateListItem {
  uuid: string
  name: string
  description?: string
  agencyCode?: string
  isActive: boolean
  latestVersion: number
  hasPublishedVersion: boolean
  elementCount: number
  processTypes: string[]
  createdAt: string
}

export interface CertificateElement {
  id: number
  elementType: string
  label: string
  configJson?: string
  sortOrder: number
}

export interface CertificateTemplateVersionSummary {
  versionNumber: number
  isPublished: boolean
  createdAt: string
}

export interface CertificateTemplateDetail {
  uuid: string
  name: string
  description?: string
  agencyId?: number
  isActive: boolean
  versionNumber: number
  isPublished: boolean
  layoutJson?: string
  processTypes: string[]
  elements: CertificateElement[]
  createdAt: string
  updatedAt: string
  versions: CertificateTemplateVersionSummary[]
}

export const CERTIFICATE_PROCESS_TYPES = ['Accreditation', 'ImportEntry', 'ExportEntry'] as const

export interface AdminCertificateListItem {
  uuid: string
  certificateNumber: string
  title: string
  status: string
  holderName: string
  entryReferenceNo?: string
  issuedAt: string
  expiresAt?: string
  revokedAt?: string
}

export interface AdminApprovedEntry {
  uuid: string
  referenceNo: string
  applicantName: string
  agencyCode: string
  hasActiveCertificate: boolean
}

export interface AuditLogListItem {
  id: number
  action: string
  entityType: string
  entityId?: string
  actorName?: string
  createdAt: string
}

export interface AdminRole {
  code: string
  name: string
  description?: string
  requiresAgency: boolean
}

export interface AdminSettings {
  settings: Record<string, string>
}

export type AdminSettingsTab =
  | 'general'
  | 'branding'
  | 'email'
  | 'files'
  | 'security'
  | 'notifications'
  | 'forms'
  | 'accreditation'
  | 'maintenance'

export const adminSettingsGroups: Record<AdminSettingsTab, string[]> = {
  general: ['system_name', 'system_description', 'contact_email', 'support_phone', 'timezone', 'date_format'],
  branding: ['primary_color', 'secondary_color', 'footer_text', 'spinner_color', 'system_logo_path', 'spinner_logo_path', 'favicon_path'],
  email: ['smtp_host', 'smtp_port', 'smtp_encryption', 'smtp_username', 'from_email', 'from_name'],
  files: ['max_file_size', 'allowed_file_types', 'file_retention_days'],
  security: ['session_timeout', 'max_login_attempts', 'min_password_length', 'require_uppercase', 'require_numbers', 'require_special_chars'],
  notifications: ['enable_email_notifications', 'notify_on_submission', 'notify_on_approval', 'enable_inapp_notifications'],
  forms: ['autosave_interval', 'required_indicator'],
  accreditation: ['accreditation_validity_days', 'renewal_reminder_days', 'compliance_deadline_days'],
  maintenance: ['maintenance_mode', 'maintenance_message', 'maintenance_allowed_ips'],
}

export const adminSettingLabels: Record<string, string> = {
  system_name: 'System Name',
  system_description: 'System Description',
  contact_email: 'Contact Email',
  support_phone: 'Support Phone',
  timezone: 'Timezone',
  date_format: 'Date Format',
  primary_color: 'Primary Color',
  secondary_color: 'Secondary Color',
  footer_text: 'Footer Text',
  spinner_color: 'Spinner Color',
  system_logo_path: 'System Logo Path',
  spinner_logo_path: 'Spinner Logo Path',
  favicon_path: 'Favicon Path',
  smtp_host: 'SMTP Host',
  smtp_port: 'SMTP Port',
  smtp_encryption: 'SMTP Encryption',
  smtp_username: 'SMTP Username',
  from_email: 'From Email',
  from_name: 'From Name',
  max_file_size: 'Max File Size (MB)',
  allowed_file_types: 'Allowed File Types',
  file_retention_days: 'File Retention (days)',
  session_timeout: 'Session Timeout (minutes)',
  max_login_attempts: 'Max Login Attempts',
  min_password_length: 'Minimum Password Length',
  require_uppercase: 'Require Uppercase',
  require_numbers: 'Require Numbers',
  require_special_chars: 'Require Special Characters',
  enable_email_notifications: 'Enable Email Notifications',
  notify_on_submission: 'Notify on Submission',
  notify_on_approval: 'Notify on Approval',
  enable_inapp_notifications: 'Enable In-App Notifications',
  autosave_interval: 'Autosave Interval (seconds)',
  required_indicator: 'Required Field Indicator',
  accreditation_validity_days: 'Accreditation Validity (days)',
  renewal_reminder_days: 'Renewal Reminder (days)',
  compliance_deadline_days: 'Compliance Deadline (days)',
  maintenance_mode: 'Maintenance Mode',
  maintenance_message: 'Maintenance Message',
  maintenance_allowed_ips: 'Allowed IPs During Maintenance',
}

export const adminApi = createApi({
  reducerPath: 'adminApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['AdminDashboard', 'AdminUsers', 'AdminUser', 'AdminAgencies', 'AdminCommodities', 'AdminPayment', 'AdminEntryPayments', 'AdminRevenue', 'AdminForms', 'AdminCertTemplates', 'AdminCertificates', 'AdminAudit', 'AdminRoles', 'AdminSettings'],
  endpoints: (builder) => ({
    getAdminDashboard: builder.query<ApiEnvelope<AdminDashboard>, void>({
      query: () => '/admin/dashboard',
      providesTags: ['AdminDashboard'],
    }),
    getAdminUsers: builder.query<ApiEnvelope<PagedResult<AdminUserListItem>>, { page?: number; pageSize?: number }>({
      query: ({ page = 1, pageSize = 20 }) => `/admin/users?page=${page}&pageSize=${pageSize}`,
      providesTags: ['AdminUsers'],
    }),
    getAdminUser: builder.query<ApiEnvelope<AdminUserDetail>, string>({
      query: (uuid) => `/admin/users/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'AdminUser', id: uuid }, 'AdminUsers'],
    }),
    createAdminUser: builder.mutation<ApiEnvelope<AdminUserListItem>, { email: string; password: string; firstName: string; lastName: string; roleCodes: string[]; agencyId?: number; roleAgencies?: Record<string, number[]> }>({
      query: (body) => ({ url: '/admin/users', method: 'POST', body }),
      invalidatesTags: ['AdminUsers', 'AdminDashboard'],
    }),
    updateAdminUser: builder.mutation<ApiEnvelope<AdminUserListItem>, { uuid: string; firstName?: string; lastName?: string; status?: string; roleCodes?: string[]; agencyId?: number | null; roleAgencies?: Record<string, number[]> }>({
      query: ({ uuid, ...body }) => ({ url: `/admin/users/${uuid}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, { uuid }) => [{ type: 'AdminUser', id: uuid }, 'AdminUsers', 'AdminDashboard'],
    }),
    getAdminRoles: builder.query<ApiEnvelope<AdminRole[]>, void>({
      query: () => '/admin/roles',
      providesTags: ['AdminRoles'],
    }),
    getAdminSettings: builder.query<ApiEnvelope<AdminSettings>, void>({
      query: () => '/admin/settings',
      providesTags: ['AdminSettings'],
    }),
    updateAdminSettings: builder.mutation<ApiEnvelope<AdminSettings>, { settings: Record<string, string> }>({
      query: (body) => ({ url: '/admin/settings', method: 'PUT', body }),
      invalidatesTags: ['AdminSettings'],
    }),
    uploadBrandingAsset: builder.mutation<ApiEnvelope<AdminSettings>, { assetType: 'system-logo' | 'spinner-logo' | 'favicon'; file: File }>({
      query: ({ assetType, file }) => {
        const body = new FormData()
        body.append('file', file)
        return { url: `/admin/settings/branding/${assetType}`, method: 'POST', body }
      },
      invalidatesTags: ['AdminSettings'],
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        await queryFulfilled
        dispatch(systemBrandingApi.util.invalidateTags(['SystemBranding']))
      },
    }),
    getAdminAgencies: builder.query<ApiEnvelope<AdminAgency[]>, void>({
      query: () => '/admin/agencies',
      providesTags: ['AdminAgencies'],
    }),
    getAdminAgency: builder.query<ApiEnvelope<AdminAgencyDetail>, number>({
      query: (id) => `/admin/agencies/${id}`,
      providesTags: (_r, _e, id) => [{ type: 'AdminAgencies', id }],
    }),
    createAdminAgency: builder.mutation<ApiEnvelope<AdminAgency>, SaveAdminAgencyRequest>({
      query: (body) => ({ url: '/admin/agencies', method: 'POST', body }),
      invalidatesTags: ['AdminAgencies', 'AdminDashboard'],
    }),
    updateAdminAgency: builder.mutation<ApiEnvelope<AdminAgency>, { id: number } & SaveAdminAgencyRequest>({
      query: ({ id, ...body }) => ({ url: `/admin/agencies/${id}`, method: 'PUT', body }),
      invalidatesTags: (_r, _e, { id }) => [{ type: 'AdminAgencies', id }, 'AdminAgencies', 'AdminDashboard'],
    }),
    deleteAdminAgency: builder.mutation<ApiEnvelope<{ deleted: boolean }>, number>({
      query: (id) => ({ url: `/admin/agencies/${id}`, method: 'DELETE' }),
      invalidatesTags: ['AdminAgencies', 'AdminDashboard'],
    }),
    uploadAdminAgencyLogo: builder.mutation<ApiEnvelope<AdminAgencyDetail>, { id: number; file: File }>({
      query: ({ id, file }) => {
        const body = new FormData()
        body.append('file', file)
        return { url: `/admin/agencies/${id}/logo`, method: 'POST', body }
      },
      invalidatesTags: (_r, _e, { id }) => [{ type: 'AdminAgencies', id }, 'AdminAgencies'],
    }),
    assignAgencySecretary: builder.mutation<ApiEnvelope<AdminAgencyDetail>, { id: number; userUuid?: string | null }>({
      query: ({ id, userUuid }) => ({
        url: `/admin/agencies/${id}/leadership/secretary`,
        method: 'PUT',
        body: { userUuid: userUuid ?? null },
      }),
      invalidatesTags: (_r, _e, { id }) => [{ type: 'AdminAgencies', id }, 'AdminAgencies', 'AdminUsers'],
    }),
    assignAgencyUndersecretaries: builder.mutation<ApiEnvelope<AdminAgencyDetail>, { id: number; userUuids: string[] }>({
      query: ({ id, userUuids }) => ({
        url: `/admin/agencies/${id}/leadership/undersecretaries`,
        method: 'PUT',
        body: { userUuids },
      }),
      invalidatesTags: (_r, _e, { id }) => [{ type: 'AdminAgencies', id }, 'AdminAgencies', 'AdminUsers'],
    }),
    getAdminCommodities: builder.query<ApiEnvelope<AdminCommodity[]>, void>({
      query: () => '/admin/commodities',
      providesTags: ['AdminCommodities'],
    }),
    createAdminCommodity: builder.mutation<ApiEnvelope<AdminCommodity>, { categoryId: number; code: string; name: string }>({
      query: (body) => ({ url: '/admin/commodities', method: 'POST', body }),
      invalidatesTags: ['AdminCommodities'],
    }),
    getAdminPaymentConfigs: builder.query<ApiEnvelope<ProcessingFeeConfig[]>, void>({
      query: () => '/admin/payment-configs',
      providesTags: ['AdminPayment'],
    }),
    getAdminPaymentSettings: builder.query<ApiEnvelope<AdminPaymentSettings>, void>({
      query: () => '/admin/payment-configs/settings',
      providesTags: ['AdminPayment'],
    }),
    updateAdminPaymentSettings: builder.mutation<ApiEnvelope<AdminPaymentSettings>, UpdateAdminPaymentSettingsRequest>({
      query: (body) => ({ url: '/admin/payment-configs/settings', method: 'PUT', body }),
      invalidatesTags: ['AdminPayment', 'AdminRevenue', 'AdminEntryPayments'],
    }),
    upsertAdminPaymentConfig: builder.mutation<ApiEnvelope<ProcessingFeeConfig>, { agencyId: number; entryType: string; amount: number; currency: string; isActive: boolean }>({
      query: (body) => ({ url: '/admin/payment-configs', method: 'POST', body }),
      invalidatesTags: ['AdminPayment'],
    }),
    getAdminEntryPayments: builder.query<ApiEnvelope<PagedResult<AdminEntryPaymentListItem>>, { page?: number; status?: string }>({
      query: ({ page = 1, status }) => {
        const params = new URLSearchParams({ page: String(page) })
        if (status) params.set('status', status)
        return `/admin/entry-payments?${params.toString()}`
      },
      providesTags: ['AdminEntryPayments'],
    }),
    getAdminRevenueSummary: builder.query<ApiEnvelope<AdminRevenueSummary>, void>({
      query: () => '/admin/entry-payments/revenue',
      providesTags: ['AdminRevenue'],
    }),
    getAdminPendingEntryCashPayments: builder.query<ApiEnvelope<AdminPendingEntryCashPayment[]>, void>({
      query: () => '/admin/entry-payments/pending-cash',
      providesTags: ['AdminEntryPayments'],
    }),
    verifyAdminEntryCashPayment: builder.mutation<ApiEnvelope<{ verified: boolean }>, { billUuid: string; approved: boolean; notes?: string }>({
      query: ({ billUuid, approved, notes }) => ({
        url: `/admin/entry-payments/pending-cash/${billUuid}/verify`,
        method: 'POST',
        body: { approved, notes },
      }),
      invalidatesTags: ['AdminEntryPayments', 'AdminRevenue'],
    }),
    getAdminForms: builder.query<ApiEnvelope<FormTemplateListItem[]>, void>({
      query: () => '/admin/forms',
      providesTags: ['AdminForms'],
    }),
    getAdminForm: builder.query<ApiEnvelope<FormTemplateDetail>, string>({
      query: (uuid) => `/admin/forms/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'AdminForms', id: uuid }],
    }),
    setAdminFormActive: builder.mutation<ApiEnvelope<FormTemplateDetail>, { uuid: string; isActive: boolean }>({
      query: ({ uuid, isActive }) => ({
        url: `/admin/forms/${uuid}/activate`,
        method: 'POST',
        body: { isActive },
      }),
      invalidatesTags: (_r, _e, { uuid }) => ['AdminForms', { type: 'AdminForms', id: uuid }, 'AdminDashboard'],
    }),
    cloneAdminForm: builder.mutation<ApiEnvelope<FormTemplateDetail>, { uuid: string; name: string }>({
      query: ({ uuid, name }) => ({
        url: `/admin/forms/${uuid}/clone`,
        method: 'POST',
        body: { name },
      }),
      invalidatesTags: ['AdminForms', 'AdminDashboard'],
    }),
    deleteAdminForm: builder.mutation<ApiEnvelope<{ deleted: boolean }>, string>({
      query: (uuid) => ({
        url: `/admin/forms/${uuid}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['AdminForms', 'AdminDashboard'],
    }),
    importAdminForm: builder.mutation<ApiEnvelope<FormTemplateDetail>, Record<string, unknown>>({
      query: (body) => ({
        url: '/admin/forms/import',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['AdminForms', 'AdminDashboard'],
    }),
    saveAdminForm: builder.mutation<ApiEnvelope<FormTemplateDetail>, { uuid?: string; name: string; formType: string; schemaJson: string; agencyIds: number[]; publish: boolean }>({
      query: ({ uuid, ...body }) => ({
        url: uuid ? `/admin/forms/${uuid}` : '/admin/forms',
        method: uuid ? 'PUT' : 'POST',
        body,
      }),
      invalidatesTags: (_r, _e, arg) => [
        'AdminForms',
        'AdminDashboard',
        ...(arg.uuid ? [{ type: 'AdminForms' as const, id: arg.uuid }] : []),
      ],
    }),
    getAdminCertificateTemplates: builder.query<ApiEnvelope<CertificateTemplateListItem[]>, void>({
      query: () => '/admin/certificate-templates',
      providesTags: ['AdminCertTemplates'],
    }),
    getAdminCertificateTemplate: builder.query<ApiEnvelope<CertificateTemplateDetail>, string>({
      query: (uuid) => `/admin/certificate-templates/${uuid}`,
      providesTags: (_r, _e, uuid) => [{ type: 'AdminCertTemplates', id: uuid }],
    }),
    saveAdminCertificateTemplate: builder.mutation<
      ApiEnvelope<CertificateTemplateDetail>,
      {
        uuid?: string
        name: string
        description?: string
        agencyId?: number
        elements: Array<{ elementType: string; label: string; configJson?: string; sortOrder: number }>
        processTypes?: string[]
        publish: boolean
        isActive?: boolean
        layoutJson?: string
      }
    >({
      query: ({ uuid, ...body }) => ({
        url: uuid ? `/admin/certificate-templates/${uuid}` : '/admin/certificate-templates',
        method: uuid ? 'PUT' : 'POST',
        body,
      }),
      invalidatesTags: ['AdminCertTemplates', 'AdminDashboard'],
    }),
    cloneAdminCertificateTemplate: builder.mutation<ApiEnvelope<CertificateTemplateDetail>, { uuid: string; name: string }>({
      query: ({ uuid, name }) => ({
        url: `/admin/certificate-templates/${uuid}/clone`,
        method: 'POST',
        body: { name },
      }),
      invalidatesTags: ['AdminCertTemplates', 'AdminDashboard'],
    }),
    importAdminCertificateTemplate: builder.mutation<ApiEnvelope<CertificateTemplateDetail>, Record<string, unknown>>({
      query: (body) => ({
        url: '/admin/certificate-templates/import',
        method: 'POST',
        body,
      }),
      invalidatesTags: ['AdminCertTemplates', 'AdminDashboard'],
    }),
    setAdminCertificateTemplateActive: builder.mutation<ApiEnvelope<CertificateTemplateDetail>, { uuid: string; isActive: boolean }>({
      query: ({ uuid, isActive }) => ({
        url: `/admin/certificate-templates/${uuid}/activate`,
        method: 'POST',
        body: { isActive },
      }),
      invalidatesTags: (_r, _e, { uuid }) => ['AdminCertTemplates', { type: 'AdminCertTemplates', id: uuid }, 'AdminDashboard'],
    }),
    deleteAdminCertificateTemplate: builder.mutation<ApiEnvelope<{ deleted: boolean }>, string>({
      query: (uuid) => ({
        url: `/admin/certificate-templates/${uuid}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['AdminCertTemplates', 'AdminDashboard'],
    }),
    getAdminCertificates: builder.query<ApiEnvelope<PagedResult<AdminCertificateListItem>>, { page?: number }>({
      query: ({ page = 1 }) => `/admin/certificates?page=${page}`,
      providesTags: ['AdminCertificates'],
    }),
    getAdminApprovedEntries: builder.query<ApiEnvelope<PagedResult<AdminApprovedEntry>>, { page?: number }>({
      query: ({ page = 1 }) => `/admin/certificates/approved-entries?page=${page}`,
      providesTags: ['AdminCertificates'],
    }),
    issueAdminCertificate: builder.mutation<ApiEnvelope<AdminCertificateListItem>, { entryUuid: string; templateId?: number; expiresAt?: string }>({
      query: (body) => ({ url: '/admin/certificates/issue', method: 'POST', body }),
      invalidatesTags: ['AdminCertificates', 'AdminDashboard'],
    }),
    revokeAdminCertificate: builder.mutation<ApiEnvelope<AdminCertificateListItem>, { uuid: string; reason: string }>({
      query: ({ uuid, reason }) => ({ url: `/admin/certificates/${uuid}/revoke`, method: 'POST', body: { reason } }),
      invalidatesTags: ['AdminCertificates', 'AdminDashboard'],
    }),
    getAdminAuditLogs: builder.query<ApiEnvelope<PagedResult<AuditLogListItem>>, { page?: number }>({
      query: ({ page = 1 }) => `/admin/audit-logs?page=${page}`,
      providesTags: ['AdminAudit'],
    }),
  }),
})

export const {
  useGetAdminDashboardQuery,
  useGetAdminUsersQuery,
  useGetAdminUserQuery,
  useCreateAdminUserMutation,
  useUpdateAdminUserMutation,
  useGetAdminRolesQuery,
  useGetAdminSettingsQuery,
  useUpdateAdminSettingsMutation,
  useUploadBrandingAssetMutation,
  useGetAdminAgenciesQuery,
  useGetAdminAgencyQuery,
  useCreateAdminAgencyMutation,
  useUpdateAdminAgencyMutation,
  useDeleteAdminAgencyMutation,
  useUploadAdminAgencyLogoMutation,
  useAssignAgencySecretaryMutation,
  useAssignAgencyUndersecretariesMutation,
  useGetAdminCommoditiesQuery,
  useCreateAdminCommodityMutation,
  useGetAdminPaymentConfigsQuery,
  useGetAdminPaymentSettingsQuery,
  useUpdateAdminPaymentSettingsMutation,
  useUpsertAdminPaymentConfigMutation,
  useGetAdminEntryPaymentsQuery,
  useGetAdminPendingEntryCashPaymentsQuery,
  useVerifyAdminEntryCashPaymentMutation,
  useGetAdminRevenueSummaryQuery,
  useGetAdminFormsQuery,
  useGetAdminFormQuery,
  useSaveAdminFormMutation,
  useSetAdminFormActiveMutation,
  useCloneAdminFormMutation,
  useDeleteAdminFormMutation,
  useImportAdminFormMutation,
  useGetAdminCertificateTemplatesQuery,
  useGetAdminCertificateTemplateQuery,
  useSaveAdminCertificateTemplateMutation,
  useCloneAdminCertificateTemplateMutation,
  useImportAdminCertificateTemplateMutation,
  useSetAdminCertificateTemplateActiveMutation,
  useDeleteAdminCertificateTemplateMutation,
  useGetAdminCertificatesQuery,
  useGetAdminApprovedEntriesQuery,
  useIssueAdminCertificateMutation,
  useRevokeAdminCertificateMutation,
  useGetAdminAuditLogsQuery,
} = adminApi
