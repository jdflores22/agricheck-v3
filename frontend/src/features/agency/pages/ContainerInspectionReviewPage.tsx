import ArrowBackIcon from '@mui/icons-material/ArrowBack'
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
  Stack,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { AccreditationStatusTimeline } from '../../client/components/AccreditationStatusTimeline'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { ContainerInspectionOutcomePanel } from '../components/ContainerInspectionOutcomePanel'
import { ContainerInspectionReviewSummary } from '../components/ContainerInspectionReviewSummary'
import {
  buildAutoContainerComment,
  getContainerOutcomeConfirmationMessage,
  getContainerPhotoReviewStats,
  listContainerPhotoReviews,
  resolveContainerFinalDecision,
  validateContainerOutcomeSubmission,
} from '../utils/containerInspectionReviewService'
import {
  useClaimContainerInspectionMutation,
  useCompleteContainerInspectionMutation,
  useGetContainerInspectionDetailQuery,
  useReviewInspectionPhotoMutation,
} from '../api/agencyApi'

export function ContainerInspectionReviewPage() {
  const { containerUuid = '' } = useParams()
  const navigate = useNavigate()
  const { data, isLoading, isError, refetch } = useGetContainerInspectionDetailQuery(containerUuid, { skip: !containerUuid })
  const [reviewPhoto] = useReviewInspectionPhotoMutation()
  const [claimContainer, { isLoading: claiming }] = useClaimContainerInspectionMutation()
  const [completeInspection, { isLoading: completing }] = useCompleteContainerInspectionMutation()
  const [outcome, setOutcome] = useState('RevisionRequired')
  const [comment, setComment] = useState('')
  const [submitError, setSubmitError] = useState('')
  const [outcomeConfirmOpen, setOutcomeConfirmOpen] = useState(false)
  const [confirmError, setConfirmError] = useState('')
  const commentManuallyEdited = useRef(false)
  const detail = data?.data

  const photoReviews = useMemo(
    () => listContainerPhotoReviews(detail?.photos ?? []),
    [detail?.photos],
  )

  const reviewStats = useMemo(
    () => getContainerPhotoReviewStats(photoReviews),
    [photoReviews],
  )

  useEffect(() => {
    if (!detail) return
    setOutcome((current) => resolveContainerFinalDecision(reviewStats, current))
  }, [reviewStats, detail])

  useEffect(() => {
    if (!detail || commentManuallyEdited.current) return
    setComment(buildAutoContainerComment(photoReviews, outcome))
  }, [photoReviews, outcome, detail])

  const handleReviewPhoto = async (payload: {
    photoUuid: string
    decision: 'Approved' | 'Rejected'
    comment?: string
  }) => {
    commentManuallyEdited.current = false
    await reviewPhoto(payload).unwrap()
    await refetch()
  }

  const openOutcomeConfirm = () => {
    setSubmitError('')
    setConfirmError('')
    const error = validateContainerOutcomeSubmission({ photos: photoReviews, outcome })
    if (error) {
      setSubmitError(error)
      return
    }
    setOutcomeConfirmOpen(true)
  }

  const closeOutcomeConfirm = (_event?: object, reason?: 'backdropClick' | 'escapeKeyDown') => {
    if (completing) return
    if (reason === 'backdropClick') return
    setOutcomeConfirmOpen(false)
    setConfirmError('')
  }

  const handleConfirmComplete = async () => {
    setConfirmError('')
    try {
      await completeInspection({
        containerUuid,
        decision: outcome,
        comment,
      }).unwrap()
      setOutcomeConfirmOpen(false)
      navigate('../inspections')
    } catch {
      setConfirmError('Unable to submit container inspection outcome. Check photo reviews and try again.')
    }
  }

  const outcomeLabel = outcome === 'RevisionRequired'
    ? 'Revision Required'
    : outcome === 'Approved'
      ? 'Approved'
      : outcome === 'Rejected'
        ? 'Rejected'
        : outcome

  if (isLoading) {
    return <Typography sx={{ color: portalColors.textMuted }}>Loading container inspection…</Typography>
  }

  if (isError || !detail) {
    return (
      <Box>
        <Alert severity="error" sx={{ mb: 2 }}>Unable to load this container inspection.</Alert>
        <Button component={RouterLink} to="../inspections" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
          Back to inspections
        </Button>
      </Box>
    )
  }

  const entry = detail.entry
  const pendingCount = detail.photos.filter((photo) => photo.reviewDecision === 'Pending').length
  const history = detail.history ?? []
  const isCompleted = Boolean(detail.inspectionOutcome)
  const canReview = detail.isAssignedToMe || isCompleted
  const isClaimedByOther = Boolean(detail.assignedInspectorName) && !detail.isAssignedToMe
  const confirmation = getContainerOutcomeConfirmationMessage({
    containerNumber: detail.containerNumber,
    entryReferenceNo: entry.referenceNo,
    outcome,
    photos: photoReviews,
  })

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Container inspection review"
        title={detail.containerNumber}
        subtitle={`${entry.referenceNo} · ${entry.companyName || entry.applicantName} · ${entry.entryType}`}
        actions={
          <Button component={RouterLink} to="../inspections" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
            Back to inspections
          </Button>
        }
      />

      <Stack direction="row" spacing={1} sx={{ mb: 3, flexWrap: 'wrap', alignItems: 'center' }}>
        <Chip size="small" label={entry.status} sx={portalStatusChipSx(entry.status)} />
        <Chip
          size="small"
          label={
            isCompleted
              ? `Outcome: ${detail.inspectionOutcome}`
              : `${pendingCount} photo${pendingCount === 1 ? '' : 's'} pending review`
          }
          sx={portalStatusChipSx(isCompleted ? (detail.inspectionOutcome ?? 'Approved') : 'Pending')}
        />
        {entry.companyName ? <Chip size="small" variant="outlined" label={entry.companyName} /> : null}
        {detail.assignedInspectorName ? (
          <Chip
            size="small"
            variant="outlined"
            label={detail.isAssignedToMe ? 'Assigned to you' : `Assigned to ${detail.assignedInspectorName}`}
          />
        ) : null}
      </Stack>

      {!isCompleted && !canReview ? (
        <Alert
          severity={isClaimedByOther ? 'warning' : 'info'}
          sx={{ mb: 3 }}
          action={
            !isClaimedByOther ? (
              <Button
                color="inherit"
                size="small"
                disabled={claiming}
                onClick={() => void claimContainer(containerUuid).then(() => refetch())}
              >
                {claiming ? 'Claiming…' : 'Claim container'}
              </Button>
            ) : undefined
          }
        >
          {isClaimedByOther
            ? `This container is already assigned to ${detail.assignedInspectorName}.`
            : 'Claim this container before reviewing photos or submitting the inspection outcome.'}
        </Alert>
      ) : null}

      {isCompleted ? (
        <Alert severity="success" sx={{ mb: 3 }}>
          Container inspection outcome submitted
          {detail.inspectionCompletedAt
            ? ` on ${new Date(detail.inspectionCompletedAt).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })}`
            : ''}.
          {detail.inspectionOutcomeComment ? ` ${detail.inspectionOutcomeComment}` : ''}
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
          <ContainerInspectionReviewSummary
            detail={detail}
            onReviewPhoto={handleReviewPhoto}
            disablePhotoReviews={isCompleted || !canReview}
          />
        </Box>

        <Stack spacing={3}>
          <PortalPanel title="Activity History">
            <Box sx={{ px: 2.5, py: 2 }}>
              <AccreditationStatusTimeline
                history={history}
                emptyMessage="No activity recorded yet."
                showActorName
              />
            </Box>
          </PortalPanel>

          {isCompleted ? (
            <PortalPanel title="Submitted Outcome">
              <Box sx={{ px: 2.5, py: 2 }}>
                <Stack spacing={1.5}>
                  <Box>
                    <Typography variant="caption" sx={{ color: portalColors.textMuted, fontWeight: 600 }}>
                      Decision
                    </Typography>
                    <Typography variant="body2">{detail.inspectionOutcome}</Typography>
                  </Box>
                  {detail.inspectionOutcomeComment ? (
                    <Box>
                      <Typography variant="caption" sx={{ color: portalColors.textMuted, fontWeight: 600 }}>
                        Comment
                      </Typography>
                      <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap' }}>
                        {detail.inspectionOutcomeComment}
                      </Typography>
                    </Box>
                  ) : null}
                </Stack>
              </Box>
            </PortalPanel>
          ) : canReview ? (
            <ContainerInspectionOutcomePanel
              photos={photoReviews}
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

      <Dialog
        open={outcomeConfirmOpen}
        onClose={closeOutcomeConfirm}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>{confirmation.title}</DialogTitle>
        <DialogContent>
          <Stack spacing={2}>
            <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', alignItems: 'center' }}>
              <Chip size="small" label={outcomeLabel} sx={portalStatusChipSx(outcome)} />
              <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
                Container {detail.containerNumber} · Entry {entry.referenceNo}
              </Typography>
            </Stack>
            <Typography sx={{ color: portalColors.textMuted }}>{confirmation.body}</Typography>
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
                  Inspection comment
                </Typography>
                <Typography component="pre" sx={{ m: 0, whiteSpace: 'pre-wrap', fontFamily: 'inherit', fontSize: '0.875rem' }}>
                  {comment}
                </Typography>
              </Box>
            ) : null}
            {confirmError ? <Alert severity="error">{confirmError}</Alert> : null}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button type="button" onClick={closeOutcomeConfirm} disabled={completing} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button
            type="button"
            onClick={() => void handleConfirmComplete()}
            variant="contained"
            disabled={completing}
            sx={portalPrimaryButtonSx}
          >
            {completing ? <CircularProgress size={20} color="inherit" /> : 'Yes, Submit Container Outcome'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
