import { useState } from 'react'
import {
  Alert,
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
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner'
import {
  useClaimDoctorContainerMutation,
  useCompleteDoctorInspectionMutation,
  useGetDoctorContainersQuery,
  useScanDoctorHandoffQrMutation,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  PortalQrScanDialog,
  doctorDriverHandoffQrPreset,
} from '../../../components/portal/PortalQrScanDialog'
import { DoctorBorderInspectionPhotos } from '../components/DoctorBorderInspectionPhotos'

export function DoctorContainersPage() {
  const { data, isLoading } = useGetDoctorContainersQuery()
  const [claimContainer] = useClaimDoctorContainerMutation()
  const [completeInspection, { isLoading: completing }] = useCompleteDoctorInspectionMutation()
  const [scanHandoff, { isLoading: scanningHandoff }] = useScanDoctorHandoffQrMutation()
  const [scanOpen, setScanOpen] = useState(false)
  const [message, setMessage] = useState('')
  const [completeTarget, setCompleteTarget] = useState<string | null>(null)
  const [decision, setDecision] = useState<'Approved' | 'Rejected'>('Approved')
  const [findings, setFindings] = useState('')

  const containers = data?.data ?? []

  const handleClaim = async (uuid: string) => {
    try {
      await claimContainer(uuid).unwrap()
      setMessage('Container claimed for inspection.')
    } catch {
      setMessage('Failed to claim container.')
    }
  }

  const submitHandoffQr = async (qrData: string): Promise<boolean> => {
    try {
      await scanHandoff({ qrData: qrData.trim() }).unwrap()
      setMessage('Delivery handoff QR accepted — trip ended and inspection started.')
      return true
    } catch {
      return false
    }
  }

  const handleComplete = async () => {
    if (!completeTarget) return
    try {
      await completeInspection({
        uuid: completeTarget,
        decision,
        findings: findings.trim() || undefined,
      }).unwrap()
      setMessage(`Inspection marked as ${decision}.`)
      setCompleteTarget(null)
      setFindings('')
      setDecision('Approved')
    } catch {
      setMessage('Failed to complete inspection.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Inspection"
        title="Doctor Inspection Queue"
        subtitle="Scan the driver’s live AgriTrack warehouse QR to end the trip, then complete inspection."
        actions={
          <Button
            size="small"
            variant="contained"
            sx={portalPrimaryButtonSx}
            startIcon={<QrCodeScannerIcon />}
            onClick={() => setScanOpen(true)}
          >
            Scan delivery QR
          </Button>
        }
      />
      {message && <Alert severity="info" sx={{ mb: 2 }} onClose={() => setMessage('')}>{message}</Alert>}
      <PortalTablePanel
        title="Inspectable containers"
        columns={['Container', 'Entry', 'Status', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && containers.length === 0}
        emptyMessage="No containers awaiting doctor inspection."
      >
        {containers.map((container) => (
          <TableRow key={container.uuid} hover>
            <TableCell>{container.containerNumber}</TableCell>
            <TableCell>{container.entryReference}</TableCell>
            <TableCell>{container.status}</TableCell>
            <TableCell>
              <Stack direction="row" spacing={1}>
                <Button size="small" variant="contained" sx={portalPrimaryButtonSx} onClick={() => handleClaim(container.uuid)}>
                  Claim
                </Button>
                <Button size="small" variant="outlined" sx={portalOutlinedButtonSx} onClick={() => setCompleteTarget(container.uuid)}>
                  Complete
                </Button>
              </Stack>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <PortalQrScanDialog
        open={scanOpen}
        busy={scanningHandoff}
        preset={doctorDriverHandoffQrPreset}
        onClose={() => setScanOpen(false)}
        onSubmit={submitHandoffQr}
      />

      <Dialog open={Boolean(completeTarget)} onClose={() => !completing && setCompleteTarget(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Complete inspection</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              select
              label="Decision"
              value={decision}
              onChange={(e) => setDecision(e.target.value as 'Approved' | 'Rejected')}
              fullWidth
            >
              <MenuItem value="Approved">Approved</MenuItem>
              <MenuItem value="Rejected">Rejected</MenuItem>
            </TextField>
            <TextField
              label="Findings (optional)"
              value={findings}
              onChange={(e) => setFindings(e.target.value)}
              fullWidth
              multiline
              minRows={3}
            />
            {completeTarget && decision === 'Approved' && (
              <DoctorBorderInspectionPhotos containerUuid={completeTarget} />
            )}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setCompleteTarget(null)} disabled={completing} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleComplete} variant="contained" disabled={completing} sx={portalPrimaryButtonSx}>
            {completing ? 'Saving…' : 'Submit decision'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
