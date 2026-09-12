import { Box, Button, Chip, MenuItem, Stack, TableCell, TableRow, TextField } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { useGetContainersQuery } from '../api/clientApi'

export function ContainersListPage() {
  const [status, setStatus] = useState('')
  const { data, isLoading } = useGetContainersQuery(status || undefined)
  const containers = data?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Logistics"
        title="My Containers"
        subtitle="Track containers linked to your entry submissions."
      />
      <Box sx={{ mb: 2, maxWidth: 240 }}>
        <TextField select label="Filter by status" value={status} onChange={(e) => setStatus(e.target.value)} fullWidth size="small">
          <MenuItem value="">All statuses</MenuItem>
          <MenuItem value="Pending">Pending</MenuItem>
          <MenuItem value="ReadyForTransport">Ready for Transport</MenuItem>
          <MenuItem value="AwaitingConfirmation">Awaiting Confirmation</MenuItem>
          <MenuItem value="Assigned">Assigned</MenuItem>
          <MenuItem value="InTransit">In Transit</MenuItem>
          <MenuItem value="UnderInspection">Under Inspection</MenuItem>
          <MenuItem value="Inspected">Inspected</MenuItem>
          <MenuItem value="AtWarehouse">At Warehouse</MenuItem>
          <MenuItem value="Released">Released</MenuItem>
        </TextField>
      </Box>
      <PortalTablePanel
        title="Containers"
        columns={['Container No.', 'Entry', 'Agency', 'Status', 'Updated', '']}
        isLoading={isLoading}
        isEmpty={!isLoading && containers.length === 0}
        emptyMessage="No containers found."
      >
        {containers.map((container) => (
          <TableRow key={container.uuid} hover>
            <TableCell>{container.containerNumber}</TableCell>
            <TableCell>{container.entryReferenceNo}</TableCell>
            <TableCell>{container.agencyCode}</TableCell>
            <TableCell>
              <Chip size="small" label={container.status} sx={portalStatusChipSx(container.status)} />
            </TableCell>
            <TableCell>{new Date(container.updatedAt).toLocaleDateString()}</TableCell>
            <TableCell>
              <Stack direction="row" spacing={1}>
                <Button size="small" component={RouterLink} to={`/client/containers/${container.uuid}`}>
                  View
                </Button>
                <Button size="small" component={RouterLink} to={`/client/entries`}>
                  Entry
                </Button>
              </Stack>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
