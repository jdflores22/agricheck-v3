import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'

interface AdminSectionCardProps {
  title: string
  icon?: ReactNode
  variant?: 'default' | 'green'
  children: ReactNode
}

export function AdminSectionCard({ title, icon, variant = 'default', children }: AdminSectionCardProps) {
  const isGreen = variant === 'green'

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
          px: 2.5,
          py: 1.75,
          borderBottom: isGreen ? 'none' : `1px solid ${portalColors.border}`,
          bgcolor: isGreen ? portalColors.primary : portalColors.bgMuted,
          color: isGreen ? '#fff' : portalColors.textDark,
        }}
      >
        {icon && (
          <Box sx={{ display: 'inline-flex', color: isGreen ? '#fff' : portalColors.primary }}>{icon}</Box>
        )}
        <Typography sx={{ fontSize: '0.9375rem', fontWeight: 600 }}>{title}</Typography>
      </Box>
      <Box sx={{ p: { xs: 2, sm: 2.5 } }}>{children}</Box>
    </Box>
  )
}
