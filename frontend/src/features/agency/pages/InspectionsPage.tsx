import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TableCell,
  TableRow,
  Typography,
} from '@mui/material'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import {
  useClaimContainerInspectionMutation,
  useGetContainerInspectionQueueQuery,
  type AgencyContainerInspectionQueueItem,
} from '../api/agencyApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function InspectionsPage() {
  const navigate = useNavigate()
  const { data: queueData, isLoading: queueLoading } = useGetContainerInspectionQueueQuery({ scope: 'unclaimed' })
  const { data: myAssignmentsData, isLoading: assignmentsLoading } = useGetContainerInspectionQueueQuery({ scope: 'mine' })
  const [claimContainer, { isLoading: claiming }] = useClaimContainerInspectionMutation()
  const [claimConfirmOpen, setClaimConfirmOpen] = useState(false)
  const [claimError, setClaimError] = useState('')
  const [pendingContainer, setPendingContainer] = useState<AgencyContainerInspectionQueueItem | null>(null)

  const queueItems = queueData?.data?.items ?? []
  const myAssignments = myAssignmentsData?.data?.items ?? []
  const reviewBasePath = '/inspector/inspections/containers'

  const openClaimConfirm = (container: AgencyContainerInspectionQueueItem) => {
    setClaimError('')
    setPendingContainer(container)
    setClaimConfirmOpen(true)
  }

  const closeClaimConfirm = () => {
    if (claiming) return
    setClaimConfirmOpen(false)
    setPendingContainer(null)
    setClaimError('')
  }

  const handleConfirmClaim = async () => {
    if (!pendingContainer) return
    setClaimError('')
    try {
      await claimContainer(pendingContainer.containerUuid).unwrap()
      setClaimConfirmOpen(false)
      navigate(`${reviewBasePath}/${pendingContainer.containerUuid}`)
    } catch {
      setClaimError('Unable to claim this container. It may already be assigned to another inspector.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Inspections"
        subtitle="Claim containers from the queue, then review photos and submit the inspection outcome."
      />

      <PortalTablePanel
        title="Unclaimed containers"
        columns={['Container', 'Entry', 'Applicant', 'Type', 'Submitted', 'Pending photos', '']}
        isLoading={queueLoading}
        isEmpty={!queueLoading && queueItems.length === 0}
        emptyMessage="No container photo uploads are waiting to be claimed."
      >
        {queueItems.map((item) => (
          <TableRow key={item.containerUuid} hover>
            <TableCell>{item.containerNumber}</TableCell>
            <TableCell>{item.entryReferenceNo}</TableCell>
            <TableCell>{item.applicantName}</TableCell>
            <TableCell>{item.entryType}</TableCell>
            <TableCell>
              {item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}
            </TableCell>
            <TableCell>
              <Chip size="small" label={`${item.pendingPhotoCount} pending`} sx={portalStatusChipSx('Pending')} />
            </TableCell>
            <TableCell align="right">
              <Button
                type="button"
                size="small"
                variant="contained"
                disabled={claiming}
                sx={portalPrimaryButtonSx}
                onClick={() => openClaimConfirm(item)}
              >
                Claim
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="My assignments"
          columns={['Container', 'Entry', 'Applicant', 'Submitted', 'Pending photos', 'Status', '']}
          isLoading={assignmentsLoading}
          isEmpty={!assignmentsLoading && myAssignments.length === 0}
          emptyMessage="You have no claimed containers yet. Claim one from the queue above."
        >
          {myAssignments.map((item) => (
            <TableRow key={item.containerUuid} hover>
              <TableCell>{item.containerNumber}</TableCell>
              <TableCell>{item.entryReferenceNo}</TableCell>
              <TableCell>{item.applicantName}</TableCell>
              <TableCell>
                {item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}
              </TableCell>
              <TableCell>
                {item.pendingPhotoCount > 0 ? (
                  <Chip size="small" label={`${item.pendingPhotoCount} pending`} sx={portalStatusChipSx('Pending')} />
                ) : (
                  <Chip size="small" label="Ready for outcome" sx={portalStatusChipSx('Approved')} />
                )}
              </TableCell>
              <TableCell>
                <Chip size="small" label="Assigned to me" sx={portalStatusChipSx('UnderReview')} />
              </TableCell>
              <TableCell align="right">
                <Button
                  size="small"
                  variant="contained"
                  component={RouterLink}
                  to={`${reviewBasePath}/${item.containerUuid}`}
                  sx={portalPrimaryButtonSx}
                >
                  Review
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>

      <Dialog
        open={claimConfirmOpen && Boolean(pendingContainer)}
        onClose={closeClaimConfirm}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Claim Container for Inspection?</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted }}>
            You are about to claim container <strong>{pendingContainer?.containerNumber}</strong>
            {pendingContainer?.entryReferenceNo ? ` for entry ${pendingContainer.entryReferenceNo}` : ''}
            {pendingContainer?.applicantName ? ` (${pendingContainer.applicantName})` : ''}.
            {pendingContainer?.pendingPhotoCount
              ? ` ${pendingContainer.pendingPhotoCount} photo${pendingContainer.pendingPhotoCount === 1 ? '' : 's'} still need review.`
              : ' All photos have been reviewed and are ready for final outcome.'}
            {' '}This container will be assigned to you and other inspectors will see that it is already claimed.
          </Typography>
          {claimError ? <Alert severity="error" sx={{ mt: 2 }}>{claimError}</Alert> : null}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button type="button" onClick={closeClaimConfirm} disabled={claiming} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button
            type="button"
            onClick={() => void handleConfirmClaim()}
            variant="contained"
            disabled={claiming}
            sx={portalPrimaryButtonSx}
          >
            {claiming ? <CircularProgress size={20} color="inherit" /> : 'Yes, Claim Container'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
