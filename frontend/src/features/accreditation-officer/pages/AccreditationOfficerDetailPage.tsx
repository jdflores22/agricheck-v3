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
import {
  useClaimAccreditationSubmissionMutation,
  useCompleteAccreditationOfficerReviewMutation,
  useGetAccreditationOfficerDetailQuery,
  useReleaseAccreditationSubmissionMutation,
  useReviewAccreditationOfficerFileMutation,
} from '../api/accreditationOfficerApi'
import { AccreditationDocumentReviewControls } from '../components/AccreditationDocumentReviewControls'
import {
  buildAutoApplicationComment,
  canSelectFinalDecision,
  getOutcomeConfirmationMessage,
  getRequiredDocumentReviewStats,
  listRequiredDocumentReviews,
  resolveFinalDecision,
  validateApplicationOutcomeSubmission,
} from '../utils/accreditationReviewService'
import {
  canOfficerReviewAccreditation,
  formatAccreditationStatusLabel,
  getAccreditationPhaseAlert,
  getAccreditationReviewPhase,
} from '../utils/accreditationStatusUtils'
import { AccreditationSubmissionSummary } from '../../client/components/AccreditationSubmissionSummary'
import { AccreditationStatusTimeline } from '../../client/components/AccreditationStatusTimeline'
import { parseFormDataJson, parseFormSchema } from '../../forms/formSchema'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function AccreditationOfficerDetailPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const { data, isLoading, refetch } = useGetAccreditationOfficerDetailQuery(uuid, {
    skip: !uuid,
    refetchOnMountOrArgChange: true,
  })
  const [claimSubmission, { isLoading: claiming }] = useClaimAccreditationSubmissionMutation()
  const [releaseSubmission, { isLoading: releasing }] = useReleaseAccreditationSubmissionMutation()
  const [reviewFile] = useReviewAccreditationOfficerFileMutation()
  const [completeReview, { isLoading: completing }] = useCompleteAccreditationOfficerReviewMutation()
  const [outcome, setOutcome] = useState('RevisionRequired')
  const [comment, setComment] = useState('')
  const [submitError, setSubmitError] = useState('')
  const [approvalSuccess, setApprovalSuccess] = useState<string | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const commentManuallyEdited = useRef(false)
  const submission = data?.data

  const schemaFields = useMemo(
    () => (submission?.formSchemaJson ? parseFormSchema(submission.formSchemaJson) : []),
    [submission?.formSchemaJson],
  )
  const formValues = useMemo(
    () => parseFormDataJson(submission?.formDataJson),
    [submission?.formDataJson],
  )

  const requiredDocuments = useMemo(
    () => listRequiredDocumentReviews(schemaFields, formValues, submission?.files ?? []),
    [schemaFields, formValues, submission?.files],
  )

  const reviewStats = useMemo(
    () => getRequiredDocumentReviewStats(requiredDocuments),
    [requiredDocuments],
  )

  useEffect(() => {
    if (!submission) return
    setOutcome((current) => resolveFinalDecision(reviewStats, current))
  }, [reviewStats, submission])

  useEffect(() => {
    if (!submission || commentManuallyEdited.current) return
    setComment(buildAutoApplicationComment(requiredDocuments, outcome))
  }, [requiredDocuments, outcome, submission])

  if (isLoading || !submission) {
    return <Typography sx={{ color: portalColors.textMuted }}>{isLoading ? 'Loading submission…' : 'Submission not found.'}</Typography>
  }

  const reviewPhase = getAccreditationReviewPhase(submission.status, submission.history)
  const statusLabel = formatAccreditationStatusLabel(submission.status, reviewPhase)
  const phaseAlert = getAccreditationPhaseAlert(reviewPhase)
  const statusChipKey =
    reviewPhase === 'resubmitted_for_review'
      ? 'ResubmittedForReview'
      : reviewPhase === 'awaiting_client_revision'
        ? 'RevisionRequired'
        : submission.status
  const canReview = canOfficerReviewAccreditation(
    submission.status,
    reviewPhase,
    submission.isAssignedToMe,
    submission.canClaim,
  )

  const summarySubmission = {
    uuid: submission.uuid,
    companyName: submission.companyName,
    submissionType: submission.submissionType,
    status: submission.status,
    reviewComments: submission.reviewComments,
    accreditationNumber: submission.accreditationNumber,
    submittedAt: submission.submittedAt,
    files: submission.files.map((file) => ({
      uuid: file.uuid,
      originalFileName: file.originalFileName,
      fileSizeBytes: file.fileSizeBytes,
      createdAt: file.createdAt,
      reviewDecision: file.reviewDecision,
      reviewComment: file.reviewComment,
      versions: file.versions,
    })),
  }

  const validationError = validateApplicationOutcomeSubmission({
    documents: requiredDocuments,
    outcome,
  })

  const confirmation = getOutcomeConfirmationMessage({
    companyName: submission.companyName,
    outcome,
    documents: requiredDocuments,
  })

  const handleClaim = async () => {
    await claimSubmission(uuid).unwrap()
  }

  const handleRelease = async () => {
    await releaseSubmission(uuid).unwrap()
    navigate('/accreditation-officer/accreditations?filter=unclaimed')
  }

  const handleSaveFileReview = async (payload: { fileUuid: string; decision: string; comment?: string }) => {
    commentManuallyEdited.current = false
    await reviewFile(payload).unwrap()
  }

  const openOutcomeConfirm = () => {
    setSubmitError('')
    const error = validateApplicationOutcomeSubmission({
      documents: requiredDocuments,
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
      const response = await completeReview({
        uuid,
        decision: outcome,
        comment,
      }).unwrap()
      setConfirmOpen(false)

      if (outcome === 'Approved') {
        const result = response.data
        await refetch()
        if (result.certificateIssued && result.certificateNumber) {
          setApprovalSuccess(
            `Application approved. Accreditation number ${result.accreditationNumber ?? 'assigned'}. Certificate ${result.certificateNumber} issued to the applicant.`,
          )
        } else if (result.certificateMessage) {
          setApprovalSuccess(
            `Application approved, but the certificate could not be issued: ${result.certificateMessage}`,
          )
        } else {
          setApprovalSuccess(`Application approved. Accreditation number ${result.accreditationNumber ?? 'assigned'}.`)
        }
        return
      }

      navigate('/accreditation-officer/dashboard')
    } catch {
      setSubmitError('Unable to submit review. Check document reviews and try again.')
      setConfirmOpen(false)
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="DA Accreditation"
        title={submission.companyName}
        subtitle={`${submission.applicantName} · ${submission.submissionType}`}
      />

      <Stack direction="row" spacing={1} sx={{ mb: 3, flexWrap: 'wrap', alignItems: 'center' }}>
        <Chip size="small" label={statusLabel} sx={portalStatusChipSx(statusChipKey)} />
        {submission.assignedOfficerName && (
          <Chip size="small" variant="outlined" label={`Officer: ${submission.assignedOfficerName}`} />
        )}
      </Stack>

      {phaseAlert && submission.isAssignedToMe ? (
        <Alert severity={phaseAlert.severity} sx={{ mb: 3 }}>
          {phaseAlert.message}
        </Alert>
      ) : null}

      {approvalSuccess ? (
        <Alert
          severity={approvalSuccess.includes('could not be issued') ? 'warning' : 'success'}
          sx={{ mb: 3 }}
          action={
            <Button color="inherit" size="small" onClick={() => navigate('/accreditation-officer/dashboard')}>
              Go to Dashboard
            </Button>
          }
        >
          {approvalSuccess}
        </Alert>
      ) : null}

      {submission.status === 'Approved' && submission.certificateNumber ? (
        <Alert severity="success" sx={{ mb: 3 }}>
          Certificate <strong>{submission.certificateNumber}</strong> has been issued for this accreditation.
          {submission.accreditationNumber ? ` Accreditation number: ${submission.accreditationNumber}.` : ''}
        </Alert>
      ) : null}

      {submission.canClaim && (
        <Alert severity="info" sx={{ mb: 3 }}>
          This application is waiting in the unclaimed queue. Claim it to begin your evaluation.
          <Box sx={{ mt: 1.5 }}>
            <Button variant="contained" sx={portalPrimaryButtonSx} disabled={claiming} onClick={handleClaim}>
              Claim for Evaluation
            </Button>
          </Box>
        </Alert>
      )}

      {!submission.canClaim && !submission.isAssignedToMe && (
        <Alert severity="warning" sx={{ mb: 3 }}>
          This application is assigned to {submission.assignedOfficerName ?? 'another officer'}.
        </Alert>
      )}

      <Box
        sx={{
          display: 'grid',
          gap: 3,
          gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1.5fr) minmax(0, 1fr)' },
        }}
      >
        <Box>
          <AccreditationSubmissionSummary
            submission={summarySubmission}
            schemaFields={schemaFields}
            formValues={formValues}
            formName={submission.formName}
            statusLabel={statusLabel}
            hideStatusSection
            buildDocumentDownloadPath={(submissionUuid, fileUuid) =>
              `/accreditation-officer/submissions/${submissionUuid}/files/${fileUuid}/download`
            }
            buildVersionDownloadPath={(submissionUuid, fileUuid, versionNumber) =>
              `/accreditation-officer/submissions/${submissionUuid}/files/${fileUuid}/versions/${versionNumber}/download`
            }
            renderBelowDocumentPreview={
              canReview
                ? (document) => (
                    <AccreditationDocumentReviewControls document={document} onSave={handleSaveFileReview} />
                  )
                : undefined
            }
          />
        </Box>

        <Stack spacing={3}>
          <PortalPanel title="Activity History">
            <Box sx={{ px: 2.5, py: 2 }}>
              <AccreditationStatusTimeline history={submission.history} showActorName />
            </Box>
          </PortalPanel>

          {canReview && (
            <PortalPanel title="Application Outcome">
              <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
                <Box
                  sx={{
                    display: 'grid',
                    gap: 1.5,
                    gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(4, 1fr)' },
                  }}
                >
                  <SummaryStat
                    label="Required Docs"
                    value={`${reviewStats.evaluated}/${reviewStats.total}`}
                  />
                  <SummaryStat label="Approved" value={reviewStats.approved} tone="success" />
                  <SummaryStat label="Needs Revision" value={reviewStats.revisionRequired} tone="warning" />
                  <SummaryStat label="Rejected" value={reviewStats.rejected} tone="error" />
                </Box>

                {!reviewStats.allEvaluated ? (
                  <Alert severity="warning">
                    {reviewStats.pending} required document{reviewStats.pending === 1 ? '' : 's'} still need
                    evaluation. Submit is disabled until all required documents are reviewed.
                  </Alert>
                ) : reviewStats.requiresRevisionOutcome ? (
                  <Alert severity="warning">
                    Some documents still need action. The application outcome must stay as{' '}
                    <strong>Revision Required</strong> until every required document is approved.
                  </Alert>
                ) : (
                  <Alert severity="success">
                    All required documents are approved. You may approve the application or reject it at the application level.
                  </Alert>
                )}

                <FormControl fullWidth>
                  <InputLabel>Application Outcome</InputLabel>
                  <Select
                    label="Application Outcome"
                    value={outcome}
                    onChange={(e) => {
                      commentManuallyEdited.current = false
                      setOutcome(e.target.value)
                      setSubmitError('')
                    }}
                  >
                    <MenuItem value="Approved" disabled={!canSelectFinalDecision(reviewStats, 'Approved')}>
                      Approved
                    </MenuItem>
                    <MenuItem
                      value="RevisionRequired"
                      disabled={!canSelectFinalDecision(reviewStats, 'RevisionRequired')}
                    >
                      Revision Required
                    </MenuItem>
                    <MenuItem value="Rejected" disabled={!canSelectFinalDecision(reviewStats, 'Rejected')}>
                      Rejected
                    </MenuItem>
                  </Select>
                </FormControl>

                <TextField
                  label="Application Comment"
                  value={comment}
                  onChange={(e) => {
                    commentManuallyEdited.current = true
                    setComment(e.target.value)
                  }}
                  multiline
                  rows={5}
                  fullWidth
                  helperText={
                    outcome === 'RevisionRequired'
                      ? 'Auto-generated from document remarks. You may edit before submitting.'
                      : 'Summary comment sent to the applicant.'
                  }
                />

                {submitError ? <Alert severity="error">{submitError}</Alert> : null}

                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                  <Button
                    variant="contained"
                    sx={portalPrimaryButtonSx}
                    disabled={completing || Boolean(validationError)}
                    onClick={openOutcomeConfirm}
                  >
                    Submit Application Outcome
                  </Button>
                  <Button variant="outlined" sx={portalOutlinedButtonSx} disabled={releasing} onClick={handleRelease}>
                    Release to Queue
                  </Button>
                </Stack>
              </Stack>
            </PortalPanel>
          )}
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
                Application comment
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

function SummaryStat({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: string | number
  tone?: 'default' | 'success' | 'warning' | 'error'
}) {
  const colors = {
    default: { bg: portalColors.bgMuted, color: portalColors.textDark },
    success: { bg: '#f0fdf4', color: '#15803d' },
    warning: { bg: '#fffbeb', color: '#b45309' },
    error: { bg: '#fef2f2', color: '#b91c1c' },
  }[tone]

  return (
    <Box sx={{ borderRadius: 2, px: 1.5, py: 1.25, bgcolor: colors.bg }}>
      <Typography variant="caption" sx={{ color: portalColors.textMuted, display: 'block' }}>
        {label}
      </Typography>
      <Typography sx={{ fontWeight: 700, color: colors.color }}>{value}</Typography>
    </Box>
  )
}
