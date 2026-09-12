import {
  Box,
  Button,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useEffect, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { HsCodePicker, type HsCodeSelection } from '../components/HsCodePicker'
import { useGetMavApplicationQuery, useSubmitMavApplicationMutation, useUpdateMavApplicationMutation } from '../api/mavApi'

function formatDate(value?: string) {
  if (!value) return '—'
  return new Date(value).toLocaleString()
}

const emptyHsSelection: HsCodeSelection = { categoryUuid: '', detailUuid: '', hsCode: '', commodityName: '' }

export function MavApplicationDetailPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading } = useGetMavApplicationQuery(uuid, { skip: !uuid })
  const [updateApp, { isLoading: saving }] = useUpdateMavApplicationMutation()
  const [submitApp, { isLoading: submitting }] = useSubmitMavApplicationMutation()
  const [editing, setEditing] = useState(false)
  const [requestedVolume, setRequestedVolume] = useState(0)
  const [hsSelection, setHsSelection] = useState<HsCodeSelection>(emptyHsSelection)
  const app = data?.data

  useEffect(() => {
    if (!app) return
    setRequestedVolume(app.requestedVolume)
    setHsSelection({
      categoryUuid: '',
      detailUuid: '',
      hsCode: app.hsCode,
      commodityName: app.commodityName,
    })
  }, [app?.uuid, app?.hsCode, app?.commodityName, app?.requestedVolume])

  if (isLoading || !app) return <Typography>Loading…</Typography>

  const isDraft = app.status === 'Draft'

  const handleSave = async (e: FormEvent) => {
    e.preventDefault()
    await updateApp({
      uuid,
      hsCode: hsSelection.hsCode,
      commodityName: hsSelection.commodityName,
      requestedVolume,
      hsDetailUuid: hsSelection.detailUuid || undefined,
    }).unwrap()
    setEditing(false)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Application"
        title={app.referenceNumber}
        subtitle={`${app.commodityName} (${app.hsCode}) · ${app.mavYear} ${app.poolType}`}
        actions={
          <Stack direction="row" spacing={1}>
            {isDraft && !editing && (
              <>
                <Button variant="outlined" onClick={() => setEditing(true)}>Edit</Button>
                <Button variant="contained" sx={portalPrimaryButtonSx} disabled={submitting} onClick={() => submitApp(uuid)}>
                  Submit
                </Button>
              </>
            )}
            {app.licenseUuid && (
              <Button component={RouterLink} to={`/mav/licenses/${app.licenseUuid}`} variant="outlined">
                View License
              </Button>
            )}
          </Stack>
        }
      />

      {editing ? (
        <PortalPanel title="Edit Draft Application">
          <Box component="form" onSubmit={handleSave} sx={{ px: 2.5, py: 2 }}>
            <Stack spacing={2}>
              <HsCodePicker value={hsSelection} onChange={setHsSelection} />
              <TextField
                label="Requested Volume (MT)"
                type="number"
                value={requestedVolume}
                onChange={(e) => setRequestedVolume(Number(e.target.value))}
                required
                fullWidth
              />
              <Stack direction="row" spacing={1}>
                <Button type="submit" variant="contained" disabled={saving || !hsSelection.detailUuid}>Save Changes</Button>
                <Button onClick={() => setEditing(false)}>Cancel</Button>
              </Stack>
            </Stack>
          </Box>
        </PortalPanel>
      ) : (
        <PortalPanel title="Application Details">
          <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
            <Typography><strong>Status:</strong> {app.status}</Typography>
            <Typography><strong>Requested Volume:</strong> {app.requestedVolume} MT</Typography>
            {app.allocatedVolume != null && (
              <Typography><strong>Allocated Volume:</strong> {app.allocatedVolume} MT</Typography>
            )}
            <Typography><strong>Submitted:</strong> {formatDate(app.submittedAt)}</Typography>
            <Typography><strong>Reviewed:</strong> {formatDate(app.reviewedAt)}</Typography>
            {app.rejectionReason && (
              <Typography color="error.main"><strong>Rejection Reason:</strong> {app.rejectionReason}</Typography>
            )}
          </Stack>
        </PortalPanel>
      )}
    </Box>
  )
}
