import { useEffect, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Stack,
  TextField,
} from '@mui/material'
import { useGetDriverProfileQuery, useUpdateDriverProfileMutation } from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function DriverProfilePage() {
  const { data } = useGetDriverProfileQuery()
  const [updateProfile, { isSuccess, isError }] = useUpdateDriverProfileMutation()
  const profile = data?.data
  const [form, setForm] = useState({
    licenseNumber: '',
    vehicleType: '',
    vehicleRegistration: '',
    phoneNumber: '',
    emergencyContact: '',
    emergencyPhone: '',
    address: '',
  })

  useEffect(() => {
    if (!profile) return
    setForm({
      licenseNumber: profile.licenseNumber ?? '',
      vehicleType: profile.vehicleType ?? '',
      vehicleRegistration: profile.vehicleRegistration ?? '',
      phoneNumber: profile.phoneNumber ?? '',
      emergencyContact: profile.emergencyContact ?? '',
      emergencyPhone: profile.emergencyPhone ?? '',
      address: profile.address ?? '',
    })
  }, [profile])

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Profile"
        title="Driver Profile"
        subtitle={`Completion: ${profile?.completionPercentage ?? 0}%`}
      />
      {isSuccess && <Alert severity="success" sx={{ mb: 2 }}>Profile updated.</Alert>}
      {isError && <Alert severity="error" sx={{ mb: 2 }}>Failed to update profile.</Alert>}

      <PortalPanel title="Profile details">
        <Stack spacing={2} sx={{ maxWidth: 560, p: 2.5 }}>
          <TextField label="License Number" value={form.licenseNumber} onChange={(e) => setForm({ ...form, licenseNumber: e.target.value })} fullWidth />
          <TextField label="Vehicle Type" value={form.vehicleType} onChange={(e) => setForm({ ...form, vehicleType: e.target.value })} fullWidth />
          <TextField label="Vehicle Registration" value={form.vehicleRegistration} onChange={(e) => setForm({ ...form, vehicleRegistration: e.target.value })} fullWidth />
          <TextField label="Phone Number" value={form.phoneNumber} onChange={(e) => setForm({ ...form, phoneNumber: e.target.value })} fullWidth />
          <TextField label="Emergency Contact" value={form.emergencyContact} onChange={(e) => setForm({ ...form, emergencyContact: e.target.value })} fullWidth />
          <TextField label="Emergency Phone" value={form.emergencyPhone} onChange={(e) => setForm({ ...form, emergencyPhone: e.target.value })} fullWidth />
          <TextField label="Address" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} fullWidth multiline minRows={2} />
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => updateProfile(form)}>Save Profile</Button>
        </Stack>
      </PortalPanel>
    </Box>
  )
}
