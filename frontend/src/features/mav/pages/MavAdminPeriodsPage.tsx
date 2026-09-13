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
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetAgenciesQuery } from '../../client/api/clientApi'
import { HsCodePicker, type HsCodeSelection } from '../components/HsCodePicker'
import {
  useCloseMavPeriodMutation,
  useCreateMavPeriodMutation,
  useGetMavAdminPeriodsQuery,
  useOpenMavPeriodMutation,
  useUpsertMavAllocationMutation,
} from '../api/mavApi'

const emptyHsSelection: HsCodeSelection = { categoryUuid: '', detailUuid: '', hsCode: '', commodityName: '' }

export function MavAdminPeriodsPage() {
  const { data, isLoading } = useGetMavAdminPeriodsQuery()
  const { data: agenciesData } = useGetAgenciesQuery()
  const [createPeriod] = useCreateMavPeriodMutation()
  const [openPeriod] = useOpenMavPeriodMutation()
  const [closePeriod] = useCloseMavPeriodMutation()
  const [upsertAllocation] = useUpsertMavAllocationMutation()
  const [createOpen, setCreateOpen] = useState(false)
  const [allocOpen, setAllocOpen] = useState<string | null>(null)
  const [periodForm, setPeriodForm] = useState({ mavYear: 2026, poolType: 'BYP', openingDate: '2026-01-01', closingDate: '2026-03-31', agencyId: '' })
  const [allocForm, setAllocForm] = useState({ commodityId: 1, hsCode: '0201', commodityName: 'Beef Products', totalVolume: 10000, minimumImportVolume: 10 })
  const [allocHs, setAllocHs] = useState<HsCodeSelection>(emptyHsSelection)

  const periods = data?.data ?? []
  const agencies = agenciesData?.data ?? []
  const selectedAllocPeriod = periods.find((p) => p.uuid === allocOpen)

  const handleCreatePeriod = async (e: FormEvent) => {
    e.preventDefault()
    await createPeriod({
      mavYear: periodForm.mavYear,
      poolType: periodForm.poolType,
      openingDate: periodForm.openingDate,
      closingDate: periodForm.closingDate,
      agencyId: periodForm.agencyId ? Number(periodForm.agencyId) : undefined,
    }).unwrap()
    setCreateOpen(false)
  }

  const handleAlloc = async (e: FormEvent) => {
    e.preventDefault()
    if (!allocOpen) return
    await upsertAllocation({ periodUuid: allocOpen, ...allocForm }).unwrap()
    setAllocOpen(null)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Administration"
        title="Application Periods"
        subtitle="Create and manage MAV application periods and allocations."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setCreateOpen(true)}>
            New Period
          </Button>
        }
      />

      <PortalTablePanel
        title="All periods"
        columns={['Year', 'Agency', 'Pool', 'Status', 'Applications', 'Allocations', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && periods.length === 0}
        emptyMessage="No application periods yet."
      >
        {periods.map((p) => (
          <TableRow key={p.uuid} hover>
            <TableCell>{p.mavYear}</TableCell>
            <TableCell>{p.agencyCode ?? '—'}</TableCell>
            <TableCell>{p.poolType}</TableCell>
            <TableCell>{p.status}</TableCell>
            <TableCell>{p.applicationCount}</TableCell>
            <TableCell>{p.allocationCount}</TableCell>
            <TableCell align="right">
              {p.status !== 'Open' && <Button size="small" onClick={() => openPeriod(p.uuid)}>Open</Button>}
              {p.status === 'Open' && <Button size="small" onClick={() => closePeriod(p.uuid)}>Close</Button>}
              <Button size="small" onClick={() => { setAllocOpen(p.uuid); setAllocHs(emptyHsSelection) }}>Allocation</Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={createOpen} onClose={() => setCreateOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleCreatePeriod}>
          <DialogTitle>Create Period</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="MAV Year" type="number" value={periodForm.mavYear} onChange={(e) => setPeriodForm({ ...periodForm, mavYear: Number(e.target.value) })} fullWidth />
              <TextField select label="Agency" value={periodForm.agencyId} onChange={(e) => setPeriodForm({ ...periodForm, agencyId: e.target.value })} required fullWidth>
                {agencies.map((agency) => (
                  <MenuItem key={agency.id} value={String(agency.id)}>{agency.code} — {agency.name}</MenuItem>
                ))}
              </TextField>
              <TextField label="Pool Type (BYP/MYP)" value={periodForm.poolType} onChange={(e) => setPeriodForm({ ...periodForm, poolType: e.target.value })} fullWidth />
              <TextField label="Opening Date" type="date" value={periodForm.openingDate} onChange={(e) => setPeriodForm({ ...periodForm, openingDate: e.target.value })} fullWidth />
              <TextField label="Closing Date" type="date" value={periodForm.closingDate} onChange={(e) => setPeriodForm({ ...periodForm, closingDate: e.target.value })} fullWidth />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setCreateOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={!periodForm.agencyId}>Create</Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={!!allocOpen} onClose={() => setAllocOpen(null)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleAlloc}>
          <DialogTitle>Commodity Allocation</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Commodity ID" type="number" value={allocForm.commodityId} onChange={(e) => setAllocForm({ ...allocForm, commodityId: Number(e.target.value) })} fullWidth />
              <HsCodePicker
                value={allocHs}
                agencyId={selectedAllocPeriod?.agencyId ?? undefined}
                onChange={(selection) => {
                  setAllocHs(selection)
                  setAllocForm((current) => ({ ...current, hsCode: selection.hsCode, commodityName: selection.commodityName }))
                }}
              />
              <TextField label="Total Volume" type="number" value={allocForm.totalVolume} onChange={(e) => setAllocForm({ ...allocForm, totalVolume: Number(e.target.value) })} fullWidth />
              <TextField label="Minimum Import Volume" type="number" value={allocForm.minimumImportVolume} onChange={(e) => setAllocForm({ ...allocForm, minimumImportVolume: Number(e.target.value) })} fullWidth />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setAllocOpen(null)}>Cancel</Button>
            <Button type="submit" variant="contained">Save</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
