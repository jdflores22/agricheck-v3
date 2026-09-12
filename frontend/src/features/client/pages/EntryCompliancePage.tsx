import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material'
import { useRef } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetEntryQuery, useResubmitComplianceMutation, useUploadComplianceFileMutation } from '../api/clientApi'
import { downloadAuthenticatedFile } from '../utils/downloadFile'

export function EntryCompliancePage() {
  const { uuid = '' } = useParams()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const activeFileRef = useRef<string | null>(null)
  const { data, isLoading, refetch } = useGetEntryQuery(uuid, { skip: !uuid })
  const [uploadCompliance, { isLoading: uploading }] = useUploadComplianceFileMutation()
  const [resubmitCompliance, { isLoading: resubmitting, isSuccess }] = useResubmitComplianceMutation()
  const entry = data?.data

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading entry…</Typography>
  if (!entry) return <Alert severity="error">Entry not found.</Alert>
  if (entry.status !== 'ForCompliance') {
    return (
      <Box>
        <Alert severity="info" sx={{ mb: 2 }}>This entry is not currently marked for compliance.</Alert>
        <Button component={RouterLink} to={`/client/entries/${uuid}`} variant="outlined" sx={portalOutlinedButtonSx}>Back to entry</Button>
      </Box>
    )
  }

  const revisionFiles = entry.files.filter((f) => f.evaluationDecision === 'RevisionRequired')

  const handlePickFile = (fileUuid: string) => {
    activeFileRef.current = fileUuid
    fileInputRef.current?.click()
  }

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    const fileUuid = activeFileRef.current
    if (!file || !fileUuid) return
    await uploadCompliance({ uuid, fileUuid, file }).unwrap()
    refetch()
    e.target.value = ''
  }

  return (
    <Box sx={{ maxWidth: 760 }}>
      <PortalPageHeader
        eyebrow="Compliance"
        title={entry.referenceNo}
        subtitle="Upload revised documents requested by the evaluator."
        actions={
          <Button component={RouterLink} to={`/client/entries/${uuid}`} variant="outlined" sx={portalOutlinedButtonSx}>
            Back
          </Button>
        }
      />
      <Chip size="small" label={entry.status} sx={{ mb: 2, ...portalStatusChipSx(entry.status) }} />
      {entry.complianceDeadlineAt && (
        <Alert severity={new Date(entry.complianceDeadlineAt) < new Date() ? 'error' : 'warning'} sx={{ mb: 2 }}>
          Compliance deadline: {new Date(entry.complianceDeadlineAt).toLocaleDateString()}
          {new Date(entry.complianceDeadlineAt) < new Date() ? ' (overdue)' : ''}
        </Alert>
      )}
      {isSuccess && <Alert severity="success" sx={{ mb: 2 }}>Compliance package resubmitted for review.</Alert>}

      <input ref={fileInputRef} type="file" hidden onChange={handleUpload} />

      <Stack spacing={2}>
        {revisionFiles.length === 0 ? (
          <Alert severity="info">No documents currently flagged for revision. You can resubmit once all revisions are uploaded.</Alert>
        ) : (
          revisionFiles.map((file) => (
            <PortalPanel key={file.uuid} title={file.originalFileName}>
              <Box sx={{ px: 2.5, py: 2 }}>
                {file.evaluationComment && (
                  <Alert severity="warning" sx={{ mb: 2 }}>{file.evaluationComment}</Alert>
                )}
                {(file.versions?.length ?? 0) > 1 && (
                  <Typography variant="caption" sx={{ display: 'block', mb: 1, color: portalColors.textMuted }}>
                    Version history: {file.versions?.map((v) => `v${v.versionNumber}${v.isCurrent ? ' (current)' : ''}`).join(', ')}
                  </Typography>
                )}
                <Stack direction="row" spacing={1}>
                  <Button variant="outlined" sx={portalOutlinedButtonSx} disabled={uploading} onClick={() => handlePickFile(file.uuid)}>
                    Upload revision
                  </Button>
                  <Button size="small" onClick={() => downloadAuthenticatedFile(`/entries/${uuid}/files/${file.uuid}/download`, file.originalFileName)}>
                    Download current
                  </Button>
                </Stack>
              </Box>
            </PortalPanel>
          ))
        )}

        <Button
          variant="contained"
          sx={portalPrimaryButtonSx}
          disabled={resubmitting || revisionFiles.length > 0}
          onClick={async () => { await resubmitCompliance(uuid).unwrap(); refetch() }}
        >
          Resubmit for Review
        </Button>
      </Stack>
    </Box>
  )
}
