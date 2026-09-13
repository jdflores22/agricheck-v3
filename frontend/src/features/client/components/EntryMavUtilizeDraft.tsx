import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import {
  Alert,
  Box,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useRef } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { useGetAvailableMicsQuery } from '../../mav/api/mavApi'

export type EntryMavUtilizeDraftValue = {
  micUuid: string
  volume: string
}

export function EntryMavUtilizeDraft({
  hsCode,
  commodityName,
  quantity,
  value,
  onChange,
}: {
  hsCode?: string
  commodityName?: string
  quantity?: number
  value: EntryMavUtilizeDraftValue
  onChange: (next: EntryMavUtilizeDraftValue) => void
}) {
  const { data, isLoading } = useGetAvailableMicsQuery()
  const primedVolume = useRef(false)
  const matchingMics = useMemo(() => {
    const mics = data?.data ?? []
    if (!hsCode) return mics
    return mics.filter((mic) => mic.hsCode === hsCode || mic.hsCode.startsWith(hsCode) || hsCode.startsWith(mic.hsCode))
  }, [data?.data, hsCode])

  useEffect(() => {
    if (primedVolume.current || !quantity || quantity <= 0) return
    primedVolume.current = true
    onChange({ micUuid: value.micUuid, volume: String(quantity) })
  }, [onChange, quantity, value.micUuid])

  const selectedMic = matchingMics.find((mic) => mic.uuid === value.micUuid)

  return (
    <PortalPanel title="Add MIC utilization">
      <Stack spacing={2} sx={{ p: 2.5 }}>
        <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
          Add volume now for this import entry. Warehouse release (minus) comes later.
          {commodityName ? ` Selected commodity: ${commodityName}${hsCode ? ` (${hsCode})` : ''}.` : ''}
        </Typography>

        {isLoading ? (
          <CircularProgress size={22} />
        ) : matchingMics.length === 0 ? (
          <Alert severity="info" icon={<ErrorOutlineOutlinedIcon />} sx={{ borderRadius: '0.75rem' }}>
            {hsCode
              ? <>No active MIC matches this HS code yet. Save the draft, then link a MIC from your <RouterLink to="/mav/licenses">MAV licenses</RouterLink>.</>
              : <>Search an HS code first, or issue a MIC from your <RouterLink to="/mav/licenses">MAV license</RouterLink>.</>}
          </Alert>
        ) : (
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
            <FormControl fullWidth size="small">
              <InputLabel>Active MIC</InputLabel>
              <Select
                label="Active MIC"
                value={value.micUuid}
                onChange={(e) => onChange({ ...value, micUuid: e.target.value })}
              >
                <MenuItem value="">Skip for now</MenuItem>
                {matchingMics.map((mic) => (
                  <MenuItem key={mic.uuid} value={mic.uuid}>
                    {mic.certificateNumber} · {mic.availableVolume} available · {mic.commodityName}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <TextField
              size="small"
              label="Volume to add"
              type="number"
              value={value.volume}
              onChange={(e) => onChange({ ...value, volume: e.target.value })}
              sx={{ minWidth: 160 }}
              slotProps={{ htmlInput: { min: 0, step: '0.001', max: selectedMic?.availableVolume } }}
            />
          </Stack>
        )}

        {selectedMic ? (
          <Box sx={{ px: 1.5, py: 1, bgcolor: portalColors.bgMuted, borderRadius: 1.5 }}>
            <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600 }}>
              Will add {value.volume || '0'} from {selectedMic.certificateNumber}
            </Typography>
            <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
              {selectedMic.commodityName} ({selectedMic.hsCode}) · {selectedMic.availableVolume} remaining
            </Typography>
          </Box>
        ) : null}
      </Stack>
    </PortalPanel>
  )
}
