/** JSON `type` field in signed AgriCheck QR payloads (must match backend codecs). */
export const QR_PAYLOAD_TYPE_TRANSPORT = 'agricheck_transport'
export const QR_PAYLOAD_TYPE_DELIVERY_HANDOFF = 'agricheck_delivery_handoff'

export type PortalQrScanRole = 'operator_client' | 'doctor_driver'

export type PortalQrScanPreset = {
  role: PortalQrScanRole
  dialogTitle: string
  /** When set, show a confirmation step before calling the API (doctor end-trip). */
  confirmBeforeSubmit?: {
    title: string
    body: string
    confirmLabel: string
  }
  cameraHint: string
  pasteHelper: string
  pastePlaceholder: string
  submitLabel: string
  busyMessage: string
  expectedPayloadType: string
  wrongTypeMessage: (scannedType: string | undefined) => string
  failureMessage: string
}

export const operatorClientTransportQrPreset: PortalQrScanPreset = {
  role: 'operator_client',
  dialogTitle: 'Scan client transport QR',
  cameraHint:
    'Point the camera at the transport QR from the client or agency (importer container tag). ' +
    'This claims the load for your fleet — not the driver’s warehouse handoff code.',
  pasteHelper: 'Paste the full JSON from a hardware scanner or the client portal transport tag.',
  pastePlaceholder: '{"type":"agricheck_transport",...}',
  submitLabel: 'Claim from QR',
  busyMessage: 'Claiming container from transport tag…',
  expectedPayloadType: QR_PAYLOAD_TYPE_TRANSPORT,
  wrongTypeMessage: (scannedType) => {
    if (scannedType === QR_PAYLOAD_TYPE_DELIVERY_HANDOFF) {
      return 'This is the driver’s AgriTrack warehouse QR. Operators must scan the client transport tag only.'
    }
    return 'This QR is not a client transport tag. Open the transport QR from the importer/agency portal.'
  },
  failureMessage:
    'Could not claim from this QR — invalid, expired, already claimed, or container not awaiting operator confirmation.',
}

export const doctorDriverHandoffQrPreset: PortalQrScanPreset = {
  role: 'doctor_driver',
  dialogTitle: 'Scan driver delivery QR',
  confirmBeforeSubmit: {
    title: 'End driver trip?',
    body:
      'Confirm that the driver is physically at the assigned warehouse. This ends their AgriTrack transit (navigation and GPS trail) and moves the container to Under Inspection for NMIS border inspection.',
    confirmLabel: 'Yes — end trip & start inspection',
  },
  cameraHint:
    'Point the camera at the driver’s live AgriTrack delivery QR on their phone (warehouse handoff screen). ' +
    'The code refreshes about every 90 seconds — keep it in frame until accepted.',
  pasteHelper: 'Paste the full JSON payload if camera scan is not available.',
  pastePlaceholder: '{"type":"agricheck_delivery_handoff",...}',
  submitLabel: 'Accept handoff',
  busyMessage: 'Verifying driver handoff with server…',
  expectedPayloadType: QR_PAYLOAD_TYPE_DELIVERY_HANDOFF,
  wrongTypeMessage: (scannedType) => {
    if (scannedType === QR_PAYLOAD_TYPE_TRANSPORT) {
      return 'This is the client transport tag. Doctors must scan the driver’s live AgriTrack delivery QR only.'
    }
    return 'This QR is not a driver delivery handoff code. Ask the driver to open the warehouse QR in AgriTrack.'
  },
  failureMessage:
    'Handoff QR rejected — expired, already used, wrong warehouse, compliance hold, or driver not at assigned warehouse.',
}

export function readQrPayloadType(qrData: string): string | undefined {
  try {
    const parsed = JSON.parse(qrData.trim()) as { type?: string }
    return typeof parsed.type === 'string' ? parsed.type : undefined
  } catch {
    return undefined
  }
}

export function validateQrForPreset(qrData: string, preset: PortalQrScanPreset): string | null {
  const trimmed = qrData.trim()
  if (!trimmed) return 'QR payload is empty.'
  const type = readQrPayloadType(trimmed)
  if (!type) {
    return 'QR payload is not valid JSON. Scan again or paste the full payload.'
  }
  if (type !== preset.expectedPayloadType) {
    return preset.wrongTypeMessage(type)
  }
  return null
}
