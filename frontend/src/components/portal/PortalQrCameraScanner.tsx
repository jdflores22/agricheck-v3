import { Alert, Box, Button, CircularProgress, Stack, Typography } from '@mui/material'
import CameraswitchOutlinedIcon from '@mui/icons-material/CameraswitchOutlined'
import { Html5Qrcode, Html5QrcodeScannerState, type CameraDevice } from 'html5-qrcode'
import { useCallback, useEffect, useId, useRef, useState } from 'react'

type PortalQrCameraScannerProps = {
  /** When false, camera is stopped and released. */
  active: boolean
  onDecoded: (text: string) => void
  /** Pause decoding after a successful read until `active` toggles or resume is called. */
  pauseAfterDecode?: boolean
  hint?: string
}

function pickDefaultCamera(cameras: CameraDevice[]): CameraDevice {
  const back = cameras.find((c) => /back|rear|environment/i.test(c.label))
  if (back) return back
  if (cameras.length > 1) return cameras[cameras.length - 1]
  return cameras[0]
}

export function PortalQrCameraScanner({
  active,
  onDecoded,
  pauseAfterDecode = true,
  hint,
}: PortalQrCameraScannerProps) {
  const reactId = useId().replace(/:/g, '')
  const elementId = `qr-reader-${reactId}`
  const scannerRef = useRef<Html5Qrcode | null>(null)
  const camerasRef = useRef<CameraDevice[]>([])
  const cameraIndexRef = useRef(0)
  const lastDecodeRef = useRef<{ text: string; at: number } | null>(null)
  const onDecodedRef = useRef(onDecoded)
  onDecodedRef.current = onDecoded

  const [starting, setStarting] = useState(false)
  const [cameraError, setCameraError] = useState<string | null>(null)
  const [session, setSession] = useState(0)

  const stopScanner = useCallback(async () => {
    const scanner = scannerRef.current
    scannerRef.current = null
    if (!scanner) return
    try {
      const state = scanner.getState()
      if (
        state === Html5QrcodeScannerState.SCANNING ||
        state === Html5QrcodeScannerState.PAUSED
      ) {
        await scanner.stop()
      }
      scanner.clear()
    } catch {
      // ignore teardown races
    }
  }, [])

  const startWithCamera = useCallback(
    async (deviceId: string) => {
      await stopScanner()
      setStarting(true)
      setCameraError(null)
      try {
        const html5 = new Html5Qrcode(elementId, { verbose: false })
        scannerRef.current = html5
        await html5.start(
          deviceId,
          {
            fps: 12,
            qrbox: (viewfinderWidth, viewfinderHeight) => {
              const minEdge = Math.min(viewfinderWidth, viewfinderHeight)
              const size = Math.floor(minEdge * 0.72)
              return { width: size, height: size }
            },
            disableFlip: false,
          },
          (decodedText) => {
            const now = Date.now()
            const last = lastDecodeRef.current
            if (last && last.text === decodedText && now - last.at < 1200) return
            lastDecodeRef.current = { text: decodedText, at: now }
            onDecodedRef.current(decodedText)
            if (pauseAfterDecode) {
              void html5.pause(true)
            }
          },
          () => {},
        )
      } catch (err) {
        setCameraError(
          err instanceof Error
            ? err.message
            : 'Could not open the camera. Allow camera access and use HTTPS (or localhost).',
        )
      } finally {
        setStarting(false)
      }
    },
    [elementId, pauseAfterDecode, stopScanner],
  )

  useEffect(() => {
    if (!active) {
      void stopScanner()
      return
    }

    let cancelled = false
    const boot = async () => {
      setStarting(true)
      setCameraError(null)
      try {
        const cameras = await Html5Qrcode.getCameras()
        if (cancelled) return
        if (cameras.length === 0) {
          setCameraError('No camera found on this device. Paste the QR payload below instead.')
          setStarting(false)
          return
        }
        camerasRef.current = cameras
        if (cameraIndexRef.current < 0 || cameraIndexRef.current >= cameras.length) {
          const preferred = pickDefaultCamera(cameras)
          cameraIndexRef.current = Math.max(0, cameras.findIndex((c) => c.id === preferred.id))
        }
        const device = cameras[cameraIndexRef.current] ?? pickDefaultCamera(cameras)
        await startWithCamera(device.id)
      } catch (err) {
        if (!cancelled) {
          setCameraError(
            err instanceof Error ? err.message : 'Camera permission denied or not available.',
          )
          setStarting(false)
        }
      }
    }

    void boot()
    return () => {
      cancelled = true
      void stopScanner()
    }
  }, [active, session, startWithCamera, stopScanner])

  const flipCamera = () => {
    const cameras = camerasRef.current
    if (cameras.length < 2) return
    cameraIndexRef.current = (cameraIndexRef.current + 1) % cameras.length
    setSession((s) => s + 1)
  }

  const resumeScanning = () => {
    const scanner = scannerRef.current
    if (!scanner) {
      setSession((s) => s + 1)
      return
    }
    try {
      if (scanner.getState() === Html5QrcodeScannerState.PAUSED) {
        scanner.resume()
      }
    } catch {
      setSession((s) => s + 1)
    }
  }

  return (
    <Stack spacing={1.5}>
      {hint && (
        <Typography variant="body2" color="text.secondary">
          {hint}
        </Typography>
      )}
      <Box
        sx={{
          position: 'relative',
          width: '100%',
          borderRadius: 2,
          overflow: 'hidden',
          bgcolor: '#0f172a',
          minHeight: 280,
          '& video': { borderRadius: 2 },
        }}
      >
        <Box id={elementId} sx={{ width: '100%' }} />
        {starting && (
          <Stack
            sx={{
              position: 'absolute',
              inset: 0,
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'rgba(15,23,42,0.55)',
            }}
          >
            <CircularProgress size={32} sx={{ color: 'common.white' }} />
          </Stack>
        )}
      </Box>
      <Stack direction="row" spacing={1} sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
        <Button
          size="small"
          startIcon={<CameraswitchOutlinedIcon />}
          onClick={flipCamera}
          disabled={!active || camerasRef.current.length < 2 || starting}
        >
          Flip camera
        </Button>
        <Button size="small" onClick={resumeScanning} disabled={!active || starting}>
          Scan again
        </Button>
      </Stack>
      {cameraError && <Alert severity="warning">{cameraError}</Alert>}
    </Stack>
  )
}
