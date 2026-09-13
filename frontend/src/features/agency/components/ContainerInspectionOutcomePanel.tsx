import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import RadioButtonUncheckedOutlinedIcon from '@mui/icons-material/RadioButtonUncheckedOutlined'
import {
  Alert,
  Box,
  Button,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  canSelectContainerFinalDecision,
  getContainerPhotoReviewStats,
  getContainerSubmissionBlockers,
  type ContainerPhotoReview,
} from '../utils/containerInspectionReviewService'

type ReadinessRowProps = {
  label: string
  done: boolean
  summary: string
  detail?: string
}

function ReadinessRow({ label, done, summary, detail }: ReadinessRowProps) {
  return (
    <Box sx={{ display: 'flex', gap: 1.25, alignItems: 'flex-start', py: 1 }}>
      {done ? (
        <CheckCircleOutlinedIcon sx={{ fontSize: 20, color: '#15803d', mt: 0.15 }} />
      ) : (
        <RadioButtonUncheckedOutlinedIcon sx={{ fontSize: 20, color: portalColors.textLight, mt: 0.15 }} />
      )}
      <Box sx={{ flex: 1, minWidth: 0 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', gap: 2, alignItems: 'baseline' }}>
          <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
            {label}
          </Typography>
          <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: done ? '#15803d' : '#b45309' }}>
            {summary}
          </Typography>
        </Box>
        {detail ? (
          <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.25 }}>
            {detail}
          </Typography>
        ) : null}
      </Box>
    </Box>
  )
}

export type ContainerInspectionOutcomePanelProps = {
  photos: ContainerPhotoReview[]
  outcome: string
  comment: string
  submitError: string
  completing: boolean
  disabled?: boolean
  onOutcomeChange: (value: string) => void
  onCommentChange: (value: string) => void
  onSubmit: () => void
}

export function ContainerInspectionOutcomePanel({
  photos,
  outcome,
  comment,
  submitError,
  completing,
  disabled = false,
  onOutcomeChange,
  onCommentChange,
  onSubmit,
}: ContainerInspectionOutcomePanelProps) {
  const reviewStats = getContainerPhotoReviewStats(photos)
  const blockers = getContainerSubmissionBlockers({ photos, outcome })
  const canSubmit = !disabled && blockers.length === 0

  const photosDone = reviewStats.total > 0 && reviewStats.allEvaluated
  const photoDetail = reviewStats.allEvaluated && reviewStats.total > 0
    ? `${reviewStats.approved} approved · ${reviewStats.rejected} rejected`
    : reviewStats.total === 0
      ? 'No required container photos were uploaded.'
      : `${reviewStats.pending} photo${reviewStats.pending === 1 ? '' : 's'} still need a review decision.`

  const outcomeHint = reviewStats.allEvaluated && reviewStats.requiresRevisionOutcome
    ? 'Some photos were rejected — only Revision Required is allowed until they are re-uploaded.'
    : reviewStats.allEvaluated && reviewStats.allApproved
      ? 'All photos are approved — you may approve or reject the container.'
      : undefined

  return (
    <PortalPanel title="Container Inspection Outcome">
      <Stack spacing={2.5} sx={{ px: 2.5, py: 2 }}>
        <Box
          sx={{
            borderRadius: 2,
            border: `1px solid ${portalColors.border}`,
            bgcolor: portalColors.bgMuted,
            px: 2,
            py: 0.5,
          }}
        >
          <Typography
            variant="caption"
            sx={{ color: portalColors.textMuted, fontWeight: 700, letterSpacing: '0.04em', textTransform: 'uppercase' }}
          >
            Before you submit
          </Typography>

          <ReadinessRow
            label="Container photo reviews"
            done={photosDone}
            summary={reviewStats.total > 0 ? `${reviewStats.evaluated}/${reviewStats.total}` : '—'}
            detail={photoDetail}
          />
        </Box>

        {blockers.length > 0 ? (
          <Alert severity="warning" sx={{ '& .MuiAlert-message': { width: '100%' } }}>
            <Typography sx={{ fontWeight: 600, fontSize: '0.875rem', mb: 0.5 }}>
              Complete these before submitting
            </Typography>
            <Stack component="ul" sx={{ m: 0, pl: 2.25 }}>
              {blockers.map((item) => (
                <Typography key={item} component="li" sx={{ fontSize: '0.8125rem' }}>
                  {item}
                </Typography>
              ))}
            </Stack>
          </Alert>
        ) : (
          <Alert severity="success">Ready to submit. Review your outcome and comment, then confirm.</Alert>
        )}

        <Stack spacing={2} sx={{ opacity: canSubmit ? 1 : 0.72 }}>
          <FormControl fullWidth disabled={!canSubmit}>
            <InputLabel>Container Outcome</InputLabel>
            <Select
              label="Container Outcome"
              value={outcome}
              onChange={(e) => onOutcomeChange(e.target.value)}
            >
              <MenuItem value="Approved" disabled={!canSelectContainerFinalDecision(reviewStats, 'Approved')}>
                Approved
              </MenuItem>
              <MenuItem
                value="RevisionRequired"
                disabled={!canSelectContainerFinalDecision(reviewStats, 'RevisionRequired')}
              >
                Revision Required
              </MenuItem>
              <MenuItem value="Rejected" disabled={!canSelectContainerFinalDecision(reviewStats, 'Rejected')}>
                Rejected
              </MenuItem>
            </Select>
          </FormControl>

          {outcomeHint ? (
            <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
              {outcomeHint}
            </Typography>
          ) : null}

          <TextField
            label="Inspection Comment"
            value={comment}
            onChange={(e) => onCommentChange(e.target.value)}
            multiline
            rows={5}
            fullWidth
            disabled={!canSubmit}
            helperText={
              outcome === 'RevisionRequired'
                ? 'Auto-generated from photo remarks. You may edit before submitting.'
                : 'Summary comment sent to the importer.'
            }
          />
        </Stack>

        {submitError ? <Alert severity="error">{submitError}</Alert> : null}

        <Button
          type="button"
          variant="contained"
          sx={portalPrimaryButtonSx}
          disabled={!canSubmit || completing}
          onClick={onSubmit}
        >
          Submit Container Outcome
        </Button>
      </Stack>
    </PortalPanel>
  )
}
