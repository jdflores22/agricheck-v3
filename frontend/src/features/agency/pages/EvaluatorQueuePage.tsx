import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
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
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useAssignEntryMutation,
  useGetEvaluatorQueueQuery,
  type AgencyEntryListItem,
} from '../api/agencyApi'

export function EvaluatorQueuePage() {
  const navigate = useNavigate()
  const { data, isLoading } = useGetEvaluatorQueueQuery({})
  const [assignEntry, { isLoading: assigning }] = useAssignEntryMutation()
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [pendingEntry, setPendingEntry] = useState<AgencyEntryListItem | null>(null)
  const items = data?.data?.items ?? []

  const openAssignConfirm = (entry: AgencyEntryListItem) => {
    setPendingEntry(entry)
    setConfirmOpen(true)
  }

  const handleConfirmAssign = async () => {
    if (!pendingEntry) return
    await assignEntry(pendingEntry.uuid).unwrap()
    setConfirmOpen(false)
    navigate(`/agency/evaluator/entries/${pendingEntry.uuid}`)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Evaluation"
        title="Evaluation Queue"
        subtitle="Entries awaiting evaluator assignment."
      />
      <PortalTablePanel
        title="Queue"
        columns={['Reference', 'Applicant', 'Commodity', 'Submitted', 'Status', 'Action']}
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage="No entries in queue."
      >
        {items.map((entry) => (
          <TableRow key={entry.uuid} hover>
            <TableCell>{entry.referenceNo}</TableCell>
            <TableCell>
              <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {entry.companyName || entry.applicantName}
              </Typography>
              {entry.companyName && (
                <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
                  {entry.applicantName}
                </Typography>
              )}
            </TableCell>
            <TableCell>{entry.commodityName ?? '—'}</TableCell>
            <TableCell>
              {entry.submittedAt ? new Date(entry.submittedAt).toLocaleDateString() : '—'}
            </TableCell>
            <TableCell>
              <Chip size="small" label={entry.status} sx={portalStatusChipSx(entry.status)} />
            </TableCell>
            <TableCell align="right">
              <Button
                size="small"
                variant="contained"
                sx={portalPrimaryButtonSx}
                disabled={assigning}
                onClick={() => openAssignConfirm(entry)}
              >
                Assign to me
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={confirmOpen} onClose={() => !assigning && setConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Claim Entry for Evaluation?</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted }}>
            You are about to claim <strong>{pendingEntry?.referenceNo}</strong>
            {pendingEntry?.companyName ? ` for ${pendingEntry.companyName}` : ''}
            {pendingEntry?.applicantName ? ` (${pendingEntry.applicantName})` : ''} for evaluation.
            {pendingEntry?.commodityName ? ` Commodity: ${pendingEntry.commodityName}.` : ''}
            {' '}This entry will be assigned to you and moved to your evaluation queue.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setConfirmOpen(false)} disabled={assigning} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleConfirmAssign} variant="contained" disabled={assigning} sx={portalPrimaryButtonSx}>
            {assigning ? <CircularProgress size={20} color="inherit" /> : 'Yes, Claim Entry'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
