import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { authColors } from './authTheme'

interface AuthFormShellProps {
  title: string
  subtitle?: ReactNode
  alerts?: ReactNode
  maxWidth?: string
  children: ReactNode
}

function AuthLogo({ size }: { size: 'sm' | 'md' }) {
  const dim = size === 'sm' ? 36 : 40
  return (
    <Box sx={{ mb: 2.5 }}>
      <Box
        sx={{
          width: dim,
          height: dim,
          borderRadius: '0.5rem',
          bgcolor: '#166534',
          color: '#fff',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <CheckCircleIcon sx={{ fontSize: size === 'sm' ? 18 : 22 }} />
      </Box>
    </Box>
  )
}

export function AuthFormShell({ title, subtitle, alerts, maxWidth = '28rem', children }: AuthFormShellProps) {
  return (
    <Box sx={{ width: '100%', maxWidth }}>
      <Box sx={{ display: { xs: 'block', md: 'none' } }}>
        <AuthLogo size="md" />
      </Box>
      <Box sx={{ display: { xs: 'none', md: 'block' } }}>
        <AuthLogo size="sm" />
      </Box>

      <Box component="header" sx={{ mb: 3 }}>
        <Typography
          component="h1"
          sx={{
            m: 0,
            fontSize: '1.75rem',
            lineHeight: 1.2,
            fontWeight: 700,
            color: authColors.textDark,
          }}
        >
          {title}
        </Typography>
        {subtitle && (
          <Typography
            sx={{
              mt: 1.5,
              fontSize: '0.9375rem',
              lineHeight: 1.6,
              color: authColors.textMuted,
            }}
          >
            {subtitle}
          </Typography>
        )}
      </Box>

      {alerts}

      <Box>{children}</Box>
    </Box>
  )
}
