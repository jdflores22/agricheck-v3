import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { portalColors } from './portalTheme'

interface PortalPanelProps {
  title: string
  action?: { label: string; to: string }
  children: ReactNode
}

export function PortalPanel({ title, action, children }: PortalPanelProps) {
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
          justifyContent: 'space-between',
          px: 2.5,
          py: 2,
          borderBottom: `1px solid ${portalColors.border}`,
        }}
      >
        <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
          {title}
        </Typography>
        {action && (
          <Typography
            component={RouterLink}
            to={action.to}
            sx={{
              fontSize: '0.875rem',
              fontWeight: 500,
              color: portalColors.primary,
              textDecoration: 'none',
              '&:hover': { color: portalColors.primaryDark },
            }}
          >
            {action.label}
          </Typography>
        )}
      </Box>
      {children}
    </Box>
  )
}
