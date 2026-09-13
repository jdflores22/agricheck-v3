import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined'
import {
  Box,
  Button,
  Chip,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalTableHeadCellSx, portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { portalColors } from '../../../components/portal/portalTheme'
import type { Entry, EntryContainer } from '../api/clientApi'
import {
  getContainerNumber,
  getContainerType,
  getNumContainersFromFormData,
} from '../utils/containerFormUtils'
import type { FormFieldSchema } from '../../forms/formSchema'
import { formatCommodityHsSummary, getAddressValueKeys, parseFormDataJson, visibleFormFields } from '../../forms/formSchema'

function formatFieldValue(field: FormFieldSchema, values: Record<string, string>): string {
  if (field.type === 'section') return ''

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
          width: { sm: 148 },
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

function getContainerExtraSummary(container: EntryContainer, schemaFields: FormFieldSchema[]): string {
  const parsedValues = {
    ...parseFormDataJson(container.formDataJson),
    container_number: container.containerNumber,
    containerNumber: container.containerNumber,
    select_container_type: container.containerType ?? '',
    container_type: container.containerType ?? '',
    containerType: container.containerType ?? '',
  }

  return visibleFormFields(schemaFields, parsedValues)
    .filter((field) => field.type !== 'section' && field.type !== 'file' && field.type !== 'geotag_photo')
    .map((field) => {
      const value = formatFieldValue(field, parsedValues)
      if (value === '—') return null
      return `${field.label}: ${value}`
    })
    .filter(Boolean)
    .join(' · ')
}

function ContainerTableRow({
  container,
  index,
  schemaFields,
}: {
  container: EntryContainer
  index: number
  schemaFields: FormFieldSchema[]
}) {
  const parsedValues = {
    ...parseFormDataJson(container.formDataJson),
    container_number: container.containerNumber,
    containerNumber: container.containerNumber,
    select_container_type: container.containerType ?? '',
    container_type: container.containerType ?? '',
    containerType: container.containerType ?? '',
  }

  const containerNumber = container.containerNumber || getContainerNumber(parsedValues) || `Container ${index + 1}`
  const containerType = container.containerType || getContainerType(parsedValues) || '—'
  const extraSummary = getContainerExtraSummary(container, schemaFields)

  return (
    <TableRow hover>
      <TableCell sx={{ width: 48, color: portalColors.textMuted, fontWeight: 600 }}>
        {container.sequenceNumber || index + 1}
      </TableCell>
      <TableCell>
        <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>{containerNumber}</Typography>
        {extraSummary ? (
          <Typography sx={{ mt: 0.25, fontSize: '0.75rem', color: portalColors.textMuted, lineHeight: 1.45 }}>
            {extraSummary}
          </Typography>
        ) : null}
      </TableCell>
      <TableCell sx={{ fontSize: '0.875rem' }}>{containerType}</TableCell>
      <TableCell align="right">
        <Button
          component={RouterLink}
          to={`/client/containers/${container.uuid}`}
          size="small"
          endIcon={<OpenInNewOutlinedIcon sx={{ fontSize: '14px !important' }} />}
          sx={{ textTransform: 'none', fontWeight: 600, minWidth: 0, px: 1 }}
        >
          View
        </Button>
      </TableCell>
    </TableRow>
  )
}

export function EntryOverviewPanel({
  entry,
  containerSchemaFields,
  showImportTrack = false,
}: {
  entry: Entry
  containerSchemaFields: FormFieldSchema[]
  showImportTrack?: boolean
}) {
  const containers = entry.containers ?? []
  const numContainers = getNumContainersFromFormData(entry.formDataJson, containers.length)

  const quantityDisplay =
    entry.detail?.quantity != null && entry.detail.quantity > 0
      ? `${entry.detail.quantity.toLocaleString()} ${entry.detail.unit ?? ''}`.trim()
      : '—'

  return (
    <Stack spacing={2}>
      <PortalPanel title="Shipment details">
        <Box sx={{ px: 2.5, py: 1.5 }}>
          <DetailRow label="Agency" value={entry.agencyName} />
          <DetailRow label="Entry type" value={entry.entryType} />
          {entry.entryType === 'Import' && showImportTrack ? (
            <DetailRow
              label="Import track"
              value={entry.mav?.importTrack === 'Mav' ? 'MAV import (in-quota)' : 'Regular import (out-quota)'}
            />
          ) : null}
          <DetailRow label="Commodity" value={entry.detail?.commodityName} />
          <DetailRow label="Quantity" value={quantityDisplay} />
          <DetailRow label="Origin" value={entry.detail?.originCountry} />
          <DetailRow label="Destination" value={entry.detail?.destinationCountry} />
          <DetailRow label="Port of entry" value={entry.detail?.portOfEntry} />
          {entry.detail?.description ? <DetailRow label="Description" value={entry.detail.description} /> : null}
          <DetailRow label="Notes" value={entry.notes} />
        </Box>
      </PortalPanel>

      <PortalPanel title="Container details">
        <Box sx={{ px: 2.5, py: 1.5 }}>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center', justifyContent: 'space-between', mb: 1.5 }}>
            <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', gap: 0.75 }}>
              <Inventory2OutlinedIcon sx={{ fontSize: 18, color: portalColors.primary }} />
              <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                {containers.length}
                {numContainers > containers.length ? ` / ${numContainers}` : ''} container(s)
              </Typography>
              {containers.length > 0 ? (
                <Chip size="small" label={`${containers.length} recorded`} sx={{ height: 22, fontWeight: 600 }} />
              ) : null}
            </Stack>
            {entry.status === 'Draft' ? (
              <Button
                component={RouterLink}
                to={`/client/entries/${entry.uuid}/edit`}
                size="small"
                sx={{ textTransform: 'none', fontWeight: 600 }}
              >
                Edit
              </Button>
            ) : null}
          </Stack>

          {containers.length === 0 ? (
            <Box sx={{ py: 2.5, textAlign: 'center' }}>
              <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 1.5 }}>
                {numContainers > 0
                  ? `Expected ${numContainers} container(s), but none saved yet.`
                  : 'No container information provided.'}
              </Typography>
              {entry.status === 'Draft' ? (
                <Button
                  component={RouterLink}
                  to={`/client/entries/${entry.uuid}/edit`}
                  variant="outlined"
                  size="small"
                  sx={portalOutlinedButtonSx}
                >
                  Add container details
                </Button>
              ) : null}
            </Box>
          ) : (
            <>
              <Box sx={{ border: `1px solid ${portalColors.border}`, borderRadius: '0.5rem', overflow: 'hidden' }}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell sx={portalTableHeadCellSx}>#</TableCell>
                      <TableCell sx={portalTableHeadCellSx}>Container</TableCell>
                      <TableCell sx={portalTableHeadCellSx}>Type</TableCell>
                      <TableCell sx={portalTableHeadCellSx} align="right">
                        Action
                      </TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {containers.map((container, index) => (
                      <ContainerTableRow
                        key={container.uuid}
                        container={container}
                        index={index}
                        schemaFields={containerSchemaFields}
                      />
                    ))}
                  </TableBody>
                </Table>
              </Box>
              {numContainers > containers.length ? (
                <Typography sx={{ mt: 1, fontSize: '0.8125rem', color: portalColors.textMuted }}>
                  {numContainers - containers.length} more container(s) expected from the entry form.
                </Typography>
              ) : null}
            </>
          )}
        </Box>
      </PortalPanel>
    </Stack>
  )
}
