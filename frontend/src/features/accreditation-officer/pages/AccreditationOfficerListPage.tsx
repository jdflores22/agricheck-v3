import { useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  Box,
  Button,
  Tab,
  Tabs,
  TableCell,
  TableRow,
} from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { AccreditationStatusChip } from '../components/AccreditationStatusChip'
import {
  useClaimAccreditationSubmissionMutation,
  useGetAccreditationOfficerSubmissionsQuery,
} from '../api/accreditationOfficerApi'

const filters = [
  { key: 'unclaimed', label: 'Unclaimed' },
  { key: 'mine', label: 'My Applications' },
  { key: 'under_review', label: 'Under Review' },
  { key: 'approved', label: 'Approved' },
  { key: 'rejected', label: 'Rejected' },
  { key: 'all', label: 'All' },
]

export function AccreditationOfficerListPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const activeFilter = searchParams.get('filter') ?? 'unclaimed'
  const apiFilter = activeFilter === 'all' ? undefined : activeFilter

  const { data, isLoading, isError } = useGetAccreditationOfficerSubmissionsQuery({ filter: apiFilter })
  const [claimSubmission, { isLoading: claiming }] = useClaimAccreditationSubmissionMutation()
  const items = data?.data?.items ?? []

  const tabIndex = useMemo(() => {
    const index = filters.findIndex((f) => f.key === activeFilter)
    return index >= 0 ? index : 0
  }, [activeFilter])

  const handleClaim = async (uuid: string) => {
    await claimSubmission(uuid).unwrap()
    navigate(`/accreditation-officer/accreditations/${uuid}`)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="DA Accreditation"
        title="Accreditation Applications"
        subtitle="Browse and claim submitted accreditation applications for evaluation."
      />

      <Tabs
        value={tabIndex}
        onChange={(_, index) => setSearchParams({ filter: filters[index]?.key ?? 'unclaimed' })}
        sx={{ mb: 2 }}
      >
        {filters.map((filter) => (
          <Tab key={filter.key} label={filter.label} />
        ))}
      </Tabs>

      <PortalTablePanel
        title={filters[tabIndex]?.label ?? 'Applications'}
        columns={
          activeFilter === 'unclaimed'
            ? ['Company', 'Applicant', 'Type', 'Submitted', 'Action']
            : ['Company', 'Applicant', 'Type', 'Status', 'Officer', 'Submitted']
        }
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage={isError ? 'Unable to load applications. Please re-login as DA Accreditation Officer.' : 'No applications found for this filter.'}
      >
        {items.map((item) => (
          <TableRow key={item.uuid} hover={activeFilter !== 'unclaimed'}>
            <TableCell
              sx={activeFilter !== 'unclaimed' ? { cursor: 'pointer' } : undefined}
              onClick={activeFilter !== 'unclaimed' ? () => navigate(`/accreditation-officer/accreditations/${item.uuid}`) : undefined}
            >
              {item.companyName}
            </TableCell>
            <TableCell>{item.applicantName}</TableCell>
            <TableCell>{item.submissionType}</TableCell>
            {activeFilter === 'unclaimed' ? (
              <>
                <TableCell>{item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}</TableCell>
                <TableCell>
                  <Button
                    size="small"
                    variant="contained"
                    sx={portalPrimaryButtonSx}
                    disabled={claiming}
                    onClick={() => handleClaim(item.uuid)}
                  >
                    Claim
                  </Button>
                </TableCell>
              </>
            ) : (
              <>
                <TableCell>
                  <AccreditationStatusChip status={item.status} displayStatus={item.displayStatus} />
                </TableCell>
                <TableCell>{item.assignedOfficerName ?? '—'}</TableCell>
                <TableCell>{item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}</TableCell>
              </>
            )}
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
