import { useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  Chip,
  TableCell,
  TableRow,
} from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { useGetAgencyAccreditationQuery } from '../api/agencyApi'

export function AccreditationReviewPage() {
  const navigate = useNavigate()
  const { data, isLoading, isError } = useGetAgencyAccreditationQuery({ filter: 'unclaimed' })
  const items = data?.data?.items ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Accreditation"
        title="Accreditation Review"
        subtitle="Submitted applications waiting to be claimed for evaluation."
      />
      <PortalTablePanel
        title="Unclaimed submissions"
        columns={['Company', 'Applicant', 'Type', 'Status', 'Submitted']}
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage={isError ? 'Unable to load submissions. Check your accreditation officer role and try again.' : 'No unclaimed submissions at the moment.'}
      >
        {items.map((item) => (
          <TableRow
            key={item.uuid}
            hover
            sx={{ cursor: 'pointer' }}
            onClick={() => navigate(`/agency/accreditation/${item.uuid}`)}
          >
            <TableCell>{item.companyName}</TableCell>
            <TableCell>{item.applicantName}</TableCell>
            <TableCell>{item.submissionType}</TableCell>
            <TableCell>
              <Chip size="small" label={item.status} sx={portalStatusChipSx(item.status)} />
            </TableCell>
            <TableCell>{item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
      <Box sx={{ mt: 2 }}>
        <Button variant="outlined" onClick={() => navigate('/accreditation-officer/dashboard')}>
          Open DA Accreditation Dashboard
        </Button>
      </Box>
    </Box>
  )
}
