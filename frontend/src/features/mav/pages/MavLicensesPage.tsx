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
  Typography,
} from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetMavLicenseQuery, useGetMyMavLicensesQuery, useIssueMicMutation } from '../api/mavApi'

export function MavLicensesPage() {
  const { data, isLoading } = useGetMyMavLicensesQuery()
  const licenses = data?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Licenses"
        title="My MAV Licenses"
        subtitle="View awarded licenses and issue import certificates."
      />

      <PortalTablePanel
        title="All licenses"
        columns={['License No.', 'Year / Pool', 'Commodity', 'Awarded', 'Available', 'Status', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && licenses.length === 0}
        emptyMessage="No licenses yet."
      >
        {licenses.map((lic) => (
          <TableRow key={lic.uuid} hover>
            <TableCell>{lic.licenseNumber}</TableCell>
            <TableCell>{lic.mavYear} / {lic.poolType}</TableCell>
            <TableCell>{lic.commodityName}</TableCell>
            <TableCell>{lic.awardedVolume}</TableCell>
            <TableCell>{lic.availableVolume}</TableCell>
            <TableCell>{lic.status}</TableCell>
            <TableCell align="right">
              <Button size="small" component={RouterLink} to={`/mav/licenses/${lic.uuid}`}>View</Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}

export function MavLicenseDetailPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading } = useGetMavLicenseQuery(uuid, { skip: !uuid })
  const [issueMic, { isLoading: issuing }] = useIssueMicMutation()
  const [open, setOpen] = useState(false)
  const [volume, setVolume] = useState(10)
  const license = data?.data

  const handleIssue = async (e: FormEvent) => {
    e.preventDefault()
    await issueMic({ licenseUuid: uuid, volume }).unwrap()
    setOpen(false)
  }

  if (isLoading || !license) return <Typography>Loading…</Typography>

  return (
    <Box>
      <PortalPageHeader
        eyebrow="License"
        title={license.licenseNumber}
        subtitle={`${license.commodityName} · ${license.mavYear} ${license.poolType} · Available: ${license.availableVolume} MT`}
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setOpen(true)} disabled={license.status !== 'Active'}>
            Issue MIC
          </Button>
        }
      />

      <PortalTablePanel
        title="Import Certificates (MIC)"
        columns={['Certificate', 'Authorized', 'Available', 'Status', 'Expires']}
        isEmpty={license.importCertificates.length === 0}
        emptyMessage="No import certificates issued yet."
      >
        {license.importCertificates.map((mic) => (
          <TableRow key={mic.uuid}>
            <TableCell>{mic.certificateNumber}</TableCell>
            <TableCell>{mic.authorizedVolume}</TableCell>
            <TableCell>{mic.availableVolume}</TableCell>
            <TableCell>{mic.status}</TableCell>
            <TableCell>{new Date(mic.expiresAt).toLocaleDateString()}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      <Dialog open={open} onClose={() => setOpen(false)} maxWidth="xs" fullWidth>
        <Box component="form" onSubmit={handleIssue}>
          <DialogTitle>Issue MIC</DialogTitle>
          <DialogContent>
            <TextField label="Volume (MT)" type="number" value={volume} onChange={(e) => setVolume(Number(e.target.value))} fullWidth required sx={{ mt: 1 }} />
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={issuing}>Issue</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
