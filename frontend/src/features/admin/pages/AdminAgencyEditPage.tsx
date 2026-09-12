import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { Alert, Box, Button, CircularProgress } from '@mui/material'
import { FormEvent, useEffect, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { AdminAgencyForm, type AdminAgencyFormValues } from '../components/AdminAgencyForm'
import { emptyAgencyForm } from '../components/adminAgencyUtils'
import { useGetAdminAgenciesQuery, useGetAdminAgencyQuery, useUpdateAdminAgencyMutation } from '../api/adminApi'

export function AdminAgencyEditPage() {
  const { id = '' } = useParams()
  const agencyId = Number(id)
  const navigate = useNavigate()
  const { data, isLoading, error } = useGetAdminAgencyQuery(agencyId, { skip: !agencyId })
  const { data: agenciesData } = useGetAdminAgenciesQuery()
  const [updateAgency, { isLoading: saving, error: saveError }] = useUpdateAdminAgencyMutation()
  const [form, setForm] = useState<AdminAgencyFormValues>(emptyAgencyForm)

  const agency = data?.data

  useEffect(() => {
    if (!agency) return
    setForm({
      code: agency.code,
      name: agency.name,
      parentId: agency.parentId ?? '',
      description: agency.description ?? '',
      address: agency.address ?? '',
      contactNumber: agency.contactNumber ?? '',
      email: agency.email ?? '',
      isActive: agency.isActive,
    })
  }, [agency])

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    await updateAgency({
      id: agencyId,
      code: form.code.trim(),
      name: form.name.trim(),
      parentId: form.parentId === '' ? null : form.parentId,
      description: form.description.trim() || null,
      address: form.address.trim() || null,
      contactNumber: form.contactNumber.trim() || null,
      email: form.email.trim() || null,
      isActive: form.isActive,
    }).unwrap()
    navigate('/admin/agencies')
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
        <CircularProgress sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  if (error || !agency) {
    return <Alert severity="error">Agency not found.</Alert>
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Agency Management"
        title="Edit Agency"
        subtitle={agency.name}
        actions={
          <Button component={RouterLink} to="/admin/agencies" variant="outlined" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
            Back to Agencies
          </Button>
        }
      />

      {saveError ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          Unable to save agency changes. Please verify the form and try again.
        </Alert>
      ) : null}

      <AdminAgencyForm
        mode="edit"
        form={form}
        agencies={agenciesData?.data ?? []}
        agencyId={agencyId}
        saving={saving}
        onChange={setForm}
        onSubmit={(event) => void handleSubmit(event)}
      />
    </Box>
  )
}
