import { useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  Box,
  Button,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import {
  useCompleteAccreditationReviewMutation,
  useGetAgencyAccreditationDetailQuery,
  useReviewAccreditationFileMutation,
} from '../api/agencyApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function AccreditationReviewDetailPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const { data, isLoading } = useGetAgencyAccreditationDetailQuery(uuid, { skip: !uuid })
  const [reviewFile] = useReviewAccreditationFileMutation()
  const [completeReview, { isLoading: completing }] = useCompleteAccreditationReviewMutation()
  const [decision, setDecision] = useState('Approved')
  const [comment, setComment] = useState('')
  const submission = data?.data

  if (isLoading || !submission) {
    return <Typography sx={{ color: portalColors.textMuted }}>{isLoading ? 'Loading submission…' : 'Submission not found.'}</Typography>
  }

  const handleComplete = async () => {
    await completeReview({ uuid, decision, comment }).unwrap()
    navigate('/agency/accreditation')
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Accreditation"
        title={submission.companyName}
        subtitle={`${submission.applicantName} · ${submission.submissionType}`}
      />
      <Chip size="small" label={submission.status} sx={{ mb: 3, ...portalStatusChipSx(submission.status) }} />

      <Stack spacing={3}>
        <PortalPanel title="Application Data">
          <Typography variant="body2" sx={{ px: 2.5, py: 2, whiteSpace: 'pre-wrap' }}>
            {submission.formDataJson ?? 'No additional form data.'}
          </Typography>
        </PortalPanel>

        <PortalPanel title="Submitted Files">
          <Box sx={{ px: 2.5, py: 2 }}>
            {submission.files.length === 0 && <Typography variant="body2">No files attached.</Typography>}
            {submission.files.map((file) => (
              <Box key={file.uuid} sx={{ display: 'flex', gap: 1, alignItems: 'center', mb: 1, flexWrap: 'wrap' }}>
                <Typography variant="body2" sx={{ minWidth: 200 }}>{file.originalFileName}</Typography>
                <Button size="small" onClick={() => reviewFile({ fileUuid: file.uuid, decision: 'Approved' })}>Approve</Button>
                <Button size="small" color="warning" onClick={() => reviewFile({ fileUuid: file.uuid, decision: 'RevisionRequired' })}>Revise</Button>
                <Button size="small" color="error" onClick={() => reviewFile({ fileUuid: file.uuid, decision: 'Rejected' })}>Reject</Button>
                {file.reviewDecision && <Chip size="small" label={file.reviewDecision} />}
              </Box>
            ))}
          </Box>
        </PortalPanel>

        <PortalPanel title="Complete Review">
          <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
            <FormControl sx={{ maxWidth: 240 }}>
              <InputLabel>Decision</InputLabel>
              <Select label="Decision" value={decision} onChange={(e) => setDecision(e.target.value)}>
                <MenuItem value="Approved">Approved</MenuItem>
                <MenuItem value="Rejected">Rejected</MenuItem>
                <MenuItem value="RevisionRequired">Revision Required</MenuItem>
              </Select>
            </FormControl>
            <TextField label="Review Comment" value={comment} onChange={(e) => setComment(e.target.value)} multiline rows={3} />
            <Button variant="contained" sx={{ ...portalPrimaryButtonSx, alignSelf: 'flex-start' }} disabled={completing} onClick={handleComplete}>
              Submit Review
            </Button>
          </Stack>
        </PortalPanel>
      </Stack>
    </Box>
  )
}
