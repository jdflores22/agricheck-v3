import { Alert, Button, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useAttachEntrySpsDocumentMutation,
  useCreateEntrySpsApplicationMutation,
  useGetEntrySpsApplicationQuery,
  useGetEntrySpsReadinessQuery,
  useSubmitEntrySpsApplicationMutation,
} from '../api/clientApi'

type EntryFileRef = { uuid: string; originalFileName: string }

export function EntrySpsicPanel({
  entryUuid,
  entryType,
  files = [],
}: {
  entryUuid: string
  entryType: string
  files?: EntryFileRef[]
}) {
  const skip = entryType !== 'Import'
  const { data, isLoading } = useGetEntrySpsApplicationQuery(entryUuid, { skip })
  const { data: readinessData, refetch: refetchReadiness } = useGetEntrySpsReadinessQuery(entryUuid, { skip })
  const [createApp, { isLoading: creating }] = useCreateEntrySpsApplicationMutation()
  const [submitApp, { isLoading: submitting }] = useSubmitEntrySpsApplicationMutation()
  const [attachDoc, { isLoading: attaching }] = useAttachEntrySpsDocumentMutation()
  const [attachCode, setAttachCode] = useState('A')
  const [attachFileUuid, setAttachFileUuid] = useState('')

  if (skip) {
    return null
  }

  const application = data?.data
  const readiness = readinessData?.data

  const handleAttach = async () => {
    if (!attachFileUuid) return
    await attachDoc({ entryUuid, code: attachCode, entryFileUuid: attachFileUuid })
    refetchReadiness()
  }

  return (
    <PortalPanel title="SPSIC application (SPS Import Clearance)">
      <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
        {readiness?.suggestedIssuer && (
          <Typography variant="body2" color="text.secondary">
            Suggested issuer: <strong>{readiness.suggestedIssuer.agencyCode}</strong>
            {readiness.hsCode ? ` · HS ${readiness.hsCode}` : ''}
            {!readiness.agencyMatchesEntry ? ' · differs from entry bureau' : ''}
          </Typography>
        )}
        {readiness?.checklist?.length ? (
          <Stack spacing={0.5}>
            <Typography variant="caption" color="text.secondary" sx={{ fontWeight: 600 }}>
              Art. III §3 checklist
            </Typography>
            {readiness.checklist.map((item) => (
              <Typography key={item.code} variant="body2" sx={{ fontSize: '0.8125rem' }}>
                {item.code}. {item.title}
                {item.isRequired ? '' : ' (optional)'} — {item.status}
              </Typography>
            ))}
          </Stack>
        ) : null}
        {application?.status === 'Draft' && files.length > 0 && readiness?.checklist?.length ? (
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ alignItems: { sm: 'center' } }}>
            <TextField select size="small" label="Checklist code" value={attachCode} onChange={(e) => setAttachCode(e.target.value)} sx={{ minWidth: 100 }}>
              {readiness.checklist.map((item) => (
                <MenuItem key={item.code} value={item.code}>{item.code}</MenuItem>
              ))}
            </TextField>
            <TextField select size="small" label="Entry document" value={attachFileUuid} onChange={(e) => setAttachFileUuid(e.target.value)} sx={{ flex: 1, minWidth: 180 }}>
              {files.map((f) => (
                <MenuItem key={f.uuid} value={f.uuid}>{f.originalFileName}</MenuItem>
              ))}
            </TextField>
            <Button variant="outlined" sx={portalOutlinedButtonSx} disabled={!attachFileUuid || attaching} onClick={handleAttach}>
              Link file
            </Button>
          </Stack>
        ) : null}
        {isLoading ? (
          <Typography variant="body2" color="text.secondary">Loading SPS application…</Typography>
        ) : !application ? (
          <>
            <Typography variant="body2" color="text.secondary">
              Start an SPS Import Clearance application when bureau license, accreditation, and destination warehouse are ready.
            </Typography>
            <Button
              variant="contained"
              sx={portalPrimaryButtonSx}
              disabled={creating}
              onClick={() => createApp(entryUuid)}
            >
              Start SPS application
            </Button>
          </>
        ) : (
          <>
            <Typography variant="body2">
              Status: <strong>{application.status}</strong>
              {application.submittedAt ? ` · submitted ${new Date(application.submittedAt).toLocaleString()}` : ''}
            </Typography>
            {application.issuedClearance ? (
              <Alert severity="success">
                SPSIC {application.issuedClearance.clearanceNumber} issued{' '}
                {new Date(application.issuedClearance.issuedAt).toLocaleDateString()}
              </Alert>
            ) : application.status === 'Draft' ? (
              <Button
                variant="outlined"
                sx={portalOutlinedButtonSx}
                disabled={submitting || readiness?.allRequiredDocumentsReady === false}
                onClick={() => submitApp(entryUuid)}
              >
                Submit to bureau
              </Button>
            ) : (
              <Alert severity="info">Awaiting bureau review and clearance issuance.</Alert>
            )}
            {application.status === 'Draft' && readiness?.allRequiredDocumentsReady === false ? (
              <Alert severity="warning">Complete required checklist items before submit.</Alert>
            ) : null}
          </>
        )}
      </Stack>
    </PortalPanel>
  )
}
