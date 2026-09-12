import {
  Box,
  Button,
  Chip,
  MenuItem,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetEntriesQuery } from '../api/clientApi'

export function EntriesListPage() {
  const [searchParams] = useSearchParams()
  const [status, setStatus] = useState(searchParams.get('status') ?? '')
  const { data, isLoading } = useGetEntriesQuery({ status: status || undefined })

  useEffect(() => {
    const queryStatus = searchParams.get('status') ?? ''
    if (queryStatus !== status) {
      setStatus(queryStatus)
    }
  }, [searchParams, status])
  const entries = data?.data?.items ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Entries"
        title="My Entries"
        subtitle="View and manage your import/export entry submissions."
        actions={
          <Button variant="contained" component={RouterLink} to="/client/entries/agencies" sx={portalPrimaryButtonSx}>
            Submit Entry
          </Button>
        }
      />
      <Box sx={{ mb: 2, maxWidth: 240 }}>
        <TextField select label="Filter by status" value={status} onChange={(e) => setStatus(e.target.value)} fullWidth size="small">
          <MenuItem value="">All statuses</MenuItem>
          <MenuItem value="Draft">Draft</MenuItem>
          <MenuItem value="Submitted">Submitted</MenuItem>
          <MenuItem value="UnderReview">Under Review</MenuItem>
          <MenuItem value="ForCompliance">For Compliance</MenuItem>
          <MenuItem value="Approved">Approved</MenuItem>
          <MenuItem value="DaIssueBilling">DA Issue Billing</MenuItem>
          <MenuItem value="ForInspection">For Inspection</MenuItem>
          <MenuItem value="ReadyForTransport">Ready for Transport</MenuItem>
          <MenuItem value="AwaitingTransport">Awaiting Transport</MenuItem>
          <MenuItem value="InTransit">In Transit</MenuItem>
          <MenuItem value="Rejected">Rejected</MenuItem>
        </TextField>
      </Box>
      <PortalTablePanel
        title="All entries"
        columns={['Reference', 'Type', 'Agency', 'Status', 'Payment', '']}
        isLoading={isLoading}
        isEmpty={!isLoading && entries.length === 0}
        emptyMessage="No entries yet."
      >
        {entries.map((entry) => (
          <TableRow key={entry.uuid} hover>
            <TableCell>{entry.referenceNo}</TableCell>
            <TableCell>{entry.entryType}</TableCell>
            <TableCell>{entry.agencyCode}</TableCell>
            <TableCell>
              <Chip size="small" label={entry.status} sx={portalStatusChipSx(entry.status)} />
            </TableCell>
            <TableCell>{entry.paymentStatus}</TableCell>
            <TableCell>
              <Button size="small" component={RouterLink} to={`/client/entries/${entry.uuid}`}>View</Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
