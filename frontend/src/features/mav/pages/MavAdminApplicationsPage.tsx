import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import {
  useApproveMavApplicationMutation,
  useGetMavAdminApplicationsQuery,
  useRejectMavApplicationMutation,
} from '../api/mavApi'

export function MavAdminApplicationsPage() {
  const { data, isLoading } = useGetMavAdminApplicationsQuery({ status: 'Submitted' })
  const [approve] = useApproveMavApplicationMutation()
  const [reject] = useRejectMavApplicationMutation()
  const [approveOpen, setApproveOpen] = useState<string | null>(null)
  const [rejectOpen, setRejectOpen] = useState<string | null>(null)
  const [allocatedVolume, setAllocatedVolume] = useState(100)
  const [reason, setReason] = useState('')

  const apps = data?.data ?? []

  const handleApprove = async (e: FormEvent) => {
    e.preventDefault()
    if (!approveOpen) return
    await approve({ uuid: approveOpen, allocatedVolume }).unwrap()
    setApproveOpen(null)
  }

  const handleReject = async (e: FormEvent) => {
    e.preventDefault()
    if (!rejectOpen) return
    await reject({ uuid: rejectOpen, reason }).unwrap()
    setRejectOpen(null)
    setReason('')
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Administration"
        title="Review Applications"
        subtitle="Approve or reject submitted MAV applications."
      />

      <PortalTablePanel
        title="Submitted applications"
        columns={['Reference', 'Applicant', 'Commodity', 'Requested', 'Status', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && apps.length === 0}
        emptyMessage="No submitted applications."
      >
        {apps.map((app) => (
          <TableRow key={app.uuid} hover>
            <TableCell>{app.referenceNumber}</TableCell>
            <TableCell>{app.applicantName}</TableCell>
            <TableCell>{app.commodityName}</TableCell>
            <TableCell>{app.requestedVolume}</TableCell>
            <TableCell>{app.status}</TableCell>
            <TableCell align="right">
              <Button size="small" component={RouterLink} to={`/mav/applications/${app.uuid}`}>View</Button>
              <Button size="small" onClick={() => { setApproveOpen(app.uuid); setAllocatedVolume(app.requestedVolume) }}>Approve</Button>
              <Button size="small" color="error" onClick={() => setRejectOpen(app.uuid)}>Reject</Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={!!approveOpen} onClose={() => setApproveOpen(null)} maxWidth="xs" fullWidth>
        <Box component="form" onSubmit={handleApprove}>
          <DialogTitle>Approve Application</DialogTitle>
          <DialogContent>
            <TextField label="Allocated Volume (MT)" type="number" value={allocatedVolume} onChange={(e) => setAllocatedVolume(Number(e.target.value))} fullWidth required sx={{ mt: 1 }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setApproveOpen(null)}>Cancel</Button>
            <Button type="submit" variant="contained">Approve & Issue License</Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={!!rejectOpen} onClose={() => setRejectOpen(null)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleReject}>
          <DialogTitle>Reject Application</DialogTitle>
          <DialogContent>
            <TextField label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} fullWidth required multiline minRows={3} sx={{ mt: 1 }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setRejectOpen(null)}>Cancel</Button>
            <Button type="submit" variant="contained" color="error">Reject</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
