import PersonAddIcon from '@mui/icons-material/PersonAdd'
import { Box, Checkbox, FormControlLabel, MenuItem, Stack, Typography } from '@mui/material'
import { FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAppDispatch } from '../../../app/hooks'
import { setCredentials } from '../authSlice'
import { useRegisterMutation } from '../api/authApi'
import { AuthAlert } from '../components/AuthAlert'
import { AuthFormShell } from '../components/AuthFormShell'
import { AuthInlineLink } from '../components/AuthInlineLink'
import { AuthSubmitButton } from '../components/AuthSubmitButton'
import { AuthTextField } from '../components/AuthTextField'
import { authColors } from '../components/authTheme'

const roleOptions = [
  { value: 'ROLE_IMPORTER', label: 'Importer' },
  { value: 'ROLE_EXPORTER', label: 'Exporter' },
  { value: 'ROLE_BROKER', label: 'Broker' },
]

export function RegisterPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [register, { isLoading }] = useRegisterMutation()
  const [formError, setFormError] = useState<string | null>(null)
  const [acceptedTerms, setAcceptedTerms] = useState(false)
  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    roleCode: 'ROLE_IMPORTER',
  })

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    setFormError(null)

    if (!acceptedTerms) {
      setFormError('Please accept the Terms of Service and Privacy Policy.')
      return
    }

    try {
      const result = await register(form).unwrap()
      if (!result.success) {
        setFormError(result.errors?.[0]?.message ?? 'Registration failed.')
        return
      }

      dispatch(
        setCredentials({
          tokens: result.data.tokens,
          user: result.data.user,
          redirectPath: result.data.redirectPath,
        }),
      )
      navigate(result.data.redirectPath)
    } catch {
      setFormError('Registration failed. Please review your details.')
    }
  }

  return (
    <AuthFormShell
      title="Sign up"
      maxWidth="32rem"
      subtitle={
        <>
          Create your AgriCheck account.{' '}
          <AuthInlineLink to="/login">Sign in</AuthInlineLink> if you already have an account.
        </>
      }
      alerts={formError ? <AuthAlert>{formError}</AuthAlert> : null}
    >
      <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2.25 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2.25}>
          <AuthTextField
            label="First name"
            value={form.firstName}
            onChange={(e) => setForm({ ...form, firstName: e.target.value })}
            placeholder="Juan"
            required
            autoFocus
            autoComplete="given-name"
          />
          <AuthTextField
            label="Last name"
            value={form.lastName}
            onChange={(e) => setForm({ ...form, lastName: e.target.value })}
            placeholder="Dela Cruz"
            required
            autoComplete="family-name"
          />
        </Stack>

        <AuthTextField
          label="Email address"
          type="email"
          value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
          placeholder="you@example.com"
          required
          autoComplete="email"
          helperText="We will send a verification email to this address."
        />

        <AuthTextField
          label="Password"
          type="password"
          value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
          placeholder="Create a strong password"
          required
          autoComplete="new-password"
          helperText="Minimum 8 characters with alphanumeric and special characters."
        />

        <AuthTextField
          select
          label="Account type"
          value={form.roleCode}
          onChange={(e) => setForm({ ...form, roleCode: e.target.value })}
        >
          {roleOptions.map((option) => (
            <MenuItem key={option.value} value={option.value}>
              {option.label}
            </MenuItem>
          ))}
        </AuthTextField>

        <FormControlLabel
          control={
            <Checkbox
              checked={acceptedTerms}
              onChange={(e) => setAcceptedTerms(e.target.checked)}
              required
              sx={{ color: authColors.textMuted, '&.Mui-checked': { color: authColors.primary } }}
            />
          }
          label={
            <Typography sx={{ fontSize: '0.875rem', color: authColors.textMuted, lineHeight: 1.5 }}>
              I agree to the{' '}
              <AuthInlineLink to="#" onClick={(e) => e.preventDefault()}>
                Terms of Service
              </AuthInlineLink>{' '}
              and{' '}
              <AuthInlineLink to="#" onClick={(e) => e.preventDefault()}>
                Privacy Policy
              </AuthInlineLink>
            </Typography>
          }
          sx={{ m: 0, alignItems: 'flex-start' }}
        />

        <AuthSubmitButton disabled={isLoading} startIcon={<PersonAddIcon />}>
          {isLoading ? 'Creating account...' : 'Create account'}
        </AuthSubmitButton>
      </Box>
    </AuthFormShell>
  )
}
