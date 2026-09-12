import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { useCancelBookingMutation, useGetBookingQuery } from '../api/clientApi'

function formatCurrency(amount?: number) {
  if (amount == null) return '—'
  return new Intl.NumberFormat('en-PH', { style: 'currency', currency: 'PHP' }).format(amount)
}

export function WarehouseBookingDetailPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading, refetch } = useGetBookingQuery(uuid, { skip: !uuid })
  const [cancelBooking, { isLoading: cancelling }] = useCancelBookingMutation()
  const booking = data?.data

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading booking…</Typography>
  if (!booking) return <Alert severity="error">Booking not found.</Alert>

  const canCancel = !['Completed', 'Cancelled'].includes(booking.status)

  const handleCancel = async () => {
    await cancelBooking(uuid).unwrap()
    refetch()
  }

  return (
    <Box sx={{ maxWidth: 640 }}>
      <PortalPageHeader
        eyebrow="Warehouse"
        title={booking.bookingNumber}
        subtitle={booking.warehouseName}
        actions={
          <Button component={RouterLink} to="/client/warehouse/bookings" variant="outlined" sx={portalOutlinedButtonSx}>
            Back
          </Button>
        }
      />
      <Chip size="small" label={booking.status} sx={{ mb: 2, ...portalStatusChipSx(booking.status) }} />
      <PortalPanel title="Booking Details">
        <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
          <Typography variant="body2">Container: {booking.containerReference}</Typography>
          <Typography variant="body2">Scheduled: {new Date(booking.scheduledDate).toLocaleString()}</Typography>
          <Typography variant="body2">Amount: {formatCurrency(booking.amount)}</Typography>
          <Typography variant="body2">Notes: {booking.notes ?? '—'}</Typography>
          {canCancel && (
            <Button variant="outlined" color="error" sx={{ alignSelf: 'flex-start', mt: 1 }} disabled={cancelling} onClick={handleCancel}>
              Cancel Booking
            </Button>
          )}
        </Stack>
      </PortalPanel>
    </Box>
  )
}
