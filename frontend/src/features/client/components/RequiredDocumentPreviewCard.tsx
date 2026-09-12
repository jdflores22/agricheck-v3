import InsertDriveFileOutlinedIcon from '@mui/icons-material/InsertDriveFileOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import { Box, Button, Chip, CircularProgress, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  fetchAuthenticatedFile,
  isImageFile,
  isPdfFile,
  revokeAuthenticatedFileUrl,
} from '../utils/authenticatedFile'

type RequiredDocumentPreviewCardProps = {
  label: string
  fileName: string
  filePath: string
  uploadedAt?: string
  fileSizeBytes?: number
  versionNumber?: number
  previewKey?: string
  onView: () => void
  onCompareVersions?: () => void
  embedded?: boolean
}

export function RequiredDocumentPreviewCard({
  label,
  fileName,
  filePath,
  uploadedAt,
  fileSizeBytes,
  versionNumber,
  previewKey,
  onView,
  onCompareVersions,
  embedded = false,
}: RequiredDocumentPreviewCardProps) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [previewKind, setPreviewKind] = useState<'pdf' | 'image' | 'other' | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)

  useEffect(() => {
    let cancelled = false
    let objectUrl: string | null = null

    setLoading(true)
    setError(false)
    setPreviewUrl(null)
    setPreviewKind(null)

    fetchAuthenticatedFile(filePath)
      .then((file) => {
        if (cancelled) {
          revokeAuthenticatedFileUrl(file.objectUrl)
          return
        }

        objectUrl = file.objectUrl
        if (isPdfFile(file.contentType, fileName)) {
          setPreviewKind('pdf')
        } else if (isImageFile(file.contentType, fileName)) {
          setPreviewKind('image')
        } else {
          setPreviewKind('other')
        }
        setPreviewUrl(file.objectUrl)
        setLoading(false)
      })
      .catch(() => {
        if (!cancelled) {
          setError(true)
          setLoading(false)
        }
      })

    return () => {
      cancelled = true
      if (objectUrl) {
        revokeAuthenticatedFileUrl(objectUrl)
      }
    }
  }, [filePath, fileName, previewKey])

  return (
    <Box
      sx={{
        border: embedded ? 0 : `1px solid ${portalColors.border}`,
        borderRadius: embedded ? 0 : '0.75rem',
        overflow: 'hidden',
        bgcolor: portalColors.bgWhite,
      }}
    >
      <Box
        sx={{
          position: 'relative',
          height: 220,
          bgcolor: portalColors.bgMuted,
          cursor: 'pointer',
          '&:hover .preview-overlay': {
            opacity: 1,
          },
        }}
        onClick={onView}
        role="button"
        tabIndex={0}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault()
            onView()
          }
        }}
      >
        {loading ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
            <CircularProgress size={24} />
          </Box>
        ) : null}

        {error ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', px: 2 }}>
            <InsertDriveFileOutlinedIcon sx={{ fontSize: 40, color: portalColors.textLight, mb: 1 }} />
            <Typography variant="caption" sx={{ color: portalColors.textMuted, textAlign: 'center' }}>
              Preview unavailable
            </Typography>
          </Box>
        ) : null}

        {!loading && !error && previewUrl && previewKind === 'pdf' ? (
          <Box
            component="iframe"
            src={`${previewUrl}#page=1&view=FitH&toolbar=0&navpanes=0`}
            title={`${label} preview`}
            sx={{
              width: '200%',
              height: '200%',
              border: 0,
              transform: 'scale(0.5)',
              transformOrigin: 'top left',
              pointerEvents: 'none',
            }}
          />
        ) : null}

        {!loading && !error && previewUrl && previewKind === 'image' ? (
          <Box
            component="img"
            src={previewUrl}
            alt={fileName}
            sx={{ width: '100%', height: '100%', objectFit: 'cover' }}
          />
        ) : null}

        {!loading && !error && previewKind === 'other' ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
            <InsertDriveFileOutlinedIcon sx={{ fontSize: 40, color: portalColors.textLight, mb: 1 }} />
            <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
              {label}
            </Typography>
          </Box>
        ) : null}

        <Box
          className="preview-overlay"
          sx={{
            position: 'absolute',
            inset: 0,
            bgcolor: 'rgba(15, 23, 42, 0.55)',
            opacity: 0,
            transition: 'opacity 0.2s ease',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Button
            variant="contained"
            startIcon={<VisibilityOutlinedIcon />}
            sx={portalPrimaryButtonSx}
            onClick={(event) => {
              event.stopPropagation()
              onView()
            }}
          >
            View
          </Button>
        </Box>
      </Box>

      <Box sx={{ p: 1.5 }}>
        <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }}>
          <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>{label}</Typography>
          {versionNumber && versionNumber > 1 ? (
            <Chip
              size="small"
              label={`V${versionNumber}`}
              sx={{
                height: 22,
                fontSize: '0.7rem',
                fontWeight: 700,
                bgcolor: '#eff6ff',
                color: '#1d4ed8',
              }}
            />
          ) : null}
        </Stack>
        <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
          {fileSizeBytes ? `${Math.round(fileSizeBytes / 1024)} KB` : '—'}
          {uploadedAt ? ` · ${new Date(uploadedAt).toLocaleString()}` : ''}
        </Typography>
        {onCompareVersions ? (
          <Button
            size="small"
            variant="outlined"
            sx={{ ...portalOutlinedButtonSx, mt: 1.25, textTransform: 'none' }}
            onClick={(event) => {
              event.stopPropagation()
              onCompareVersions()
            }}
          >
            Compare versions
          </Button>
        ) : null}
      </Box>
    </Box>
  )
}
