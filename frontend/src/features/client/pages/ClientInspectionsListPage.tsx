import { Box, Button, Chip, MenuItem, TableCell, TableRow, TextField } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { useGetInspectionsQuery } from '../api/clientApi'

export function ClientInspectionsListPage() {
  const [status, setStatus] = useState('')
  const { data, isLoading } = useGetInspectionsQuery(status || undefined)
  const inspections = data?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Logistics"
        title="My Inspections"
        subtitle="Track scheduled and completed inspections for your entries."
      />
      <Box sx={{ mb: 2, maxWidth: 240 }}>
        <TextField select label="Filter by status" value={status} onChange={(e) => setStatus(e.target.value)} fullWidth size="small">
          <MenuItem value="">All statuses</MenuItem>
          <MenuItem value="Scheduled">Scheduled</MenuItem>
          <MenuItem value="InProgress">In Progress</MenuItem>
          <MenuItem value="Completed">Completed</MenuItem>
          <MenuItem value="Failed">Failed</MenuItem>
        </TextField>
      </Box>
      <PortalTablePanel
        title="Inspections"
        columns={['Entry', 'Agency', 'Status', 'Scheduled', 'Completed', '']}
        isLoading={isLoading}
        isEmpty={!isLoading && inspections.length === 0}
        emptyMessage="No inspections found."
      >
        {inspections.map((inspection) => (
          <TableRow key={inspection.uuid} hover>
            <TableCell>{inspection.entryReferenceNo}</TableCell>
            <TableCell>{inspection.agencyCode}</TableCell>
            <TableCell>
              <Chip size="small" label={inspection.status} sx={portalStatusChipSx(inspection.status)} />
            </TableCell>
            <TableCell>{inspection.scheduledAt ? new Date(inspection.scheduledAt).toLocaleString() : '—'}</TableCell>
            <TableCell>{inspection.completedAt ? new Date(inspection.completedAt).toLocaleString() : '—'}</TableCell>
            <TableCell>
              <Button size="small" component={RouterLink} to={`/client/inspections/${inspection.uuid}`}>
                View
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
