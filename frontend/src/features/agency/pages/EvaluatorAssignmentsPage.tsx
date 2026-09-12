import { useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  Chip,
  TableCell,
  TableRow,
  Typography,
} from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetMyAssignmentsQuery } from '../api/agencyApi'

export function EvaluatorAssignmentsPage() {
  const navigate = useNavigate()
  const { data, isLoading } = useGetMyAssignmentsQuery({})
  const items = data?.data?.items ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Evaluation"
        title="My Assignments"
        subtitle="Entries currently assigned to you for review."
      />
      <PortalTablePanel
        title="Active assignments"
        columns={['Reference', 'Applicant', 'Commodity', 'Submitted', 'Status', 'Action']}
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage="No active assignments."
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
                onClick={() => navigate(`/agency/evaluator/entries/${entry.uuid}`)}
              >
                Evaluate
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
