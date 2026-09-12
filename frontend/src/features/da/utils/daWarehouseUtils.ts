import type { DaWarehouse } from '../api/daApi'

export interface LocationFilters {
  regionId: string
  provinceId: string
  cityId: string
  barangayId: string
}

export const emptyLocationFilters: LocationFilters = {
  regionId: '',
  provinceId: '',
  cityId: '',
  barangayId: '',
}

export function filterWarehouses(warehouses: DaWarehouse[], filters: LocationFilters) {
  return warehouses.filter((warehouse) => {
    if (filters.regionId && String(warehouse.regionId ?? '') !== filters.regionId) return false
    if (filters.provinceId && String(warehouse.provinceId ?? '') !== filters.provinceId) return false
    if (filters.cityId && String(warehouse.cityId ?? '') !== filters.cityId) return false
    if (filters.barangayId && String(warehouse.barangayId ?? '') !== filters.barangayId) return false
    return true
  })
}

export interface BarangayWarehouseGroup {
  key: string
  regionId: number | null
  regionName: string
  provinceId: number | null
  provinceName: string
  cityId: number | null
  cityName: string
  barangayId: number | null
  barangayName: string
  warehouses: DaWarehouse[]
}

export interface RegionWarehouseSection {
  regionId: number | null
  regionName: string
  groups: BarangayWarehouseGroup[]
}

export function groupWarehousesByLocation(warehouses: DaWarehouse[]): BarangayWarehouseGroup[] {
  const map = new Map<string, BarangayWarehouseGroup>()

  for (const warehouse of warehouses) {
    const key = [
      warehouse.regionId ?? 'none',
      warehouse.provinceId ?? 'none',
      warehouse.cityId ?? 'none',
      warehouse.barangayId ?? 'none',
    ].join(':')

    const existing = map.get(key)
    if (existing) {
      existing.warehouses.push(warehouse)
      continue
    }

    map.set(key, {
      key,
      regionId: warehouse.regionId ?? null,
      regionName: warehouse.regionName ?? 'Unclassified Region',
      provinceId: warehouse.provinceId ?? null,
      provinceName: warehouse.provinceName ?? 'Unclassified Province',
      cityId: warehouse.cityId ?? null,
      cityName: warehouse.cityName ?? 'Unclassified City/Municipality',
      barangayId: warehouse.barangayId ?? null,
      barangayName: warehouse.barangayName ?? 'Unclassified Barangay',
      warehouses: [warehouse],
    })
  }

  return Array.from(map.values()).sort((a, b) => {
    const region = a.regionName.localeCompare(b.regionName)
    if (region !== 0) return region
    const province = a.provinceName.localeCompare(b.provinceName)
    if (province !== 0) return province
    const city = a.cityName.localeCompare(b.cityName)
    if (city !== 0) return city
    return a.barangayName.localeCompare(b.barangayName)
  })
}

export function buildRegionSections(warehouses: DaWarehouse[]): RegionWarehouseSection[] {
  const groups = groupWarehousesByLocation(warehouses)
  const sections = new Map<string, RegionWarehouseSection>()

  for (const group of groups) {
    const sectionKey = String(group.regionId ?? 'unset')
    const section = sections.get(sectionKey) ?? {
      regionId: group.regionId,
      regionName: group.regionName,
      groups: [],
    }
    section.groups.push(group)
    sections.set(sectionKey, section)
  }

  return Array.from(sections.values()).sort((a, b) => a.regionName.localeCompare(b.regionName))
}

export function formatLocationPath(group: BarangayWarehouseGroup) {
  return [group.provinceName, group.cityName, group.barangayName].join(' · ')
}

export const PHILIPPINES_MAP_CENTER: [number, number] = [12.8797, 121.774]
export const PHILIPPINES_MAP_ZOOM = 6

export function hasWarehouseCoordinates(warehouse: DaWarehouse) {
  return warehouse.latitude != null
    && warehouse.longitude != null
    && !Number.isNaN(Number(warehouse.latitude))
    && !Number.isNaN(Number(warehouse.longitude))
}

export function getWarehousesWithCoordinates(warehouses: DaWarehouse[]) {
  return warehouses.filter(hasWarehouseCoordinates)
}
