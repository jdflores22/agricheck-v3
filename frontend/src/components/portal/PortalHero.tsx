import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'

interface PortalHeroProps {
  eyebrow?: string
  title: string
  subtitle?: string
  actions?: ReactNode
}

export function PortalHero({ eyebrow, title, subtitle, actions }: PortalHeroProps) {
  return (
    <Box
      sx={{
        mb: 3,
        display: 'flex',
        flexDirection: { xs: 'column', sm: 'row' },
        alignItems: { sm: 'center' },
        justifyContent: 'space-between',
        gap: 2,
        borderRadius: '0.75rem',
        border: '1px solid #15803d',
        background: 'linear-gradient(135deg, #22c55e 0%, #16a34a 100%)',
        color: '#fff',
        px: { xs: 2.5, sm: 3 },
        py: { xs: 2.5, sm: 3 },
        boxShadow: '0 1px 3px rgba(22, 163, 74, 0.15)',
      }}
    >
      <Box>
        {eyebrow && (
          <Typography
            sx={{
              fontSize: '0.625rem',
              fontWeight: 600,
              letterSpacing: '0.14em',
              textTransform: 'uppercase',
              color: 'rgba(255,255,255,0.8)',
            }}
          >
            {eyebrow}
          </Typography>
        )}
        <Typography sx={{ mt: eyebrow ? 0.5 : 0, fontSize: '1.25rem', fontWeight: 600 }}>
          {title}
        </Typography>
        {subtitle && (
          <Typography sx={{ mt: 0.5, fontSize: '0.875rem', color: 'rgba(255,255,255,0.88)' }}>
            {subtitle}
          </Typography>
        )}
      </Box>
      {actions}
    </Box>
  )
}
