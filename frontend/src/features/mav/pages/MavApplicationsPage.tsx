import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { HsCodePicker, type HsCodeSelection } from '../components/HsCodePicker'
import {
  useCreateMavApplicationMutation,
  useGetMyMavApplicationsQuery,
  useGetOpenMavPeriodsQuery,
  useSubmitMavApplicationMutation,
} from '../api/mavApi'

const emptyHsSelection: HsCodeSelection = { categoryUuid: '', detailUuid: '', hsCode: '', commodityName: '' }

export function MavApplicationsPage() {
  const { data, isLoading } = useGetMyMavApplicationsQuery()
  const { data: periodsData } = useGetOpenMavPeriodsQuery()
  const [createApp, { isLoading: creating }] = useCreateMavApplicationMutation()
  const [submitApp] = useSubmitMavApplicationMutation()
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ periodUuid: '', requestedVolume: 100, hsSelection: emptyHsSelection })

  const apps = data?.data ?? []
  const periods = periodsData?.data ?? []

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault()
    await createApp({
      periodUuid: form.periodUuid,
      hsCode: form.hsSelection.hsCode,
      commodityName: form.hsSelection.commodityName,
      requestedVolume: form.requestedVolume,
      hsDetailUuid: form.hsSelection.detailUuid || undefined,
    }).unwrap()
    setOpen(false)
    setForm({ periodUuid: '', requestedVolume: 100, hsSelection: emptyHsSelection })
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Applications"
        title="My MAV Applications"
        subtitle="Submit and track your MAV allocation applications."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setOpen(true)} disabled={periods.length === 0}>
            New Application
          </Button>
        }
      />

      <PortalTablePanel
        title="All applications"
        columns={['Reference', 'Year / Pool', 'Commodity', 'Volume (MT)', 'Status', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && apps.length === 0}
        emptyMessage="No applications yet."
      >
        {apps.map((app) => (
          <TableRow key={app.uuid} hover>
            <TableCell>{app.referenceNumber}</TableCell>
            <TableCell>{app.mavYear} / {app.poolType}</TableCell>
            <TableCell>{app.commodityName} ({app.hsCode})</TableCell>
            <TableCell>{app.requestedVolume}</TableCell>
            <TableCell>{app.status}</TableCell>
            <TableCell align="right">
              <Button size="small" component={RouterLink} to={`/mav/applications/${app.uuid}`}>View</Button>
              {app.status === 'Draft' && (
                <Button size="small" onClick={() => submitApp(app.uuid)}>Submit</Button>
              )}
              {app.status === 'Approved' && (
                <Button size="small" component={RouterLink} to="/mav/licenses">Licenses</Button>
              )}
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleCreate}>
          <DialogTitle>New MAV Application</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField select label="Application Period" value={form.periodUuid} onChange={(e) => setForm({ ...form, periodUuid: e.target.value })} required fullWidth>
                {periods.map((p) => <MenuItem key={p.uuid} value={p.uuid}>{p.mavYear} {p.poolType} ({p.status})</MenuItem>)}
              </TextField>
              <HsCodePicker
                value={form.hsSelection}
                onChange={(hsSelection) => setForm({ ...form, hsSelection })}
              />
              <TextField label="Requested Volume (MT)" type="number" value={form.requestedVolume} onChange={(e) => setForm({ ...form, requestedVolume: Number(e.target.value) })} required fullWidth />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={creating || !form.hsSelection.detailUuid}>Create Draft</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
