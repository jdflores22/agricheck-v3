/** V2-aligned portal palette */
export const portalColors = {
  primary: '#166534',
  primaryDark: '#14532d',
  navActive: '#166534',
  textDark: '#1c1917',
  textMuted: '#78716c',
  textLight: '#a8a29e',
  border: '#e7e5e4',
  borderStrong: '#d6d3d1',
  bgPage: '#fafaf9',
  bgWhite: '#ffffff',
  bgMuted: '#f5f5f4',
  successSoft: '#f0fdf4',
  successText: '#166534',
} as const

/** Green shades for charts and analytics — stays within the portal brand */
export const portalAnalyticsColors = {
  darkest: '#14532d',
  dark: '#166534',
  base: '#15803d',
  mid: '#16a34a',
  light: '#22c55e',
  pale: '#4ade80',
  track: '#e7e5e4',
  soft: '#f0fdf4',
  softMid: '#dcfce7',
  softStrong: '#bbf7d0',
} as const

export const portalNavItemSx = {
  display: 'flex',
  alignItems: 'center',
  gap: 1.5,
  width: '100%',
  minHeight: 44,
  px: 1.5,
  py: 1.25,
  borderRadius: '0.5rem',
  fontSize: '0.875rem',
  fontWeight: 500,
  color: portalColors.textMuted,
  textDecoration: 'none',
  transition: 'background-color 0.15s ease, color 0.15s ease',
  '&:hover': {
    bgcolor: portalColors.bgMuted,
    color: portalColors.primary,
  },
  '&.active': {
    bgcolor: portalColors.navActive,
    color: '#ffffff',
    '& .MuiSvgIcon-root': { color: '#ffffff' },
  },
  '& .MuiSvgIcon-root': {
    fontSize: 20,
    color: 'inherit',
  },
} as const
