import { Box } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { useGetSecretaryReportQuery } from '../api/agencyApi'

const statCards = [
  { key: 'totalEntries', label: 'Total Entries' },
  { key: 'submittedEntries', label: 'Submitted' },
  { key: 'underReviewEntries', label: 'Under Review' },
  { key: 'approvedEntries', label: 'Approved' },
  { key: 'rejectedEntries', label: 'Rejected' },
  { key: 'completedInspections', label: 'Completed Inspections' },
  { key: 'pendingAccreditation', label: 'Pending Accreditation' },
  { key: 'issuedBillings', label: 'Issued Billings' },
  { key: 'paidBillings', label: 'Paid Billings' },
]

export function SecretaryReportsPage() {
  const { data, isLoading } = useGetSecretaryReportQuery()
  const report = data?.data

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Reports"
        title="Secretary Reports"
        subtitle="Agency-wide operational statistics and summaries."
      />
      <PortalStatGrid
        items={statCards}
        stats={report as unknown as Record<string, number | undefined>}
        isLoading={isLoading}
        columns={{ xs: '1fr 1fr', md: 'repeat(3, 1fr)' }}
      />
    </Box>
  )
}
