import { useRef } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  Stack,
  Typography,
} from '@mui/material'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  CONTAINER_INSPECTION_PHOTO_LABELS,
  CONTAINER_INSPECTION_PHOTO_TYPES,
  useGetContainerInspectionsQuery,
  useUploadContainerInspectionPhotoMutation,
  type ContainerInspectionPhotoType,
  type ClientContainerInspectionStatus,
} from '../api/clientApi'

function PhotoUploadRow({
  entryUuid,
  containerUuid,
  photoType,
  existing,
}: {
  entryUuid: string
  containerUuid: string
  photoType: ContainerInspectionPhotoType
  existing?: { originalFileName: string; reviewDecision: string; reviewComment?: string }
}) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [uploadPhoto, { isLoading }] = useUploadContainerInspectionPhotoMutation()

  const handleUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    try {
      await uploadPhoto({ entryUuid, containerUuid, photoType, file }).unwrap()
    } finally {
      e.target.value = ''
    }
  }

  return (
    <Box
      sx={{
        display: 'flex',
        flexWrap: 'wrap',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 1.5,
        py: 1.25,
        borderBottom: `1px solid ${portalColors.border}`,
        '&:last-child': { borderBottom: 0 },
      }}
    >
      <Box sx={{ minWidth: 0, flex: 1 }}>
        <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>
          {CONTAINER_INSPECTION_PHOTO_LABELS[photoType]}
        </Typography>
        {existing ? (
          <Stack direction="row" spacing={1} sx={{ mt: 0.5, alignItems: 'center', flexWrap: 'wrap' }}>
            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
              {existing.originalFileName}
            </Typography>
            <Chip size="small" label={existing.reviewDecision} sx={portalStatusChipSx(existing.reviewDecision)} />
          </Stack>
        ) : (
          <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mt: 0.5 }}>
            No file uploaded
          </Typography>
        )}
        {existing?.reviewComment && (
          <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.5 }}>
            {existing.reviewComment}
          </Typography>
        )}
      </Box>
      <input ref={fileInputRef} type="file" hidden accept="image/*,.pdf" onChange={handleUpload} />
      <Button
        size="small"
        variant={existing ? 'outlined' : 'contained'}
        sx={existing ? portalOutlinedButtonSx : portalPrimaryButtonSx}
        disabled={isLoading}
        onClick={() => fileInputRef.current?.click()}
      >
        {isLoading ? 'Uploading…' : existing ? 'Replace' : 'Upload'}
      </Button>
    </Box>
  )
}

function ContainerInspectionCard({
  entryUuid,
  container,
}: {
  entryUuid: string
  container: ClientContainerInspectionStatus
}) {
  return (
    <PortalPanel title={`Container ${container.containerNumber}`}>
      <Box sx={{ px: 2.5, py: 1.5 }}>
        <Stack direction="row" spacing={1} sx={{ mb: 1.5 }}>
          <Chip
            size="small"
            label={container.isComplete ? 'Complete' : 'Incomplete'}
            sx={portalStatusChipSx(container.isComplete ? 'Approved' : 'Pending')}
          />
          {container.isApproved && (
            <Chip size="small" label="Approved" sx={portalStatusChipSx('Approved')} />
          )}
        </Stack>
        {CONTAINER_INSPECTION_PHOTO_TYPES.map((photoType) => {
          const existing = container.photos.find((p) => p.photoType === photoType)
          return (
            <PhotoUploadRow
              key={photoType}
              entryUuid={entryUuid}
              containerUuid={container.containerUuid}
              photoType={photoType}
              existing={existing}
            />
          )
        })}
      </Box>
    </PortalPanel>
  )
}

export function EntryContainerInspectionPanel({ entryUuid }: { entryUuid: string }) {
  const { data, isLoading } = useGetContainerInspectionsQuery(entryUuid)
  const containers = data?.data ?? []

  if (isLoading) {
    return <Typography sx={{ color: portalColors.textMuted }}>Loading inspection requirements…</Typography>
  }

  if (containers.length === 0) {
    return (
      <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
        No containers are ready for inspection photo upload yet.
      </Alert>
    )
  }

  return (
    <Stack spacing={2}>
      <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
        Upload all six required photos for each container. Photos will be reviewed by the agency before transport tagging.
      </Alert>
      {containers.map((container) => (
        <ContainerInspectionCard key={container.containerUuid} entryUuid={entryUuid} container={container} />
      ))}
    </Stack>
  )
}
