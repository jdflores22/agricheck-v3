import { Button, ButtonProps } from '@mui/material'
import { authColors } from './authTheme'

export function AuthSubmitButton({ children, sx, ...props }: ButtonProps) {
  return (
    <Button
      type="submit"
      fullWidth
      disableElevation
      sx={{
        mt: 0.5,
        minHeight: 48,
        borderRadius: '0.625rem',
        bgcolor: authColors.primary,
        color: '#fff',
        fontSize: '1rem',
        fontWeight: 600,
        textTransform: 'none',
        gap: 1,
        '&:hover': { bgcolor: authColors.primaryDark },
        '&.Mui-disabled': { opacity: 0.65, color: '#fff', bgcolor: authColors.primary },
        ...sx,
      }}
      {...props}
    >
      {children}
    </Button>
  )
}
