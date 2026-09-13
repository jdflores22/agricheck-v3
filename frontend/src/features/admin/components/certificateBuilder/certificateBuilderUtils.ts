import { getApiOrigin } from '../../../../app/apiBase'
import type { CertificateElement } from '../../api/adminApi'

export const MM_TO_PX = 3.7795

export type BuilderElementType = 'TEXT' | 'IMAGE' | 'QR_CODE' | 'SHAPE' | 'LINE'

export interface CertificateLayoutConfig {
  paperSize: string
  orientation: string
  marginTop: number
  marginRight: number
  marginBottom: number
  marginLeft: number
  backgroundColor: string
  backgroundImage?: string | null
  width?: number | null
  height?: number | null
}

export interface BuilderElement {
  clientId: string
  elementType: BuilderElementType
  content: string
  x: number
  y: number
  width: number
  height: number
  fontFamily: string
  fontSize: number
  fontWeight: string
  fontStyle: string
  textAlign: string
  textColor: string
  rotation: number
  backgroundColor: string
  borderWidth: number
  borderColor: string
  borderRadius: number
  imagePath?: string | null
  zIndex: number
  displayOrder: number
  qrLogoPath?: string | null
  qrLogoSize?: number
  qrForegroundColor?: string
  qrBackgroundColor?: string
  qrStyle?: string
}

export const PAPER_SIZES: Record<string, { width: number; height: number }> = {
  A4: { width: 210, height: 297 },
  LETTER: { width: 216, height: 279 },
  LEGAL: { width: 216, height: 356 },
}

export const DEFAULT_LAYOUT: CertificateLayoutConfig = {
  paperSize: 'A4',
  orientation: 'PORTRAIT',
  marginTop: 10,
  marginRight: 10,
  marginBottom: 10,
  marginLeft: 10,
  backgroundColor: '#ffffff',
}

export const PALETTE_ITEMS: Array<{ type: BuilderElementType; label: string; description: string }> = [
  { type: 'TEXT', label: 'Text', description: 'Text label or content' },
  { type: 'IMAGE', label: 'Image', description: 'Logo or graphic' },
  { type: 'QR_CODE', label: 'QR Code', description: 'Verification QR code' },
  { type: 'SHAPE', label: 'Shape', description: 'Rectangle or border' },
  { type: 'LINE', label: 'Line', description: 'Horizontal or vertical line' },
]

export const FONT_FAMILIES: Array<{ value: string; label: string }> = [
  { value: 'helvetica', label: 'Helvetica' },
  { value: 'times', label: 'Times New Roman' },
  { value: 'courier', label: 'Courier' },
  { value: 'dejavusans', label: 'DejaVu Sans' },
]

export const TEMPLATE_VARIABLES: Record<string, Array<{ name: string; description: string }>> = {
  Accreditation: [
    { name: '{{ certificate.number }}', description: 'Certificate number' },
    { name: '{{ company.name }}', description: 'Company name' },
    { name: '{{ company.address }}', description: 'Company address' },
    { name: '{{ company.tin }}', description: 'Company TIN' },
    { name: '{{ company.business_type }}', description: 'Type of business' },
    { name: '{{ company.nature_of_business }}', description: 'Nature of business / primary activity' },
    { name: '{{ accreditation.type }}', description: 'Accreditation type' },
    { name: '{{ accreditation.number }}', description: 'Accreditation number' },
    { name: '{{ date.issued }}', description: 'Issue date' },
    { name: '{{ date.expires }}', description: 'Expiration date' },
    { name: '{{ officer.name }}', description: 'Officer name' },
    { name: '{{ officer.title }}', description: 'Officer title' },
  ],
  ImportEntry: [
    { name: '{{ certificate.number }}', description: 'Certificate number' },
    { name: '{{ company.name }}', description: 'Company / importer name' },
    { name: '{{ company.address }}', description: 'Company address' },
    { name: '{{ company.tin }}', description: 'Company TIN' },
    { name: '{{ entry.reference }}', description: 'Entry reference number' },
    { name: '{{ entry.commodity }}', description: 'Commodity name' },
    { name: '{{ entry.quantity }}', description: 'Quantity' },
    { name: '{{ entry.unit }}', description: 'Unit of measure' },
    { name: '{{ entry.hs_code }}', description: 'HS code' },
    { name: '{{ entry.submitted_at }}', description: 'Entry submission date' },
    { name: '{{ agency.name }}', description: 'Agency name' },
    { name: '{{ agency.code }}', description: 'Agency code' },
    { name: '{{ billing.number }}', description: 'Billing number' },
    { name: '{{ billing.total_amount }}', description: 'Billing amount' },
    { name: '{{ billing.paid_at }}', description: 'Payment date' },
    { name: '{{ officer.name }}', description: 'Officer name' },
    { name: '{{ officer.title }}', description: 'Officer title' },
    { name: '{{ date.issued }}', description: 'Issue date' },
    { name: '{{ date.expires }}', description: 'Expiration date' },
  ],
  ExportEntry: [
    { name: '{{ certificate.number }}', description: 'Certificate number' },
    { name: '{{ company.name }}', description: 'Company / exporter name' },
    { name: '{{ company.address }}', description: 'Company address' },
    { name: '{{ company.tin }}', description: 'Company TIN' },
    { name: '{{ entry.reference }}', description: 'Entry reference number' },
    { name: '{{ entry.commodity }}', description: 'Commodity name' },
    { name: '{{ entry.quantity }}', description: 'Quantity' },
    { name: '{{ entry.unit }}', description: 'Unit of measure' },
    { name: '{{ entry.hs_code }}', description: 'HS code' },
    { name: '{{ entry.submitted_at }}', description: 'Entry submission date' },
    { name: '{{ agency.name }}', description: 'Agency name' },
    { name: '{{ agency.code }}', description: 'Agency code' },
    { name: '{{ billing.number }}', description: 'Billing number' },
    { name: '{{ billing.total_amount }}', description: 'Billing amount' },
    { name: '{{ billing.paid_at }}', description: 'Payment date' },
    { name: '{{ officer.name }}', description: 'Officer name' },
    { name: '{{ officer.title }}', description: 'Officer title' },
    { name: '{{ date.issued }}', description: 'Issue date' },
    { name: '{{ date.expires }}', description: 'Expiration date' },
  ],
}

export function parseLayout(layoutJson?: string | null): CertificateLayoutConfig {
  if (!layoutJson) return { ...DEFAULT_LAYOUT }
  try {
    const parsed = JSON.parse(layoutJson) as Partial<CertificateLayoutConfig>
    return { ...DEFAULT_LAYOUT, ...parsed }
  } catch {
    return { ...DEFAULT_LAYOUT }
  }
}

export function getPaperDimensions(layout: CertificateLayoutConfig) {
  if (layout.paperSize === 'CUSTOM' && layout.width && layout.height) {
    return { width: layout.width, height: layout.height }
  }
  const size = PAPER_SIZES[layout.paperSize] ?? PAPER_SIZES.A4
  if (layout.orientation.toUpperCase() === 'LANDSCAPE') {
    return { width: size.height, height: size.width }
  }
  return size
}

export function resolveCertificateAssetUrl(imagePath?: string | null) {
  if (!imagePath) return undefined
  if (imagePath.startsWith('http') || imagePath.startsWith('data:')) return imagePath
  const normalized = imagePath.replace(/^certificate-templates\//, '')
  return `${getApiOrigin()}/uploads/certificates/${normalized}`
}

function normalizeElementType(type: string): BuilderElementType {
  const upper = type.replace(/([a-z])([A-Z])/g, '$1_$2').toUpperCase()
  if (upper === 'QR_CODE' || upper === 'QRCODE') return 'QR_CODE'
  if (upper === 'TEXT' || upper === 'FIELD') return 'TEXT'
  if (upper === 'IMAGE') return 'IMAGE'
  if (upper === 'SHAPE') return 'SHAPE'
  if (upper === 'LINE') return 'LINE'
  return 'TEXT'
}

function toApiElementType(type: BuilderElementType): string {
  switch (type) {
    case 'TEXT':
      return 'Text'
    case 'IMAGE':
      return 'Image'
    case 'QR_CODE':
      return 'QrCode'
    case 'SHAPE':
      return 'Shape'
    case 'LINE':
      return 'Line'
    default:
      return 'Text'
  }
}

export function apiElementToBuilder(element: CertificateElement): BuilderElement {
  let config: Record<string, unknown> = {}
  if (element.configJson) {
    try {
      config = JSON.parse(element.configJson) as Record<string, unknown>
    } catch {
      config = {}
    }
  }

  return {
    clientId: String(element.id),
    elementType: normalizeElementType(element.elementType),
    content: String(config.content ?? element.label ?? ''),
    x: Number(config.x ?? 0),
    y: Number(config.y ?? 0),
    width: Number(config.width ?? 50),
    height: Number(config.height ?? 20),
    fontFamily: String(config.fontFamily ?? 'helvetica'),
    fontSize: Number(config.fontSize ?? 12),
    fontWeight: String(config.fontWeight ?? 'normal'),
    fontStyle: String(config.fontStyle ?? 'normal'),
    textAlign: String(config.textAlign ?? 'left'),
    textColor: String(config.textColor ?? '#000000'),
    rotation: Number(config.rotation ?? 0),
    backgroundColor: String(config.backgroundColor ?? 'transparent'),
    borderWidth: Number(config.borderWidth ?? 0),
    borderColor: String(config.borderColor ?? '#000000'),
    borderRadius: Number(config.borderRadius ?? 0),
    imagePath: (config.imagePath as string | null | undefined) ?? null,
    zIndex: Number(config.zIndex ?? element.sortOrder),
    displayOrder: Number(config.displayOrder ?? element.sortOrder),
    qrLogoPath: (config.qrLogoPath as string | null | undefined) ?? null,
    qrLogoSize: Number(config.qrLogoSize ?? 20),
    qrForegroundColor: String(config.qrForegroundColor ?? '#000000'),
    qrBackgroundColor: String(config.qrBackgroundColor ?? '#FFFFFF'),
    qrStyle: String(config.qrStyle ?? 'square'),
  }
}

function buildLabel(element: BuilderElement) {
  const trimmed = element.content.trim()
  if (trimmed) return trimmed.length <= 64 ? trimmed : `${trimmed.slice(0, 61)}...`
  return element.elementType === 'QR_CODE' ? 'Verification QR' : element.elementType
}

export function builderElementToApiInput(element: BuilderElement, sortOrder: number) {
  return {
    elementType: toApiElementType(element.elementType),
    label: buildLabel(element),
    sortOrder,
    configJson: JSON.stringify({
      content: element.content,
      x: element.x,
      y: element.y,
      width: element.width,
      height: element.height,
      fontFamily: element.fontFamily,
      fontSize: element.fontSize,
      fontWeight: element.fontWeight,
      fontStyle: element.fontStyle,
      textAlign: element.textAlign,
      textColor: element.textColor,
      rotation: element.rotation,
      backgroundColor: element.backgroundColor,
      borderWidth: element.borderWidth,
      borderColor: element.borderColor,
      borderRadius: element.borderRadius,
      imagePath: element.imagePath,
      zIndex: element.zIndex,
      displayOrder: sortOrder,
      qrLogoPath: element.qrLogoPath,
      qrLogoSize: element.qrLogoSize,
      qrForegroundColor: element.qrForegroundColor,
      qrBackgroundColor: element.qrBackgroundColor,
      qrStyle: element.qrStyle,
    }),
  }
}

export function createDefaultElement(type: BuilderElementType, x: number, y: number, order: number): BuilderElement {
  return {
    clientId: `new_${Date.now()}_${order}`,
    elementType: type,
    content: type === 'TEXT' ? 'New Text' : '',
    x: Math.round(x * 10) / 10,
    y: Math.round(y * 10) / 10,
    width: type === 'QR_CODE' ? 40 : type === 'LINE' ? 80 : 50,
    height: type === 'LINE' ? 1 : type === 'QR_CODE' ? 40 : 20,
    fontFamily: 'helvetica',
    fontSize: 12,
    fontWeight: 'normal',
    fontStyle: 'normal',
    textAlign: 'left',
    textColor: '#000000',
    rotation: 0,
    backgroundColor: type === 'SHAPE' ? '#d1d5db' : 'transparent',
    borderWidth: type === 'LINE' ? 1 : 0,
    borderColor: '#000000',
    borderRadius: 0,
    imagePath: null,
    zIndex: order,
    displayOrder: order,
    qrLogoSize: 20,
    qrForegroundColor: '#000000',
    qrBackgroundColor: '#FFFFFF',
    qrStyle: 'square',
  }
}

export function colorInputValue(value: string | undefined, fallback = '#000000') {
  return value && value !== 'transparent' && /^#[0-9a-f]{3,8}$/i.test(value) ? value : fallback
}

export function resolveFontFamily(fontFamily: string) {
  switch (fontFamily.toLowerCase()) {
    case 'times':
      return '"Times New Roman", Times, serif'
    case 'courier':
      return 'Courier, monospace'
    case 'dejavusans':
      return '"DejaVu Sans", sans-serif'
    default:
      return 'Helvetica, Arial, sans-serif'
  }
}
