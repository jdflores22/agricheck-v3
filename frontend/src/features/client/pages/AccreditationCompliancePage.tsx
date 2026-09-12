import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Snackbar,
  Stack,
  Typography,
} from '@mui/material'
import { useMemo, useRef, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { AccreditationStatusChip } from '../../accreditation/AccreditationStatusChip'
import { parseFormDataJson, parseFormSchema } from '../../forms/formSchema'
import { useLoadingSpinner } from '../../system/SystemBrandingProvider'
import {
  useGetAccreditationSubmissionQuery,
  useGetClientFormQuery,
  useGetClientFormsQuery,
  useResubmitAccreditationComplianceMutation,
  useUploadAccreditationComplianceFileMutation,
} from '../api/clientApi'
import { downloadAuthenticatedFile } from '../utils/downloadFile'
import {
  canResubmitCompliance,
  getReviewerCommentIntro,
  listComplianceDocuments,
  parseReviewerCommentBullets,
} from '../utils/accreditationComplianceUtils'

export function AccreditationCompliancePage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const activeFileRef = useRef<string | null>(null)
  const { showLoadingSpinner, hideLoadingSpinner } = useLoadingSpinner()
  const { data, isLoading, isFetching, refetch } = useGetAccreditationSubmissionQuery(uuid, { skip: !uuid })
  const { data: formsData } = useGetClientFormsQuery({ formType: 'ACCREDITATION' })
  const selectedFormUuid = formsData?.data?.[0]?.uuid
  const { data: formSchemaData } = useGetClientFormQuery(selectedFormUuid ?? '', { skip: !selectedFormUuid })
  const [uploadCompliance] = useUploadAccreditationComplianceFileMutation()
  const [resubmitCompliance] = useResubmitAccreditationComplianceMutation()
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [uploadingFileUuid, setUploadingFileUuid] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const submission = data?.data

  const schemaFields = useMemo(
    () => (formSchemaData?.data?.schemaJson ? parseFormSchema(formSchemaData.data.schemaJson) : []),
    [formSchemaData?.data?.schemaJson],
  )
  const formValues = useMemo(() => parseFormDataJson(submission?.formDataJson), [submission?.formDataJson])
  const complianceDocuments = useMemo(
    () => listComplianceDocuments(submission?.files ?? [], formValues, schemaFields),
    [submission?.files, formValues, schemaFields],
  )
  const resubmitReady = useMemo(() => canResubmitCompliance(submission?.files ?? []), [submission?.files])
  const reviewerIntro = getReviewerCommentIntro(submission?.reviewComments)
  const reviewerBullets = parseReviewerCommentBullets(submission?.reviewComments)

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', py: 10 }}>
        <CircularProgress size={36} sx={{ color: portalColors.primary, mb: 2 }} />
        <Typography sx={{ color: portalColors.textMuted }}>Loading compliance requirements…</Typography>
      </Box>
    )
  }

  if (!submission) return <Alert severity="error">Submission not found.</Alert>
  if (submission.status !== 'RevisionRequired') {
    return (
      <Box>
        <Alert severity="info" sx={{ mb: 2 }}>
          This submission is not currently marked for revision.
        </Alert>
        <Button component={RouterLink} to={`/client/accreditation/${uuid}`} variant="outlined" sx={portalOutlinedButtonSx}>
          Back to submission
        </Button>
      </Box>
    )
  }

  const handlePickFile = (fileUuid: string) => {
    activeFileRef.current = fileUuid
    fileInputRef.current?.click()
  }

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    const fileUuid = activeFileRef.current
    if (!file || !fileUuid) return

    setSubmitError('')
    setUploadingFileUuid(fileUuid)
    showLoadingSpinner()

    try {
      await uploadCompliance({ uuid, fileUuid, file }).unwrap()
      await refetch()
      setSuccessMessage('Revision uploaded successfully.')
    } catch {
      setSubmitError('Unable to upload the revised document. Please try again.')
    } finally {
      hideLoadingSpinner()
      setUploadingFileUuid(null)
      e.target.value = ''
    }
  }

  const handleConfirmResubmit = async () => {
    setSubmitError('')
    setConfirmOpen(false)
    showLoadingSpinner()

    try {
      await resubmitCompliance(uuid).unwrap()
      navigate(`/client/accreditation/${uuid}`, {
        replace: true,
        state: { complianceResubmitted: true },
      })
    } catch {
      setSubmitError('Unable to resubmit your revised documents. Please try again.')
    } finally {
      hideLoadingSpinner()
    }
  }

  return (
    <Box sx={{ maxWidth: 760 }}>
      <PortalPageHeader
        eyebrow="Accreditation compliance"
        title={submission.companyName}
        subtitle="Upload revised accreditation documents requested by the reviewer."
        actions={
          <Button component={RouterLink} to={`/client/accreditation/${uuid}`} variant="outlined" sx={portalOutlinedButtonSx}>
            Back
          </Button>
        }
      />

      <Box sx={{ mb: 2 }}>
        <AccreditationStatusChip status={submission.status} displayStatus={submission.displayStatus} history={submission.history} />
      </Box>

      {submission.reviewComments ? (
        <Alert severity="warning" sx={{ mb: 2 }}>
          {reviewerIntro ? <Typography sx={{ mb: reviewerBullets.length ? 1 : 0 }}>{reviewerIntro}</Typography> : null}
          {reviewerBullets.length > 0 ? (
            <Box component="ul" sx={{ m: 0, pl: 2.5 }}>
              {reviewerBullets.map((item) => (
                <Box component="li" key={item} sx={{ mb: 0.5 }}>
                  {item}
                </Box>
              ))}
            </Box>
          ) : null}
        </Alert>
      ) : null}

      {submitError ? <Alert severity="error" sx={{ mb: 2 }}>{submitError}</Alert> : null}

      <input ref={fileInputRef} type="file" hidden onChange={handleUpload} />

      <Stack spacing={2}>
        {complianceDocuments.length === 0 ? (
          <Alert severity="info">No documents currently flagged for revision.</Alert>
        ) : (
          complianceDocuments.map((document) => (
            <PortalPanel key={document.uuid} title={document.label}>
              <Box sx={{ px: 2.5, py: 2 }}>
                <Typography variant="body2" sx={{ color: portalColors.textMuted, mb: 1.5 }}>
                  Current file: {document.originalFileName}
                </Typography>

                {document.reviewComment && document.needsUpload ? (
                  <Alert severity="warning" sx={{ mb: 2 }}>
                    {document.reviewComment}
                  </Alert>
                ) : null}

                {document.revisionUploaded ? (
                  <Alert severity="success" icon={<CheckCircleOutlinedIcon />} sx={{ mb: 2 }}>
                    Revised document uploaded. You can resubmit once all required revisions are complete.
                  </Alert>
                ) : null}

                {(document.versions?.length ?? 0) > 1 ? (
                  <Typography variant="caption" sx={{ display: 'block', mb: 1.5, color: portalColors.textMuted }}>
                    {document.versions?.length} versions on file
                  </Typography>
                ) : null}

                <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                  <Button
                    variant="outlined"
                    sx={portalOutlinedButtonSx}
                    disabled={Boolean(uploadingFileUuid) || isFetching}
                    onClick={() => handlePickFile(document.uuid)}
                    startIcon={
                      uploadingFileUuid === document.uuid ? <CircularProgress size={16} color="inherit" /> : <UploadFileOutlinedIcon />
                    }
                  >
                    {uploadingFileUuid === document.uuid ? 'Uploading…' : document.revisionUploaded ? 'Replace revision' : 'Upload revision'}
                  </Button>
                  <Button
                    size="small"
                    onClick={() =>
                      downloadAuthenticatedFile(
                        `/accreditation/submissions/${uuid}/files/${document.uuid}/download`,
                        document.originalFileName,
                      )
                    }
                  >
                    Download current
                  </Button>
                </Stack>
              </Box>
            </PortalPanel>
          ))
        )}

        {!resubmitReady ? (
          <Alert severity="info">
            Upload a revised copy for each flagged document before resubmitting your application for review.
          </Alert>
        ) : (
          <Alert severity="success" icon={<CheckCircleOutlinedIcon />}>
            All required revisions are uploaded. You may now resubmit your application for review.
          </Alert>
        )}

        <Button
          variant="contained"
          sx={portalPrimaryButtonSx}
          disabled={!resubmitReady || Boolean(uploadingFileUuid) || isFetching}
          onClick={() => {
            setSubmitError('')
            setConfirmOpen(true)
          }}
        >
          Resubmit for Review
        </Button>
      </Stack>

      <Dialog open={confirmOpen} onClose={() => setConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Resubmit Revised Documents?</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted }}>
            You are about to resubmit the revised accreditation documents for <strong>{submission.companyName}</strong>.
            Your application will return to under review status and the assigned officer will evaluate your updates.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setConfirmOpen(false)} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleConfirmResubmit} variant="contained" sx={portalPrimaryButtonSx}>
            Yes, Resubmit for Review
          </Button>
        </DialogActions>
      </Dialog>

      <Snackbar
        open={Boolean(successMessage)}
        autoHideDuration={4000}
        onClose={() => setSuccessMessage('')}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="success" onClose={() => setSuccessMessage('')} sx={{ width: '100%' }}>
          {successMessage}
        </Alert>
      </Snackbar>
    </Box>
  )
}
