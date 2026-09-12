import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useRef, useState, useEffect, useMemo } from 'react'
import {
  useGetAccreditationSubmissionQuery,
  useGetClientFormQuery,
  useGetClientFormsQuery,
  useSubmitAccreditationMutation,
  useUpdateAccreditationMutation,
  useUploadAccreditationFileMutation,
} from '../api/clientApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { downloadAuthenticatedFile } from '../utils/downloadFile'
import { DynamicFormRenderer } from '../components/DynamicFormRenderer'
import { AccreditationStatusTimeline } from '../components/AccreditationStatusTimeline'
import { AccreditationSubmissionSummary, AccreditationStatusCallout } from '../components/AccreditationSubmissionSummary'
import { AccreditationStatusChip } from '../../accreditation/AccreditationStatusChip'
import {
  parseFormDataJson,
  parseFormSchema,
  resolveCompanyNameFromFormValues,
  serializeFormDataJson,
} from '../../forms/formSchema'
import { useLoadingSpinner } from '../../system/SystemBrandingProvider'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'

export function AccreditationDetailPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const { showLoadingSpinner, hideLoadingSpinner } = useLoadingSpinner()
  const { data, isLoading, refetch } = useGetAccreditationSubmissionQuery(uuid, { skip: !uuid })
  const { data: formsData } = useGetClientFormsQuery({ formType: 'ACCREDITATION' })
  const selectedFormUuid = formsData?.data?.[0]?.uuid
  const { data: formSchemaData } = useGetClientFormQuery(selectedFormUuid ?? '', { skip: !selectedFormUuid })
  const [submitAccreditation, { isLoading: submitting }] = useSubmitAccreditationMutation()
  const [updateAccreditation, { isLoading: saving }] = useUpdateAccreditationMutation()
  const [uploadFile, { isLoading: uploading }] = useUploadAccreditationFileMutation()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const submission = data?.data

  const [companyName, setCompanyName] = useState('')
  const [formNotes, setFormNotes] = useState('')
  const [dynamicValues, setDynamicValues] = useState<Record<string, string>>({})
  const [uploadingField, setUploadingField] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)

  const schemaFields = useMemo(
    () => (formSchemaData?.data?.schemaJson ? parseFormSchema(formSchemaData.data.schemaJson) : []),
    [formSchemaData?.data?.schemaJson],
  )
  const useDynamicForm = schemaFields.length > 0
  const formHasFileFields = useMemo(
    () => schemaFields.some((field) => field.type === 'file' || field.type === 'geotag_photo'),
    [schemaFields],
  )

  useEffect(() => {
    if (!submission) return

    setCompanyName(submission.companyName)

    if (useDynamicForm) {
      const parsed = parseFormDataJson(submission.formDataJson)
      setDynamicValues(parsed)
      const resolvedName = resolveCompanyNameFromFormValues(parsed, submission.companyName)
      if (resolvedName) {
        setCompanyName(resolvedName)
      }
      return
    }

    setFormNotes(submission.formDataJson && submission.formDataJson !== '{}' ? submission.formDataJson : '')
  }, [submission, useDynamicForm])

  useBreadcrumbLabel(submission?.companyName || companyName || undefined)

  if (isLoading) {
    return <Typography sx={{ color: portalColors.textMuted }}>Loading submission…</Typography>
  }

  if (!submission) {
    return (
      <Box>
        <PortalPageHeader eyebrow="Accreditation" title="Submission not found" />
        <Button component={RouterLink} to="/client/accreditation" variant="outlined" sx={portalOutlinedButtonSx}>
          Back to list
        </Button>
      </Box>
    )
  }

  const canEdit = submission.status === 'Draft'
  const canSubmit = submission.status === 'Draft'
  const isSubmittedView = submission.status !== 'Draft'
  const submissionTypeLabel = submission.submissionType === 'RENEWAL' ? 'Renewal Application' : 'New Application'

  const handleSaveDraft = async () => {
    const resolvedCompanyName = useDynamicForm
      ? resolveCompanyNameFromFormValues(dynamicValues, companyName)
      : companyName

    await updateAccreditation({
      uuid,
      body: {
        companyName: resolvedCompanyName,
        formDataJson: useDynamicForm ? serializeFormDataJson(dynamicValues) : formNotes || '{}',
      },
    }).unwrap()
    refetch()
  }

  const handleDynamicChange = (name: string, value: string) => {
    setDynamicValues((prev) => {
      const next = { ...prev, [name]: value }
      const resolvedName = resolveCompanyNameFromFormValues(next, companyName)
      if (resolvedName !== companyName) {
        setCompanyName(resolvedName)
      }
      return next
    })
  }

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    await uploadFile({ uuid, file }).unwrap()
    refetch()
    e.target.value = ''
  }

  const handleFormFileUpload = async (fieldName: string, file: File) => {
    setUploadingField(fieldName)
    try {
      const result = await uploadFile({ uuid, file }).unwrap()
      const uploaded = result.data as { uuid: string; originalFileName: string }
      const nextValues = {
        ...dynamicValues,
        [fieldName]: uploaded.originalFileName,
        [`${fieldName}_file_uuid`]: uploaded.uuid,
      }
      setDynamicValues(nextValues)
      await updateAccreditation({
        uuid,
        body: {
          companyName: resolveCompanyNameFromFormValues(nextValues, companyName),
          formDataJson: serializeFormDataJson(nextValues),
        },
      }).unwrap()
      refetch()
    } finally {
      setUploadingField(null)
    }
  }

  const handleDownloadFile = async (fileUuid: string, fileName: string) => {
    await downloadAuthenticatedFile(`/accreditation/submissions/${uuid}/files/${fileUuid}/download`, fileName)
  }

  const handleConfirmSubmit = async () => {
    setConfirmOpen(false)
    setSubmitError(null)
    showLoadingSpinner()

    try {
      if (canEdit) {
        await handleSaveDraft()
      }
      await submitAccreditation(uuid).unwrap()
      navigate(`/client/accreditation/${uuid}/confirmation`, { replace: true })
    } catch {
      setSubmitError('Failed to submit your accreditation application. Please try again.')
    } finally {
      hideLoadingSpinner()
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Accreditation"
        title={companyName || submission.companyName}
        subtitle={submissionTypeLabel}
        actions={
          <Button component={RouterLink} to="/client/accreditation" variant="outlined" sx={portalOutlinedButtonSx}>
            Back
          </Button>
        }
      >
        <AccreditationStatusChip
          status={submission.status}
          displayStatus={submission.displayStatus}
          history={submission.history}
        />
      </PortalPageHeader>

      {isSubmittedView && submission.status === 'Approved' && submission.certificateUuid ? (
        <Alert
          severity="success"
          sx={{ mb: 3 }}
          action={
            <Stack direction="row" spacing={1}>
              <Button
                size="small"
                color="inherit"
                component={RouterLink}
                to={`/client/certificates/${submission.certificateUuid}`}
              >
                View Certificate
              </Button>
              <Button
                size="small"
                color="inherit"
                onClick={() =>
                  downloadAuthenticatedFile(
                    `/certificates/${submission.certificateUuid}/pdf`,
                    `certificate-${submission.certificateNumber ?? submission.certificateUuid}.pdf`,
                  )
                }
              >
                Download PDF
              </Button>
            </Stack>
          }
        >
          Your accreditation has been approved
          {submission.accreditationNumber ? ` (${submission.accreditationNumber})` : ''}.
          {submission.certificateNumber ? ` Certificate ${submission.certificateNumber} is now available.` : ''}
        </Alert>
      ) : null}

      {isSubmittedView ? (
        <Box sx={{ mb: 3 }}>
          <AccreditationStatusCallout submission={submission} />
        </Box>
      ) : null}

      {submitError && <Alert severity="error" sx={{ mb: 2 }}>{submitError}</Alert>}

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 2fr) minmax(280px, 1fr)' },
          gap: 3,
          alignItems: 'start',
        }}
      >
        <Stack spacing={3}>
          {isSubmittedView ? (
            <AccreditationSubmissionSummary
              submission={submission}
              schemaFields={schemaFields}
              formValues={dynamicValues}
              formName={formSchemaData?.data?.name}
              hideStatusSection
              buildDocumentDownloadPath={(submissionUuid, fileUuid) =>
                `/accreditation/submissions/${submissionUuid}/files/${fileUuid}/download`
              }
              buildVersionDownloadPath={(submissionUuid, fileUuid, versionNumber) =>
                `/accreditation/submissions/${submissionUuid}/files/${fileUuid}/versions/${versionNumber}/download`
              }
            />
          ) : (
            <>
              <PortalPanel title="Application Data">
                <Box sx={{ px: 2.5, py: 2 }}>
                  <Stack spacing={2}>
                    {useDynamicForm && formSchemaData?.data ? (
                      <>
                        <Alert severity="info">Using accreditation form: {formSchemaData.data.name}</Alert>
                        <DynamicFormRenderer
                          schemaJson={formSchemaData.data.schemaJson}
                          values={dynamicValues}
                          onChange={handleDynamicChange}
                          onFileUpload={handleFormFileUpload}
                          onFileDownload={handleDownloadFile}
                          uploadingField={uploadingField}
                        />
                      </>
                    ) : (
                      <>
                        <Alert severity="warning">
                          No published accreditation form template found. Contact admin or enter details manually.
                        </Alert>
                        <TextField label="Company Name" value={companyName} onChange={(e) => setCompanyName(e.target.value)} fullWidth />
                        <TextField
                          label="Additional Information"
                          value={formNotes}
                          onChange={(e) => setFormNotes(e.target.value)}
                          multiline
                          rows={4}
                          fullWidth
                          placeholder="Business address, contact details, and other accreditation information."
                        />
                      </>
                    )}
                    <Button variant="outlined" sx={portalOutlinedButtonSx} disabled={saving} onClick={handleSaveDraft}>
                      Save Draft
                    </Button>
                  </Stack>
                </Box>
              </PortalPanel>

              {!useDynamicForm && (
                <PortalPanel title="Supporting Documents">
                  <Box sx={{ px: 2.5, py: 2 }}>
                    <input ref={fileInputRef} type="file" hidden onChange={handleFileUpload} />
                    <Button
                      variant="outlined"
                      sx={{ mb: 2, ...portalOutlinedButtonSx }}
                      disabled={uploading}
                      onClick={() => fileInputRef.current?.click()}
                    >
                      Upload Document
                    </Button>
                    {submission.files.length === 0 ? (
                      <Typography sx={{ color: portalColors.textMuted }}>No files attached.</Typography>
                    ) : (
                      <PortalTablePanel title="" columns={['File', 'Size', 'Uploaded', '']} isEmpty={false}>
                        {submission.files.map((file) => (
                          <TableRow key={file.uuid}>
                            <TableCell>{file.originalFileName}</TableCell>
                            <TableCell>{Math.round(file.fileSizeBytes / 1024)} KB</TableCell>
                            <TableCell>{new Date(file.createdAt).toLocaleString()}</TableCell>
                            <TableCell>
                              <Button size="small" onClick={() => handleDownloadFile(file.uuid, file.originalFileName)}>
                                Download
                              </Button>
                            </TableCell>
                          </TableRow>
                        ))}
                      </PortalTablePanel>
                    )}
                  </Box>
                </PortalPanel>
              )}

              {useDynamicForm && formHasFileFields && submission.files.length > 0 && (
                <Alert severity="info">
                  Required documents are uploaded through the form fields above. Total attached files: {submission.files.length}.
                </Alert>
              )}

              {canSubmit && (
                <Button
                  variant="contained"
                  sx={{ ...portalPrimaryButtonSx, alignSelf: 'flex-start' }}
                  disabled={submitting}
                  onClick={() => setConfirmOpen(true)}
                >
                  Submit for Review
                </Button>
              )}
            </>
          )}
        </Stack>

        <Stack spacing={3}>
          <PortalPanel title="Quick Actions">
            <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
              {submission.status === 'RevisionRequired' && (
                <Button
                  component={RouterLink}
                  to={`/client/accreditation/${uuid}/compliance`}
                  variant="contained"
                  fullWidth
                  sx={portalPrimaryButtonSx}
                >
                  Submit Compliance
                </Button>
              )}
              <Button component={RouterLink} to="/client/accreditation" variant="outlined" fullWidth sx={portalOutlinedButtonSx}>
                Back to Application Hub
              </Button>
              <Button component={RouterLink} to="/client" variant="outlined" fullWidth sx={portalOutlinedButtonSx}>
                Dashboard
              </Button>
            </Stack>
          </PortalPanel>

          <PortalPanel title="Status History">
            <Box sx={{ px: 2.5, py: 2 }}>
              <AccreditationStatusTimeline history={submission.history} />
            </Box>
          </PortalPanel>
        </Stack>
      </Box>

      <Dialog open={confirmOpen} onClose={() => !submitting && setConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Submit Accreditation Application?</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted }}>
            You are about to submit <strong>{companyName || submission.companyName}</strong> for DA accreditation review.
            After submission, you will not be able to edit this application unless revisions are requested.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setConfirmOpen(false)} disabled={submitting} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleConfirmSubmit} variant="contained" disabled={submitting} sx={portalPrimaryButtonSx}>
            Yes, Submit Application
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
