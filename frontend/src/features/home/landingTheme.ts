import { portalColors } from '../../components/portal/portalTheme'

export const landingColors = {
  ...portalColors,
  grassDark: '#16a34a',
  heroGradient:
    'radial-gradient(circle at 15% 20%, rgba(22, 101, 52, 0.08) 0%, transparent 42%), radial-gradient(circle at 85% 80%, rgba(22, 163, 74, 0.06) 0%, transparent 40%), linear-gradient(180deg, #ffffff 0%, #fafaf9 100%)',
  ctaGradient: `linear-gradient(135deg, ${portalColors.primaryDark} 0%, ${portalColors.primary} 55%, #16a34a 100%)`,
} as const

export const landingContainerSx = {
  width: '100%',
  maxWidth: '72rem',
  mx: 'auto',
  px: { xs: 2.5, sm: 2.5 },
} as const

export const landingBtnPrimarySx = {
  display: 'inline-flex',
  alignItems: 'center',
  justifyContent: 'center',
  gap: 1,
  minHeight: 44,
  px: 2.25,
  py: 1.25,
  borderRadius: '0.625rem',
  fontSize: '0.9375rem',
  fontWeight: 600,
  textDecoration: 'none',
  border: `1px solid ${landingColors.primary}`,
  bgcolor: landingColors.primary,
  color: '#fff',
  transition: 'background-color 0.15s ease, border-color 0.15s ease',
  '&:hover': {
    bgcolor: landingColors.primaryDark,
    borderColor: landingColors.primaryDark,
    color: '#fff',
  },
} as const

export const landingBtnSecondarySx = {
  ...landingBtnPrimarySx,
  bgcolor: '#fff',
  color: landingColors.textDark,
  borderColor: landingColors.borderStrong,
  '&:hover': {
    bgcolor: '#fff',
    borderColor: landingColors.primary,
    color: landingColors.primary,
  },
} as const
