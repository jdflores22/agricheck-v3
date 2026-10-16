import { Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, TableCell, TableRow, TextField } from '@mui/material'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetAgencySpsApplicationsQuery, useIssueAgencySpsClearanceMutation } from '../api/agencyApi'

export function AgencySpsReviewPage() {
  const { data, isLoading } = useGetAgencySpsApplicationsQuery({ page: 1 })
  const items = data?.data?.items ?? []
  const [issueUuid, setIssueUuid] = useState<string | null>(null)
  const [clearanceNumber, setClearanceNumber] = useState('')
  const [issuedAt, setIssuedAt] = useState('')
  const [issue, { isLoading: issuing }] = useIssueAgencySpsClearanceMutation()

  const handleIssue = async () => {
    if (!issueUuid) return
    await issue({
      uuid: issueUuid,
      clearanceNumber,
      issuedAt: new Date(issuedAt).toISOString(),
    }).unwrap()
    setIssueUuid(null)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="SPSIC"
        title="SPS import clearance queue"
        subtitle="Issue clearance numbers for submitted entry applications."
      />
      <PortalTablePanel
        title="Submitted SPS applications"
        columns={['Entry', 'Importer', 'Submitted', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && items.length === 0}
        emptyMessage="No SPS applications awaiting issue."
      >
        {items.map((item) => (
          <TableRow key={item.uuid}>
            <TableCell>
              <Button component={RouterLink} to={`/agency/evaluator/entries/${item.entryUuid}`} size="small">
                {item.referenceNo}
              </Button>
            </TableCell>
            <TableCell>{item.importerName}</TableCell>
            <TableCell>{item.submittedAt ? new Date(item.submittedAt).toLocaleDateString() : '—'}</TableCell>
            <TableCell>
              <Button size="small" onClick={() => setIssueUuid(item.uuid)}>Issue SPSIC</Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
      <Dialog open={Boolean(issueUuid)} onClose={() => setIssueUuid(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Record issued SPSIC</DialogTitle>
        <DialogContent>
          <TextField fullWidth size="small" label="Clearance number" sx={{ mt: 1, mb: 2 }} value={clearanceNumber} onChange={(e) => setClearanceNumber(e.target.value)} />
          <TextField fullWidth size="small" type="date" label="Issued date" slotProps={{ inputLabel: { shrink: true } }} value={issuedAt} onChange={(e) => setIssuedAt(e.target.value)} />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setIssueUuid(null)}>Cancel</Button>
          <Button variant="contained" sx={portalPrimaryButtonSx} disabled={issuing} onClick={handleIssue}>Issue</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
