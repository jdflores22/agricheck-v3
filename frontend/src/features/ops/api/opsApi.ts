import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../../auth/types'
import { baseQueryWithReauth } from '../../auth/api/baseQuery'

export interface WarehouseDashboard {
  storedContainers: number
  pendingReleases: number
  releasedToday: number
  activeFacilities: number
}

export interface WarehouseInventoryItem {
  uuid: string
  entryUuid: string
  containerNumber: string
  entryReference: string
  facilityName: string
  locationCode?: string
  status: string
  receivedAt: string
  mavLibraryLinked: boolean
  mavHsCode?: string | null
  mavCommodityName?: string | null
  mavHsLabel?: string | null
  mavCategoryName?: string | null
  volumeKg?: number | null
}

export interface ReleaseAuthorizationItem {
  uuid: string
  entryReference: string
  recipientName: string
  recipientIdNumber: string
  authorizedAt: string
  releaseCount: number
  mavLibraryLinked: boolean
  mavHsCode?: string | null
  mavCommodityName?: string | null
  mavHsLabel?: string | null
}

export interface ContainerItem {
  uuid: string
  containerNumber: string
  entryUuid: string
  entryReference: string
  status: string
  departureTime?: string
  arrivalTime?: string
  lastLatitude?: number
  lastLongitude?: number
  lastLocationAt?: string
  assignedDriverUuid?: string
  assignedDriverName?: string
  assignedVehiclePlate?: string
  mavLibraryLinked?: boolean
  mavHsCode?: string | null
  mavCommodityName?: string | null
  mavHsLabel?: string | null
  mavCategoryName?: string | null
  volumeKg?: number | null
}

export interface OperatorDriver {
  userUuid: string
  fullName: string
  email: string
  phoneNumber?: string
  vehicleType?: string
  vehicleRegistration?: string
  profileComplete: boolean
}

export interface OperatorVehicle {
  uuid: string
  plateNumber: string
  vehicleType: string
  description?: string
  defaultDriverUuid?: string
  defaultDriverName?: string
  isActive: boolean
}

export type OpsContainerListItem = ContainerItem

export interface ContainerDeliveryStop {
  name: string
  latitude: number
  longitude: number
}

export interface ContainerDeliveryLegSummary {
  leg: string
  title: string
  arrivedAtUtc?: string | null
  departedAtUtc?: string | null
  photoRelativeUrl?: string | null
  statusLabel?: string | null
  captureLatitude?: number | null
  captureLongitude?: number | null
}

export interface ContainerTrack {
  containerUuid: string
  containerNumber: string
  entryReference: string
  status: string
  currentLatitude?: number | null
  currentLongitude?: number | null
  pickup: ContainerDeliveryStop
  destination: ContainerDeliveryStop
  trail: { latitude: number; longitude: number; recordedAt: string }[]
  handoffQrAvailable: boolean
  deliveryComplianceHoldActive: boolean
  deliveryComplianceHoldReason?: string | null
  deliverySummary?: ContainerDeliveryLegSummary[] | null
  assignedDriverName?: string | null
  assignedVehiclePlate?: string | null
}

export interface OperatorDeliveryStops {
  containerUuid: string
  containerNumber: string
  pickup: ContainerDeliveryStop
  destination: ContainerDeliveryStop
}

export interface BookOperatorDeliveryStopsBody {
  pickupLabel: string
  pickupLatitude: number
  pickupLongitude: number
  destinationLabel: string
  destinationLatitude: number
  destinationLongitude: number
}

export interface DriverDashboard {
  assignedContainers: number
  inTransitContainers: number
  profileCompletion: number
  faceVerified: boolean
}

export interface OperatorInviteCode {
  code: string
  label?: string
  maxUses: number
  usedCount: number
  expiresAt?: string
  isActive: boolean
  createdAt: string
}

export interface DriverProfile {
  licenseNumber?: string
  licenseExpiryDate?: string
  vehicleType?: string
  vehicleRegistration?: string
  phoneNumber?: string
  emergencyContact?: string
  emergencyPhone?: string
  address?: string
  completionPercentage: number
  faceVerified: boolean
  submittedAt?: string
  approvedAt?: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface WarehouseFacility {
  id: number
  code: string
  name: string
  location?: string
  capacity?: number
}

export interface WarehouseImporterContractListItem {
  allocationUuid?: string | null
  warehouseFacilityId: number
  warehouseCode: string
  warehouseName: string
  importerUserUuid: string
  importerEmail: string
  importerDisplayName: string
  isLinkedImporter: boolean
  contractReference?: string | null
  allocatedCapacityKg?: number | null
  committedKg: number
  headroomKg?: number | null
  allowedCommodityClasses: string[]
  allocationStatus?: string | null
  validFrom?: string | null
  validTo?: string | null
  facilityAllowedCommodityClasses: string[]
  facilityScopeHint?: string | null
  cswLeadAgencyCode?: string | null
}

export interface WarehouseComplianceFacilityListItem {
  warehouseFacilityId: number
  warehouseCode: string
  warehouseName: string
  warehouseLocation?: string | null
  hasActiveWarehouseLto: boolean
  warehouseLtoNumber?: string | null
  warehouseLtoExpiresAt?: string | null
  ltoApplicationStatus: string
  rsasStatus: string
  cswStatus?: string | null
  destinationReady: boolean
}

export interface WarehouseOperatingLicenseSummary {
  licenseNumber: string
  status: string
  issuedAt: string
  expiresAt: string
  notes?: string | null
}

export interface WarehouseLtoApplication {
  uuid: string
  warehouseFacilityId: number
  warehouseCode: string
  warehouseName: string
  operatorLegalName: string
  contactEmail?: string | null
  contactPhone?: string | null
  notes?: string | null
  status: string
  submittedAt: string
  reviewComments?: string | null
  reviewedAt?: string | null
}

export interface WarehouseComplianceDetail {
  warehouseFacilityId: number
  warehouseCode: string
  warehouseName: string
  warehouseLocation?: string | null
  hasActiveWarehouseLto: boolean
  activeLicense?: WarehouseOperatingLicenseSummary | null
  pendingLtoApplication?: WarehouseLtoApplication | null
  latestLtoApplication?: WarehouseLtoApplication | null
  rsasRegistration?: {
    uuid: string
    warehouseFacilityId: number
    status: string
    ownerName: string
    maxCapacityKg?: number | null
    rsasExternalRef?: string | null
    isBmbeExempt: boolean
    productsJson?: string | null
    cswAccreditation?: { leadAgencyCode?: string | null } | null
  } | null
  cswStatus: string
  destinationReady: boolean
}

export const opsApi = createApi({
  reducerPath: 'opsApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['WarehouseDashboard', 'WarehouseInventory', 'WarehouseCompliance', 'WarehouseImporterContracts', 'ReleaseAuth', 'DriverDashboard', 'DriverProfile', 'DriverContainers', 'OperatorContainers', 'OperatorDrivers', 'OperatorVehicles', 'OperatorInviteCodes', 'DoctorContainers'],
  endpoints: (builder) => ({
    getWarehouseDashboard: builder.query<ApiEnvelope<WarehouseDashboard>, void>({
      query: () => '/ops/warehouse/dashboard',
      providesTags: ['WarehouseDashboard'],
    }),
    getWarehouseInventory: builder.query<ApiEnvelope<PagedResult<WarehouseInventoryItem>>, number | void>({
      query: (page = 1) => ({ url: '/ops/warehouse/inventory', params: { page, pageSize: 20 } }),
      providesTags: ['WarehouseInventory'],
    }),
    getReceivableContainers: builder.query<ApiEnvelope<ContainerItem[]>, void>({
      query: () => '/ops/warehouse/inventory/receivable',
    }),
    receiveContainer: builder.mutation<ApiEnvelope<WarehouseInventoryItem>, { containerUuid: string; warehouseFacilityId: number; locationCode?: string }>({
      query: (body) => ({ url: '/ops/warehouse/inventory/receive', method: 'POST', body }),
      invalidatesTags: ['WarehouseDashboard', 'WarehouseInventory'],
    }),
    getReleaseAuthorizations: builder.query<ApiEnvelope<ReleaseAuthorizationItem[]>, void>({
      query: () => '/ops/warehouse/release-authorizations',
      providesTags: ['ReleaseAuth'],
    }),
    createReleaseAuthorization: builder.mutation<ApiEnvelope<ReleaseAuthorizationItem>, { entryUuid: string; recipientName: string; recipientIdNumber: string }>({
      query: (body) => ({ url: '/ops/warehouse/release-authorizations', method: 'POST', body }),
      invalidatesTags: ['ReleaseAuth', 'WarehouseDashboard'],
    }),
    executeRelease: builder.mutation<ApiEnvelope<unknown>, { inventoryUuid: string; authorizationUuid: string; recipientSignaturePath?: string }>({
      query: (body) => ({ url: '/ops/warehouse/releases', method: 'POST', body }),
      invalidatesTags: ['WarehouseDashboard', 'WarehouseInventory', 'ReleaseAuth'],
    }),
    getWarehouseFacilities: builder.query<ApiEnvelope<WarehouseFacility[]>, void>({
      query: () => '/ops/warehouse/facilities',
    }),
    getWarehouseComplianceFacilities: builder.query<ApiEnvelope<WarehouseComplianceFacilityListItem[]>, void>({
      query: () => '/ops/warehouse/compliance/facilities',
      providesTags: ['WarehouseCompliance'],
    }),
    getWarehouseComplianceDetail: builder.query<ApiEnvelope<WarehouseComplianceDetail>, number>({
      query: (warehouseFacilityId) => `/ops/warehouse/compliance/facilities/${warehouseFacilityId}`,
      providesTags: (_r, _e, id) => [{ type: 'WarehouseCompliance', id }],
    }),
    submitWarehouseLtoApplication: builder.mutation<
      ApiEnvelope<WarehouseLtoApplication>,
      {
        warehouseFacilityId: number
        operatorLegalName: string
        contactEmail?: string
        contactPhone?: string
        notes?: string
      }
    >({
      query: (body) => ({ url: '/ops/warehouse/compliance/lto-applications', method: 'POST', body }),
      invalidatesTags: ['WarehouseCompliance'],
    }),
    upsertWarehouseRsas: builder.mutation<
      ApiEnvelope<WarehouseComplianceDetail['rsasRegistration'] & { uuid: string }>,
      {
        warehouseFacilityId: number
        ownerName: string
        rsasExternalRef?: string
        maxCapacityKg?: number
        isBmbeExempt?: boolean
        productsJson?: string
      }
    >({
      query: (body) => ({ url: '/ops/warehouse/compliance/rsas', method: 'PUT', body }),
      invalidatesTags: ['WarehouseCompliance'],
    }),
    submitWarehouseRsas: builder.mutation<ApiEnvelope<unknown>, string>({
      query: (uuid) => ({ url: `/ops/warehouse/compliance/rsas/${uuid}/submit`, method: 'POST' }),
      invalidatesTags: ['WarehouseCompliance'],
    }),
    getWarehouseImporterContracts: builder.query<
      ApiEnvelope<WarehouseImporterContractListItem[]>,
      number | void
    >({
      query: (warehouseFacilityId) => ({
        url: '/ops/warehouse/importer-contracts',
        params: warehouseFacilityId ? { warehouseFacilityId } : undefined,
      }),
      providesTags: ['WarehouseImporterContracts'],
    }),
    upsertWarehouseImporterContract: builder.mutation<
      ApiEnvelope<WarehouseImporterContractListItem>,
      {
        allocationUuid?: string | null
        warehouseFacilityId: number
        importerUserUuid: string
        contractReference: string
        allocatedCapacityKg: number
        allowedCommodityClasses: string[]
        status?: string
      }
    >({
      query: (body) => ({ url: '/ops/warehouse/importer-contracts', method: 'PUT', body }),
      invalidatesTags: ['WarehouseImporterContracts'],
    }),
    getDriverDashboard: builder.query<ApiEnvelope<DriverDashboard>, void>({
      query: () => '/ops/driver/dashboard',
      providesTags: ['DriverDashboard'],
    }),
    getDriverProfile: builder.query<ApiEnvelope<DriverProfile>, void>({
      query: () => '/ops/driver/profile',
      providesTags: ['DriverProfile'],
    }),
    updateDriverProfile: builder.mutation<ApiEnvelope<DriverProfile>, Partial<DriverProfile>>({
      query: (body) => ({ url: '/ops/driver/profile', method: 'PUT', body }),
      invalidatesTags: ['DriverProfile', 'DriverDashboard'],
    }),
    getDriverContainers: builder.query<ApiEnvelope<ContainerItem[]>, void>({
      query: () => '/ops/driver/containers',
      providesTags: ['DriverContainers'],
    }),
    getDriverContainerTrack: builder.query<ApiEnvelope<ContainerTrack>, string>({
      query: (uuid) => `/ops/driver/containers/${uuid}/track`,
      providesTags: (_result, _error, uuid) => [{ type: 'DriverContainers', id: uuid }],
    }),
    updateContainerStatus: builder.mutation<ApiEnvelope<ContainerItem>, { uuid: string; status: string }>({
      query: ({ uuid, status }) => ({ url: `/ops/driver/containers/${uuid}/status`, method: 'PUT', body: { status } }),
      invalidatesTags: ['DriverContainers', 'DriverDashboard'],
    }),
    recordContainerLocation: builder.mutation<ApiEnvelope<unknown>, { uuid: string; latitude: number; longitude: number }>({
      query: ({ uuid, latitude, longitude }) => ({
        url: `/ops/driver/containers/${uuid}/location`,
        method: 'POST',
        body: { latitude, longitude },
      }),
      invalidatesTags: ['DriverContainers'],
    }),
    getClaimableContainers: builder.query<ApiEnvelope<OpsContainerListItem[]>, string | void>({
      query: (search) => ({
        url: '/ops/operator/containers/claimable',
        params: search ? { search } : undefined,
      }),
      providesTags: ['OperatorContainers'],
    }),
    getClaimedContainers: builder.query<ApiEnvelope<OpsContainerListItem[]>, void>({
      query: () => '/ops/operator/containers/claimed',
      providesTags: ['OperatorContainers'],
    }),
    claimOperatorContainer: builder.mutation<ApiEnvelope<OpsContainerListItem>, string>({
      query: (uuid) => ({ url: `/ops/operator/containers/${uuid}/claim`, method: 'POST' }),
      invalidatesTags: ['OperatorContainers'],
    }),
    provisionRouteTestContainer: builder.mutation<ApiEnvelope<OpsContainerListItem>, void>({
      query: () => ({ url: '/ops/operator/containers/route-test-provision', method: 'POST' }),
      invalidatesTags: ['OperatorContainers'],
    }),
    scanAndClaimOperatorContainer: builder.mutation<ApiEnvelope<OpsContainerListItem>, { qrData: string }>({
      query: (body) => ({ url: '/ops/operator/containers/scan', method: 'POST', body }),
      invalidatesTags: ['OperatorContainers'],
    }),
    getOperatorDrivers: builder.query<ApiEnvelope<OperatorDriver[]>, void>({
      query: () => '/ops/operator/drivers',
      providesTags: ['OperatorDrivers'],
    }),
    getOperatorVehicles: builder.query<ApiEnvelope<OperatorVehicle[]>, void>({
      query: () => '/ops/operator/vehicles',
      providesTags: ['OperatorVehicles'],
    }),
    createOperatorVehicle: builder.mutation<
      ApiEnvelope<OperatorVehicle>,
      { plateNumber: string; vehicleType: string; description?: string; defaultDriverUuid?: string }
    >({
      query: (body) => ({ url: '/ops/operator/vehicles', method: 'POST', body }),
      invalidatesTags: ['OperatorVehicles'],
    }),
    getOperatorInviteCodes: builder.query<ApiEnvelope<OperatorInviteCode[]>, void>({
      query: () => '/ops/operator/invite-codes',
      providesTags: ['OperatorInviteCodes'],
    }),
    createOperatorInviteCode: builder.mutation<ApiEnvelope<OperatorInviteCode>, { label?: string }>({
      query: (body) => ({ url: '/ops/operator/invite-codes', method: 'POST', body }),
      invalidatesTags: ['OperatorInviteCodes'],
    }),
    assignDriverToContainer: builder.mutation<
      ApiEnvelope<OpsContainerListItem>,
      { uuid: string; driverUserUuid: string; vehicleUuid?: string }
    >({
      query: ({ uuid, driverUserUuid, vehicleUuid }) => ({
        url: `/ops/operator/containers/${uuid}/assign-driver`,
        method: 'POST',
        body: { driverUserUuid, vehicleUuid },
      }),
      invalidatesTags: ['OperatorContainers', 'DriverContainers'],
    }),
    getOperatorDeliveryStops: builder.query<ApiEnvelope<OperatorDeliveryStops>, string>({
      query: (uuid) => `/ops/operator/containers/${uuid}/delivery-stops`,
    }),
    bookOperatorDeliveryStops: builder.mutation<
      ApiEnvelope<OperatorDeliveryStops>,
      { uuid: string; body: BookOperatorDeliveryStopsBody }
    >({
      query: ({ uuid, body }) => ({
        url: `/ops/operator/containers/${uuid}/delivery-stops`,
        method: 'PUT',
        body,
      }),
      invalidatesTags: ['OperatorContainers'],
    }),
    getDoctorContainers: builder.query<ApiEnvelope<OpsContainerListItem[]>, void>({
      query: () => '/ops/doctor/containers',
      providesTags: ['DoctorContainers'],
    }),
    claimDoctorContainer: builder.mutation<ApiEnvelope<OpsContainerListItem>, string>({
      query: (uuid) => ({ url: `/ops/doctor/containers/${uuid}/claim`, method: 'POST' }),
      invalidatesTags: ['DoctorContainers'],
    }),
    scanDoctorHandoffQr: builder.mutation<ApiEnvelope<OpsContainerListItem>, { qrData: string }>({
      query: (body) => ({ url: '/ops/doctor/containers/scan-handoff', method: 'POST', body }),
      invalidatesTags: ['DoctorContainers'],
    }),
    completeDoctorInspection: builder.mutation<
      ApiEnvelope<OpsContainerListItem>,
      { uuid: string; decision: 'Approved' | 'Rejected'; findings?: string }
    >({
      query: ({ uuid, decision, findings }) => ({
        url: `/ops/doctor/containers/${uuid}/complete`,
        method: 'POST',
        body: { decision, findings },
      }),
      invalidatesTags: ['DoctorContainers'],
    }),
  }),
})

export const {
  useGetWarehouseDashboardQuery,
  useGetWarehouseInventoryQuery,
  useGetReceivableContainersQuery,
  useReceiveContainerMutation,
  useGetReleaseAuthorizationsQuery,
  useCreateReleaseAuthorizationMutation,
  useExecuteReleaseMutation,
  useGetWarehouseFacilitiesQuery,
  useGetWarehouseComplianceFacilitiesQuery,
  useGetWarehouseComplianceDetailQuery,
  useSubmitWarehouseLtoApplicationMutation,
  useUpsertWarehouseRsasMutation,
  useSubmitWarehouseRsasMutation,
  useGetWarehouseImporterContractsQuery,
  useUpsertWarehouseImporterContractMutation,
  useGetDriverDashboardQuery,
  useGetDriverProfileQuery,
  useUpdateDriverProfileMutation,
  useGetDriverContainersQuery,
  useGetDriverContainerTrackQuery,
  useUpdateContainerStatusMutation,
  useRecordContainerLocationMutation,
  useGetClaimableContainersQuery,
  useGetClaimedContainersQuery,
  useGetOperatorDriversQuery,
  useGetOperatorVehiclesQuery,
  useCreateOperatorVehicleMutation,
  useGetOperatorInviteCodesQuery,
  useCreateOperatorInviteCodeMutation,
  useClaimOperatorContainerMutation,
  useProvisionRouteTestContainerMutation,
  useScanAndClaimOperatorContainerMutation,
  useAssignDriverToContainerMutation,
  useGetOperatorDeliveryStopsQuery,
  useBookOperatorDeliveryStopsMutation,
  useGetDoctorContainersQuery,
  useClaimDoctorContainerMutation,
  useScanDoctorHandoffQrMutation,
  useCompleteDoctorInspectionMutation,
} = opsApi
