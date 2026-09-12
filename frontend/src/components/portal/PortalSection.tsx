import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { portalColors } from './portalTheme'

interface PortalSectionProps {
  icon: ReactNode
  title: string
  children: ReactNode
}

export function PortalSection({ icon, title, children }: PortalSectionProps) {
  return (
    <Box
      sx={{
        borderRadius: '0.75rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
        overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1,
          px: 2,
          py: 1.5,
          borderBottom: `1px solid ${portalColors.border}`,
        }}
      >
        <Box sx={{ color: portalColors.primary, display: 'flex', alignItems: 'center' }}>{icon}</Box>
        <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
          {title}
        </Typography>
      </Box>
      <Box sx={{ p: { xs: 2, sm: 2.5 } }}>{children}</Box>
    </Box>
  )
}

interface PortalFieldProps {
  label: string
  value: ReactNode
  fullWidth?: boolean
}

export function PortalField({ label, value, fullWidth }: PortalFieldProps) {
  return (
    <Box sx={{ gridColumn: fullWidth ? '1 / -1' : undefined }}>
      <Typography
        sx={{
          fontSize: '0.75rem',
          fontWeight: 600,
          letterSpacing: '0.06em',
          textTransform: 'uppercase',
          color: portalColors.textMuted,
        }}
      >
        {label}
      </Typography>
      <Typography sx={{ mt: 0.5, fontSize: '0.875rem', fontWeight: 500, color: portalColors.textDark }}>
        {value}
      </Typography>
    </Box>
  )
}
