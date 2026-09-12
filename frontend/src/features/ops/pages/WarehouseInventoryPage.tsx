import { Box, TableCell, TableRow } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { useGetWarehouseInventoryQuery } from '../api/opsApi'

export function WarehouseInventoryPage() {
  const { data, isLoading } = useGetWarehouseInventoryQuery()
  const items = data?.data?.items ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Warehouse Inventory"
        subtitle="View all containers currently in storage."
      />
      <PortalTablePanel
        title="All inventory"
        columns={['Container', 'Entry', 'Facility', 'Location', 'Status', 'Received']}
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage="No inventory records yet."
      >
        {items.map((item) => (
          <TableRow key={item.uuid} hover>
            <TableCell>{item.containerNumber}</TableCell>
            <TableCell>{item.entryReference}</TableCell>
            <TableCell>{item.facilityName}</TableCell>
            <TableCell>{item.locationCode ?? '—'}</TableCell>
            <TableCell>{item.status}</TableCell>
            <TableCell>{new Date(item.receivedAt).toLocaleString()}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
