/** V2-aligned auth palette and layout tokens */
export const authColors = {
  primary: '#166534',
  primaryDark: '#14532d',
  textDark: '#1c1917',
  textMuted: '#78716c',
  border: '#d6d3d1',
  placeholder: '#a8a29e',
  brandBg: '#f5f5f4',
  white: '#ffffff',
  errorBg: '#fef2f2',
  errorBorder: '#fecaca',
  errorText: '#991b1b',
  successBg: '#f0fdf4',
  successBorder: '#bbf7d0',
  successText: '#166534',
  warningBg: '#fffbeb',
  warningBorder: '#fde68a',
  warningText: '#92400e',
  infoBg: '#fafaf9',
  linkDivider: '#d6d3d1',
} as const

export const authFieldSx = {
  '& .MuiInputLabel-root': {
    fontSize: '0.8125rem',
    fontWeight: 500,
    color: authColors.textDark,
    position: 'static' as const,
    transform: 'none',
    mb: 0.5,
    '&.Mui-focused': { color: authColors.textDark },
  },
  '& .MuiFormHelperText-root': {
    fontSize: '0.75rem',
    color: authColors.textMuted,
    mt: 0.5,
    mx: 0,
  },
  '& .MuiOutlinedInput-root': {
    minHeight: 48,
    borderRadius: '0.625rem',
    fontSize: '0.9375rem',
    bgcolor: authColors.white,
    '& fieldset': { borderColor: authColors.border },
    '&:hover fieldset': { borderColor: authColors.border },
    '&.Mui-focused fieldset': {
      borderColor: authColors.primary,
      borderWidth: 1,
    },
    '&.Mui-focused': {
      boxShadow: '0 0 0 3px rgba(22, 101, 52, 0.12)',
    },
  },
  '& .MuiOutlinedInput-input::placeholder': {
    color: authColors.placeholder,
    opacity: 1,
  },
}
