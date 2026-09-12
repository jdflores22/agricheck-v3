import { Box, Typography } from '@mui/material'
import { portalColors } from './portalTheme'

interface PortalStatCardProps {
  label: string
  value: string | number
  caption?: string
}

export function PortalStatCard({ label, value, caption }: PortalStatCardProps) {
  return (
    <Box
      sx={{
        borderRadius: '0.75rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
        px: 2.5,
        py: 2.5,
      }}
    >
      <Typography
        sx={{
          fontSize: '0.75rem',
          fontWeight: 500,
          letterSpacing: '0.12em',
          textTransform: 'uppercase',
          color: portalColors.textMuted,
        }}
      >
        {label}
      </Typography>
      <Typography
        sx={{
          mt: 1,
          fontSize: '1.875rem',
          fontWeight: 600,
          letterSpacing: '-0.02em',
          fontVariantNumeric: 'tabular-nums',
          color: portalColors.textDark,
        }}
      >
        {value}
      </Typography>
      {caption && (
        <Typography sx={{ mt: 0.5, fontSize: '0.875rem', color: portalColors.textMuted }}>
          {caption}
        </Typography>
      )}
    </Box>
  )
}
