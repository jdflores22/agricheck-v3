import {
  Alert,
  Box,
  Button,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
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
import { useAppSelector } from '../../../app/hooks'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetAdminApprovedEntriesQuery,
  useGetAdminCertificatesQuery,
  useIssueAdminCertificateMutation,
  useRevokeAdminCertificateMutation,
} from '../api/adminApi'

export function AdminCertificatesPage() {
  const { data: certsData, isLoading } = useGetAdminCertificatesQuery({ page: 1 })
  const { data: entriesData } = useGetAdminApprovedEntriesQuery({ page: 1 })
  const [issueCert, { isLoading: issuing, error: issueError }] = useIssueAdminCertificateMutation()
  const [revokeCert, { isLoading: revoking }] = useRevokeAdminCertificateMutation()
  const [issueOpen, setIssueOpen] = useState(false)
  const [revokeOpen, setRevokeOpen] = useState<string | null>(null)
  const [selectedEntry, setSelectedEntry] = useState('')
  const [revokeReason, setRevokeReason] = useState('')
  const accessToken = useAppSelector((state) => state.auth.accessToken)

  const downloadPdf = async (uuid: string, fileName: string) => {
    const res = await fetch(`/api/v1/admin/certificates/${uuid}/pdf`, {
      headers: accessToken ? { Authorization: `Bearer ${accessToken}` } : {},
    })
    if (!res.ok) return
    const blob = await res.blob()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = `${fileName}.pdf`
    link.click()
    URL.revokeObjectURL(url)
  }

  const certificates = certsData?.data?.items ?? []
  const approvedEntries = entriesData?.data?.items ?? []

  const handleIssue = async (e: FormEvent) => {
    e.preventDefault()
    if (!selectedEntry) return
    await issueCert({ entryUuid: selectedEntry }).unwrap()
    setIssueOpen(false)
    setSelectedEntry('')
  }

  const handleRevoke = async (e: FormEvent) => {
    e.preventDefault()
    if (!revokeOpen) return
    await revokeCert({ uuid: revokeOpen, reason: revokeReason }).unwrap()
    setRevokeOpen(null)
    setRevokeReason('')
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Certificates"
        subtitle="Issue and manage certificates for approved entries."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setIssueOpen(true)}>
            Issue Certificate
          </Button>
        }
      />

      <PortalTablePanel
        title="All certificates"
        columns={['Number', 'Title', 'Holder', 'Entry', 'Status', 'Issued', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && certificates.length === 0}
        emptyMessage="No certificates issued yet."
      >
        {certificates.map((cert) => (
          <TableRow key={cert.uuid} hover>
            <TableCell>{cert.certificateNumber}</TableCell>
            <TableCell>{cert.title}</TableCell>
            <TableCell>{cert.holderName}</TableCell>
            <TableCell>{cert.entryReferenceNo ?? '—'}</TableCell>
            <TableCell>
              <Chip size="small" label={cert.status} sx={portalStatusChipSx(cert.status)} />
            </TableCell>
            <TableCell>{new Date(cert.issuedAt).toLocaleDateString()}</TableCell>
            <TableCell align="right">
              <Button size="small" onClick={() => downloadPdf(cert.uuid, cert.certificateNumber)}>PDF</Button>
              {cert.status === 'Active' && (
                <Button size="small" color="error" onClick={() => setRevokeOpen(cert.uuid)}>Revoke</Button>
              )}
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={issueOpen} onClose={() => setIssueOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleIssue}>
          <DialogTitle>Issue Certificate</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              {issueError && <Alert severity="error">Could not issue certificate. Entry must be approved and have no active certificate.</Alert>}
              <FormControl fullWidth required>
                <InputLabel>Approved Entry</InputLabel>
                <Select
                  label="Approved Entry"
                  value={selectedEntry}
                  onChange={(e) => setSelectedEntry(e.target.value)}
                >
                  {approvedEntries.filter((entry) => !entry.hasActiveCertificate).map((entry) => (
                    <MenuItem key={entry.uuid} value={entry.uuid}>
                      {entry.referenceNo} — {entry.applicantName} ({entry.agencyCode})
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setIssueOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={issuing || !selectedEntry}>Issue</Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={!!revokeOpen} onClose={() => setRevokeOpen(null)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleRevoke}>
          <DialogTitle>Revoke Certificate</DialogTitle>
          <DialogContent>
            <TextField
              label="Reason"
              value={revokeReason}
              onChange={(e) => setRevokeReason(e.target.value)}
              required
              fullWidth
              multiline
              minRows={3}
              sx={{ mt: 1 }}
            />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setRevokeOpen(null)}>Cancel</Button>
            <Button type="submit" variant="contained" color="error" disabled={revoking || !revokeReason.trim()}>Revoke</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
