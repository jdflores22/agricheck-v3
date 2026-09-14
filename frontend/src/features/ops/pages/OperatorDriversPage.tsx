import { Alert, Box, Chip, TableCell, TableRow } from '@mui/material'
import { useGetOperatorDriversQuery } from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'

export function OperatorDriversPage() {
  const { data, isLoading } = useGetOperatorDriversQuery()
  const drivers = data?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="AgriTrack"
        title="Fleet drivers"
        subtitle="Drivers who registered with your invite code and selected you as their operator."
      />

      {drivers.length === 0 && !isLoading && (
        <Alert severity="info" sx={{ mb: 2 }}>
          No drivers yet. Share invite codes from Driver Invites so drivers can join your fleet in the AgriTrack app.
        </Alert>
      )}

      <PortalTablePanel
        title="Registered drivers"
        columns={['Name', 'Email', 'Phone', 'Vehicle', 'Profile']}
        isLoading={isLoading}
        isEmpty={!isLoading && drivers.length === 0}
        emptyMessage="No drivers registered under your operator account."
      >
        {drivers.map((driver) => (
          <TableRow key={driver.userUuid} hover>
            <TableCell>{driver.fullName}</TableCell>
            <TableCell>{driver.email}</TableCell>
            <TableCell>{driver.phoneNumber ?? '—'}</TableCell>
            <TableCell>
              {driver.vehicleRegistration
                ? `${driver.vehicleRegistration}${driver.vehicleType ? ` (${driver.vehicleType})` : ''}`
                : '—'}
            </TableCell>
            <TableCell>
              <Chip
                size="small"
                label={driver.profileComplete ? 'Complete' : 'Incomplete'}
                color={driver.profileComplete ? 'success' : 'default'}
              />
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
