import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import { Box, Link, Stack } from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { useResetPasswordMutation } from '../api/authApi'
import { AuthAlert } from '../components/AuthAlert'
import { AuthFormShell } from '../components/AuthFormShell'
import { AuthSubmitButton } from '../components/AuthSubmitButton'
import { AuthTextField } from '../components/AuthTextField'
import { authColors } from '../components/authTheme'

export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const [resetPassword, { isLoading, isSuccess }] = useResetPasswordMutation()
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const token = searchParams.get('token') ?? ''

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setError(null)

    if (password !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    try {
      const result = await resetPassword({ token, newPassword: password }).unwrap()
      if (!result.success) {
        setError(result.errors?.[0]?.message ?? 'Reset failed.')
      }
    } catch {
      setError('Unable to reset password.')
    }
  }

  return (
    <AuthFormShell
      title="Reset password"
      subtitle="Enter your new password below."
      alerts={
        <>
          {isSuccess && (
            <AuthAlert variant="success">Password updated. You can now sign in.</AuthAlert>
          )}
          {error && <AuthAlert>{error}</AuthAlert>}
        </>
      }
    >
      <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2.25 }}>
        <AuthTextField
          label="New password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Enter your new password"
          required
          autoFocus
          helperText="Minimum 8 characters with letters, numbers, and special characters."
        />
        <AuthTextField
          label="Confirm password"
          type="password"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          placeholder="Confirm your new password"
          required
        />

        <AuthSubmitButton disabled={isLoading || !token} startIcon={<CheckCircleOutlinedIcon />}>
          {isLoading ? 'Updating...' : 'Reset password'}
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
