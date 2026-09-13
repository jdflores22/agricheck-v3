import { useMemo, useState } from 'react'
import { Box, Stack, Typography } from '@mui/material'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { DocumentViewModal } from '../../client/components/DocumentViewModal'
import { RequiredDocumentPreviewCard } from '../../client/components/RequiredDocumentPreviewCard'
import {
  CONTAINER_INSPECTION_PHOTO_LABELS,
  CONTAINER_INSPECTION_PHOTO_TYPES,
} from '../../client/api/clientApi'
import type { AgencyContainerInspectionDetail } from '../api/agencyApi'
import { ContainerInspectionPhotoReviewControls } from './ContainerInspectionPhotoReviewControls'

function CompactDetail({
  label,
  value,
  sub,
}: {
  label: string
  value?: string | null
  sub?: string
}) {
  return (
    <Box sx={{ minWidth: 0 }}>
      <Typography variant="caption" sx={{ color: portalColors.textMuted, fontWeight: 600 }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 500, mt: 0.15 }}>
        {value ?? '—'}
      </Typography>
      {sub ? (
        <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
          {sub}
        </Typography>
      ) : null}
    </Box>
  )
}

type ViewingDocument = {
  title: string
  fileName: string
  filePath: string
}

export function ContainerInspectionReviewSummary({
  detail,
  onReviewPhoto,
  disablePhotoReviews = false,
  photoGridColumns = 2,
}: {
  detail: AgencyContainerInspectionDetail
  onReviewPhoto: (payload: { photoUuid: string; decision: 'Approved' | 'Rejected'; comment?: string }) => Promise<void>
  disablePhotoReviews?: boolean
  photoGridColumns?: 2 | 3
}) {
  const [viewingDocument, setViewingDocument] = useState<ViewingDocument | null>(null)
  const entry = detail.entry
  const submittedAt = detail.submittedAt ? new Date(detail.submittedAt) : null

  const photosByType = useMemo(
    () => new Map(detail.photos.map((photo) => [photo.photoType, photo])),
    [detail.photos],
  )

  const certificatePath = `/agency/workflow/entries/${entry.uuid}/certificate/pdf`
  const hasCertificate = Boolean(entry.certificateUuid || entry.certificateNumber)

  const pendingCount = detail.photos.filter((photo) => photo.reviewDecision === 'Pending').length

  return (
    <>
      <Stack spacing={3}>
        <PortalPanel title="Container & Entry Details">
          <Box
            sx={{
              px: 2.5,
              py: 1.75,
              display: 'grid',
              gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(3, minmax(0, 1fr))' },
              gap: 1.75,
              columnGap: 2.5,
            }}
          >
            <CompactDetail
              label="Submitted"
              value={submittedAt ? submittedAt.toLocaleDateString(undefined, { dateStyle: 'medium' }) : 'Not submitted'}
              sub={submittedAt ? submittedAt.toLocaleTimeString(undefined, { timeStyle: 'short' }) : undefined}
            />
            <CompactDetail label="Container" value={detail.containerNumber} sub={detail.containerType} />
            <CompactDetail label="Entry reference" value={entry.referenceNo} />
            <CompactDetail label="Company" value={entry.companyName} sub={entry.applicantName} />
            <CompactDetail label="Entry type" value={entry.entryType} />
            <CompactDetail label="Commodity" value={entry.commodityName} />
            <CompactDetail
              label="Quantity"
              value={entry.quantity != null ? `${entry.quantity} ${entry.unit ?? ''}`.trim() : undefined}
            />
            <CompactDetail label="Agency" value={entry.agencyCode} />
            <CompactDetail label="Origin" value={entry.originCountry} />
            <CompactDetail label="Destination" value={entry.destinationCountry} />
            <CompactDetail label="Port of entry" value={entry.portOfEntry} />
            {entry.description ? (
              <Box sx={{ gridColumn: { xs: '1 / -1', md: 'span 3' } }}>
                <CompactDetail label="Description" value={entry.description} />
              </Box>
            ) : null}
          </Box>
        </PortalPanel>

        {hasCertificate ? (
          <PortalPanel title="Entry Certificate">
            <Box sx={{ px: 2.5, py: 2 }}>
              {entry.certificateTitle ? (
                <Typography sx={{ fontSize: '0.875rem', mb: 1.5 }}>{entry.certificateTitle}</Typography>
              ) : null}
              <Box
                sx={{
                  maxWidth: 420,
                  border: `1px solid ${portalColors.border}`,
                  borderRadius: '0.75rem',
                  overflow: 'hidden',
                }}
              >
                <RequiredDocumentPreviewCard
                  label={entry.certificateNumber ? `Certificate ${entry.certificateNumber}` : 'Entry certificate'}
                  fileName={`certificate-${entry.certificateNumber ?? entry.uuid}.pdf`}
                  filePath={certificatePath}
                  onView={() =>
                    setViewingDocument({
                      title: entry.certificateTitle || 'Entry certificate',
                      fileName: `certificate-${entry.certificateNumber ?? entry.uuid}.pdf`,
                      filePath: certificatePath,
                    })
                  }
                />
              </Box>
            </Box>
          </PortalPanel>
        ) : null}

        <PortalPanel title={`Container Photos${pendingCount > 0 ? ` (${pendingCount} pending)` : ''}`}>
          <Box
            sx={{
              px: 2.5,
              py: 2,
              display: 'grid',
              gridTemplateColumns: photoGridColumns === 3
                ? { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))', md: 'repeat(3, minmax(0, 1fr))' }
                : { xs: '1fr', sm: '1fr 1fr' },
              gap: 2,
            }}
          >
            {CONTAINER_INSPECTION_PHOTO_TYPES.map((photoType) => {
              const photo = photosByType.get(photoType)
              const label = CONTAINER_INSPECTION_PHOTO_LABELS[photoType]

              if (!photo) {
                return (
                  <Box
                    key={photoType}
                    sx={{
                      border: `1px dashed ${portalColors.border}`,
                      borderRadius: '0.75rem',
                      p: 2,
                      minHeight: 280,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      color: portalColors.textMuted,
                      fontSize: '0.875rem',
                      textAlign: 'center',
                    }}
                  >
                    {label} — not uploaded
                  </Box>
                )
              }

              const filePath = `/agency/workflow/inspection-photos/${photo.uuid}/download`

              return (
                <Box
                  key={photo.uuid}
                  sx={{
                    border: `1px solid ${portalColors.border}`,
                    borderRadius: '0.75rem',
                    overflow: 'hidden',
                  }}
                >
                  <RequiredDocumentPreviewCard
                    label={label}
                    fileName={photo.originalFileName}
                    filePath={filePath}
                    uploadedAt={photo.createdAt}
                    previewKey={photo.uuid}
                    embedded
                    onView={() =>
                      setViewingDocument({
                        title: label,
                        fileName: photo.originalFileName,
                        filePath,
                      })
                    }
                  />
                  {!disablePhotoReviews ? (
                    <ContainerInspectionPhotoReviewControls
                      photo={photo}
                      onSave={onReviewPhoto}
                    />
                  ) : photo.reviewDecision !== 'Pending' ? (
                    <Box sx={{ px: 2, pb: 2 }}>
                      <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
                        Reviewed as {photo.reviewDecision}
                        {photo.reviewComment ? ` · ${photo.reviewComment}` : ''}
                      </Typography>
                    </Box>
                  ) : null}
                </Box>
              )
            })}
          </Box>
        </PortalPanel>
      </Stack>

      <DocumentViewModal
        open={Boolean(viewingDocument)}
        onClose={() => setViewingDocument(null)}
        title={viewingDocument?.title ?? 'Document'}
        fileName={viewingDocument?.fileName ?? 'document'}
        filePath={viewingDocument?.filePath ?? ''}
        previewKey={viewingDocument?.filePath}
      />
    </>
  )
}
