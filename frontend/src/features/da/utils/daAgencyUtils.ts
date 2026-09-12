import type { DaAgencySummary } from '../api/daApi'

export function partitionDaAgencies(agencies: DaAgencySummary[]) {
  const daAgency = agencies.find((agency) => agency.code === 'DA') ?? null
  const attachedAgencies = agencies
    .filter((agency) => agency.code !== 'DA')
    .sort((a, b) => a.name.localeCompare(b.name))

  return { daAgency, attachedAgencies }
}

export function summarizeAttachedAgencies(agencies: DaAgencySummary[]) {
  return agencies.reduce(
    (totals, agency) => ({
      totalEntries: totals.totalEntries + agency.totalEntries,
      pendingEntries: totals.pendingEntries + agency.pendingEntries,
      openBillings: totals.openBillings + agency.openBillings,
    }),
    { totalEntries: 0, pendingEntries: 0, openBillings: 0 },
  )
}
