import { Alert, Button, Stack, TextField, Typography } from '@mui/material'
import { FormEvent, useEffect, useState } from 'react'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useFileEntryErfiMutation,
  useGetEntryErfiQuery,
  useUpsertEntryErfiMutation,
} from '../api/clientApi'

export function EntryErfiPanel({
  entryUuid,
  entryType,
  hasIssuedClearance,
}: {
  entryUuid: string
  entryType: string
  hasIssuedClearance: boolean
}) {
  const skip = entryType !== 'Import'
  const { data, isLoading, refetch } = useGetEntryErfiQuery(entryUuid, {
    skip: skip || !hasIssuedClearance,
  })
  const [upsert, { isLoading: saving }] = useUpsertEntryErfiMutation()
  const [file, { isLoading: filing }] = useFileEntryErfiMutation()

  const submission = data?.data
  const [eta, setEta] = useState('')
  const [bol, setBol] = useState('')

  useEffect(() => {
    if (submission?.etaAtPoe) {
      setEta(submission.etaAtPoe.slice(0, 16))
    }
    if (submission?.bolReference) {
      setBol(submission.bolReference)
    }
  }, [submission?.etaAtPoe, submission?.bolReference])

  if (skip) {
    return null
  }

  const prerequisitesMet = hasIssuedClearance

  const onSave = async (e: FormEvent) => {
    if (!prerequisitesMet) return
    e.preventDefault()
    await upsert({
      uuid: entryUuid,
      body: {
        etaAtPoe: eta ? new Date(eta).toISOString() : undefined,
        bolReference: bol || undefined,
      },
    })
    refetch()
  }

  const onFile = async () => {
    if (!prerequisitesMet) return
    await upsert({
      uuid: entryUuid,
      body: {
        etaAtPoe: eta ? new Date(eta).toISOString() : undefined,
        bolReference: bol || undefined,
      },
    })
    await file(entryUuid)
    refetch()
  }

  return (
    <PortalPanel title="e-RFI — Request for Inspection (pre-arrival)">
      <Stack spacing={2} component="form" onSubmit={onSave} sx={{ px: 2.5, py: 2 }}>
        <Typography variant="body2" color="text.secondary">
          File at least 48 hours before arrival at the port of entry. IFM final destination must match the entry warehouse (AC 06 Art. IV).
        </Typography>
        {!prerequisitesMet && (
          <Alert severity="info">
            Complete and obtain an issued <strong>SPS Import Clearance (SPSIC)</strong> above before you can save or file e-RFI.
          </Alert>
        )}
        {isLoading && prerequisitesMet ? (
          <Typography variant="body2" color="text.secondary">Loading e-RFI…</Typography>
        ) : !prerequisitesMet ? (
          <>
            <TextField
              label="ETA at port of entry"
              type="datetime-local"
              size="small"
              value={eta}
              onChange={(ev) => setEta(ev.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              disabled
            />
            <TextField label="BOL / AWB reference" size="small" value={bol} disabled />
            <Button variant="contained" sx={portalPrimaryButtonSx} disabled>
              File e-RFI (requires issued SPSIC)
            </Button>
          </>
        ) : (
          <>
            {submission?.status === 'Filed' ? (
              <Alert severity="success">
                e-RFI filed {submission.filedAt ? new Date(submission.filedAt).toLocaleString() : ''}
                {submission.clearanceNumber ? ` · SPSIC ${submission.clearanceNumber}` : ''}
              </Alert>
            ) : (
              <>
                <TextField
                  label="ETA at port of entry"
                  type="datetime-local"
                  size="small"
                  value={eta}
                  onChange={(ev) => setEta(ev.target.value)}
                  slotProps={{ inputLabel: { shrink: true } }}
                  required
                />
                <TextField
                  label="BOL / AWB reference"
                  size="small"
                  value={bol}
                  onChange={(ev) => setBol(ev.target.value)}
                />
                <Stack direction="row" spacing={1}>
                  <Button type="submit" variant="outlined" sx={portalOutlinedButtonSx} disabled={saving || filing}>
                    Save draft
                  </Button>
                  <Button
                    variant="contained"
                    sx={portalPrimaryButtonSx}
                    disabled={!eta || saving || filing}
                    onClick={onFile}
                  >
                    File e-RFI
                  </Button>
                </Stack>
              </>
            )}
          </>
        )}
      </Stack>
    </PortalPanel>
  )
}
