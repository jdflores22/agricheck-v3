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
  containerNumber: string
  entryReference: string
  facilityName: string
  locationCode?: string
  status: string
  receivedAt: string
}

export interface ReleaseAuthorizationItem {
  uuid: string
  entryReference: string
  recipientName: string
  recipientIdNumber: string
  authorizedAt: string
  releaseCount: number
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
}

export type OpsContainerListItem = ContainerItem

export interface DriverDashboard {
  assignedContainers: number
  inTransitContainers: number
  profileCompletion: number
  faceVerified: boolean
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

export const opsApi = createApi({
  reducerPath: 'opsApi',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['WarehouseDashboard', 'WarehouseInventory', 'ReleaseAuth', 'DriverDashboard', 'DriverProfile', 'DriverContainers', 'OperatorContainers', 'DoctorContainers'],
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
      query: () => '/warehouse/facilities',
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
    getClaimableContainers: builder.query<ApiEnvelope<OpsContainerListItem[]>, void>({
      query: () => '/ops/operator/containers/claimable',
      providesTags: ['OperatorContainers'],
    }),
    claimOperatorContainer: builder.mutation<ApiEnvelope<OpsContainerListItem>, string>({
      query: (uuid) => ({ url: `/ops/operator/containers/${uuid}/claim`, method: 'POST' }),
      invalidatesTags: ['OperatorContainers'],
    }),
    assignDriverToContainer: builder.mutation<ApiEnvelope<OpsContainerListItem>, { uuid: string; driverUserUuid: string }>({
      query: ({ uuid, driverUserUuid }) => ({
        url: `/ops/operator/containers/${uuid}/assign-driver`,
        method: 'POST',
        body: { driverUserUuid },
      }),
      invalidatesTags: ['OperatorContainers', 'DriverContainers'],
    }),
    getDoctorContainers: builder.query<ApiEnvelope<OpsContainerListItem[]>, void>({
      query: () => '/ops/doctor/containers',
      providesTags: ['DoctorContainers'],
    }),
    claimDoctorContainer: builder.mutation<ApiEnvelope<OpsContainerListItem>, string>({
      query: (uuid) => ({ url: `/ops/doctor/containers/${uuid}/claim`, method: 'POST' }),
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
  useGetDriverDashboardQuery,
  useGetDriverProfileQuery,
  useUpdateDriverProfileMutation,
  useGetDriverContainersQuery,
  useUpdateContainerStatusMutation,
  useRecordContainerLocationMutation,
  useGetClaimableContainersQuery,
  useClaimOperatorContainerMutation,
  useAssignDriverToContainerMutation,
  useGetDoctorContainersQuery,
  useClaimDoctorContainerMutation,
  useCompleteDoctorInspectionMutation,
} = opsApi
