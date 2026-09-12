import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import { Alert, Box, Button, TableCell, TableRow, Typography } from '@mui/material'
import { PortalTablePanel } from '../../components/portal/PortalTablePanel'
import { portalColors } from '../../components/portal/portalTheme'
import { useGetRegisteredWarehousesQuery } from '../addresses/addressApi'

interface RegisteredWarehousesPanelProps {
  title?: string
  subtitle?: string
}

export function RegisteredWarehousesPanel({
  title = 'Registered Warehouses',
  subtitle = 'DA-registered warehouse facilities available for container entries.',
}: RegisteredWarehousesPanelProps) {
  const { data, isLoading, isError, refetch } = useGetRegisteredWarehousesQuery()
  const warehouses = data?.data ?? []

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
        <WarehouseOutlinedIcon sx={{ color: portalColors.primary }} />
        <Box>
          <Typography sx={{ fontWeight: 600, color: portalColors.textDark }}>{title}</Typography>
          <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>{subtitle}</Typography>
        </Box>
      </Box>

      {isError ? (
        <Alert
          severity="warning"
          action={
            <Button color="inherit" size="small" onClick={() => refetch()}>
              Retry
            </Button>
          }
        >
          Unable to load registered warehouses.
        </Alert>
      ) : (
        <PortalTablePanel
          title={`${warehouses.length} warehouse${warehouses.length === 1 ? '' : 's'}`}
          columns={['Code', 'Warehouse', 'Address', 'Location']}
          isLoading={isLoading}
          isEmpty={!isLoading && warehouses.length === 0}
          emptyMessage="No registered warehouses found."
        >
          {warehouses.map((warehouse) => (
            <TableRow key={warehouse.id} hover>
              <TableCell>{warehouse.code}</TableCell>
              <TableCell>
                <Typography sx={{ fontWeight: 600 }}>{warehouse.name}</Typography>
              </TableCell>
              <TableCell sx={{ maxWidth: 360 }}>
                <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                  {warehouse.formattedAddress}
                </Typography>
              </TableCell>
              <TableCell>
                {warehouse.latitude != null && warehouse.longitude != null
                  ? `${warehouse.latitude.toFixed(4)}, ${warehouse.longitude.toFixed(4)}`
                  : '—'}
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      )}
    </Box>
  )
}
