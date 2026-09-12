import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import { Box, Link, Stack } from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useResendVerificationMutation } from '../api/authApi'
import { AuthAlert } from '../components/AuthAlert'
import { AuthFormShell } from '../components/AuthFormShell'
import { AuthSubmitButton } from '../components/AuthSubmitButton'
import { AuthTextField } from '../components/AuthTextField'
import { authColors } from '../components/authTheme'

export function ResendVerificationPage() {
  const [resend, { isLoading, isSuccess }] = useResendVerificationMutation()
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)
    try {
      await resend({ email }).unwrap()
    } catch {
      setError('Unable to process request.')
    }
  }

  return (
    <AuthFormShell
      title="Resend verification"
      subtitle="Enter your email and we will send a new verification link if your account is unverified."
      alerts={
        <>
          {isSuccess && (
            <AuthAlert variant="success">
              If the account exists and is unverified, a new verification email has been sent.
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
          placeholder="you@example.com"
          required
          autoFocus
          autoComplete="email"
        />

        <AuthSubmitButton disabled={isLoading} startIcon={<EmailOutlinedIcon />}>
          {isLoading ? 'Sending...' : 'Resend verification email'}
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
    </AuthFormShell>
  )
}
