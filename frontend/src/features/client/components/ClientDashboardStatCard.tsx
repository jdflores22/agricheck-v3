import type { ReactNode } from 'react'
import { Box, Typography } from '@mui/material'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import { Link as RouterLink } from 'react-router-dom'
import { portalColors } from '../../../components/portal/portalTheme'

type StatVariant = 'default' | 'warn' | 'info' | 'danger' | 'locked'

const variantIconColors: Record<StatVariant, string> = {
  default: portalColors.primary,
  warn: '#ca8a04',
  info: '#0369a1',
  danger: '#b91c1c',
  locked: portalColors.textLight,
}

interface ClientDashboardStatCardProps {
  to?: string
  label: string
  value: string | number
  meta: string
  icon: ReactNode
  variant?: StatVariant
  locked?: boolean
  extra?: ReactNode
}

export function ClientDashboardStatCard({
  to,
  label,
  value,
  meta,
  icon,
  variant = 'default',
  locked = false,
  extra,
}: ClientDashboardStatCardProps) {
  const resolvedVariant = locked ? 'locked' : variant
  const iconColor = variantIconColors[resolvedVariant]

  const cardSx = {
    display: 'block',
    height: '100%',
    border: `1px solid ${portalColors.border}`,
    borderRadius: '0.75rem',
    bgcolor: locked ? '#fafaf9' : portalColors.bgWhite,
    p: '1rem 1.125rem',
    opacity: locked ? 0.72 : 1,
    pointerEvents: locked ? 'none' : 'auto',
    textDecoration: 'none',
    transition: 'border-color 0.15s ease, box-shadow 0.15s ease',
    ...(to && !locked
      ? {
          cursor: 'pointer',
          '&:hover': {
            borderColor: '#bbf7d0',
            boxShadow: '0 4px 14px rgba(22, 163, 74, 0.08)',
          },
        }
      : {}),
  } as const

  const content = (
    <>
      <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 1.5 }}>
        <Box>
          <Typography
            sx={{
              mb: 0.5,
              fontSize: '0.6875rem',
              fontWeight: 600,
              letterSpacing: '0.12em',
              textTransform: 'uppercase',
              color: portalColors.textMuted,
            }}
          >
            {label}
          </Typography>
          <Typography
            sx={{
              fontSize: '1.75rem',
              fontWeight: 600,
              lineHeight: 1.1,
              color: portalColors.textDark,
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            {value}
          </Typography>
          {extra}
        </Box>
        <Box sx={{ flexShrink: 0, color: iconColor, opacity: 0.45, '& .MuiSvgIcon-root': { fontSize: 22 } }}>
          {icon}
        </Box>
      </Box>
      {locked ? (
        <Typography
          component="span"
          sx={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 0.5,
            mt: 1.25,
            fontSize: '0.75rem',
            color: portalColors.textMuted,
          }}
        >
          <LockOutlinedIcon sx={{ fontSize: 14 }} />
          Locked
        </Typography>
      ) : (
        <Typography
          component="span"
          sx={{
            display: 'block',
            mt: 1.25,
            fontSize: '0.8125rem',
            fontWeight: 500,
            color: portalColors.primary,
          }}
        >
          {meta}
        </Typography>
      )}
    </>
  )

  if (to && !locked) {
    return (
      <Box component={RouterLink} to={to} sx={cardSx}>
        {content}
      </Box>
    )
  }

  return <Box sx={cardSx}>{content}</Box>
}
