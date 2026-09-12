import CloseIcon from '@mui/icons-material/Close'
import CompareArrowsIcon from '@mui/icons-material/CompareArrows'
import {
  Box,
  CircularProgress,
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  Typography,
} from '@mui/material'
import { useEffect, useState } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'
import {
  fetchAuthenticatedFile,
  isImageFile,
  isPdfFile,
  revokeAuthenticatedFileUrl,
  type AuthenticatedFile,
} from '../utils/authenticatedFile'

type CompareVersion = {
  versionNumber: number
  fileName: string
  filePath: string
  fileSizeBytes?: number
}

type DocumentVersionCompareModalProps = {
  open: boolean
  onClose: () => void
  title: string
  previous: CompareVersion
  current: CompareVersion
}

function VersionPreviewPanel({ label, version }: { label: string; version: CompareVersion }) {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [file, setFile] = useState<AuthenticatedFile | null>(null)

  useEffect(() => {
    let cancelled = false

    setLoading(true)
    setError(false)
    setFile(null)

    fetchAuthenticatedFile(version.filePath)
      .then((loaded) => {
        if (cancelled) {
          revokeAuthenticatedFileUrl(loaded.objectUrl)
          return
        }
        setFile(loaded)
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
    }
  }, [version.filePath, version.versionNumber])

  useEffect(
    () => () => {
      if (file) {
        revokeAuthenticatedFileUrl(file.objectUrl)
      }
    },
    [file],
  )

  const showPdf = file ? isPdfFile(file.contentType, version.fileName) : false
  const showImage = file ? isImageFile(file.contentType, version.fileName) : false

  return (
    <Box
      sx={{
        border: `1px solid ${portalColors.border}`,
        borderRadius: '0.75rem',
        overflow: 'hidden',
        bgcolor: portalColors.bgWhite,
        minHeight: 420,
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      <Box sx={{ px: 2, py: 1.25, borderBottom: `1px solid ${portalColors.border}`, bgcolor: portalColors.bgMuted }}>
        <Typography sx={{ fontWeight: 700, fontSize: '0.875rem' }}>{label}</Typography>
        <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
          V{version.versionNumber}
          {version.fileSizeBytes ? ` · ${Math.round(version.fileSizeBytes / 1024)} KB` : ''}
        </Typography>
      </Box>

      <Box sx={{ flex: 1, bgcolor: '#f5f5f4', position: 'relative' }}>
        {loading ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', minHeight: 360 }}>
            <CircularProgress size={28} />
          </Box>
        ) : null}

        {error ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', minHeight: 360, p: 2 }}>
            <Typography variant="body2" sx={{ color: portalColors.textMuted, textAlign: 'center' }}>
              Unable to load this version.
            </Typography>
          </Box>
        ) : null}

        {file && showPdf ? (
          <Box component="iframe" src={file.objectUrl} title={version.fileName} sx={{ width: '100%', height: '100%', minHeight: 360, border: 0 }} />
        ) : null}

        {file && showImage ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', minHeight: 360, p: 2 }}>
            <Box component="img" src={file.objectUrl} alt={version.fileName} sx={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
          </Box>
        ) : null}

        {file && !showPdf && !showImage ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', minHeight: 360, p: 2 }}>
            <Typography variant="body2" sx={{ color: portalColors.textMuted, textAlign: 'center' }}>
              Preview unavailable for this file type.
            </Typography>
          </Box>
        ) : null}
      </Box>
    </Box>
  )
}

export function DocumentVersionCompareModal({
  open,
  onClose,
  title,
  previous,
  current,
}: DocumentVersionCompareModalProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xl"
      fullWidth
      slotProps={{ paper: { sx: { height: { xs: '94vh', md: '90vh' } } } }}
    >
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1, pr: 1 }}>
        <CompareArrowsIcon sx={{ color: portalColors.primary }} />
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography sx={{ fontWeight: 600, fontSize: '1rem' }} noWrap>
            Compare Versions
          </Typography>
          <Typography variant="caption" sx={{ color: portalColors.textMuted }} noWrap>
            {title} · V{previous.versionNumber} vs V{current.versionNumber}
          </Typography>
        </Box>
        <IconButton onClick={onClose} aria-label="Close">
          <CloseIcon />
        </IconButton>
      </DialogTitle>

      <DialogContent sx={{ p: 2.5 }}>
        <Stack
          direction={{ xs: 'column', md: 'row' }}
          spacing={2}
          sx={{ height: '100%' }}
        >
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <VersionPreviewPanel label={`Previous Version (V${previous.versionNumber})`} version={previous} />
          </Box>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <VersionPreviewPanel label={`Current Version (V${current.versionNumber})`} version={current} />
          </Box>
        </Stack>
      </DialogContent>
    </Dialog>
  )
}
