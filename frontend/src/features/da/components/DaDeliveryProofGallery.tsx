import { Box, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'
import { RequiredDocumentPreviewCard } from '../../client/components/RequiredDocumentPreviewCard'
import { DocumentViewModal } from '../../client/components/DocumentViewModal'
import type { DaDeliveryProofPhoto } from '../api/daApi'

export function daDeliveryProofDownloadPath(photoUuid: string) {
  return `/da/logistics/delivery-proof-photos/${photoUuid}/download`
}

interface DaDeliveryProofGalleryProps {
  photos: DaDeliveryProofPhoto[]
}

export function DaDeliveryProofGallery({ photos }: DaDeliveryProofGalleryProps) {
  const [viewing, setViewing] = useState<{
    title: string
    fileName: string
    filePath: string
  } | null>(null)

  if (photos.length === 0) {
    return (
      <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted }}>
        No geotagged arrival photos yet. Driver must confirm pickup and warehouse check-in with camera proof in
        AgriTrack.
      </Typography>
    )
  }

  return (
    <>
      <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
        {photos.map((photo) => {
          const filePath = daDeliveryProofDownloadPath(photo.photoUuid)
          return (
            <Box key={photo.photoUuid} sx={{ flex: 1, minWidth: 0 }}>
              <RequiredDocumentPreviewCard
                label={photo.label}
                fileName={photo.originalFileName}
                filePath={filePath}
                uploadedAt={photo.capturedAtUtc ?? undefined}
                previewKey={photo.photoUuid}
                embedded
                onView={() =>
                  setViewing({
                    title: photo.label,
                    fileName: photo.originalFileName,
                    filePath,
                  })
                }
              />
              {photo.captureLatitude != null && photo.captureLongitude != null && (
                <Typography sx={{ fontSize: '0.72rem', color: portalColors.textMuted, mt: 0.5, px: 0.5 }}>
                  GPS {Number(photo.captureLatitude).toFixed(5)}, {Number(photo.captureLongitude).toFixed(5)}
                  {photo.capturedAtUtc ? ` · ${new Date(photo.capturedAtUtc).toLocaleString()}` : ''}
                </Typography>
              )}
            </Box>
          )
        })}
      </Stack>
      <DocumentViewModal
        open={Boolean(viewing)}
        onClose={() => setViewing(null)}
        title={viewing?.title ?? 'Geotag proof'}
        fileName={viewing?.fileName ?? 'photo.jpg'}
        filePath={viewing?.filePath ?? ''}
        previewKey={viewing?.filePath}
      />
    </>
  )
}
