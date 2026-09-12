import type { ReactNode } from 'react'
import { Box, Typography } from '@mui/material'
import { portalColors } from '../../../components/portal/portalTheme'

interface ClientSectionCardProps {
  title: string
  icon?: ReactNode
  headerAction?: ReactNode
  children: ReactNode
}

export function ClientSectionCard({ title, icon, headerAction, children }: ClientSectionCardProps) {
  return (
    <Box
      component="article"
      sx={{
        border: `1px solid ${portalColors.border}`,
        borderRadius: '0.75rem',
        bgcolor: portalColors.bgWhite,
        overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          justifyContent: 'space-between',
          gap: 1.5,
          px: { xs: 2, sm: 2.5 },
          py: 1.5,
          borderBottom: '1px solid #f5f5f4',
        }}
      >
        <Typography
          component="h3"
          sx={{
            m: 0,
            fontSize: '0.9375rem',
            fontWeight: 600,
            color: portalColors.textDark,
            display: 'flex',
            alignItems: 'center',
            gap: 0.75,
            '& .MuiSvgIcon-root': { fontSize: 18, color: '#16a34a' },
          }}
        >
          {icon}
          {title}
        </Typography>
        {headerAction}
      </Box>
      <Box sx={{ p: { xs: 2, sm: 2.5 } }}>{children}</Box>
    </Box>
  )
}

export function ClientFieldLabel({ children }: { children: ReactNode }) {
  return (
    <Typography
      sx={{
        display: 'block',
        mb: 0.5,
        fontSize: '0.75rem',
        fontWeight: 600,
        letterSpacing: '0.06em',
        textTransform: 'uppercase',
        color: portalColors.textMuted,
      }}
    >
      {children}
    </Typography>
  )
}

export function ClientDetailGrid({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' },
        gap: 2.5,
      }}
    >
      {children}
    </Box>
  )
}

export function ClientDetailItem({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box>
      <ClientFieldLabel>{label}</ClientFieldLabel>
      {children}
    </Box>
  )
}

export const clientPrimaryButtonSx = {
  minHeight: 44,
  px: 2,
  py: 1.25,
  borderRadius: '0.5rem',
  fontSize: '0.875rem',
  fontWeight: 500,
  textTransform: 'none',
  color: '#fff',
  border: '1px solid #16a34a',
  background: `linear-gradient(135deg, #16a34a 0%, ${portalColors.primary} 100%)`,
  boxShadow: 'none',
  '&:hover': {
    background: 'linear-gradient(135deg, #15803d 0%, #16a34a 100%)',
    borderColor: '#15803d',
    boxShadow: 'none',
  },
} as const

export const clientOutlineButtonSx = {
  minHeight: 44,
  px: 2,
  py: 1.25,
  borderRadius: '0.5rem',
  fontSize: '0.875rem',
  fontWeight: 500,
  textTransform: 'none',
  color: portalColors.textDark,
  borderColor: portalColors.border,
  bgcolor: '#fff',
  '&:hover': {
    bgcolor: portalColors.bgPage,
    borderColor: '#bbf7d0',
    color: '#16a34a',
  },
} as const

export const clientBadgeSx = {
  display: 'inline-flex',
  alignItems: 'center',
  px: 1.25,
  py: 0.5,
  borderRadius: '0.375rem',
  bgcolor: portalColors.bgMuted,
  border: `1px solid ${portalColors.border}`,
  fontSize: '0.875rem',
  fontWeight: 600,
  color: portalColors.textDark,
} as const
