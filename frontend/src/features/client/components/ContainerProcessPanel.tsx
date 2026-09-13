import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import QrCode2OutlinedIcon from '@mui/icons-material/QrCode2Outlined'
import RadioButtonUncheckedIcon from '@mui/icons-material/RadioButtonUnchecked'
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  Stack,
  Step,
  StepLabel,
  Stepper,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useGetRegisteredWarehousesQuery } from '../../addresses/addressApi'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalTableHeadCellSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalAnalyticsColors, portalColors } from '../../../components/portal/portalTheme'
import type { ClientContainerDetail } from '../api/clientApi'
import type { FormFieldSchema } from '../../forms/formSchema'
import { formatCommodityHsSummary, getAddressValueKeys, parseFormDataJson, visibleFormFields } from '../../forms/formSchema'

function formatFieldValue(
  field: FormFieldSchema,
  values: Record<string, string>,
  warehouseNameById: Map<string, string>,
): string {
  if (field.type === 'section') return ''

  if (field.type === 'warehouse') {
    const storedLabel = values[`${field.name}_label`]?.trim()
    if (storedLabel) return storedLabel

    const raw = values[field.name]?.trim()
    if (!raw) return '—'

    return warehouseNameById.get(raw) ?? raw
  }

  if (field.type === 'commodity') {
    return formatCommodityHsSummary(field.name, values) || '—'
  }

  if (field.type === 'address') {
    const keys = getAddressValueKeys(field.name)
    const parts = [
      values[keys.street],
      values[keys.barangayName],
      values[keys.cityName],
      values[keys.provinceName],
      values[keys.regionName],
      values[keys.zipCode],
    ].filter(Boolean)
    return parts.join(', ') || '—'
  }

  if (field.type === 'checkbox') {
    const value = values[field.name]
    if (value === 'yes' || value === 'true' || value === '1') return 'Yes'
    if (value === 'no' || value === 'false' || value === '0') return 'No'
    return value?.trim() || '—'
  }

  if (field.type === 'file' || field.type === 'geotag_photo') {
    return values[field.name]?.trim() || '—'
  }

  const raw = values[field.name]?.trim()
  if (!raw) return '—'

  if ((field.type === 'select' || field.type === 'radio') && field.options?.length) {
    const match = field.options.find((option) => option.value === raw)
    return match?.label ?? raw
  }

  return raw
}

function DetailRow({ label, value }: { label: string; value?: string | number | null }) {
  const display = value === undefined || value === null || value === '' ? '—' : String(value)

  return (
    <Stack
      direction={{ xs: 'column', sm: 'row' }}
      spacing={{ xs: 0.25, sm: 2 }}
      sx={{
        py: 1.125,
        borderBottom: `1px solid ${portalColors.border}`,
        '&:last-child': { borderBottom: 0 },
      }}
    >
      <Typography
        sx={{
          width: { sm: 168 },
          flexShrink: 0,
          fontSize: '0.8125rem',
          color: portalColors.textMuted,
        }}
      >
        {label}
      </Typography>
      <Typography sx={{ fontSize: '0.875rem', color: portalColors.textDark, wordBreak: 'break-word' }}>
        {display}
      </Typography>
    </Stack>
  )
}

function ProcessStepIcon({ state }: { state: string }) {
  if (state === 'complete') {
    return <CheckCircleOutlinedIcon sx={{ fontSize: 22, color: portalAnalyticsColors.base }} />
  }
  if (state === 'active') {
    return <ScheduleOutlinedIcon sx={{ fontSize: 22, color: portalColors.primary }} />
  }
  return <RadioButtonUncheckedIcon sx={{ fontSize: 22, color: portalColors.textLight }} />
}

export function ContainerProcessPanel({
  container,
  schemaFields,
}: {
  container: ClientContainerDetail
  schemaFields: FormFieldSchema[]
}) {
  const { data: warehouseData } = useGetRegisteredWarehousesQuery()
  const warehouseNameById = new Map(
    (warehouseData?.data ?? []).map((warehouse) => [String(warehouse.id), warehouse.name]),
  )

  const parsedValues = {
    ...parseFormDataJson(container.formDataJson),
    container_number: container.containerNumber,
    containerNumber: container.containerNumber,
    select_container_type: container.containerType ?? '',
    container_type: container.containerType ?? '',
    containerType: container.containerType ?? '',
  }

  const detailFields = visibleFormFields(schemaFields, parsedValues).filter(
    (field) => field.type !== 'section' && field.type !== 'file' && field.type !== 'geotag_photo',
  )

  const activeStep = Math.max(
    0,
    container.processSteps.findIndex((step) => step.state === 'active'),
  )

  return (
    <Stack spacing={2}>
      <PortalPanel title="Container process">
        <Box sx={{ px: { xs: 1.5, sm: 2.5 }, py: 2 }}>
          <Stepper
            activeStep={activeStep}
            alternativeLabel
            sx={{
              mb: 2,
              '& .MuiStepConnector-line': { borderColor: portalColors.border },
              '& .MuiStepLabel-label': { fontSize: '0.75rem', mt: 0.75 },
              '& .MuiStepLabel-label.Mui-active': { color: portalColors.primary, fontWeight: 700 },
              '& .MuiStepLabel-label.Mui-completed': { color: portalAnalyticsColors.base, fontWeight: 600 },
            }}
          >
            {container.processSteps.map((step) => (
              <Step key={step.key} completed={step.state === 'complete'}>
                <StepLabel slots={{ stepIcon: () => <ProcessStepIcon state={step.state} /> }}>
                  {step.label}
                </StepLabel>
              </Step>
            ))}
          </Stepper>

          {container.processSteps[activeStep] ? (
            <Box
              sx={{
                px: 2,
                py: 1.5,
                borderRadius: '0.5rem',
                bgcolor: portalAnalyticsColors.soft,
                border: `1px solid ${portalAnalyticsColors.softStrong}`,
              }}
            >
              <Typography sx={{ fontSize: '0.875rem', fontWeight: 700, color: portalColors.primary, mb: 0.5 }}>
                Current stage: {container.processSteps[activeStep].label}
              </Typography>
              <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, lineHeight: 1.5 }}>
                {container.processSteps[activeStep].description}
              </Typography>
              {container.processSteps[activeStep].completedAt ? (
                <Typography sx={{ mt: 0.75, fontSize: '0.75rem', color: portalColors.textMuted }}>
                  Updated {new Date(container.processSteps[activeStep].completedAt!).toLocaleString()}
                </Typography>
              ) : null}
            </Box>
          ) : null}
        </Box>
      </PortalPanel>

      {container.transportTag?.qrCodeData ? (
        <PortalPanel title="Transport QR for driver">
          <Box sx={{ px: 2.5, py: 2 }}>
            <Stack spacing={2} sx={{ alignItems: { xs: 'stretch', sm: 'flex-start' } }}>
              <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, lineHeight: 1.6 }}>
                Share this QR code with your driver or transport operator. They can scan it in AgriTrack to claim this
                container for warehouse transport and 2nd border inspection.
              </Typography>
              {container.transportTag.scheduledWarehouseDate ? (
                <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                  Scheduled warehouse inspection:{' '}
                  <strong>
                    {new Date(`${container.transportTag.scheduledWarehouseDate.slice(0, 10)}T00:00:00`).toLocaleDateString()}
                  </strong>
                </Typography>
              ) : null}
              <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                Tagged {new Date(container.transportTag.taggedAt).toLocaleString()}
              </Typography>
              <Box
                sx={{
                  alignSelf: { xs: 'center', sm: 'flex-start' },
                  p: 1.5,
                  borderRadius: '0.75rem',
                  border: `1px solid ${portalColors.border}`,
                  bgcolor: '#fff',
                }}
              >
                <Box
                  component="img"
                  src={container.transportTag.qrCodeData}
                  alt="Transport QR code for driver"
                  sx={{ width: 220, height: 220, objectFit: 'contain', display: 'block' }}
                />
              </Box>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', color: portalColors.primary }}>
                <QrCode2OutlinedIcon fontSize="small" />
                <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600 }}>
                  Show this code to the driver before dispatch
                </Typography>
              </Stack>
            </Stack>
          </Box>
        </PortalPanel>
      ) : null}

      <Stack direction={{ xs: 'column', lg: 'row' }} spacing={2}>
        <Box sx={{ flex: 1 }}>
          <PortalPanel title="Container information">
            <Box sx={{ px: 2.5, py: 1.5 }}>
              <DetailRow label="Container number" value={container.containerNumber} />
              <DetailRow label="Container type" value={container.containerType} />
              <DetailRow label="Sequence" value={container.sequenceNumber} />
              <DetailRow label="Entry reference" value={container.entryReferenceNo} />
              <DetailRow label="Entry status" value={container.entryStatus} />
              <DetailRow label="Agency" value={`${container.agencyCode} · ${container.agencyName}`} />
              {detailFields.map((field) => (
                <DetailRow
                  key={field.name}
                  label={field.label}
                  value={formatFieldValue(field, parsedValues, warehouseNameById)}
                />
              ))}
            </Box>
          </PortalPanel>
        </Box>

        <Box sx={{ flex: 1 }}>
          <PortalPanel title="Logistics">
            <Box sx={{ px: 2.5, py: 1.5 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1.5 }}>
                <Chip size="small" label={container.status} sx={portalStatusChipSx(container.status)} />
                <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                  Last updated {new Date(container.updatedAt).toLocaleString()}
                </Typography>
              </Stack>
              <DetailRow
                label="Departure"
                value={container.departureTime ? new Date(container.departureTime).toLocaleString() : undefined}
              />
              <DetailRow
                label="Arrival"
                value={container.arrivalTime ? new Date(container.arrivalTime).toLocaleString() : undefined}
              />
              <DetailRow label="Registered" value={new Date(container.createdAt).toLocaleString()} />
            </Box>
          </PortalPanel>
        </Box>
      </Stack>

      <PortalPanel title="Warehouse">
        <Box sx={{ px: 2.5, py: 2 }}>
          {container.warehouseInfo ? (
            <Box sx={{ mb: 2 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1 }}>
                <WarehouseOutlinedIcon sx={{ fontSize: 18, color: portalColors.primary }} />
                <Typography sx={{ fontSize: '0.875rem', fontWeight: 700 }}>Warehouse intake</Typography>
              </Stack>
              <DetailRow label="Facility" value={container.warehouseInfo.facilityName} />
              <DetailRow label="Location code" value={container.warehouseInfo.locationCode} />
              <DetailRow
                label="Received"
                value={
                  container.warehouseInfo.receivedAt
                    ? new Date(container.warehouseInfo.receivedAt).toLocaleString()
                    : undefined
                }
              />
              <DetailRow label="Inventory status" value={container.warehouseInfo.status} />
            </Box>
          ) : null}

          {container.canBookWarehouse ? (
            <Stack spacing={1.5}>
              <Alert severity="success" sx={{ fontSize: '0.8125rem' }}>
                This container is processed at the warehouse. You can now schedule warehouse storage.
              </Alert>
              <Button
                component={RouterLink}
                to={`/client/warehouse/bookings?containerUuid=${container.uuid}`}
                variant="contained"
                sx={{ ...portalPrimaryButtonSx, alignSelf: 'flex-start' }}
              >
                Book warehouse storage
              </Button>
            </Stack>
          ) : (
            <Stack spacing={1.5}>
              <Alert
                severity="info"
                icon={<LockOutlinedIcon fontSize="inherit" />}
                sx={{ fontSize: '0.8125rem' }}
              >
                {container.warehouseBookingBlockedReason ??
                  'Warehouse booking is not available until this container is processed.'}
              </Alert>
              <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, lineHeight: 1.55 }}>
                Based on the V1 AgriCheck flow, warehouse storage booking opens only after the container is received
                and processed at the warehouse — not while it is still pending, assigned, or in transit.
              </Typography>
            </Stack>
          )}

          {container.bookings.length > 0 ? (
            <Box sx={{ mt: 2.5 }}>
              <Typography sx={{ fontSize: '0.8125rem', fontWeight: 700, mb: 1 }}>Related bookings</Typography>
              <Box sx={{ border: `1px solid ${portalColors.border}`, borderRadius: '0.5rem', overflow: 'hidden' }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={portalTableHeadCellSx}>Booking</TableCell>
                      <TableCell sx={portalTableHeadCellSx}>Warehouse</TableCell>
                      <TableCell sx={portalTableHeadCellSx}>Status</TableCell>
                      <TableCell sx={portalTableHeadCellSx} align="right">
                        Action
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {container.bookings.map((booking) => (
                      <TableRow key={booking.uuid} hover>
                        <TableCell sx={{ fontSize: '0.875rem' }}>{booking.bookingNumber}</TableCell>
                        <TableCell sx={{ fontSize: '0.875rem' }}>{booking.warehouseName}</TableCell>
                        <TableCell>
                          <Chip size="small" label={booking.status} sx={portalStatusChipSx(booking.status)} />
                        </TableCell>
                        <TableCell align="right">
                          <Button
                            component={RouterLink}
                            to={`/client/warehouse/bookings/${booking.uuid}`}
                            size="small"
                            sx={{ textTransform: 'none', fontWeight: 600, minWidth: 0, px: 1 }}
                          >
                            View
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </Box>
            </Box>
          ) : null}
        </Box>
      </PortalPanel>
    </Stack>
  )
}
