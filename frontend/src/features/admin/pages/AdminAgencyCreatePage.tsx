import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Alert, Box, Button } from '@mui/material'
import { FormEvent, useState } from 'react'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { AdminAgencyForm, type AdminAgencyFormValues } from '../components/AdminAgencyForm'
import { emptyAgencyForm } from '../components/adminAgencyUtils'
import { useCreateAdminAgencyMutation, useGetAdminAgenciesQuery } from '../api/adminApi'

export function AdminAgencyCreatePage() {
  const navigate = useNavigate()
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [createAgency, { isLoading: saving, error }] = useCreateAdminAgencyMutation()
  const [form, setForm] = useState<AdminAgencyFormValues>(emptyAgencyForm)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    await createAgency({
      code: form.code.trim(),
      name: form.name.trim(),
      parentId: form.parentId === '' ? null : form.parentId,
      description: form.description.trim() || null,
      address: form.address.trim() || null,
      contactNumber: form.contactNumber.trim() || null,
      email: form.email.trim() || null,
    }).unwrap()
    navigate('/admin/agencies')
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Agency Management"
        title="Create New Agency"
        subtitle="Add a government agency to the system"
        actions={
          <Button component={RouterLink} to="/admin/agencies" variant="outlined" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
            Back to Agencies
          </Button>
        }
      />

      {error ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          Unable to create agency. Please verify the form and try again.
        </Alert>
      ) : null}

      <AdminAgencyForm
        mode="create"
        form={form}
        agencies={agenciesData?.data ?? []}
        saving={saving}
        onChange={setForm}
        onSubmit={(event) => void handleSubmit(event)}
      />
    </Box>
  )
}
