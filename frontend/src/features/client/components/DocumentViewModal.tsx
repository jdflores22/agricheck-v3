import CloseIcon from '@mui/icons-material/Close'
import OpenInNewIcon from '@mui/icons-material/OpenInNew'
import PictureAsPdfOutlinedIcon from '@mui/icons-material/PictureAsPdfOutlined'
import {
  Box,
  Button,
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
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  downloadAuthenticatedFile,
  fetchAuthenticatedFile,
  isImageFile,
  isPdfFile,
  revokeAuthenticatedFileUrl,
  type AuthenticatedFile,
} from '../utils/authenticatedFile'

type DocumentViewModalProps = {
  open: boolean
  onClose: () => void
  title: string
  fileName: string
  filePath: string
  previewKey?: string
}

export function DocumentViewModal({ open, onClose, title, fileName, filePath, previewKey }: DocumentViewModalProps) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(false)
  const [file, setFile] = useState<AuthenticatedFile | null>(null)

  useEffect(() => {
    if (!open || !filePath) {
      return undefined
    }

    let cancelled = false
    setLoading(true)
    setError(false)
    setFile(null)

    fetchAuthenticatedFile(filePath)
      .then((loaded) => {
        if (!cancelled) {
          setFile(loaded)
          setLoading(false)
        } else {
          revokeAuthenticatedFileUrl(loaded.objectUrl)
        }
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
  }, [open, filePath, previewKey])

  useEffect(() => {
    if (open) return undefined
    if (file) {
      revokeAuthenticatedFileUrl(file.objectUrl)
      setFile(null)
    }
    return undefined
  }, [open, file])

  const showPdf = file ? isPdfFile(file.contentType, fileName) : false
  const showImage = file ? isImageFile(file.contentType, fileName) : false

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="lg"
      fullWidth
      slotProps={{ paper: { sx: { height: { xs: '92vh', md: '88vh' } } } }}
    >
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1, pr: 1 }}>
        <PictureAsPdfOutlinedIcon sx={{ color: portalColors.primary }} />
        <Box sx={{ flex: 1, minWidth: 0 }}>
          <Typography sx={{ fontWeight: 600, fontSize: '1rem' }} noWrap>
            {title}
          </Typography>
          <Typography variant="caption" sx={{ color: portalColors.textMuted }} noWrap>
            {fileName}
          </Typography>
        </Box>
        <Stack direction="row" spacing={0.5} sx={{ alignItems: 'center' }}>
          {file ? (
            <>
              <Button
                size="small"
                variant="outlined"
                sx={portalOutlinedButtonSx}
                onClick={() => window.open(file.objectUrl, '_blank', 'noopener,noreferrer')}
                startIcon={<OpenInNewIcon />}
              >
                Open
              </Button>
              <Button
                size="small"
                variant="contained"
                sx={portalPrimaryButtonSx}
                onClick={() => downloadAuthenticatedFile(filePath, fileName)}
              >
                Download
              </Button>
            </>
          ) : null}
          <IconButton onClick={onClose} aria-label="Close">
            <CloseIcon />
          </IconButton>
        </Stack>
      </DialogTitle>

      <DialogContent sx={{ p: 0, bgcolor: '#f5f5f4', position: 'relative' }}>
        {loading ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', minHeight: 320 }}>
            <Stack spacing={1.5} sx={{ alignItems: 'center' }}>
              <CircularProgress size={32} />
              <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
                Loading document…
              </Typography>
            </Stack>
          </Box>
        ) : null}

        {error ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', minHeight: 320, p: 3 }}>
            <Typography sx={{ color: portalColors.textMuted }}>Unable to load this document.</Typography>
          </Box>
        ) : null}

        {file && showPdf ? (
          <Box component="iframe" src={file.objectUrl} title={fileName} sx={{ width: '100%', height: '100%', minHeight: 480, border: 0 }} />
        ) : null}

        {file && showImage ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', minHeight: 320, p: 2 }}>
            <Box component="img" src={file.objectUrl} alt={fileName} sx={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
          </Box>
        ) : null}

        {file && !showPdf && !showImage ? (
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%', minHeight: 320, p: 3 }}>
            <Stack spacing={1.5} sx={{ alignItems: 'center' }}>
              <Typography sx={{ color: portalColors.textMuted }}>Preview is not available for this file type.</Typography>
              <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => downloadAuthenticatedFile(filePath, fileName)}>
                Download File
              </Button>
            </Stack>
          </Box>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
