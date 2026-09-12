import { Link, LinkProps } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { authColors } from './authTheme'

type AuthInlineLinkProps = LinkProps<typeof RouterLink>

export function AuthInlineLink(props: AuthInlineLinkProps) {
  return (
    <Link
      component={RouterLink}
      underline="hover"
      sx={{
        color: authColors.primary,
        fontWeight: 600,
        textDecoration: 'none',
        '&:hover': { color: authColors.primaryDark },
      }}
      {...props}
    />
  )
}
