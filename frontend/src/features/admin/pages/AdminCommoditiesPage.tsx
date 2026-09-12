import {
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
} from '@mui/material'
import { FormEvent, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useCreateAdminCommodityMutation, useGetAdminCommoditiesQuery } from '../api/adminApi'

export function AdminCommoditiesPage() {
  const { data, isLoading } = useGetAdminCommoditiesQuery()
  const [createCommodity, { isLoading: creating }] = useCreateAdminCommodityMutation()
  const [open, setOpen] = useState(false)
  const [form, setForm] = useState({ categoryId: 1, code: '', name: '' })

  const commodities = data?.data ?? []

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    await createCommodity(form).unwrap()
    setOpen(false)
    setForm({ categoryId: 1, code: '', name: '' })
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Commodities"
        subtitle="Manage commodity catalog entries."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setOpen(true)}>
            Add Commodity
          </Button>
        }
      />

      <PortalTablePanel
        title="All commodities"
        columns={['Code', 'Name', 'Category', 'Active']}
        isLoading={isLoading}
        isEmpty={!isLoading && commodities.length === 0}
        emptyMessage="No commodities found."
      >
        {commodities.map((item) => (
          <TableRow key={item.id} hover>
            <TableCell>{item.code}</TableCell>
            <TableCell>{item.name}</TableCell>
            <TableCell>{item.categoryName}</TableCell>
            <TableCell>{item.isActive ? 'Yes' : 'No'}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleSubmit}>
          <DialogTitle>Create Commodity</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Category ID" type="number" value={form.categoryId} onChange={(e) => setForm({ ...form, categoryId: Number(e.target.value) })} required fullWidth />
              <TextField label="Code" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value.toUpperCase() })} required fullWidth />
              <TextField label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required fullWidth />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={creating}>Create</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
