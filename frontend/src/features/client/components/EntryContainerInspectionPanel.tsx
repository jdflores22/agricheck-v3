import { useRef, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  LinearProgress,
  Tab,
  Tabs,
  Typography,
} from '@mui/material'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
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

  const uploaded = Boolean(existing)

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
      <Box sx={{ minWidth: 0, flex: 1, display: 'flex', gap: 1.25, alignItems: 'flex-start' }}>
        <CheckCircleOutlinedIcon
          sx={{
            mt: 0.15,
            fontSize: 20,
            color: uploaded ? portalColors.successText : portalColors.borderStrong,
          }}
        />
        <Box sx={{ minWidth: 0 }}>
          <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>
            {CONTAINER_INSPECTION_PHOTO_LABELS[photoType]}
          </Typography>
          {existing ? (
            <Box
              sx={{
                mt: 0.5,
                display: 'flex',
                flexWrap: 'wrap',
                alignItems: 'center',
                gap: 1,
              }}
            >
              <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                {existing.originalFileName}
              </Typography>
              <Chip size="small" label={existing.reviewDecision} sx={portalStatusChipSx(existing.reviewDecision)} />
            </Box>
          ) : (
            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mt: 0.5 }}>
              Required
            </Typography>
          )}
          {existing?.reviewComment && (
            <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.5 }}>
              {existing.reviewComment}
            </Typography>
          )}
        </Box>
      </Box>
      <input ref={fileInputRef} type="file" hidden accept="image/*,.pdf" onChange={handleUpload} />
      <Button
        size="small"
        variant={uploaded ? 'outlined' : 'contained'}
        sx={uploaded ? portalOutlinedButtonSx : portalPrimaryButtonSx}
        disabled={isLoading}
        onClick={() => fileInputRef.current?.click()}
      >
        {isLoading ? 'Uploading…' : uploaded ? 'Replace' : 'Upload'}
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
  const uploadedCount = container.photos.length
  const requiredCount = CONTAINER_INSPECTION_PHOTO_TYPES.length

  return (
    <PortalPanel title={`Container ${container.containerNumber}`}>
      <Box sx={{ px: 2.5, py: 1.5 }}>
        <Box sx={{ mb: 1.5, display: 'flex', flexWrap: 'wrap', gap: 1 }}>
          <Chip
            size="small"
            label={container.isComplete ? 'Complete' : `${uploadedCount}/${requiredCount} uploaded`}
            sx={portalStatusChipSx(container.isComplete ? 'Approved' : 'Pending')}
          />
          {container.isApproved && (
            <Chip size="small" label="Approved" sx={portalStatusChipSx('Approved')} />
          )}
        </Box>
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

function InspectionProgressSummary({ containers }: { containers: ClientContainerInspectionStatus[] }) {
  const completeContainers = containers.filter((c) => c.isComplete).length
  const totalPhotos = containers.length * CONTAINER_INSPECTION_PHOTO_TYPES.length
  const uploadedPhotos = containers.reduce((sum, c) => sum + c.photos.length, 0)
  const progress = totalPhotos > 0 ? Math.round((uploadedPhotos / totalPhotos) * 100) : 0

  return (
    <Box
      sx={{
        mb: 2,
        px: 2.5,
        py: 2,
        borderRadius: '0.75rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
      }}
    >
      <Box
        sx={{
          mb: 1,
          display: 'flex',
          justifyContent: 'space-between',
          gap: 2,
        }}
      >
        <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>
          {completeContainers}/{containers.length} containers complete
        </Typography>
        <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
          {uploadedPhotos}/{totalPhotos} photos
        </Typography>
      </Box>
      <LinearProgress
        variant="determinate"
        value={progress}
        sx={{
          height: 6,
          borderRadius: 999,
          bgcolor: portalColors.bgMuted,
          '& .MuiLinearProgress-bar': { bgcolor: portalColors.primary, borderRadius: 999 },
        }}
      />
    </Box>
  )
}

type EntryContainerInspectionPanelProps = {
  entryUuid: string
  layout?: 'stack' | 'tabs'
  showProgress?: boolean
}

export function EntryContainerInspectionPanel({
  entryUuid,
  layout = 'stack',
  showProgress = false,
}: EntryContainerInspectionPanelProps) {
  const { data, isLoading } = useGetContainerInspectionsQuery(entryUuid)
  const containers = data?.data ?? []
  const [activeTab, setActiveTab] = useState(0)

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

  const useTabs = layout === 'tabs' && containers.length > 1
  const activeContainer = containers[useTabs ? activeTab : 0]

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      {showProgress && <InspectionProgressSummary containers={containers} />}

      <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
        Upload all six required photos for each container. The agency will review them before transport tagging.
      </Alert>

      {useTabs && (
        <Tabs
          value={activeTab}
          onChange={(_, value) => setActiveTab(value)}
          variant="scrollable"
          scrollButtons="auto"
          sx={{
            borderBottom: `1px solid ${portalColors.border}`,
            '& .MuiTab-root': { textTransform: 'none', fontWeight: 500, minHeight: 44 },
          }}
        >
          {containers.map((container) => (
            <Tab
              key={container.containerUuid}
              label={
                <Box sx={{ display: 'inline-flex', alignItems: 'center', gap: 1 }}>
                  <span>{container.containerNumber}</span>
                  {container.isComplete && (
                    <CheckCircleOutlinedIcon sx={{ fontSize: 16, color: portalColors.successText }} />
                  )}
                </Box>
              }
            />
          ))}
        </Tabs>
      )}

      {useTabs && activeContainer ? (
        <ContainerInspectionCard entryUuid={entryUuid} container={activeContainer} />
      ) : (
        containers.map((container) => (
          <ContainerInspectionCard key={container.containerUuid} entryUuid={entryUuid} container={container} />
        ))
      )}
    </Box>
  )
}
