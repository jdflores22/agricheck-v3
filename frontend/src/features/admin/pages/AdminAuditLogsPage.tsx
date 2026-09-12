import { Box, TableCell, TableRow } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { useGetAdminAuditLogsQuery } from '../api/adminApi'

export function AdminAuditLogsPage() {
  const { data, isLoading } = useGetAdminAuditLogsQuery({ page: 1 })
  const logs = data?.data?.items ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Audit Logs"
        subtitle="Review system activity and administrative actions."
      />

      <PortalTablePanel
        title="Recent activity"
        columns={['Time', 'Action', 'Entity', 'Actor']}
        isLoading={isLoading}
        isEmpty={!isLoading && logs.length === 0}
        emptyMessage="No audit logs found."
      >
        {logs.map((log) => (
          <TableRow key={log.id} hover>
            <TableCell>{new Date(log.createdAt).toLocaleString()}</TableCell>
            <TableCell>{log.action}</TableCell>
            <TableCell>{log.entityType} {log.entityId ? `#${log.entityId}` : ''}</TableCell>
            <TableCell>{log.actorName ?? 'System'}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
