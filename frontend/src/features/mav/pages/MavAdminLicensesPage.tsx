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
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { useGetMavAdminLicensesQuery, useRevokeMavLicenseMutation } from '../api/mavApi'

export function MavAdminLicensesPage() {
  const { data, isLoading } = useGetMavAdminLicensesQuery()
  const [revoke] = useRevokeMavLicenseMutation()
  const [revokeOpen, setRevokeOpen] = useState<string | null>(null)
  const [reason, setReason] = useState('')
  const licenses = data?.data ?? []

  const handleRevoke = async (e: FormEvent) => {
    e.preventDefault()
    if (!revokeOpen) return
    await revoke({ uuid: revokeOpen, reason }).unwrap()
    setRevokeOpen(null)
    setReason('')
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Administration"
        title="MAV Licenses"
        subtitle="Manage and revoke issued MAV licenses."
      />

      <PortalTablePanel
        title="All licenses"
        columns={['License', 'Holder', 'Commodity', 'Awarded', 'Available', 'Status', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && licenses.length === 0}
        emptyMessage="No licenses found."
      >
        {licenses.map((lic) => (
          <TableRow key={lic.uuid} hover>
            <TableCell>{lic.licenseNumber}</TableCell>
            <TableCell>{lic.holderName}</TableCell>
            <TableCell>{lic.commodityName}</TableCell>
            <TableCell>{lic.awardedVolume}</TableCell>
            <TableCell>{lic.availableVolume}</TableCell>
            <TableCell>{lic.status}</TableCell>
            <TableCell align="right">
              {lic.status === 'Active' && <Button size="small" color="error" onClick={() => setRevokeOpen(lic.uuid)}>Revoke</Button>}
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={!!revokeOpen} onClose={() => setRevokeOpen(null)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleRevoke}>
          <DialogTitle>Revoke License</DialogTitle>
          <DialogContent>
            <TextField label="Reason" value={reason} onChange={(e) => setReason(e.target.value)} required fullWidth multiline minRows={3} sx={{ mt: 1 }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setRevokeOpen(null)}>Cancel</Button>
            <Button type="submit" variant="contained" color="error">Revoke</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
