import LoginIcon from '@mui/icons-material/Login'
import { Box, Checkbox, FormControlLabel, Link, Stack, Typography } from '@mui/material'
import { FormEvent, useEffect, useState } from 'react'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { useAppDispatch } from '../../../app/hooks'
import { setCredentials } from '../authSlice'
import { useLoginMutation } from '../api/authApi'
import { AuthAlert } from '../components/AuthAlert'
import { AuthFormShell } from '../components/AuthFormShell'
import { AuthInlineLink } from '../components/AuthInlineLink'
import { AuthSubmitButton } from '../components/AuthSubmitButton'
import { AuthTextField } from '../components/AuthTextField'
import { authColors } from '../components/authTheme'
import { resolveDashboardPath } from '../../../components/portal/portalUtils'

const REMEMBER_EMAIL_KEY = 'agricheck.rememberEmail'

export function LoginPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [login, { isLoading, error }] = useLoginMutation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRememberMe] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  useEffect(() => {
    const savedEmail = localStorage.getItem(REMEMBER_EMAIL_KEY)
    if (savedEmail) {
      setEmail(savedEmail)
      setRememberMe(true)
    }
  }, [])

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setFormError(null)

    if (rememberMe) {
      localStorage.setItem(REMEMBER_EMAIL_KEY, email)
    } else {
      localStorage.removeItem(REMEMBER_EMAIL_KEY)
    }

    try {
      const result = await login({ email, password }).unwrap()
      if (!result.success) {
        setFormError(result.errors?.[0]?.message ?? 'Login failed.')
        return
      }

      const destination = resolveDashboardPath(result.data.redirectPath, result.data.user.roles)
      dispatch(
        setCredentials({
          tokens: result.data.tokens,
          user: result.data.user,
          redirectPath: destination,
        }),
      )
      navigate(destination)
    } catch {
      setFormError('Unable to sign in. Check your credentials and try again.')
    }
  }

  return (
    <AuthFormShell
      title="Sign in"
      subtitle={
        <>
          Sign in to AgriCheck.{' '}
          <AuthInlineLink to="/register">Register</AuthInlineLink> if you need an account.
        </>
      }
      alerts={
        (formError || error) ? (
          <AuthAlert>{formError ?? 'Login failed.'}</AuthAlert>
        ) : null
      }
    >
      <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2.25 }}>
        <AuthTextField
          label="Email address"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="you@example.com"
          required
          autoFocus
          autoComplete="email"
        />
        <AuthTextField
          label="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Enter your password"
          required
          autoComplete="current-password"
        />

        <FormControlLabel
          control={
            <Checkbox
              checked={rememberMe}
              onChange={(e) => setRememberMe(e.target.checked)}
              sx={{ color: authColors.textMuted, '&.Mui-checked': { color: authColors.primary } }}
            />
          }
          label={
            <Typography sx={{ fontSize: '0.875rem', color: authColors.textMuted }}>
              Remember me
            </Typography>
          }
          sx={{ m: 0, alignItems: 'flex-start' }}
        />

        <AuthSubmitButton disabled={isLoading} startIcon={<LoginIcon />}>
          {isLoading ? 'Signing in...' : 'Sign in'}
        </AuthSubmitButton>

        <Stack
          direction="row"
          spacing={1}
          sx={{ pt: 0.5, fontSize: '0.875rem', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'center' }}
        >
          <Link
            component={RouterLink}
            to="/forgot-password"
            underline="hover"
            sx={{ color: authColors.primary, fontWeight: 500, fontSize: 'inherit' }}
          >
            Forgot your password?
          </Link>
          <Typography component="span" sx={{ color: authColors.linkDivider, fontSize: 'inherit' }}>
            ·
          </Typography>
          <Link
            component={RouterLink}
            to="/resend-verification"
            underline="hover"
            sx={{ color: authColors.primary, fontWeight: 500, fontSize: 'inherit' }}
          >
            Resend verification email
          </Link>
        </Stack>
      </Box>
    </AuthFormShell>
  )
}
