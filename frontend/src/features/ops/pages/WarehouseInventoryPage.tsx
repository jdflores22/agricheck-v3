import { Box, TableCell, TableRow, Typography } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { useGetWarehouseInventoryQuery } from '../api/opsApi'
import { formatMavLibraryCommodity } from '../utils/mavCommodityDisplay'

export function WarehouseInventoryPage() {
  const { data, isLoading } = useGetWarehouseInventoryQuery()
  const items = data?.data?.items ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Warehouse Inventory"
        subtitle="Stored containers with MAV library commodity (centralized HS catalog)."
      />
      <PortalTablePanel
        title="All inventory"
        columns={['Container', 'Entry', 'MAV commodity (HS)', 'Category', 'Facility', 'Location', 'Status', 'Received']}
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage="No inventory records yet."
      >
        {items.map((item) => {
          const mav = formatMavLibraryCommodity(item)
          return (
            <TableRow key={item.uuid} hover>
              <TableCell>{item.containerNumber}</TableCell>
              <TableCell>{item.entryReference}</TableCell>
              <TableCell>
                <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>{mav.primary}</Typography>
                {mav.secondary && (
                  <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{mav.secondary}</Typography>
                )}
              </TableCell>
              <TableCell>{item.mavCategoryName ?? '—'}</TableCell>
              <TableCell>{item.facilityName}</TableCell>
              <TableCell>{item.locationCode ?? '—'}</TableCell>
              <TableCell>{item.status}</TableCell>
              <TableCell>{new Date(item.receivedAt).toLocaleString()}</TableCell>
            </TableRow>
          )
        })}
      </PortalTablePanel>
    </Box>
  )
}
