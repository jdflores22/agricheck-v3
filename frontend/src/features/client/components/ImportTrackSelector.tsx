import { FormControlLabel, Radio, RadioGroup, Stack, Typography } from '@mui/material'
import { portalColors } from '../../../components/portal/portalTheme'

export type ImportTrack = 'Regular' | 'Mav'

export function ImportTrackSelector({
  value,
  importerHasMavAccess,
  disabled,
  onChange,
}: {
  value: ImportTrack
  importerHasMavAccess: boolean
  disabled?: boolean
  onChange: (track: ImportTrack) => void
}) {
  return (
    <Stack spacing={0.5}>
      <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
        Import track
      </Typography>
      <RadioGroup
        row
        value={value}
        onChange={(event) => onChange(event.target.value as ImportTrack)}
        sx={{ gap: 1.5, ml: -0.5 }}
      >
        <FormControlLabel
          value="Regular"
          disabled={disabled}
          control={<Radio size="small" />}
          label="Regular"
          sx={{ '& .MuiFormControlLabel-label': { fontSize: '0.875rem' } }}
        />
        <FormControlLabel
          value="Mav"
          disabled={disabled || !importerHasMavAccess}
          control={<Radio size="small" />}
          label="MAV"
          sx={{ '& .MuiFormControlLabel-label': { fontSize: '0.875rem' } }}
        />
      </RadioGroup>
      <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, lineHeight: 1.45 }}>
        {value === 'Mav'
          ? 'In-quota. Uses MIC volume and reduces remaining MAV.'
          : importerHasMavAccess
            ? 'Out-quota. Counts in stock, does not use MIC.'
            : 'Out-quota. No MAV license or MIC on this account.'}
      </Typography>
    </Stack>
  )
}
