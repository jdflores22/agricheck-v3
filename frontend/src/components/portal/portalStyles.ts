import type { SxProps, Theme } from '@mui/material'
import { portalColors } from './portalTheme'

export const portalPrimaryButtonSx: SxProps<Theme> = {
  bgcolor: portalColors.primary,
  textTransform: 'none',
  fontWeight: 600,
  borderRadius: '0.5rem',
  boxShadow: 'none',
  '&:hover': { bgcolor: portalColors.primaryDark, boxShadow: 'none' },
}

export const portalOutlinedButtonSx: SxProps<Theme> = {
  borderColor: portalColors.borderStrong,
  color: portalColors.textDark,
  textTransform: 'none',
  fontWeight: 500,
  borderRadius: '0.5rem',
  '&:hover': {
    borderColor: portalColors.primary,
    bgcolor: portalColors.bgMuted,
    color: portalColors.primary,
  },
}

export const portalTableHeadCellSx = {
  fontWeight: 600,
  fontSize: '0.8125rem',
  color: portalColors.textDark,
  bgcolor: portalColors.bgMuted,
  borderBottom: `1px solid ${portalColors.border}`,
} as const

export const portalEmptyStateSx = {
  py: 4,
  textAlign: 'center',
  color: portalColors.textMuted,
  fontSize: '0.875rem',
} as const
