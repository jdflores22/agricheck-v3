import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { AccreditationStatusTimeline } from '../../client/components/AccreditationStatusTimeline'
import { applyMavEntrySchema, parseFormDataJson, parseFormSchema } from '../../forms/formSchema'
import { AccreditationDocumentReviewControls } from '../../accreditation-officer/components/AccreditationDocumentReviewControls'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { EntryEvaluationSummary } from '../components/EntryEvaluationSummary'
import { EntryMavEvaluationPanel } from '../components/EntryMavEvaluationPanel'
import { EntryOutcomePanel } from '../components/EntryOutcomePanel'
import {
  buildAutoEntryComment,
  getEntryOutcomeConfirmationMessage,
  getRequiredEntryDocumentReviewStats,
  listRequiredEntryDocumentReviews,
  resolveEntryFinalDecision,
  validateEntryOutcomeSubmission,
} from '../utils/entryEvaluationReviewService'
import {
  useAddEvaluatorNoteMutation,
  useCompleteEvaluationMutation,
  useEvaluateFileMutation,
  useGetAgencyEntryQuery,
  useUpdateComplianceMutation,
} from '../api/agencyApi'

export function EntryEvaluationPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const { data, isLoading, refetch } = useGetAgencyEntryQuery(uuid, { skip: !uuid })
  const [evaluateFile] = useEvaluateFileMutation()
  const [updateCompliance] = useUpdateComplianceMutation()
  const [addNote] = useAddEvaluatorNoteMutation()
  const [completeEvaluation, { isLoading: completing }] = useCompleteEvaluationMutation()
  const [outcome, setOutcome] = useState('RevisionRequired')
  const [comment, setComment] = useState('')
  const [noteText, setNoteText] = useState('')
  const [submitError, setSubmitError] = useState('')
  const [confirmOpen, setConfirmOpen] = useState(false)
  const commentManuallyEdited = useRef(false)
  const entry = data?.data

  const isMavTrack = entry?.entryType === 'Import' && (
    entry.mav?.importTrack === 'Mav'
    || (!entry.mav?.importTrack && Boolean(entry.mav?.mavNo || (entry.mav?.micUtilizations?.length ?? 0) > 0))
  )
  const schemaFields = useMemo(
    () => (entry?.formSchemaJson
      ? applyMavEntrySchema(parseFormSchema(entry.formSchemaJson), {
          showMavFields: isMavTrack,
          requireCommodityHs: false,
        })
      : []),
    [entry?.formSchemaJson, isMavTrack],
  )
  const formValues = useMemo(
    () => parseFormDataJson(entry?.formDataJson),
    [entry?.formDataJson],
  )

  const requiredDocuments = useMemo(
    () => listRequiredEntryDocumentReviews(schemaFields, formValues, entry?.files ?? []),
    [schemaFields, formValues, entry?.files],
  )

  const reviewStats = useMemo(
    () => getRequiredEntryDocumentReviewStats(requiredDocuments),
    [requiredDocuments],
  )

  useEffect(() => {
    if (!entry) return
    setOutcome((current) => resolveEntryFinalDecision(reviewStats, current))
  }, [reviewStats, entry])

  useEffect(() => {
    if (!entry || commentManuallyEdited.current) return
    setComment(buildAutoEntryComment(requiredDocuments, outcome))
  }, [requiredDocuments, outcome, entry])

  if (isLoading || !entry) {
    return <Typography sx={{ color: portalColors.textMuted }}>{isLoading ? 'Loading entry…' : 'Entry not found.'}</Typography>
  }

  const canEvaluate =
    entry.isAssignedToMe &&
    !['Approved', 'Rejected'].includes(entry.status)

  const confirmation = getEntryOutcomeConfirmationMessage({
    referenceNo: entry.referenceNo,
    companyName: entry.companyName,
    outcome,
    documents: requiredDocuments,
  })

  const handleSaveFileReview = async (payload: { fileUuid: string; decision: string; comment?: string }) => {
    commentManuallyEdited.current = false
    await evaluateFile(payload).unwrap()
    await refetch()
  }

  const handleComplianceChange = async (itemId: number, status: string) => {
    await updateCompliance({
      entryUuid: uuid,
      items: [{ itemId, status }],
    }).unwrap()
    await refetch()
  }

  const openOutcomeConfirm = () => {
    setSubmitError('')
    const error = validateEntryOutcomeSubmission({
      documents: requiredDocuments,
      compliance: entry.compliance,
      outcome,
    })
    if (error) {
      setSubmitError(error)
      return
    }
    setConfirmOpen(true)
  }

  const handleConfirmComplete = async () => {
    setSubmitError('')
    try {
      await completeEvaluation({ entryUuid: uuid, decision: outcome, comment }).unwrap()
      setConfirmOpen(false)
      navigate('/agency/evaluator/assignments')
    } catch {
      setSubmitError('Unable to submit evaluation. Check document reviews and try again.')
      setConfirmOpen(false)
    }
  }

  const history = entry.statusHistory.map((item) => ({
    status: item.toStatus,
    comment: item.comment,
    createdAt: item.createdAt,
  }))

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Evaluation"
        title={entry.referenceNo}
        subtitle={`${entry.companyName || entry.applicantName} · ${entry.entryType}`}
        actions={
          <Button variant="outlined" sx={portalOutlinedButtonSx} onClick={() => navigate('/agency/evaluator/assignments')}>
            Back to assignments
          </Button>
        }
      />

      <Stack direction="row" spacing={1} sx={{ mb: 3, flexWrap: 'wrap', alignItems: 'center' }}>
        <Chip size="small" label={entry.status} sx={portalStatusChipSx(entry.status)} />
        {entry.companyName ? (
          <Chip size="small" variant="outlined" label={entry.companyName} />
        ) : null}
      </Stack>

      {!entry.isAssignedToMe ? (
        <Alert severity="warning" sx={{ mb: 3 }}>
          This entry is not assigned to you. Claim it from the evaluation queue before reviewing documents.
        </Alert>
      ) : null}

      {entry.status === 'ForCompliance' ? (
        <Alert severity="info" sx={{ mb: 3 }}>
          This entry is waiting for the applicant to revise and resubmit documents.
        </Alert>
      ) : null}

      <Box
        sx={{
          display: 'grid',
          gap: 3,
          gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1.5fr) minmax(0, 1fr)' },
        }}
      >
        <Box>
          <EntryEvaluationSummary
            entry={{
              uuid: entry.uuid,
              referenceNo: entry.referenceNo,
              entryType: entry.entryType,
              importTrack: entry.mav?.importTrack,
              companyName: entry.companyName,
              applicantName: entry.applicantName,
              submittedAt: entry.submittedAt,
              notes: entry.notes,
              files: entry.files.map((file) => ({
                uuid: file.uuid,
                originalFileName: file.originalFileName,
                fileSizeBytes: file.fileSizeBytes ?? 0,
                createdAt: file.createdAt ?? '',
                evaluationDecision: file.evaluationDecision,
                evaluationComment: file.evaluationComment,
                versions: file.versions,
              })),
            }}
            schemaFields={schemaFields}
            formValues={formValues}
            formName={entry.formName}
            buildDocumentDownloadPath={(entryUuid, fileUuid) =>
              `/agency/evaluator/entries/${entryUuid}/files/${fileUuid}/download`
            }
            buildVersionDownloadPath={(entryUuid, fileUuid, versionNumber) =>
              `/agency/evaluator/entries/${entryUuid}/files/${fileUuid}/versions/${versionNumber}/download`
            }
            renderBelowDocumentPreview={
              canEvaluate
                ? (document) => (
                    <AccreditationDocumentReviewControls document={document} onSave={handleSaveFileReview} />
                  )
                : undefined
            }
          />
        </Box>

        <Stack spacing={3}>
          <EntryMavEvaluationPanel entry={entry} canReview={canEvaluate} onUpdated={() => refetch()} />

          <PortalPanel title="Activity History">
            <Box sx={{ px: 2.5, py: 2 }}>
              <AccreditationStatusTimeline history={history} emptyMessage="No activity recorded yet." />
            </Box>
          </PortalPanel>

          <PortalPanel title="Compliance Checklist">
            <Box sx={{ px: 2.5, py: 2 }}>
              {entry.compliance.length === 0 ? (
                <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
                  Checklist items are loading or not configured for this agency.
                </Typography>
              ) : (
                entry.compliance.map((item) => (
                  <Box key={item.itemId} sx={{ display: 'flex', gap: 2, alignItems: 'center', mb: 1.5 }}>
                    <Typography variant="body2" sx={{ flex: 1 }}>
                      {item.label}
                      {item.isRequired ? '' : ' (optional)'}
                    </Typography>
                    <FormControl size="small" sx={{ minWidth: 120 }} disabled={!canEvaluate}>
                      <InputLabel>Status</InputLabel>
                      <Select
                        label="Status"
                        value={item.status}
                        onChange={(e) => handleComplianceChange(item.itemId, e.target.value)}
                      >
                        <MenuItem value="Pending">Pending</MenuItem>
                        <MenuItem value="Passed">Passed</MenuItem>
                        <MenuItem value="Failed">Failed</MenuItem>
                        <MenuItem value="Na">N/A</MenuItem>
                      </Select>
                    </FormControl>
                  </Box>
                ))
              )}
            </Box>
          </PortalPanel>

          <PortalPanel title="Evaluator Notes">
            <Box sx={{ px: 2.5, py: 2 }}>
              {entry.notesList.length === 0 ? (
                <Typography variant="body2" sx={{ color: portalColors.textMuted, mb: 2 }}>
                  No notes yet.
                </Typography>
              ) : (
                entry.notesList.map((note) => (
                  <Box key={`${note.authorName}-${note.createdAt}`} sx={{ mb: 1.5 }}>
                    <Typography variant="body2">{note.note}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {note.authorName} · {new Date(note.createdAt).toLocaleString()} {note.isInternal ? '(internal)' : ''}
                    </Typography>
                  </Box>
                ))
              )}
              {canEvaluate ? (
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ mt: 2 }}>
                  <TextField
                    label="Add internal note"
                    value={noteText}
                    onChange={(e) => setNoteText(e.target.value)}
                    fullWidth
                    multiline
                    minRows={2}
                  />
                  <Button
                    variant="outlined"
                    sx={{ ...portalOutlinedButtonSx, alignSelf: { sm: 'flex-start' } }}
                    onClick={async () => {
                      if (!noteText.trim()) return
                      await addNote({ entryUuid: uuid, note: noteText, isInternal: true }).unwrap()
                      setNoteText('')
                      await refetch()
                    }}
                  >
                    Add Note
                  </Button>
                </Stack>
              ) : null}
            </Box>
          </PortalPanel>

          {canEvaluate ? (
            <EntryOutcomePanel
              compliance={entry.compliance}
              documents={requiredDocuments}
              outcome={outcome}
              comment={comment}
              submitError={submitError}
              completing={completing}
              onOutcomeChange={(value) => {
                commentManuallyEdited.current = false
                setOutcome(value)
                setSubmitError('')
              }}
              onCommentChange={(value) => {
                commentManuallyEdited.current = true
                setComment(value)
              }}
              onSubmit={openOutcomeConfirm}
            />
          ) : null}
        </Stack>
      </Box>

      <Dialog open={confirmOpen} onClose={() => !completing && setConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>{confirmation.title}</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted, mb: 2 }}>{confirmation.body}</Typography>
          {comment.trim() ? (
            <Box
              sx={{
                borderRadius: 2,
                bgcolor: portalColors.bgMuted,
                px: 2,
                py: 1.5,
              }}
            >
              <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block', mb: 0.5 }}>
                Evaluation comment
              </Typography>
              <Typography component="pre" sx={{ m: 0, whiteSpace: 'pre-wrap', fontFamily: 'inherit', fontSize: '0.875rem' }}>
                {comment}
              </Typography>
            </Box>
          ) : null}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setConfirmOpen(false)} disabled={completing} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleConfirmComplete} variant="contained" disabled={completing} sx={portalPrimaryButtonSx}>
            {completing ? <CircularProgress size={20} color="inherit" /> : 'Yes, Submit Outcome'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
