import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import { Box, Link, Stack, Typography } from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useForgotPasswordMutation } from '../api/authApi'
import { AuthAlert } from '../components/AuthAlert'
import { AuthFormShell } from '../components/AuthFormShell'
import { AuthSubmitButton } from '../components/AuthSubmitButton'
import { AuthTextField } from '../components/AuthTextField'
import { authColors } from '../components/authTheme'

export function ForgotPasswordPage() {
  const [forgotPassword, { isLoading, isSuccess }] = useForgotPasswordMutation()
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    try {
      await forgotPassword({ email }).unwrap()
    } catch {
      setError('Unable to process request.')
    }
  }

  return (
    <AuthFormShell
      title="Reset password"
      subtitle="Enter your email to receive reset instructions."
      alerts={
        <>
          {isSuccess && (
            <AuthAlert variant="success">
              If the email exists, a reset link has been sent.
            </AuthAlert>
          )}
          {error && <AuthAlert>{error}</AuthAlert>}
        </>
      }
    >
      <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2.25 }}>
        <AuthTextField
          label="Email address"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Enter your email address"
          required
          autoFocus
          autoComplete="email"
        />

        <AuthSubmitButton disabled={isLoading} startIcon={<EmailOutlinedIcon />}>
          {isLoading ? 'Sending...' : 'Send reset link'}
        </AuthSubmitButton>

        <Stack sx={{ pt: 1, alignItems: 'center' }}>
          <Link
            component={RouterLink}
            to="/login"
            underline="hover"
            sx={{ color: authColors.primary, fontWeight: 500, fontSize: '0.875rem' }}
          >
            Back to sign in
          </Link>
        </Stack>
      </Box>

      <Box
        sx={{
          mt: 3,
          p: '0.875rem 1rem',
          borderRadius: '0.75rem',
          border: `1px solid ${authColors.border}`,
          bgcolor: authColors.white,
        }}
      >
        <Typography sx={{ fontWeight: 600, fontSize: '0.875rem', color: authColors.textDark }}>
          Security notice
        </Typography>
        <Typography sx={{ mt: 0.5, fontSize: '0.75rem', lineHeight: 1.6, color: authColors.textMuted }}>
          Password reset links expire after 1 hour. If you do not receive an email, check your spam folder or contact
          support.
        </Typography>
      </Box>
    </AuthFormShell>
  )
}
