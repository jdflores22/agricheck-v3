import { useMemo, useState, type ReactNode } from 'react'
import { Box, Stack, Typography } from '@mui/material'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { DocumentVersionCompareModal } from '../../client/components/DocumentVersionCompareModal'
import { DocumentViewModal } from '../../client/components/DocumentViewModal'
import { RequiredDocumentPreviewCard } from '../../client/components/RequiredDocumentPreviewCard'
import type { RequiredDocumentViewModel } from '../../client/components/AccreditationSubmissionSummary'
import {
  FormFieldSchema,
  formatCommodityHsSummary,
  getAddressValueKeys,
  visibleFormFields,
} from '../../forms/formSchema'
import {
  resolveCurrentSubmissionFile,
  resolveVersionComparePair,
  type SubmissionFileVersion,
  type SubmissionFileWithVersions,
} from '../../client/utils/submissionFileUtils'

type EntrySummaryFile = SubmissionFileWithVersions & {
  evaluationDecision?: string
  evaluationComment?: string
}

type EntrySummary = {
  uuid: string
  referenceNo: string
  entryType: string
  importTrack?: string
  companyName?: string
  applicantName: string
  submittedAt?: string
  notes?: string
  files: EntrySummaryFile[]
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

function formatFieldValue(field: FormFieldSchema, values: Record<string, string>): string {
  if (field.type === 'commodity') {
    return formatCommodityHsSummary(field.name, values) || '—'
  }

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

export function EntryEvaluationSummary({
  entry,
  schemaFields,
  formValues,
  formName,
  renderBelowDocumentPreview,
  buildDocumentDownloadPath,
  buildVersionDownloadPath,
}: {
  entry: EntrySummary
  schemaFields: FormFieldSchema[]
  formValues: Record<string, string>
  formName?: string
  renderBelowDocumentPreview?: (document: RequiredDocumentViewModel) => ReactNode
  buildDocumentDownloadPath?: (entryUuid: string, fileUuid: string) => string
  buildVersionDownloadPath?: (entryUuid: string, fileUuid: string, versionNumber: number) => string
}) {
  const [viewingDocument, setViewingDocument] = useState<ViewingDocument | null>(null)
  const [comparingDocument, setComparingDocument] = useState<ComparingDocument | null>(null)
  const submittedAt = entry.submittedAt ? new Date(entry.submittedAt) : null

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
    () => new Map(entry.files.map((file) => [file.uuid, file])),
    [entry.files],
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
            ? buildDocumentDownloadPath(entry.uuid, fileUuid)
            : `/agency/evaluator/entries/${entry.uuid}/files/${fileUuid}/download`

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
            reviewDecision: matchedFile?.evaluationDecision,
            reviewComment: matchedFile?.evaluationComment,
          }
        })
        .filter((item): item is NonNullable<typeof item> => item !== null),
    [fileFields, formValues, filesByUuid, entry.uuid, buildDocumentDownloadPath],
  )

  return (
    <>
      <Stack spacing={3}>
        <PortalPanel title="Entry Details">
          <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                gap: 1.5,
              }}
            >
              <MetaTile
                label="Submitted"
                value={submittedAt ? submittedAt.toLocaleDateString(undefined, { dateStyle: 'medium' }) : 'Not submitted'}
                sub={submittedAt ? submittedAt.toLocaleTimeString(undefined, { timeStyle: 'short' }) : undefined}
              />
              <MetaTile label="Reference" value={entry.referenceNo} />
              <MetaTile label="Company" value={entry.companyName || '—'} sub={entry.applicantName} />
              <MetaTile label="Entry Type" value={entry.entryType} />
              {entry.entryType === 'Import' && entry.importTrack ? (
                <MetaTile
                  label="Import Track"
                  value={entry.importTrack === 'Mav' ? 'MAV (in-quota)' : 'Regular (out-quota)'}
                />
              ) : null}
              {formName ? <MetaTile label="Form" value={formName} /> : null}
            </Box>
            {entry.notes ? (
              <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
                Notes: {entry.notes}
              </Typography>
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
              entry.uuid,
              comparingDocument.fileUuid,
              comparingDocument.previous.versionNumber,
            ),
          }}
          current={{
            versionNumber: comparingDocument.current.versionNumber,
            fileName: comparingDocument.current.originalFileName,
            fileSizeBytes: comparingDocument.current.fileSizeBytes,
            filePath: buildVersionDownloadPath(
              entry.uuid,
              comparingDocument.fileUuid,
              comparingDocument.current.versionNumber,
            ),
          }}
        />
      ) : null}
    </>
  )
}
