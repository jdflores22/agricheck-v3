import { useEffect } from 'react'
import { Alert, Box, Button, Stack, Typography } from '@mui/material'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { clientApi, useGetAccreditationSubmissionQuery, useGetClientFormQuery, useGetClientFormsQuery } from '../api/clientApi'

export function AccreditationConfirmationPage() {
  const { uuid = '' } = useParams()
  const prefetchDashboard = clientApi.usePrefetch('getDashboard')
  const { data, isLoading } = useGetAccreditationSubmissionQuery(uuid, { skip: !uuid })

  useEffect(() => {
    prefetchDashboard(undefined, { force: true })
  }, [prefetchDashboard])
  const { data: formsData } = useGetClientFormsQuery({ formType: 'ACCREDITATION' })
  const selectedFormUuid = formsData?.data?.[0]?.uuid
  const { data: formSchemaData } = useGetClientFormQuery(selectedFormUuid ?? '', { skip: !selectedFormUuid })
  const submission = data?.data

  if (isLoading) {
    return <Typography sx={{ color: portalColors.textMuted }}>Loading…</Typography>
  }

  if (!submission) {
    return <Alert severity="error">Accreditation submission not found.</Alert>
  }

  const submittedAt = submission.submittedAt
    ? new Date(submission.submittedAt).toLocaleString()
    : 'Just now'

  return (
    <Box sx={{ maxWidth: 720, mx: 'auto' }}>
      <Stack spacing={1} sx={{ mb: 3, textAlign: 'center', alignItems: 'center' }}>
        <CheckCircleOutlinedIcon sx={{ fontSize: 56, color: '#15803d' }} />
        <Typography variant="h5" sx={{ fontWeight: 600 }}>Submission Successful</Typography>
        <Typography sx={{ color: portalColors.textMuted }}>
          Your accreditation application has been submitted and is now under review.
        </Typography>
      </Stack>

      <PortalPanel title="Submission Details">
        <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
          <DetailRow label="Company" value={submission.companyName} />
          <DetailRow
            label="Submission Type"
            value={submission.submissionType === 'RENEWAL' ? 'Renewal Application' : 'New Application'}
          />
          <DetailRow label="Submitted On" value={submittedAt} />
          <DetailRow label="Form Type" value={formSchemaData?.data?.name ?? 'Accreditation Form'} />
          <DetailRow label="Status" value={submission.status} />

          <Box
            sx={{
              mt: 1,
              p: 2,
              borderRadius: '0.5rem',
              bgcolor: '#eff6ff',
              borderLeft: '4px solid #2563eb',
            }}
          >
            <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center' }}>
              <InfoOutlinedIcon sx={{ color: '#2563eb', fontSize: 20 }} />
              <Typography sx={{ fontWeight: 600, color: '#1d4ed8' }}>What Happens Next?</Typography>
            </Stack>
            <Box component="ul" sx={{ m: 0, pl: 2.5, color: portalColors.textDark }}>
              <Typography component="li" variant="body2" sx={{ mb: 0.75 }}>
                Your application will be reviewed by the accreditation team.
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 0.75 }}>
                You will receive notifications about status updates.
              </Typography>
              <Typography component="li" variant="body2" sx={{ mb: 0.75 }}>
                You may be asked to upload additional documents if needed.
              </Typography>
              <Typography component="li" variant="body2">
                Once approved, you can begin submitting entries.
              </Typography>
            </Box>
          </Box>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ pt: 1 }}>
            <Button component={RouterLink} to={`/client/accreditation/${uuid}`} variant="contained" sx={portalPrimaryButtonSx}>
              View Application
            </Button>
            <Button component={RouterLink} to="/client/accreditation" variant="outlined" sx={portalOutlinedButtonSx}>
              Accreditation Application
            </Button>
            <Button component={RouterLink} to="/client" variant="outlined" sx={portalOutlinedButtonSx}>
              Dashboard
            </Button>
          </Stack>
        </Stack>
      </PortalPanel>
    </Box>
  )
}

function DetailRow({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'space-between',
        gap: 2,
        py: 0.75,
        borderBottom: `1px solid ${portalColors.border}`,
      }}
    >
      <Typography variant="body2" sx={{ fontWeight: 600, color: portalColors.textMuted }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ textAlign: 'right' }}>{value}</Typography>
    </Box>
  )
}
