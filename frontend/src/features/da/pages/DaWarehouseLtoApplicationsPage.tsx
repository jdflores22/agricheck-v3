import { FormEvent, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetDaPendingWarehouseLtoApplicationsQuery,
  useGetDaRecentWarehouseLtoApplicationsQuery,
  useResolveDaWarehouseLtoApplicationMutation,
  type DaWarehouseLtoApplicationListItem,
} from '../api/daApi'

export function DaWarehouseLtoApplicationsPage() {
  const { data, isLoading } = useGetDaPendingWarehouseLtoApplicationsQuery()
  const { data: recentData } = useGetDaRecentWarehouseLtoApplicationsQuery()
  const items = data?.data ?? []
  const recent = (recentData?.data ?? []).filter((r) => r.status !== 'Pending')
  const [resolve, { isLoading: resolving }] = useResolveDaWarehouseLtoApplicationMutation()

  const [selected, setSelected] = useState<DaWarehouseLtoApplicationListItem | null>(null)
  const [licenseNumber, setLicenseNumber] = useState('')
  const [issuedAt, setIssuedAt] = useState('')
  const [expiresAt, setExpiresAt] = useState('')
  const [notes, setNotes] = useState('')
  const [rejectComments, setRejectComments] = useState('')

  const openApprove = (row: DaWarehouseLtoApplicationListItem) => {
    setSelected(row)
    setLicenseNumber('')
    const today = new Date().toISOString().slice(0, 10)
    setIssuedAt(today)
    const nextYear = new Date()
    nextYear.setFullYear(nextYear.getFullYear() + 1)
    setExpiresAt(nextYear.toISOString().slice(0, 10))
    setNotes('')
    setRejectComments('')
  }

  const handleApprove = async (e: FormEvent) => {
    e.preventDefault()
    if (!selected) return
    await resolve({
      uuid: selected.uuid,
      body: {
        approve: true,
        issueLicense: {
          licenseNumber,
          issuedAt: new Date(issuedAt).toISOString(),
          expiresAt: new Date(expiresAt).toISOString(),
          notes: notes || undefined,
        },
      },
    }).unwrap()
    setSelected(null)
  }

  const handleReject = async () => {
    if (!selected) return
    await resolve({
      uuid: selected.uuid,
      body: { approve: false, reviewComments: rejectComments || 'Rejected' },
    }).unwrap()
    setSelected(null)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="DA oversight"
        title="Warehouse LTO applications"
        subtitle="Review operator-submitted applications and issue the DA warehouse operating license."
      />

      {items.length === 0 && !isLoading && (
        <Alert severity="info" sx={{ mb: 2 }}>
          No <strong>pending</strong> applications in the database. Common reasons: (1) the warehouse already has an active
          LTO from dev backfill or direct issue on <strong>DA → Warehouses → detail</strong>—the operator cannot submit
          while LTO is active; (2) the submit failed (warehouse portal now shows the API error). Use{' '}
          <strong>Warehouses → Manila Cold Storage → Issue warehouse LTO</strong> when LTO was added outside this queue.
        </Alert>
      )}

      <PortalTablePanel
        title="Pending applications"
        columns={['Warehouse', 'Operator', 'Submitted', 'Contact', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage="No pending LTO applications."
      >
        {items.map((row) => (
          <TableRow key={row.uuid} hover>
            <TableCell>
              <Typography sx={{ fontWeight: 600 }}>{row.warehouseCode}</Typography>
              <Typography sx={{ fontSize: '0.8125rem' }}>{row.warehouseName}</Typography>
            </TableCell>
            <TableCell>{row.operatorLegalName}</TableCell>
            <TableCell>{new Date(row.submittedAt).toLocaleString()}</TableCell>
            <TableCell>{row.submittedByEmail ?? '—'}</TableCell>
            <TableCell>
              <Button size="small" variant="contained" sx={portalPrimaryButtonSx} onClick={() => openApprove(row)}>
                Review
              </Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      {recent.length > 0 && (
        <Box sx={{ mt: 3 }}>
          <PortalTablePanel
            title="Recent applications (resolved)"
            columns={['Warehouse', 'Operator', 'Status', 'Submitted']}
            isEmpty={false}
          >
            {recent.map((row) => (
              <TableRow key={row.uuid} hover>
                <TableCell>
                  <Typography sx={{ fontWeight: 600 }}>{row.warehouseCode}</Typography>
                  <Typography sx={{ fontSize: '0.8125rem' }}>{row.warehouseName}</Typography>
                </TableCell>
                <TableCell>{row.operatorLegalName}</TableCell>
                <TableCell>{row.status}</TableCell>
                <TableCell>{new Date(row.submittedAt).toLocaleString()}</TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
        </Box>
      )}

      <Dialog open={selected !== null} onClose={() => setSelected(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Resolve LTO application</DialogTitle>
        <form onSubmit={handleApprove}>
          <DialogContent>
            {selected && (
              <Stack spacing={2}>
                <Alert severity="info">
                  {selected.warehouseName} — {selected.operatorLegalName}
                </Alert>
                <TextField label="License number" value={licenseNumber} onChange={(e) => setLicenseNumber(e.target.value)} required fullWidth />
                <TextField
                  label="Issued date"
                  type="date"
                  value={issuedAt}
                  onChange={(e) => setIssuedAt(e.target.value)}
                  required
                  fullWidth
                  slotProps={{ inputLabel: { shrink: true } }}
                />
                <TextField
                  label="Expiry date"
                  type="date"
                  value={expiresAt}
                  onChange={(e) => setExpiresAt(e.target.value)}
                  required
                  fullWidth
                  slotProps={{ inputLabel: { shrink: true } }}
                />
                <TextField label="Notes" value={notes} onChange={(e) => setNotes(e.target.value)} fullWidth multiline minRows={2} />
                <TextField
                  label="Rejection comments (if rejecting)"
                  value={rejectComments}
                  onChange={(e) => setRejectComments(e.target.value)}
                  fullWidth
                  multiline
                  minRows={2}
                />
              </Stack>
            )}
          </DialogContent>
          <DialogActions>
            <Button color="error" onClick={handleReject} disabled={resolving}>
              Reject
            </Button>
            <Button onClick={() => setSelected(null)}>Cancel</Button>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={resolving || !licenseNumber.trim()}>
              Approve & issue LTO
            </Button>
          </DialogActions>
        </form>
      </Dialog>
    </Box>
  )
}
