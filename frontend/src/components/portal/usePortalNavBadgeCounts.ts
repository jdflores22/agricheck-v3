import { useMemo } from 'react'
import { useAppSelector } from '../../app/hooks'
import { selectCurrentUser } from '../../features/auth/authSlice'
import { useGetAccreditationOfficerDashboardQuery } from '../../features/accreditation-officer/api/accreditationOfficerApi'
import {
  useGetAgencyDashboardQuery,
  useGetContainerInspectionQueueQuery,
  useGetTransportTagQueueQuery,
} from '../../features/agency/api/agencyApi'
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
  const needsContainerInspectionQueue = navPaths.includes('/inspector/inspections')
  const needsTransportTagQueue = navPaths.includes('/agency/transport-tags')
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
  const { data: containerInspectionQueueData } = useGetContainerInspectionQueueQuery(
    { page: 1, scope: 'unclaimed' },
    { skip: skipAgency || !needsContainerInspectionQueue, pollingInterval: 60_000 },
  )
  const { data: myContainerInspectionData } = useGetContainerInspectionQueueQuery(
    { page: 1, scope: 'mine' },
    { skip: skipAgency || !needsContainerInspectionQueue, pollingInterval: 60_000 },
  )
  const { data: transportTagQueueData } = useGetTransportTagQueueQuery(undefined, {
    skip: skipAgency || !needsTransportTagQueue,
    pollingInterval: 60_000,
  })
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

    const pendingContainerInspections =
      (containerInspectionQueueData?.data?.totalCount ?? 0) +
      (myContainerInspectionData?.data?.totalCount ?? 0)

    if (needsAgency) {
      const agency = agencyData?.data
      if (agency) {
        const agencyCounts: PortalNavBadgeCounts = {
          '/agency/evaluator/queue': agency.queueCount,
          '/agency/evaluator/assignments': agency.myAssignments,
          '/agency/billing': agency.awaitingBilling + agency.openBillings,
          '/agency/billing-reports': agency.pendingCashPayments,
          '/agency/accreditation': agency.pendingAccreditation,
        }
        for (const [path, value] of Object.entries(agencyCounts)) {
          if (navPaths.includes(path)) {
            counts[path] = value
          }
        }
      }
    }

    if (navPaths.includes('/inspector/inspections')) {
      counts['/inspector/inspections'] = pendingContainerInspections
    }

    if (navPaths.includes('/agency/transport-tags')) {
      const pendingTransportTags = (transportTagQueueData?.data?.ready ?? []).filter((item) => !item.hasTransportTag).length
      counts['/agency/transport-tags'] = pendingTransportTags
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
    needsContainerInspectionQueue,
    needsTransportTagQueue,
    agencyData,
    containerInspectionQueueData,
    myContainerInspectionData,
    transportTagQueueData,
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
