import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TableCell,
  TableRow,
  Typography,
} from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalStatGrid } from '../../../components/portal/PortalStatGrid'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { AccreditationStatusChip } from '../components/AccreditationStatusChip'
import {
  useClaimAccreditationSubmissionMutation,
  useGetAccreditationOfficerDashboardQuery,
  type AccreditationOfficerListItem,
} from '../api/accreditationOfficerApi'

const statCards = [
  { key: 'unclaimedCount', label: 'Unclaimed Queue' },
  { key: 'myApplicationsCount', label: 'My Applications' },
  { key: 'approvedCount', label: 'Approved' },
  { key: 'rejectedCount', label: 'Rejected' },
]

export function AccreditationOfficerDashboardPage() {
  const navigate = useNavigate()
  const { data, isLoading } = useGetAccreditationOfficerDashboardQuery()
  const [claimSubmission, { isLoading: claiming }] = useClaimAccreditationSubmissionMutation()
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [pendingClaim, setPendingClaim] = useState<AccreditationOfficerListItem | null>(null)
  const dashboard = data?.data

  const openClaimConfirm = (item: AccreditationOfficerListItem) => {
    setPendingClaim(item)
    setConfirmOpen(true)
  }

  const handleConfirmClaim = async () => {
    if (!pendingClaim) return
    await claimSubmission(pendingClaim.uuid).unwrap()
    setConfirmOpen(false)
    navigate(`/accreditation-officer/accreditations/${pendingClaim.uuid}`)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="DA Accreditation"
        title="Accreditation Officer Dashboard"
        subtitle="Review submitted company accreditation applications."
      />

      <PortalStatGrid
        items={statCards}
        stats={dashboard as unknown as Record<string, number | undefined>}
        isLoading={isLoading}
        columns={{ xs: '1fr 1fr', md: 'repeat(4, 1fr)' }}
      />

      <Box sx={{ mt: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 1 }}>
          <Button size="small" onClick={() => navigate('/accreditation-officer/accreditations?filter=unclaimed')}>
            View all unclaimed
          </Button>
        </Box>
        <PortalTablePanel
          title="Unclaimed applications"
          columns={['Company', 'Applicant', 'Type', 'Submitted', 'Action']}
          isLoading={isLoading}
          isEmpty={!isLoading && (dashboard?.unclaimed.length ?? 0) === 0}
          emptyMessage="No unclaimed applications at the moment."
        >
          {(dashboard?.unclaimed ?? []).map((item) => (
            <TableRow key={item.uuid} hover>
              <TableCell>{item.companyName}</TableCell>
              <TableCell>{item.applicantName}</TableCell>
              <TableCell>{item.submissionType}</TableCell>
              <TableCell>{item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}</TableCell>
              <TableCell>
                <Button
                  size="small"
                  variant="contained"
                  sx={portalPrimaryButtonSx}
                  disabled={claiming}
                  onClick={() => openClaimConfirm(item)}
                >
                  Claim
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Box sx={{ mt: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 1 }}>
          <Button size="small" onClick={() => navigate('/accreditation-officer/accreditations?filter=mine')}>
            View all mine
          </Button>
        </Box>
        <PortalTablePanel
          title="My applications"
          columns={['Company', 'Applicant', 'Status', 'Claimed', '']}
          isLoading={isLoading}
          isEmpty={!isLoading && (dashboard?.myApplications.length ?? 0) === 0}
          emptyMessage="You have not claimed any applications yet."
        >
          {(dashboard?.myApplications ?? []).map((item) => (
            <TableRow
              key={item.uuid}
              hover
              sx={{ cursor: 'pointer' }}
              onClick={() => navigate(`/accreditation-officer/accreditations/${item.uuid}`)}
            >
              <TableCell>{item.companyName}</TableCell>
              <TableCell>{item.applicantName}</TableCell>
              <TableCell>
                <AccreditationStatusChip status={item.status} displayStatus={item.displayStatus} />
              </TableCell>
              <TableCell>{item.claimedAt ? new Date(item.claimedAt).toLocaleDateString() : '—'}</TableCell>
              <TableCell align="right">Review</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Dialog open={confirmOpen} onClose={() => !claiming && setConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Claim Application for Evaluation?</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted }}>
            You are about to claim <strong>{pendingClaim?.companyName}</strong>
            {pendingClaim?.applicantName ? ` (${pendingClaim.applicantName})` : ''} for evaluation.
            This application will be assigned to you and moved to under review status.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setConfirmOpen(false)} disabled={claiming} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleConfirmClaim} variant="contained" disabled={claiming} sx={portalPrimaryButtonSx}>
            {claiming ? <CircularProgress size={20} color="inherit" /> : 'Yes, Claim Application'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
