import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { portalColors } from './portalTheme'

interface PortalQuickActionProps {
  to: string
  icon: ReactNode
  title: string
  caption: string
}

export function PortalQuickAction({ to, icon, title, caption }: PortalQuickActionProps) {
  return (
    <Box
      component={RouterLink}
      to={to}
      sx={{
        display: 'flex',
        minHeight: 108,
        flexDirection: 'column',
        justifyContent: 'space-between',
        borderRadius: '0.75rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
        p: 2,
        textDecoration: 'none',
        transition: 'border-color 0.15s ease, background-color 0.15s ease',
        '&:hover': {
          borderColor: 'rgba(22, 101, 52, 0.4)',
          bgcolor: portalColors.bgMuted,
        },
        '& .MuiSvgIcon-root': {
          color: portalColors.primary,
          fontSize: 22,
        },
      }}
    >
      {icon}
      <Box>
        <Typography sx={{ fontSize: '0.875rem', fontWeight: 500, color: portalColors.textDark }}>
          {title}
        </Typography>
        <Typography sx={{ mt: 0.25, fontSize: '0.75rem', color: portalColors.textMuted }}>
          {caption}
        </Typography>
      </Box>
    </Box>
  )
}
