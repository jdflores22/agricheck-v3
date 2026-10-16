import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, TableCell, TableRow, TextField } from '@mui/material'
import { useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useApproveAgencyLicenseApplicationMutation,
  useGetAgencyLicenseApplicationsQuery,
  useRejectAgencyLicenseApplicationMutation,
} from '../api/agencyApi'

export function AgencyLicenseReviewPage() {
  const { data, isLoading } = useGetAgencyLicenseApplicationsQuery({ page: 1 })
  const items = data?.data?.items ?? []
  const [approveUuid, setApproveUuid] = useState<string | null>(null)
  const [licenseNumber, setLicenseNumber] = useState('')
  const [issuedAt, setIssuedAt] = useState('')
  const [expiresAt, setExpiresAt] = useState('')
  const [approve, { isLoading: approving }] = useApproveAgencyLicenseApplicationMutation()
  const [reject] = useRejectAgencyLicenseApplicationMutation()

  const handleApprove = async () => {
    if (!approveUuid) return
    await approve({
      uuid: approveUuid,
      licenseNumber,
      issuedAt: new Date(issuedAt).toISOString(),
      expiresAt: new Date(expiresAt).toISOString(),
    }).unwrap()
    setApproveUuid(null)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Compliance"
        title="Bureau license applications"
        subtitle="Review importer LTO/LTI applications for your agency."
      />
      <PortalTablePanel
        title="Submitted applications"
        columns={['Applicant', 'Type', 'Status', 'Submitted', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage="No license applications pending."
      >
        {items.map((item) => (
          <TableRow key={item.uuid}>
            <TableCell>{item.applicantName}</TableCell>
            <TableCell>{item.licenseTypeCode}</TableCell>
            <TableCell>{item.status}</TableCell>
            <TableCell>{item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}</TableCell>
            <TableCell>
              {item.status === 'Submitted' && (
                <>
                  <Button size="small" onClick={() => setApproveUuid(item.uuid)}>Approve</Button>
                  <Button size="small" color="error" onClick={() => reject({ uuid: item.uuid, reviewComments: 'Incomplete requirements' })}>
                    Reject
                  </Button>
                </>
              )}
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
      <Dialog open={Boolean(approveUuid)} onClose={() => setApproveUuid(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Issue bureau license</DialogTitle>
        <DialogContent>
          <TextField fullWidth size="small" label="License number" sx={{ mt: 1, mb: 2 }} value={licenseNumber} onChange={(e) => setLicenseNumber(e.target.value)} />
          <TextField fullWidth size="small" type="date" label="Issued" slotProps={{ inputLabel: { shrink: true } }} sx={{ mb: 2 }} value={issuedAt} onChange={(e) => setIssuedAt(e.target.value)} />
          <TextField fullWidth size="small" type="date" label="Expires" slotProps={{ inputLabel: { shrink: true } }} value={expiresAt} onChange={(e) => setExpiresAt(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setApproveUuid(null)}>Cancel</Button>
          <Button variant="contained" sx={portalPrimaryButtonSx} disabled={approving} onClick={handleApprove}>Approve</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
