import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Tab,
  Tabs,
  TextField,
  Typography,
} from '@mui/material'
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner'
import { type ReactNode, useState } from 'react'
import { PortalQrCameraScanner } from './PortalQrCameraScanner'
import { portalPrimaryButtonSx } from './portalStyles'
import { type PortalQrScanPreset, validateQrForPreset } from './portalQrScanPresets'

export type { PortalQrScanPreset } from './portalQrScanPresets'
export {
  doctorDriverHandoffQrPreset,
  operatorClientTransportQrPreset,
} from './portalQrScanPresets'

type PortalQrScanDialogProps = {
  open: boolean
  busy: boolean
  preset: PortalQrScanPreset
  onClose: () => void
  /** Return true when the server accepted the scan. */
  onSubmit: (qrData: string) => Promise<boolean>
}

export function PortalQrScanDialog({
  open,
  busy,
  preset,
  onClose,
  onSubmit,
}: PortalQrScanDialogProps) {
  const [tab, setTab] = useState<'camera' | 'paste'>('camera')
  const [pasteValue, setPasteValue] = useState('')
  const [localError, setLocalError] = useState<string | null>(null)
  const [scannerKey, setScannerKey] = useState(0)
  const [pendingQr, setPendingQr] = useState<string | null>(null)

  const handleClose = () => {
    if (busy) return
    setLocalError(null)
    setPendingQr(null)
    onClose()
  }

  const submit = async (raw: string) => {
    const qrData = raw.trim()
    if (!qrData) return
    const typeError = validateQrForPreset(qrData, preset)
    if (typeError) {
      setLocalError(typeError)
      setScannerKey((k) => k + 1)
      return
    }
    if (preset.confirmBeforeSubmit) {
      setPendingQr(qrData)
      return
    }
    setLocalError(null)
    const ok = await onSubmit(qrData)
    if (ok) {
      setPasteValue('')
      onClose()
    } else {
      setLocalError(preset.failureMessage)
      setScannerKey((k) => k + 1)
    }
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
        <QrCodeScannerIcon color="primary" fontSize="small" />
        {preset.dialogTitle}
      </DialogTitle>
      <DialogContent>
        <Tabs
          value={tab}
          onChange={(_, value: 'camera' | 'paste') => {
            setTab(value)
            setLocalError(null)
          }}
          sx={{ mb: 2, borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Camera scan" value="camera" />
          <Tab label="Paste payload" value="paste" />
        </Tabs>

        {localError && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setLocalError(null)}>
            {localError}
          </Alert>
        )}

        {busy && (
          <StackRow>
            <CircularProgress size={20} />
            <Typography variant="body2">{preset.busyMessage}</Typography>
          </StackRow>
        )}

        {tab === 'camera' ? (
          <PortalQrCameraScanner
            key={scannerKey}
            active={open && tab === 'camera' && !busy}
            hint={preset.cameraHint}
            onDecoded={(text) => {
              if (!busy) void submit(text)
            }}
          />
        ) : (
          <Box>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
              {preset.pasteHelper}
            </Typography>
            <TextField
              label="QR payload"
              value={pasteValue}
              onChange={(e) => setPasteValue(e.target.value)}
              fullWidth
              multiline
              minRows={5}
              placeholder={preset.pastePlaceholder}
              disabled={busy}
            />
          </Box>
        )}
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={handleClose} disabled={busy}>
          Cancel
        </Button>
        {tab === 'paste' && (
          <Button
            variant="contained"
            sx={portalPrimaryButtonSx}
            onClick={() => void submit(pasteValue)}
            disabled={busy || !pasteValue.trim()}
          >
            {preset.submitLabel}
          </Button>
        )}
      </DialogActions>

      {preset.confirmBeforeSubmit && (
        <Dialog open={pendingQr != null} onClose={() => !busy && setPendingQr(null)} maxWidth="xs" fullWidth>
          <DialogTitle>{preset.confirmBeforeSubmit.title}</DialogTitle>
          <DialogContent>
            <DialogContentText>{preset.confirmBeforeSubmit.body}</DialogContentText>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => setPendingQr(null)} disabled={busy}>
              Cancel
            </Button>
            <Button
              variant="contained"
              sx={portalPrimaryButtonSx}
              disabled={busy || !pendingQr}
              onClick={() => {
                const qr = pendingQr
                if (!qr) return
                void (async () => {
                  setLocalError(null)
                  const ok = await onSubmit(qr)
                  if (ok) {
                    setPasteValue('')
                    setPendingQr(null)
                    onClose()
                  } else {
                    setLocalError(preset.failureMessage)
                    setPendingQr(null)
                    setScannerKey((k) => k + 1)
                  }
                })()
              }}
            >
              {preset.confirmBeforeSubmit.confirmLabel}
            </Button>
          </DialogActions>
        </Dialog>
      )}
    </Dialog>
  )
}

function StackRow({ children }: { children: ReactNode }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
      {children}
    </Box>
  )
}
