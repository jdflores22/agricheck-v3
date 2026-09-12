import { createApi } from '@reduxjs/toolkit/query/react'
import type { ApiEnvelope } from '../auth/types'
import { baseQueryWithReauth } from '../auth/api/baseQuery'

export interface AddressOption {
  id: number
  code: string
  name: string
}

export interface AddressBarangayOption extends AddressOption {
  zipCode: string
}

export interface RegisteredWarehouse {
  id: number
  code: string
  name: string
  formattedAddress: string
  regionName?: string | null
  provinceName?: string | null
  cityName?: string | null
  barangayName?: string | null
  streetAddress?: string | null
  zipCode?: string | null
  latitude?: number | null
  longitude?: number | null
}

export const addressApi = createApi({
  reducerPath: 'addressApi',
  baseQuery: baseQueryWithReauth,
  endpoints: (builder) => ({
    getRegions: builder.query<ApiEnvelope<AddressOption[]>, void>({
      query: () => '/addresses/regions',
    }),
    getProvinces: builder.query<ApiEnvelope<AddressOption[]>, number>({
      query: (regionId) => `/addresses/regions/${regionId}/provinces`,
    }),
    getCities: builder.query<ApiEnvelope<AddressOption[]>, number>({
      query: (provinceId) => `/addresses/provinces/${provinceId}/cities`,
    }),
    getBarangays: builder.query<ApiEnvelope<AddressBarangayOption[]>, number>({
      query: (cityId) => `/addresses/cities/${cityId}/barangays`,
    }),
    getRegisteredWarehouses: builder.query<ApiEnvelope<RegisteredWarehouse[]>, void>({
      query: () => '/addresses/warehouses',
    }),
  }),
})

export const {
  useGetRegionsQuery,
  useGetProvincesQuery,
  useGetCitiesQuery,
  useGetBarangaysQuery,
  useGetRegisteredWarehousesQuery,
} = addressApi
