import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import NotificationsOutlinedIcon from '@mui/icons-material/NotificationsOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import ShieldOutlinedIcon from '@mui/icons-material/ShieldOutlined'
import WorkOutlineOutlinedIcon from '@mui/icons-material/WorkOutlineOutlined'
import { Box, Button, Chip, FormControlLabel, Stack, Switch, Typography } from '@mui/material'
import { FormEvent, useEffect, useState } from 'react'
import { useAppSelector } from '../../../app/hooks'
import { selectCurrentUser } from '../authSlice'
import { useChangePasswordMutation, useMeQuery } from '../api/authApi'
import { AuthAlert } from '../components/AuthAlert'
import { AuthSubmitButton } from '../components/AuthSubmitButton'
import { AuthTextField } from '../components/AuthTextField'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalSection } from '../../../components/portal/PortalSection'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  formatRoleLabel,
  getRoleBadgeStyle,
  getStatusBadgeStyle,
} from '../../../components/portal/portalUtils'
import { useGetClientProfileQuery, useUpdateClientProfileMutation } from '../../client/api/clientApi'
import {
  useGetNotificationPreferencesQuery,
  useUpdateNotificationPreferencesMutation,
} from '../../notifications/api/notificationsApi'

export function ProfilePage() {
  const user = useAppSelector(selectCurrentUser)
  const { data, isLoading } = useMeQuery(undefined, { skip: !user })
  const profile = data?.data?.user ?? user
  const isClient = profile?.roles.some((r) => ['ROLE_IMPORTER', 'ROLE_EXPORTER', 'ROLE_BROKER'].includes(r))
  const { data: clientProfileData } = useGetClientProfileQuery(undefined, { skip: !isClient })
  const [updateProfile, { isLoading: savingProfile }] = useUpdateClientProfileMutation()
  const [changePassword, { isLoading: isChanging }] = useChangePasswordMutation()
  const [passwords, setPasswords] = useState({ currentPassword: '', newPassword: '' })
  const [profileForm, setProfileForm] = useState({ firstName: '', lastName: '', phone: '', companyName: '', address: '' })
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [profileMessage, setProfileMessage] = useState<string | null>(null)
  const [notifMessage, setNotifMessage] = useState<string | null>(null)
  const { data: notifPrefsData } = useGetNotificationPreferencesQuery(undefined, { skip: !user })
  const [updateNotifPrefs, { isLoading: savingNotifPrefs }] = useUpdateNotificationPreferencesMutation()
  const [notifPrefs, setNotifPrefs] = useState({ emailEnabled: true, inAppEnabled: true })

  useEffect(() => {
    if (clientProfileData?.data) {
      setProfileForm({
        firstName: clientProfileData.data.firstName,
        lastName: clientProfileData.data.lastName,
        phone: clientProfileData.data.phone ?? '',
        companyName: clientProfileData.data.companyName ?? '',
        address: clientProfileData.data.address ?? '',
      })
    } else if (profile) {
      setProfileForm((prev) => ({
        ...prev,
        firstName: profile.firstName ?? prev.firstName,
        lastName: profile.lastName ?? prev.lastName,
      }))
    }
  }, [clientProfileData, profile])

  useEffect(() => {
    if (notifPrefsData?.data) {
      setNotifPrefs({
        emailEnabled: notifPrefsData.data.emailEnabled,
        inAppEnabled: notifPrefsData.data.inAppEnabled,
      })
    }
  }, [notifPrefsData])

  const handleUpdateProfile = async (event: FormEvent) => {
    event.preventDefault()
    setProfileMessage(null)
    try {
      await updateProfile(profileForm).unwrap()
      setProfileMessage('Profile updated successfully.')
    } catch {
      setProfileMessage('Unable to update profile.')
    }
  }

  const handleSaveNotificationPreferences = async () => {
    setNotifMessage(null)
    try {
      await updateNotifPrefs(notifPrefs).unwrap()
      setNotifMessage('Notification preferences saved.')
    } catch {
      setNotifMessage('Unable to save notification preferences.')
    }
  }

  const handleChangePassword = async (event: FormEvent) => {
    event.preventDefault()
    setMessage(null)
    setError(null)
    try {
      const result = await changePassword(passwords).unwrap()
      if (result.success) {
        setMessage('Password updated successfully.')
        setPasswords({ currentPassword: '', newPassword: '' })
      } else {
        setError(result.errors?.[0]?.message ?? 'Unable to change password.')
      }
    } catch {
      setError('Unable to change password.')
    }
  }

  if (isLoading && !profile) {
    return (
      <Typography sx={{ py: 6, textAlign: 'center', color: portalColors.textMuted }}>
        Loading profile…
      </Typography>
    )
  }

  const visibleRoles = (profile?.roles ?? []).filter((role) => role !== 'ROLE_USER')
  const statusStyle = getStatusBadgeStyle(profile?.status ?? '')

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Account"
        title="My Profile"
        subtitle="View your personal information and manage account security"
      />

      <Box
        sx={{
          display: 'grid',
          gap: 3,
          gridTemplateColumns: { xs: '1fr', xl: '2fr 1fr' },
        }}
      >
        <Stack spacing={3}>
          <PortalSection icon={<PersonOutlineOutlinedIcon fontSize="small" />} title="Personal Information">
            {isClient ? (
              <Box component="form" onSubmit={handleUpdateProfile} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                {profileMessage && <AuthAlert variant={profileMessage.includes('success') ? 'success' : undefined}>{profileMessage}</AuthAlert>}
                <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
                  <AuthTextField label="First Name" value={profileForm.firstName} onChange={(e) => setProfileForm({ ...profileForm, firstName: e.target.value })} required />
                  <AuthTextField label="Last Name" value={profileForm.lastName} onChange={(e) => setProfileForm({ ...profileForm, lastName: e.target.value })} required />
                  <AuthTextField label="Email" value={profile?.email ?? ''} disabled fullWidth sx={{ gridColumn: { sm: '1 / -1' } }} />
                  <AuthTextField label="Phone" value={profileForm.phone} onChange={(e) => setProfileForm({ ...profileForm, phone: e.target.value })} fullWidth sx={{ gridColumn: { sm: '1 / -1' } }} />
                </Box>
                <Button type="submit" variant="contained" disabled={savingProfile} sx={{ ...portalPrimaryButtonSx, alignSelf: 'flex-start' }}>
                  Save Profile
                </Button>
              </Box>
            ) : (
              <Box sx={{ display: 'grid', gap: 2.5, gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' } }}>
                <Typography variant="body2"><strong>First Name:</strong> {profile?.firstName ?? '—'}</Typography>
                <Typography variant="body2"><strong>Last Name:</strong> {profile?.lastName ?? '—'}</Typography>
                <Typography variant="body2" sx={{ gridColumn: { sm: '1 / -1' } }}><strong>Email:</strong> {profile?.email ?? '—'}</Typography>
              </Box>
            )}
          </PortalSection>

          <PortalSection icon={<WorkOutlineOutlinedIcon fontSize="small" />} title="Business Information">
            {isClient ? (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                <AuthTextField label="Company Name" value={profileForm.companyName} onChange={(e) => setProfileForm({ ...profileForm, companyName: e.target.value })} />
                <AuthTextField label="Business Address" value={profileForm.address} onChange={(e) => setProfileForm({ ...profileForm, address: e.target.value })} multiline rows={3} />
              </Box>
            ) : (
              <Box sx={{ py: 4, textAlign: 'center' }}>
                <Typography sx={{ fontWeight: 500, color: portalColors.textDark }}>No business information</Typography>
              </Box>
            )}
          </PortalSection>

          <PortalSection icon={<NotificationsOutlinedIcon fontSize="small" />} title="Notification Preferences">
            {notifMessage && (
              <AuthAlert variant={notifMessage.includes('saved') ? 'success' : undefined}>{notifMessage}</AuthAlert>
            )}
            <Stack spacing={1}>
              <FormControlLabel
                control={
                  <Switch
                    checked={notifPrefs.inAppEnabled}
                    onChange={(e) => setNotifPrefs({ ...notifPrefs, inAppEnabled: e.target.checked })}
                  />
                }
                label="In-app notifications"
              />
              <FormControlLabel
                control={
                  <Switch
                    checked={notifPrefs.emailEnabled}
                    onChange={(e) => setNotifPrefs({ ...notifPrefs, emailEnabled: e.target.checked })}
                  />
                }
                label="Email notifications (when enabled by admin)"
              />
              <Button
                variant="contained"
                disabled={savingNotifPrefs}
                onClick={handleSaveNotificationPreferences}
                sx={{ ...portalPrimaryButtonSx, alignSelf: 'flex-start', mt: 1 }}
              >
                Save preferences
              </Button>
            </Stack>
          </PortalSection>

          <PortalSection icon={<LockOutlinedIcon fontSize="small" />} title="Change Password">
            {message && <AuthAlert variant="success">{message}</AuthAlert>}
            {error && <AuthAlert>{error}</AuthAlert>}
            {!profile?.emailVerified && (
              <AuthAlert variant="warning">
                Email not verified yet. Check API logs for the verification token in development.
              </AuthAlert>
            )}
            <Box component="form" onSubmit={handleChangePassword} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <AuthTextField
                label="Current password"
                type="password"
                value={passwords.currentPassword}
                onChange={(e) => setPasswords({ ...passwords, currentPassword: e.target.value })}
                required
                autoComplete="current-password"
              />
              <AuthTextField
                label="New password"
                type="password"
                value={passwords.newPassword}
                onChange={(e) => setPasswords({ ...passwords, newPassword: e.target.value })}
                required
                autoComplete="new-password"
                helperText="Minimum 8 characters with upper, lower, and number"
              />
              <AuthSubmitButton disabled={isChanging} sx={{ maxWidth: 240 }}>
                {isChanging ? 'Updating…' : 'Update password'}
              </AuthSubmitButton>
            </Box>
          </PortalSection>
        </Stack>

        <Stack spacing={3}>
          <PortalSection icon={<ShieldOutlinedIcon fontSize="small" />} title="Account Status">
            <Stack spacing={2.5}>
              <Box>
                <Typography
                  sx={{
                    fontSize: '0.75rem',
                    fontWeight: 600,
                    letterSpacing: '0.06em',
                    textTransform: 'uppercase',
                    color: portalColors.textMuted,
                    mb: 1,
                  }}
                >
                  Status
                </Typography>
                <Chip
                  label={profile?.status ?? 'Unknown'}
                  size="small"
                  sx={{
                    ...statusStyle,
                    fontWeight: 600,
                    fontSize: '0.75rem',
                    height: 28,
                    borderRadius: '999px',
                  }}
                />
              </Box>
              <Box>
                <Typography
                  sx={{
                    fontSize: '0.75rem',
                    fontWeight: 600,
                    letterSpacing: '0.06em',
                    textTransform: 'uppercase',
                    color: portalColors.textMuted,
                    mb: 0.5,
                  }}
                >
                  Email verification
                </Typography>
                <Typography sx={{ fontSize: '0.875rem', color: portalColors.textDark }}>
                  {profile?.emailVerified ? 'Verified' : 'Not verified'}
                </Typography>
              </Box>
              {profile?.mustChangePassword && (
                <AuthAlert variant="warning">You must change your password on next login.</AuthAlert>
              )}
            </Stack>
          </PortalSection>

          <PortalSection icon={<PersonOutlineOutlinedIcon fontSize="small" />} title="Roles">
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
              {visibleRoles.length === 0 && (
                <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                  No roles assigned.
                </Typography>
              )}
              {visibleRoles.map((role) => {
                const label = formatRoleLabel(role)
                if (!label) return null
                const style = getRoleBadgeStyle(role)
                return (
                  <Chip
                    key={role}
                    label={label}
                    size="small"
                    sx={{
                      ...style,
                      fontWeight: 600,
                      fontSize: '0.75rem',
                      height: 28,
                      borderRadius: '999px',
                    }}
                  />
                )
              })}
            </Box>
          </PortalSection>
        </Stack>
      </Box>
    </Box>
  )
}
