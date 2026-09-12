import {
  Alert,
  Box,
  Button,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { FormEvent, useEffect, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useCancelBookingMutation,
  useCreateBookingMutation,
  useGetBookingsQuery,
  useGetContainerQuery,
  useGetWarehouseFacilitiesQuery,
} from '../api/clientApi'

export function WarehouseBookingsPage() {
  const [searchParams] = useSearchParams()
  const containerUuid = searchParams.get('containerUuid') ?? ''
  const { data: containerData } = useGetContainerQuery(containerUuid, { skip: !containerUuid })
  const container = containerData?.data
  const { data: facilitiesData } = useGetWarehouseFacilitiesQuery()
  const { data: bookingsData, isLoading, refetch } = useGetBookingsQuery()
  const [createBooking, { isLoading: creating }] = useCreateBookingMutation()
  const [cancelBooking] = useCancelBookingMutation()
  const [form, setForm] = useState({ warehouseFacilityId: '', containerReference: '', scheduledDate: '', notes: '' })
  const bookings = bookingsData?.data?.items ?? []

  useEffect(() => {
    if (container?.containerNumber) {
      setForm((current) => ({ ...current, containerReference: container.containerNumber }))
    }
  }, [container?.containerNumber])

  const canBook = !containerUuid || container?.canBookWarehouse === true

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault()
    if (!canBook) return
    await createBooking({
      warehouseFacilityId: Number(form.warehouseFacilityId),
      containerReference: form.containerReference,
      scheduledDate: new Date(form.scheduledDate).toISOString(),
      notes: form.notes,
    }).unwrap()
    setForm((current) => ({
      ...current,
      warehouseFacilityId: '',
      scheduledDate: '',
      notes: '',
    }))
    refetch()
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Warehouse Bookings"
        subtitle="Schedule container storage at warehouse facilities."
      />

      {containerUuid && container && !container.canBookWarehouse && (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {container.warehouseBookingBlockedReason ??
            'Warehouse booking is not available for this container yet.'}
          {' '}
          <Button component={RouterLink} to={`/client/containers/${container.uuid}`} size="small">
            View container
          </Button>
        </Alert>
      )}

      {containerUuid && container?.canBookWarehouse && (
        <Alert severity="success" sx={{ mb: 2 }}>
          Booking warehouse storage for container <strong>{container.containerNumber}</strong>.
        </Alert>
      )}

      <PortalPanel title="Create booking">
        <Box component="form" onSubmit={handleCreate} sx={{ p: 2.5 }}>
          <Stack spacing={2}>
            <TextField select label="Warehouse" value={form.warehouseFacilityId} onChange={(e) => setForm({ ...form, warehouseFacilityId: e.target.value })} required fullWidth disabled={!canBook}>
              {(facilitiesData?.data ?? []).map((f) => <MenuItem key={f.id} value={String(f.id)}>{f.name}</MenuItem>)}
            </TextField>
            <TextField label="Container Reference" value={form.containerReference} onChange={(e) => setForm({ ...form, containerReference: e.target.value })} required fullWidth disabled={!canBook || Boolean(containerUuid)} />
            <TextField label="Scheduled Date" type="datetime-local" value={form.scheduledDate} onChange={(e) => setForm({ ...form, scheduledDate: e.target.value })} required fullWidth disabled={!canBook} />
            <TextField label="Notes" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} fullWidth disabled={!canBook} />
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={creating || !canBook}>Create Booking</Button>
          </Stack>
        </Box>
      </PortalPanel>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="All bookings"
          columns={['Booking No.', 'Warehouse', 'Container', 'Status', 'Scheduled', '']}
          isLoading={isLoading}
          isEmpty={!isLoading && bookings.length === 0}
          emptyMessage="No bookings yet."
        >
          {bookings.map((b) => (
            <TableRow key={b.uuid} hover>
              <TableCell>{b.bookingNumber}</TableCell>
              <TableCell>{b.warehouseName}</TableCell>
              <TableCell>{b.containerReference}</TableCell>
              <TableCell>{b.status}</TableCell>
              <TableCell>{new Date(b.scheduledDate).toLocaleString()}</TableCell>
              <TableCell>
                <Stack direction="row" spacing={1}>
                  <Button size="small" component={RouterLink} to={`/client/warehouse/bookings/${b.uuid}`}>View</Button>
                  {b.status !== 'Cancelled' && b.status !== 'Completed' && (
                    <Button size="small" color="error" onClick={() => cancelBooking(b.uuid).then(() => refetch())}>Cancel</Button>
                  )}
                </Stack>
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
