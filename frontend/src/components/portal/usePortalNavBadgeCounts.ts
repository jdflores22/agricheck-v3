import { useMemo } from 'react'
import { useAppSelector } from '../../app/hooks'
import { selectCurrentUser } from '../../features/auth/authSlice'
import { useGetAccreditationOfficerDashboardQuery } from '../../features/accreditation-officer/api/accreditationOfficerApi'
import { useGetAgencyDashboardQuery } from '../../features/agency/api/agencyApi'
import { useGetDashboardQuery } from '../../features/client/api/clientApi'
import {
  useGetMavAdminDashboardQuery,
  useGetMavImporterDashboardQuery,
} from '../../features/mav/api/mavApi'
import { useGetDriverDashboardQuery, useGetWarehouseDashboardQuery } from '../../features/ops/api/opsApi'
import type { PortalKey } from './portalNavConfig'

export type PortalNavBadgeCounts = Record<string, number>

function countIfPositive(value: number | undefined): number | undefined {
  if (value === undefined || value <= 0) return undefined
  return value
}

function hasWarehouseAccess(roles: string[]): boolean {
  return roles.includes('ROLE_WAREHOUSE_STAFF') || roles.includes('ROLE_ADMIN')
}

function hasDriverAccess(roles: string[]): boolean {
  return roles.includes('ROLE_DRIVER') || roles.includes('ROLE_OPERATOR') || roles.includes('ROLE_ADMIN')
}

export function usePortalNavBadgeCounts(portalKey: PortalKey, navPaths: string[] = []): PortalNavBadgeCounts {
  const user = useAppSelector(selectCurrentUser)
  const roles = user?.roles ?? []
  const needsAgency =
    portalKey === 'agency' ||
    portalKey === 'inspector' ||
    navPaths.some((path) => path.startsWith('/agency'))
  const needsAccreditation =
    portalKey === 'accreditation-officer' ||
    navPaths.some((path) => path.startsWith('/accreditation-officer'))
  const skipAgency = !needsAgency
  const skipAccreditation = !needsAccreditation
  const skipClient = portalKey !== 'client'
  const skipMav = portalKey !== 'mav'
  const skipWarehouse = portalKey !== 'warehouse' || !hasWarehouseAccess(roles)
  const skipDriver = portalKey !== 'driver' || !hasDriverAccess(roles)

  const { data: agencyData } = useGetAgencyDashboardQuery(undefined, { skip: skipAgency })
  const { data: accreditationData } = useGetAccreditationOfficerDashboardQuery(undefined, {
    skip: skipAccreditation,
  })
  const { data: clientData } = useGetDashboardQuery(undefined, { skip: skipClient })
  const { data: mavImporterData } = useGetMavImporterDashboardQuery(undefined, { skip: skipMav })
  const { data: mavAdminData } = useGetMavAdminDashboardQuery(undefined, { skip: skipMav })
  const { data: warehouseData } = useGetWarehouseDashboardQuery(undefined, { skip: skipWarehouse })
  const { data: driverData } = useGetDriverDashboardQuery(undefined, { skip: skipDriver })

  return useMemo(() => {
    const counts: PortalNavBadgeCounts = {}

    if (needsAgency) {
      const agency = agencyData?.data
      if (agency) {
        const agencyCounts: PortalNavBadgeCounts = {
          '/agency/evaluator/queue': agency.queueCount,
          '/agency/evaluator/assignments': agency.myAssignments,
          '/agency/inspections': agency.pendingInspections,
          '/agency/billing': agency.openBillings,
          '/agency/accreditation': agency.pendingAccreditation,
        }
        Object.assign(counts, agencyCounts)

        if (portalKey === 'inspector' || navPaths.some((path) => path.startsWith('/inspector'))) {
          counts['/inspector/inspections'] = agency.pendingInspections
        }
      }
    }

    if (needsAccreditation) {
      const accreditation = accreditationData?.data
      if (accreditation) {
        counts['/accreditation-officer/accreditations'] =
          accreditation.unclaimedCount + accreditation.myApplicationsCount
      }
    }

    if (portalKey === 'client') {
      const client = clientData?.data
      if (client) {
        counts['/client/entries'] = client.entries.pending + client.entries.forCompliance
        counts['/client/bills'] = client.logistics.unpaidBills
        counts['/client/inspections'] = client.logistics.pendingInspections
      }
    }

    if (portalKey === 'mav') {
      const importer = mavImporterData?.data
      const admin = mavAdminData?.data
      if (importer) {
        counts['/mav/applications'] = importer.pendingApplications
      }
      if (admin) {
        counts['/mav/admin/applications'] = admin.pendingApplications
        counts['/mav/admin/compliance'] = admin.complianceAlerts
      }
    }

    if (portalKey === 'warehouse') {
      const warehouse = warehouseData?.data
      if (warehouse) {
        counts['/warehouse/releases'] = warehouse.pendingReleases
      }
    }

    if (portalKey === 'driver') {
      const driver = driverData?.data
      if (driver) {
        counts['/driver/containers'] = driver.assignedContainers + driver.inTransitContainers
      }
    }

    return Object.fromEntries(
      Object.entries(counts).filter(([, value]) => value > 0),
    ) as PortalNavBadgeCounts
  }, [
    portalKey,
    navPaths,
    needsAgency,
    needsAccreditation,
    agencyData,
    accreditationData,
    clientData,
    mavImporterData,
    mavAdminData,
    warehouseData,
    driverData,
  ])
}

export function getNavBadgeCount(path: string, badgeCounts: PortalNavBadgeCounts): number | undefined {
  return countIfPositive(badgeCounts[path])
}
