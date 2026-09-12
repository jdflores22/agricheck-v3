import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import ReportProblemOutlinedIcon from '@mui/icons-material/ReportProblemOutlined'
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined'
import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useMemo, useState, type ReactNode } from 'react'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { AccreditationStatusChip } from '../../accreditation/AccreditationStatusChip'
import { isResubmittedForReview } from '../../accreditation/accreditationStatusUtils'
import {
  FormFieldSchema,
  getAddressValueKeys,
  visibleFormFields,
} from '../../forms/formSchema'
import { DocumentVersionCompareModal } from './DocumentVersionCompareModal'
import { DocumentViewModal } from './DocumentViewModal'
import { RequiredDocumentPreviewCard } from './RequiredDocumentPreviewCard'
import {
  resolveCurrentSubmissionFile,
  resolveVersionComparePair,
  type SubmissionFileVersion,
  type SubmissionFileWithVersions,
} from '../utils/submissionFileUtils'

type SubmissionFile = SubmissionFileWithVersions

export type RequiredDocumentViewModel = {
  field: FormFieldSchema
  fileName: string
  fileUuid: string
  filePath: string
  uploadedAt?: string
  fileSizeBytes?: number
  versionNumber?: number
  previewKey?: string
  versions?: SubmissionFileVersion[]
  reviewDecision?: string
  reviewComment?: string
}

type SubmissionSummary = {
  uuid: string
  companyName: string
  submissionType: string
  status: string
  displayStatus?: string
  reviewComments?: string
  accreditationNumber?: string
  submittedAt?: string
  files: SubmissionFile[]
  history?: Array<{ status: string; comment?: string; createdAt: string }>
}

type ViewingDocument = {
  title: string
  fileName: string
  filePath: string
  previewKey?: string
}

type ComparingDocument = {
  title: string
  fileUuid: string
  previous: SubmissionFileVersion
  current: SubmissionFileVersion
}

function formatSubmissionType(type: string) {
  return type === 'RENEWAL' ? 'Renewal Application' : 'New Application'
}

function formatFieldValue(field: FormFieldSchema, values: Record<string, string>): string {
  if (field.type === 'address') {
    const keys = getAddressValueKeys(field.name)
    const parts = [
      values[keys.street],
      values[keys.barangayName],
      values[keys.cityName],
      values[keys.provinceName],
      values[keys.regionName],
      values[keys.zipCode],
    ].filter(Boolean)
    return parts.join(', ') || '—'
  }

  if (field.type === 'checkbox') {
    const value = values[field.name]
    if (value === 'true' || value === '1') return 'Yes'
    if (value === 'false' || value === '0') return 'No'
    return value?.trim() || '—'
  }

  const raw = values[field.name]?.trim()
  if (!raw) return '—'

  if ((field.type === 'select' || field.type === 'radio') && field.options?.length) {
    const match = field.options.find((option) => option.value === raw)
    return match?.label ?? raw
  }

  return raw
}

function MetaTile({ label, value, sub }: { label: string; value: string; sub?: string }) {
  return (
    <Box
      sx={{
        p: 2,
        borderRadius: '0.5rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
      }}
    >
      <Typography variant="caption" sx={{ color: portalColors.textMuted, fontWeight: 600, textTransform: 'uppercase' }}>
        {label}
      </Typography>
      <Typography sx={{ fontWeight: 600, mt: 0.5 }}>{value}</Typography>
      {sub ? (
        <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
          {sub}
        </Typography>
      ) : null}
    </Box>
  )
}

function StatusCallout({ submission }: { submission: SubmissionSummary }) {
  const { status, reviewComments, uuid, displayStatus, history } = submission
  const resubmitted = isResubmittedForReview({ status, displayStatus, history })

  if (resubmitted) {
    return (
      <Alert severity="success" icon={<CheckCircleOutlinedIcon />}>
        Your revised documents were resubmitted. The accreditation team is reviewing your application again.
      </Alert>
    )
  }

  if (status === 'Submitted' || status === 'UnderReview') {
    return (
      <Alert severity="info" icon={<InfoOutlinedIcon />}>
        Your application is under review. We will notify you when the status changes.
      </Alert>
    )
  }

  if (status === 'Approved') {
    return (
      <Alert severity="success" icon={<CheckCircleOutlinedIcon />}>
        Congratulations! Your accreditation has been approved. You can now submit entries and access all system features.
      </Alert>
    )
  }

  if (status === 'RevisionRequired') {
    return (
      <Alert
        severity="warning"
        icon={<WarningAmberOutlinedIcon />}
        action={
          <Button color="inherit" size="small" component={RouterLink} to={`/client/accreditation/${uuid}/compliance`}>
            Open compliance
          </Button>
        }
      >
        Additional documents or revisions are required. Review the officer comments and upload corrected files.
        {reviewComments ? ` ${reviewComments}` : ''}
      </Alert>
    )
  }

  if (status === 'Rejected') {
    return (
      <Alert severity="error" icon={<ReportProblemOutlinedIcon />}>
        Your accreditation application was not approved.
        {reviewComments ? ` ${reviewComments}` : ' Please contact the Department of Agriculture for more information.'}
      </Alert>
    )
  }

  return null
}

export function AccreditationStatusCallout({ submission }: { submission: SubmissionSummary }) {
  return <StatusCallout submission={submission} />
}

export function AccreditationSubmissionSummary({
  submission,
  schemaFields,
  formValues,
  formName,
  statusLabel,
  hideStatusSection = false,
  renderBelowDocumentPreview,
  buildDocumentDownloadPath,
  buildVersionDownloadPath,
}: {
  submission: SubmissionSummary
  schemaFields: FormFieldSchema[]
  formValues: Record<string, string>
  formName?: string
  statusLabel?: string
  hideStatusSection?: boolean
  renderBelowDocumentPreview?: (document: RequiredDocumentViewModel) => ReactNode
  buildDocumentDownloadPath?: (submissionUuid: string, fileUuid: string) => string
  buildVersionDownloadPath?: (submissionUuid: string, fileUuid: string, versionNumber: number) => string
}) {
  const [viewingDocument, setViewingDocument] = useState<ViewingDocument | null>(null)
  const [comparingDocument, setComparingDocument] = useState<ComparingDocument | null>(null)
  const submittedAt = submission.submittedAt ? new Date(submission.submittedAt) : null

  const visibleFields = useMemo(
    () => visibleFormFields(schemaFields, formValues),
    [schemaFields, formValues],
  )

  const fileFields = useMemo(
    () => visibleFields.filter((field) => field.type === 'file' || field.type === 'geotag_photo'),
    [visibleFields],
  )

  const textFields = useMemo(
    () => visibleFields.filter((field) => field.type !== 'file' && field.type !== 'geotag_photo'),
    [visibleFields],
  )

  const filesByUuid = useMemo(
    () => new Map(submission.files.map((file) => [file.uuid, file])),
    [submission.files],
  )

  const requiredDocuments = useMemo(
    () =>
      fileFields
        .map((field) => {
          const fileNameFromForm = formValues[field.name]?.trim()
          const fileUuid = formValues[`${field.name}_file_uuid`]?.trim()
          if (!fileNameFromForm || !fileUuid) return null

          const matchedFile = filesByUuid.get(fileUuid)
          const currentFile = resolveCurrentSubmissionFile(matchedFile)
          const filePath = buildDocumentDownloadPath
            ? buildDocumentDownloadPath(submission.uuid, fileUuid)
            : `/accreditation/submissions/${submission.uuid}/files/${fileUuid}/download`
          return {
            field,
            fileName: currentFile?.fileName ?? matchedFile?.originalFileName ?? fileNameFromForm,
            fileUuid,
            filePath,
            uploadedAt: currentFile?.uploadedAt ?? matchedFile?.createdAt,
            fileSizeBytes: currentFile?.fileSizeBytes ?? matchedFile?.fileSizeBytes,
            versionNumber: currentFile?.versionNumber,
            previewKey: currentFile?.previewKey,
            versions: matchedFile?.versions,
            reviewDecision: matchedFile?.reviewDecision,
            reviewComment: matchedFile?.reviewComment,
          }
        })
        .filter((item): item is NonNullable<typeof item> => item !== null),
    [fileFields, formValues, filesByUuid, submission.uuid, buildDocumentDownloadPath],
  )

  return (
    <>
      <Stack spacing={3}>
        <PortalPanel title="Submission Details">
          <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
            {!hideStatusSection ? (
              <>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
                  <AccreditationStatusChip
                    status={submission.status}
                    displayStatus={submission.displayStatus ?? statusLabel}
                    history={submission.history}
                  />
                  <Chip size="small" label={formatSubmissionType(submission.submissionType)} variant="outlined" />
                </Box>
                <StatusCallout submission={submission} />
              </>
            ) : null}

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                gap: 1.5,
              }}
            >
              <MetaTile
                label="Submission Date"
                value={submittedAt ? submittedAt.toLocaleDateString(undefined, { dateStyle: 'medium' }) : 'Not submitted'}
                sub={submittedAt ? submittedAt.toLocaleTimeString(undefined, { timeStyle: 'short' }) : undefined}
              />
              <MetaTile label="Company" value={submission.companyName} />
              {submission.accreditationNumber ? (
                <MetaTile label="Accreditation Number" value={submission.accreditationNumber} />
              ) : null}
              {formName ? <MetaTile label="Form Type" value={formName} /> : null}
              {hideStatusSection ? (
                <MetaTile label="Application Type" value={formatSubmissionType(submission.submissionType)} />
              ) : null}
            </Box>

            {submission.reviewComments && submission.status !== 'RevisionRequired' && submission.status !== 'Rejected' ? (
              <Alert severity="info">{submission.reviewComments}</Alert>
            ) : null}
          </Stack>
        </PortalPanel>

        {textFields.length > 0 ? (
          <PortalPanel title="Submitted Information">
            <Box
              sx={{
                px: 2.5,
                py: 2,
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                gap: 2,
              }}
            >
              {textFields.map((field) => {
                if (field.type === 'section') {
                  return (
                    <Box key={field.id} sx={{ gridColumn: '1 / -1', pt: 1 }}>
                      <Typography sx={{ fontWeight: 600, fontSize: '0.95rem' }}>{field.label}</Typography>
                    </Box>
                  )
                }

                return (
                  <Box
                    key={field.id}
                    sx={{
                      pb: 1.5,
                      borderBottom: `1px solid ${portalColors.border}`,
                    }}
                  >
                    <Typography variant="caption" sx={{ color: portalColors.textMuted, fontWeight: 600 }}>
                      {field.label}
                    </Typography>
                    <Typography variant="body2" sx={{ mt: 0.5, whiteSpace: 'pre-wrap' }}>
                      {formatFieldValue(field, formValues)}
                    </Typography>
                  </Box>
                )
              })}
            </Box>
          </PortalPanel>
        ) : null}

        {requiredDocuments.length > 0 ? (
          <PortalPanel title="Required Documents">
            <Box
              sx={{
                px: 2.5,
                py: 2,
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', lg: '1fr 1fr 1fr' },
                gap: 2,
              }}
            >
              {requiredDocuments.map((document) => (
                <Box
                  key={document.field.id}
                  sx={{
                    border: renderBelowDocumentPreview ? `1px solid ${portalColors.border}` : undefined,
                    borderRadius: renderBelowDocumentPreview ? '0.75rem' : undefined,
                    overflow: renderBelowDocumentPreview ? 'hidden' : undefined,
                  }}
                >
                  <RequiredDocumentPreviewCard
                    label={document.field.label}
                    fileName={document.fileName}
                    filePath={document.filePath}
                    uploadedAt={document.uploadedAt}
                    fileSizeBytes={document.fileSizeBytes}
                    versionNumber={document.versionNumber}
                    previewKey={document.previewKey}
                    embedded={Boolean(renderBelowDocumentPreview)}
                    onView={() =>
                      setViewingDocument({
                        title: document.field.label,
                        fileName: document.fileName,
                        filePath: document.filePath,
                        previewKey: document.previewKey,
                      })
                    }
                    onCompareVersions={
                      buildVersionDownloadPath && resolveVersionComparePair(document.versions)
                        ? () => {
                            const pair = resolveVersionComparePair(document.versions)
                            if (!pair) return
                            setComparingDocument({
                              title: document.field.label,
                              fileUuid: document.fileUuid,
                              previous: pair.previous,
                              current: pair.current,
                            })
                          }
                        : undefined
                    }
                  />
                  {renderBelowDocumentPreview ? renderBelowDocumentPreview(document) : null}
                </Box>
              ))}
            </Box>
          </PortalPanel>
        ) : null}
      </Stack>

      <DocumentViewModal
        open={Boolean(viewingDocument)}
        onClose={() => setViewingDocument(null)}
        title={viewingDocument?.title ?? 'Document'}
        fileName={viewingDocument?.fileName ?? 'document'}
        filePath={viewingDocument?.filePath ?? ''}
        previewKey={viewingDocument?.previewKey}
      />

      {comparingDocument && buildVersionDownloadPath ? (
        <DocumentVersionCompareModal
          open={Boolean(comparingDocument)}
          onClose={() => setComparingDocument(null)}
          title={comparingDocument.title}
          previous={{
            versionNumber: comparingDocument.previous.versionNumber,
            fileName: comparingDocument.previous.originalFileName,
            fileSizeBytes: comparingDocument.previous.fileSizeBytes,
            filePath: buildVersionDownloadPath(
              submission.uuid,
              comparingDocument.fileUuid,
              comparingDocument.previous.versionNumber,
            ),
          }}
          current={{
            versionNumber: comparingDocument.current.versionNumber,
            fileName: comparingDocument.current.originalFileName,
            fileSizeBytes: comparingDocument.current.fileSizeBytes,
            filePath: buildVersionDownloadPath(
              submission.uuid,
              comparingDocument.fileUuid,
              comparingDocument.current.versionNumber,
            ),
          }}
        />
      ) : null}
    </>
  )
}
