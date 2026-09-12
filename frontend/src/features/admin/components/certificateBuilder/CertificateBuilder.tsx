import CropFreeIcon from '@mui/icons-material/CropFree'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import GridOnIcon from '@mui/icons-material/GridOn'
import ImageIcon from '@mui/icons-material/Image'
import MinimizeIcon from '@mui/icons-material/Minimize'
import QrCode2Icon from '@mui/icons-material/QrCode2'
import RedoIcon from '@mui/icons-material/Redo'
import SquareIcon from '@mui/icons-material/Square'
import TextFieldsIcon from '@mui/icons-material/TextFields'
import UndoIcon from '@mui/icons-material/Undo'
import VisibilityIcon from '@mui/icons-material/Visibility'
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogContent,
  DialogTitle,
  Divider,
  FormControl,
  FormControlLabel,
  InputLabel,
  List,
  ListItemButton,
  ListItemText,
  MenuItem,
  Select,
  Slider,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { DragEvent, MouseEvent as ReactMouseEvent, useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { portalColors } from '../../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../../components/portal/portalStyles'
import type { CertificateTemplateDetail } from '../../api/adminApi'
import {
  BuilderElement,
  BuilderElementType,
  CertificateLayoutConfig,
  FONT_FAMILIES,
  MM_TO_PX,
  PALETTE_ITEMS,
  TEMPLATE_VARIABLES,
  apiElementToBuilder,
  builderElementToApiInput,
  colorInputValue,
  createDefaultElement,
  getPaperDimensions,
  parseLayout,
  resolveCertificateAssetUrl,
  resolveFontFamily,
} from './certificateBuilderUtils'

const paletteIcons: Record<BuilderElementType, typeof TextFieldsIcon> = {
  TEXT: TextFieldsIcon,
  IMAGE: ImageIcon,
  QR_CODE: QrCode2Icon,
  SHAPE: SquareIcon,
  LINE: MinimizeIcon,
}

interface CertificateBuilderProps {
  template: CertificateTemplateDetail
  saving: boolean
  onSave: (payload: {
    elements: ReturnType<typeof builderElementToApiInput>[]
    layoutJson: string
  }) => Promise<void>
  onPreview: () => Promise<void>
  onUploadImage: (file: File) => Promise<string>
}

export function CertificateBuilder({ template, saving, onSave, onPreview, onUploadImage }: CertificateBuilderProps) {
  const [layout, setLayout] = useState<CertificateLayoutConfig>(() => parseLayout(template.layoutJson))
  const [elements, setElements] = useState<BuilderElement[]>(() => template.elements.map(apiElementToBuilder))
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [zoom, setZoom] = useState(100)
  const [gridEnabled, setGridEnabled] = useState(false)
  const [variableOpen, setVariableOpen] = useState(false)
  const [history, setHistory] = useState<BuilderElement[][]>([])
  const [historyIndex, setHistoryIndex] = useState(-1)
  const canvasRef = useRef<HTMLDivElement | null>(null)
  const dragRef = useRef<{ id: string; startX: number; startY: number; originX: number; originY: number; moved: boolean } | null>(null)

  const selected = useMemo(() => elements.find((el) => el.clientId === selectedId) ?? null, [elements, selectedId])
  const paper = useMemo(() => getPaperDimensions(layout), [layout])
  const scale = zoom / 100
  const canvasWidth = paper.width * MM_TO_PX * scale
  const canvasHeight = paper.height * MM_TO_PX * scale

  const primaryProcess = template.processTypes[0] ?? 'Accreditation'
  const variables = TEMPLATE_VARIABLES[primaryProcess] ?? TEMPLATE_VARIABLES.Accreditation

  const pushHistory = useCallback((next: BuilderElement[]) => {
    setHistory((prev) => {
      const trimmed = prev.slice(0, historyIndex + 1)
      const updated = [...trimmed, structuredClone(next)].slice(-50)
      setHistoryIndex(updated.length - 1)
      return updated
    })
  }, [historyIndex])

  useEffect(() => {
    setLayout(parseLayout(template.layoutJson))
    const loaded = template.elements.map(apiElementToBuilder)
    setElements(loaded)
    setHistory([structuredClone(loaded)])
    setHistoryIndex(0)
  }, [template.uuid, template.layoutJson, template.elements])

  const updateElements = (updater: (current: BuilderElement[]) => BuilderElement[]) => {
    setElements((current) => {
      const next = updater(current)
      pushHistory(next)
      return next
    })
  }

  const updateSelected = (patch: Partial<BuilderElement>, trackHistory = true) => {
    if (!selectedId) return
    if (trackHistory) {
      updateElements((current) => current.map((el) => (el.clientId === selectedId ? { ...el, ...patch } : el)))
      return
    }
    setElements((current) => current.map((el) => (el.clientId === selectedId ? { ...el, ...patch } : el)))
  }

  const handleUndo = () => {
    if (historyIndex <= 0) return
    const nextIndex = historyIndex - 1
    setHistoryIndex(nextIndex)
    setElements(structuredClone(history[nextIndex]))
  }

  const handleRedo = () => {
    if (historyIndex >= history.length - 1) return
    const nextIndex = historyIndex + 1
    setHistoryIndex(nextIndex)
    setElements(structuredClone(history[nextIndex]))
  }

  const handleDeleteSelected = () => {
    if (!selectedId) return
    updateElements((current) => current.filter((el) => el.clientId !== selectedId))
    setSelectedId(null)
  }

  const handlePaletteDragStart = (event: DragEvent, type: BuilderElementType) => {
    event.dataTransfer.setData('elementType', type)
    event.dataTransfer.effectAllowed = 'copy'
  }

  const handleCanvasDrop = (event: DragEvent) => {
    event.preventDefault()
    const type = event.dataTransfer.getData('elementType') as BuilderElementType
    if (!type || !canvasRef.current) return
    const rect = canvasRef.current.getBoundingClientRect()
    const xPx = event.clientX - rect.left
    const yPx = event.clientY - rect.top
    const xMm = (xPx / scale) / MM_TO_PX
    const yMm = (yPx / scale) / MM_TO_PX
    const created = createDefaultElement(type, xMm, yMm, elements.length + 1)
    updateElements((current) => [...current, created])
    setSelectedId(created.clientId)
  }

  const handleElementMouseDown = (event: ReactMouseEvent, element: BuilderElement) => {
    event.stopPropagation()
    setSelectedId(element.clientId)
    dragRef.current = {
      id: element.clientId,
      startX: event.clientX,
      startY: event.clientY,
      originX: element.x,
      originY: element.y,
      moved: false,
    }
  }

  const handleElementClick = (event: ReactMouseEvent) => {
    event.stopPropagation()
  }

  const handleCanvasClick = (event: ReactMouseEvent<HTMLDivElement>) => {
    if (event.target === event.currentTarget) {
      setSelectedId(null)
    }
  }

  useEffect(() => {
    const onMove = (event: MouseEvent) => {
      if (!dragRef.current) return
      const deltaX = (event.clientX - dragRef.current.startX) / scale / MM_TO_PX
      const deltaY = (event.clientY - dragRef.current.startY) / scale / MM_TO_PX
      if (Math.abs(deltaX) > 0.2 || Math.abs(deltaY) > 0.2) {
        dragRef.current.moved = true
      }
      setElements((current) =>
        current.map((el) =>
          el.clientId === dragRef.current!.id
            ? {
                ...el,
                x: Math.round((dragRef.current!.originX + deltaX) * 10) / 10,
                y: Math.round((dragRef.current!.originY + deltaY) * 10) / 10,
              }
            : el,
        ),
      )
    }
    const onUp = () => {
      if (!dragRef.current) return
      if (dragRef.current.moved) {
        setElements((current) => {
          pushHistory(current)
          return current
        })
      }
      dragRef.current = null
    }
    window.addEventListener('mousemove', onMove)
    window.addEventListener('mouseup', onUp)
    return () => {
      window.removeEventListener('mousemove', onMove)
      window.removeEventListener('mouseup', onUp)
    }
  }, [pushHistory, scale])

  const handleSave = async () => {
    const sorted = [...elements].sort((a, b) => a.zIndex - b.zIndex || a.displayOrder - b.displayOrder)
    await onSave({
      layoutJson: JSON.stringify(layout),
      elements: sorted.map((element, index) => builderElementToApiInput({ ...element, displayOrder: index + 1 }, index + 1)),
    })
  }

  const sortedElements = useMemo(
    () => [...elements].sort((a, b) => a.zIndex - b.zIndex || a.displayOrder - b.displayOrder),
    [elements],
  )

  return (
    <Box>
      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap', alignItems: 'center' }}>
        <Button component={RouterLink} to={`/admin/certificate-templates/${template.uuid}`} variant="outlined" sx={portalOutlinedButtonSx}>
          View Details
        </Button>
        <Button variant="outlined" sx={portalOutlinedButtonSx} startIcon={<UndoIcon />} onClick={handleUndo} disabled={historyIndex <= 0}>
          Undo
        </Button>
        <Button variant="outlined" sx={portalOutlinedButtonSx} startIcon={<RedoIcon />} onClick={handleRedo} disabled={historyIndex >= history.length - 1}>
          Redo
        </Button>
        {[50, 75, 100, 150, 200].map((value) => (
          <Button
            key={value}
            variant={zoom === value ? 'contained' : 'outlined'}
            sx={zoom === value ? portalPrimaryButtonSx : portalOutlinedButtonSx}
            onClick={() => setZoom(value)}
          >
            {value}%
          </Button>
        ))}
        <Button variant={gridEnabled ? 'contained' : 'outlined'} sx={gridEnabled ? portalPrimaryButtonSx : portalOutlinedButtonSx} startIcon={<GridOnIcon />} onClick={() => setGridEnabled((v) => !v)}>
          Grid
        </Button>
        <Button variant="outlined" sx={portalOutlinedButtonSx} startIcon={<VisibilityIcon />} onClick={() => void onPreview()}>
          Preview
        </Button>
        <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => void handleSave()} disabled={saving}>
          Save
        </Button>
      </Stack>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', xl: '250px minmax(0, 1fr) 340px' },
          gap: 2,
          alignItems: 'start',
        }}
      >
        <Box sx={{ border: `1px solid ${portalColors.border}`, borderRadius: 2, overflow: 'hidden', bgcolor: '#fff' }}>
          <Box sx={{ px: 2, py: 1.5, bgcolor: portalColors.primary, color: '#fff', fontWeight: 600 }}>Elements</Box>
          <Stack spacing={1} sx={{ p: 1.5 }}>
            {PALETTE_ITEMS.map((item) => {
              const Icon = paletteIcons[item.type]
              return (
                <Box
                  key={item.type}
                  draggable
                  onDragStart={(event) => handlePaletteDragStart(event, item.type)}
                  sx={{
                    p: 1.5,
                    border: `1px solid ${portalColors.border}`,
                    borderRadius: 1.5,
                    cursor: 'grab',
                    '&:hover': { borderColor: portalColors.primary, bgcolor: '#f0fdf4' },
                  }}
                >
                  <Stack direction="row" spacing={1.5} sx={{ alignItems: 'center' }}>
                    <Icon sx={{ color: portalColors.primary }} />
                    <Box>
                      <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>{item.label}</Typography>
                      <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{item.description}</Typography>
                    </Box>
                  </Stack>
                </Box>
              )
            })}
          </Stack>
        </Box>

        <Box sx={{ border: `1px solid ${portalColors.border}`, borderRadius: 2, overflow: 'hidden', bgcolor: '#fff' }}>
          <Stack direction="row" sx={{ px: 2, py: 1.5, bgcolor: portalColors.primary, color: '#fff', alignItems: 'center', justifyContent: 'space-between' }}>
            <Typography sx={{ fontWeight: 600 }}>Certificate Canvas</Typography>
            <Chip size="small" label={`${elements.length} elements`} sx={{ bgcolor: 'rgba(255,255,255,0.15)', color: '#fff' }} />
          </Stack>
          <Box sx={{ p: 2, bgcolor: '#f5f5f5', overflow: 'auto' }}>
            <Box
              ref={canvasRef}
              onDragOver={(event) => event.preventDefault()}
              onDrop={handleCanvasDrop}
              onClick={handleCanvasClick}
              sx={{
                position: 'relative',
                width: canvasWidth,
                height: canvasHeight,
                mx: 'auto',
                overflow: 'visible',
                bgcolor: layout.backgroundColor,
                boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
                backgroundImage: layout.backgroundImage ? `url(${resolveCertificateAssetUrl(layout.backgroundImage)})` : undefined,
                backgroundSize: 'cover',
                backgroundPosition: 'center',
                ...(gridEnabled
                  ? {
                      backgroundImage: `linear-gradient(rgba(0,0,0,0.05) 1px, transparent 1px), linear-gradient(90deg, rgba(0,0,0,0.05) 1px, transparent 1px), ${layout.backgroundImage ? `url(${resolveCertificateAssetUrl(layout.backgroundImage)})` : 'none'}`,
                      backgroundSize: `${10 * MM_TO_PX * scale}px ${10 * MM_TO_PX * scale}px, ${10 * MM_TO_PX * scale}px ${10 * MM_TO_PX * scale}px, cover`,
                    }
                  : {}),
                '&::before, &::after': {
                  content: '""',
                  position: 'absolute',
                  background: 'rgba(59,130,246,0.25)',
                  pointerEvents: 'none',
                  zIndex: 9999,
                },
                '&::before': { left: '50%', top: 0, width: '1px', height: '100%' },
                '&::after': { left: 0, top: '50%', width: '100%', height: '1px' },
              }}
            >
              {sortedElements.map((element) => {
                const isSelected = element.clientId === selectedId
                const left = element.x * MM_TO_PX * scale
                const top = element.y * MM_TO_PX * scale
                const width = element.width * MM_TO_PX * scale
                const height = element.height * MM_TO_PX * scale
                const imageUrl = resolveCertificateAssetUrl(element.imagePath)

                return (
                  <Box
                    key={element.clientId}
                    onMouseDown={(event) => handleElementMouseDown(event, element)}
                    onClick={handleElementClick}
                    sx={{
                      position: 'absolute',
                      left,
                      top,
                      width,
                      height,
                      zIndex: element.zIndex,
                      outline: isSelected ? `2px solid ${portalColors.primary}` : 'none',
                      outlineOffset: '1px',
                      boxShadow: isSelected ? `0 0 0 2px rgba(22, 163, 74, 0.2)` : 'none',
                      cursor: 'move',
                      overflow: element.rotation ? 'visible' : 'hidden',
                      transform: element.rotation ? `rotate(${element.rotation}deg)` : undefined,
                      transformOrigin: 'center center',
                      bgcolor:
                        element.elementType === 'SHAPE'
                          ? element.backgroundColor !== 'transparent'
                            ? element.backgroundColor
                            : '#d1d5db'
                          : element.backgroundColor !== 'transparent'
                            ? element.backgroundColor
                            : undefined,
                      borderRadius: element.borderRadius ? `${element.borderRadius}px` : undefined,
                      border: element.borderWidth ? `${element.borderWidth}px solid ${element.borderColor}` : undefined,
                    }}
                  >
                    {element.elementType === 'TEXT' ? (
                      <Box
                        sx={{
                          width: '100%',
                          height: '100%',
                          fontFamily: resolveFontFamily(element.fontFamily),
                          fontSize: `${element.fontSize * scale}pt`,
                          fontWeight: element.fontWeight,
                          fontStyle: element.fontStyle,
                          textAlign: element.textAlign as 'left' | 'center' | 'right' | 'justify',
                          color: element.textColor,
                          p: '2px',
                          overflow: element.rotation ? 'visible' : 'hidden',
                          whiteSpace: element.rotation ? 'nowrap' : 'pre-wrap',
                          display: element.rotation ? 'flex' : 'block',
                          alignItems: element.rotation ? 'center' : undefined,
                        }}
                      >
                        {element.content || 'Text'}
                      </Box>
                    ) : null}
                    {element.elementType === 'IMAGE' ? (
                      imageUrl ? (
                        <Box component="img" src={imageUrl} alt="" sx={{ width: '100%', height: '100%', objectFit: 'contain' }} />
                      ) : (
                        <Stack sx={{ width: '100%', height: '100%', alignItems: 'center', justifyContent: 'center', color: '#999' }}>
                          <ImageIcon />
                        </Stack>
                      )
                    ) : null}
                    {element.elementType === 'QR_CODE' ? (
                      <Stack sx={{ width: '100%', height: '100%', alignItems: 'center', justifyContent: 'center', bgcolor: element.qrBackgroundColor ?? '#f0f0f0' }}>
                        <QrCode2Icon sx={{ color: element.qrForegroundColor ?? '#000' }} />
                      </Stack>
                    ) : null}
                    {element.elementType === 'SHAPE' ? (
                      <Box sx={{ width: '100%', height: '100%' }} />
                    ) : null}
                    {element.elementType === 'LINE' ? (
                      <Box sx={{ width: '100%', height: `${Math.max(element.borderWidth, 1)}px`, bgcolor: element.borderColor, mt: `${Math.max(height / 2 - 1, 0)}px` }} />
                    ) : null}
                  </Box>
                )
              })}
            </Box>
          </Box>
        </Box>

        <Box sx={{ border: `1px solid ${portalColors.border}`, borderRadius: 2, overflow: 'hidden', bgcolor: '#fff' }}>
          <Box sx={{ px: 2, py: 1.5, bgcolor: portalColors.primary, color: '#fff', fontWeight: 600 }}>Properties</Box>
          <Box sx={{ p: 2 }}>
            {!selected ? (
              <Stack spacing={2}>
                <Typography sx={{ color: portalColors.textMuted, textAlign: 'center', py: 4 }}>
                  Select an element to edit its properties, or drag a new element onto the canvas.
                </Typography>
                <Divider />
                <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>Layout Settings</Typography>
                <FormControl fullWidth size="small">
                  <InputLabel>Paper Size</InputLabel>
                  <Select label="Paper Size" value={layout.paperSize} onChange={(e) => setLayout({ ...layout, paperSize: e.target.value })}>
                    <MenuItem value="A4">A4</MenuItem>
                    <MenuItem value="LETTER">Letter</MenuItem>
                    <MenuItem value="LEGAL">Legal</MenuItem>
                  </Select>
                </FormControl>
                <FormControl fullWidth size="small">
                  <InputLabel>Orientation</InputLabel>
                  <Select label="Orientation" value={layout.orientation} onChange={(e) => setLayout({ ...layout, orientation: e.target.value })}>
                    <MenuItem value="PORTRAIT">Portrait</MenuItem>
                    <MenuItem value="LANDSCAPE">Landscape</MenuItem>
                  </Select>
                </FormControl>
                <TextField
                  label="Background Color"
                  type="color"
                  size="small"
                  value={layout.backgroundColor}
                  onChange={(e) => setLayout({ ...layout, backgroundColor: e.target.value })}
                  fullWidth
                />
              </Stack>
            ) : (
              <Stack spacing={2}>
                <Box>
                  <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 0.5 }}>Element Type</Typography>
                  <Chip size="small" label={selected.elementType.replace('_', ' ')} sx={{ fontWeight: 600 }} />
                </Box>

                <Stack direction="row" spacing={1}>
                  <TextField label="Position X (mm)" type="number" size="small" slotProps={{ htmlInput: { step: 0.1 } }} value={selected.x} onChange={(e) => updateSelected({ x: Number(e.target.value) })} fullWidth />
                  <TextField label="Position Y (mm)" type="number" size="small" slotProps={{ htmlInput: { step: 0.1 } }} value={selected.y} onChange={(e) => updateSelected({ y: Number(e.target.value) })} fullWidth />
                </Stack>
                <Stack direction="row" spacing={1}>
                  <TextField label="Width (mm)" type="number" size="small" slotProps={{ htmlInput: { step: 0.1 } }} value={selected.width} onChange={(e) => updateSelected({ width: Number(e.target.value) })} fullWidth />
                  <TextField label="Height (mm)" type="number" size="small" slotProps={{ htmlInput: { step: 0.1 } }} value={selected.height} onChange={(e) => updateSelected({ height: Number(e.target.value) })} fullWidth />
                </Stack>

                {selected.elementType === 'TEXT' ? (
                  <>
                    <TextField label="Content" value={selected.content} onChange={(e) => updateSelected({ content: e.target.value }, false)} onBlur={() => pushHistory(elements)} multiline rows={3} fullWidth size="small" />
                    <Button variant="outlined" sx={portalOutlinedButtonSx} startIcon={<CropFreeIcon />} onClick={() => setVariableOpen(true)}>
                      Insert Variable
                    </Button>
                    <Stack direction="row" spacing={1}>
                      <FormControl fullWidth size="small">
                        <InputLabel>Font Family</InputLabel>
                        <Select label="Font Family" value={selected.fontFamily} onChange={(e) => updateSelected({ fontFamily: e.target.value })}>
                          {FONT_FAMILIES.map((font) => (
                            <MenuItem key={font.value} value={font.value}>
                              {font.label}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                      <TextField label="Font Size (pt)" type="number" size="small" slotProps={{ htmlInput: { min: 8, max: 72 } }} value={selected.fontSize} onChange={(e) => updateSelected({ fontSize: Number(e.target.value) })} fullWidth />
                    </Stack>
                    <Stack direction="row" spacing={1}>
                      <FormControl fullWidth size="small">
                        <InputLabel>Font Weight</InputLabel>
                        <Select label="Font Weight" value={selected.fontWeight} onChange={(e) => updateSelected({ fontWeight: e.target.value })}>
                          <MenuItem value="normal">Normal</MenuItem>
                          <MenuItem value="bold">Bold</MenuItem>
                        </Select>
                      </FormControl>
                      <FormControl fullWidth size="small">
                        <InputLabel>Font Style</InputLabel>
                        <Select label="Font Style" value={selected.fontStyle} onChange={(e) => updateSelected({ fontStyle: e.target.value })}>
                          <MenuItem value="normal">Normal</MenuItem>
                          <MenuItem value="italic">Italic</MenuItem>
                        </Select>
                      </FormControl>
                    </Stack>
                    <FormControl fullWidth size="small">
                      <InputLabel>Text Alignment</InputLabel>
                      <Select label="Text Alignment" value={selected.textAlign} onChange={(e) => updateSelected({ textAlign: e.target.value })}>
                        <MenuItem value="left">Left</MenuItem>
                        <MenuItem value="center">Center</MenuItem>
                        <MenuItem value="right">Right</MenuItem>
                        <MenuItem value="justify">Justify</MenuItem>
                      </Select>
                    </FormControl>
                    <TextField label="Text Color" type="color" size="small" value={colorInputValue(selected.textColor)} onChange={(e) => updateSelected({ textColor: e.target.value })} fullWidth />
                    <TextField
                      label="Rotation (degrees)"
                      type="number"
                      size="small"
                      slotProps={{ htmlInput: { min: -180, max: 180, step: 1 } }}
                      value={selected.rotation}
                      onChange={(e) => updateSelected({ rotation: Number(e.target.value) })}
                      fullWidth
                      helperText="Rotate text from -180° to 180°"
                    />
                  </>
                ) : null}

                {selected.elementType === 'IMAGE' ? (
                  <Stack spacing={1.5}>
                    <Button variant="outlined" component="label" sx={portalOutlinedButtonSx}>
                      Upload Image
                      <input
                        hidden
                        type="file"
                        accept="image/*"
                        onChange={async (e) => {
                          const file = e.target.files?.[0]
                          if (!file) return
                          const imagePath = await onUploadImage(file)
                          updateSelected({ imagePath })
                        }}
                      />
                    </Button>
                    <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>PNG, JPG, or GIF (max 5MB)</Typography>
                    {resolveCertificateAssetUrl(selected.imagePath) ? (
                      <Box
                        component="img"
                        src={resolveCertificateAssetUrl(selected.imagePath)}
                        alt="Preview"
                        sx={{ width: '100%', borderRadius: 1, border: `1px solid ${portalColors.border}` }}
                      />
                    ) : null}
                  </Stack>
                ) : null}

                {selected.elementType === 'QR_CODE' ? (
                  <Stack spacing={2}>
                    <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>QR Code Customization</Typography>
                    <Alert severity="info" sx={{ fontSize: '0.75rem' }}>
                      Logo and colors appear in generated PDFs and Preview, not in the canvas editor.
                    </Alert>
                    <Button variant="outlined" component="label" sx={portalOutlinedButtonSx}>
                      Logo in QR Code
                      <input
                        hidden
                        type="file"
                        accept="image/*"
                        onChange={async (e) => {
                          const file = e.target.files?.[0]
                          if (!file) return
                          const qrLogoPath = await onUploadImage(file)
                          updateSelected({ qrLogoPath })
                        }}
                      />
                    </Button>
                    {resolveCertificateAssetUrl(selected.qrLogoPath) ? (
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <Box
                          component="img"
                          src={resolveCertificateAssetUrl(selected.qrLogoPath)}
                          alt="QR Logo"
                          sx={{ width: 48, height: 48, objectFit: 'contain', border: `1px solid ${portalColors.border}`, borderRadius: 1 }}
                        />
                        <Button variant="outlined" color="error" size="small" onClick={() => updateSelected({ qrLogoPath: null, qrLogoSize: 20 })}>
                          Remove
                        </Button>
                      </Stack>
                    ) : null}
                    <Box>
                      <Typography sx={{ fontSize: '0.875rem', mb: 1 }}>Logo Size (%)</Typography>
                      <Slider
                        value={selected.qrLogoSize ?? 20}
                        min={10}
                        max={40}
                        step={1}
                        valueLabelDisplay="auto"
                        onChange={(_, value) => updateSelected({ qrLogoSize: value as number })}
                      />
                      <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
                        Size of logo relative to QR code: {selected.qrLogoSize ?? 20}%
                      </Typography>
                    </Box>
                    <Stack direction="row" spacing={1}>
                      <TextField
                        label="QR Code Color"
                        type="color"
                        size="small"
                        value={colorInputValue(selected.qrForegroundColor)}
                        onChange={(e) => updateSelected({ qrForegroundColor: e.target.value })}
                        fullWidth
                      />
                      <TextField
                        label="Background Color"
                        type="color"
                        size="small"
                        value={colorInputValue(selected.qrBackgroundColor, '#FFFFFF')}
                        onChange={(e) => updateSelected({ qrBackgroundColor: e.target.value })}
                        fullWidth
                      />
                    </Stack>
                    <FormControl fullWidth size="small">
                      <InputLabel>QR Code Style</InputLabel>
                      <Select label="QR Code Style" value={selected.qrStyle ?? 'square'} onChange={(e) => updateSelected({ qrStyle: e.target.value })}>
                        <MenuItem value="square">Square</MenuItem>
                        <MenuItem value="round">Round</MenuItem>
                        <MenuItem value="dot">Dot</MenuItem>
                      </Select>
                    </FormControl>
                    <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
                      Style is saved; visual rendering depends on QR library support.
                    </Typography>
                  </Stack>
                ) : null}

                <Divider />
                <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>Styling</Typography>
                <TextField
                  label="Background Color"
                  type="color"
                  size="small"
                  disabled={selected.backgroundColor === 'transparent'}
                  value={colorInputValue(selected.backgroundColor, selected.elementType === 'SHAPE' ? '#d1d5db' : '#ffffff')}
                  onChange={(e) => updateSelected({ backgroundColor: e.target.value })}
                  fullWidth
                />
                <FormControlLabel
                  control={
                    <Checkbox
                      checked={selected.backgroundColor === 'transparent'}
                      onChange={(e) =>
                        updateSelected({
                          backgroundColor: e.target.checked
                            ? 'transparent'
                            : selected.elementType === 'SHAPE'
                              ? '#d1d5db'
                              : '#ffffff',
                        })
                      }
                    />
                  }
                  label="Transparent"
                />
                <Stack direction="row" spacing={1}>
                  <TextField
                    label="Border Width (px)"
                    type="number"
                    size="small"
                    slotProps={{ htmlInput: { min: 0, max: 10 } }}
                    value={selected.borderWidth}
                    onChange={(e) => updateSelected({ borderWidth: Number(e.target.value) })}
                    fullWidth
                  />
                  <TextField
                    label="Border Color"
                    type="color"
                    size="small"
                    value={colorInputValue(selected.borderColor)}
                    onChange={(e) => updateSelected({ borderColor: e.target.value })}
                    fullWidth
                  />
                </Stack>
                <Stack direction="row" spacing={1}>
                  <TextField
                    label="Border Radius (px)"
                    type="number"
                    size="small"
                    slotProps={{ htmlInput: { min: 0, max: 50 } }}
                    value={selected.borderRadius}
                    onChange={(e) => updateSelected({ borderRadius: Number(e.target.value) })}
                    fullWidth
                  />
                  <TextField
                    label="Layer (Z-Index)"
                    type="number"
                    size="small"
                    slotProps={{ htmlInput: { min: 0, max: 100 } }}
                    value={selected.zIndex}
                    onChange={(e) => updateSelected({ zIndex: Number(e.target.value) })}
                    fullWidth
                  />
                </Stack>
                <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>Higher layer values appear on top</Typography>

                <Button variant="outlined" color="error" startIcon={<DeleteOutlinedIcon />} onClick={handleDeleteSelected}>
                  Delete Element
                </Button>
              </Stack>
            )}
          </Box>
        </Box>
      </Box>

      <Dialog open={variableOpen} onClose={() => setVariableOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Insert Template Variable</DialogTitle>
        <DialogContent>
          <List>
            {variables.map((item) => (
              <ListItemButton
                key={item.name}
                onClick={() => {
                  updateSelected({ content: `${selected?.content ?? ''}${item.name}` })
                  setVariableOpen(false)
                }}
              >
                <ListItemText primary={item.name} secondary={item.description} />
              </ListItemButton>
            ))}
          </List>
        </DialogContent>
      </Dialog>

      <Alert severity="info" sx={{ mt: 2 }}>
        Drag elements from the palette onto the canvas. Move elements by dragging them. Use Preview to generate a sample PDF.
      </Alert>
    </Box>
  )
}
