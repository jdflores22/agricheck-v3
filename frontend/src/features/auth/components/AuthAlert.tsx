import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import WarningAmberIcon from '@mui/icons-material/WarningAmber'
import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { authColors } from './authTheme'

type AuthAlertVariant = 'error' | 'warning' | 'success' | 'info'

const variantStyles: Record<AuthAlertVariant, { bg: string; border: string; color: string; icon: ReactNode }> = {
  error: {
    bg: authColors.errorBg,
    border: authColors.errorBorder,
    color: authColors.errorText,
    icon: <ErrorOutlineOutlinedIcon sx={{ fontSize: 18, mt: 0.25 }} />,
  },
  warning: {
    bg: authColors.warningBg,
    border: authColors.warningBorder,
    color: authColors.warningText,
    icon: <WarningAmberIcon sx={{ fontSize: 18, mt: 0.25 }} />,
  },
  success: {
    bg: authColors.successBg,
    border: authColors.successBorder,
    color: authColors.successText,
    icon: <CheckCircleOutlinedIcon sx={{ fontSize: 18, mt: 0.25 }} />,
  },
  info: {
    bg: authColors.infoBg,
    border: '#e7e5e4',
    color: authColors.textMuted,
    icon: <InfoOutlinedIcon sx={{ fontSize: 18, mt: 0.25 }} />,
  },
}

interface AuthAlertProps {
  variant?: AuthAlertVariant
  children: ReactNode
}

export function AuthAlert({ variant = 'error', children }: AuthAlertProps) {
  const style = variantStyles[variant]

  return (
    <Box
      role="alert"
      sx={{
        display: 'flex',
        alignItems: 'flex-start',
        gap: 1.25,
        mb: 2,
        p: '0.875rem 1rem',
        borderRadius: '0.75rem',
        border: `1px solid ${style.border}`,
        bgcolor: style.bg,
        color: style.color,
        fontSize: '0.875rem',
        lineHeight: 1.5,
      }}
    >
      <Box sx={{ flexShrink: 0 }}>{style.icon}</Box>
      <Typography component="span" sx={{ fontSize: 'inherit', color: 'inherit' }}>
        {children}
      </Typography>
    </Box>
  )
}
