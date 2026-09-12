import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import DescriptionOutlinedIcon from '@mui/icons-material/DescriptionOutlined'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import HourglassEmptyOutlinedIcon from '@mui/icons-material/HourglassEmptyOutlined'
import SendOutlinedIcon from '@mui/icons-material/SendOutlined'
import ShieldOutlinedIcon from '@mui/icons-material/ShieldOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { Link as RouterLink, useNavigate } from 'react-router-dom'
import { FormEvent, useMemo, useState } from 'react'
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import type { ApiEnvelope } from '../../auth/types'
import {
  useCreateAccreditationMutation,
  useGetAccreditationSubmissionQuery,
  useGetAccreditationSubmissionsQuery,
} from '../api/clientApi'
import { AccreditationStatusTimeline } from '../components/AccreditationStatusTimeline'
import { AccreditationStatusChip } from '../../accreditation/AccreditationStatusChip'
import { isResubmittedForReview } from '../../accreditation/accreditationStatusUtils'
import { downloadAuthenticatedFile } from '../utils/downloadFile'

const ACCREDITATION_BENEFITS = [
  'Submit entries to DA agencies (BAI, BFAR, BPI, and more)',
  'Track your submissions in real time',
  'Receive inspection schedules and results',
  'Access compliance documents and certificates',
  'Manage warehouse bookings and containers',
]

const PROGRESS_STEPS = [
  { label: 'Draft', statuses: ['Draft'] },
  { label: 'Submitted', statuses: ['Submitted'] },
  { label: 'Under Review', statuses: ['UnderReview', 'RevisionRequired'] },
  { label: 'Approved', statuses: ['Approved'] },
] as const

type ListApplication = {
  uuid: string
  companyName: string
  submissionType: string
  status: string
  displayStatus?: string
  submittedAt?: string
  createdAt: string
}

function getApiErrorMessage(error: unknown): string | null {
  if (!error || typeof error !== 'object' || !('data' in error)) {
    return null
  }

  const data = (error as FetchBaseQueryError).data as ApiEnvelope<unknown> | undefined
  return data?.errors?.[0]?.message ?? null
}

function getApiErrorCode(error: unknown): string | null {
  if (!error || typeof error !== 'object' || !('data' in error)) {
    return null
  }

  const data = (error as FetchBaseQueryError).data as ApiEnvelope<unknown> | undefined
  return data?.errors?.[0]?.code ?? null
}

function formatStatus(status: string) {
  return status.replace(/([A-Z])/g, ' $1').trim()
}

function formatSubmissionType(type: string) {
  return type === 'RENEWAL' ? 'Renewal Application' : 'New Application'
}

function getActiveStepIndex(status: string) {
  if (status === 'Rejected') return 2
  const index = PROGRESS_STEPS.findIndex((step) => step.statuses.includes(status as never))
  return index >= 0 ? index : 0
}

function MetaTile({ label, value, sub }: { label: string; value: string; sub?: string }) {
  return (
    <Box
      sx={{
        p: 2,
        borderRadius: '0.5rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgWhite,
      }}
    >
      <Typography variant="caption" sx={{ color: portalColors.textMuted, fontWeight: 600, textTransform: 'uppercase' }}>
        {label}
      </Typography>
      <Typography sx={{ fontWeight: 600, mt: 0.5 }}>{value}</Typography>
      {sub ? (
        <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
          {sub}
        </Typography>
      ) : null}
    </Box>
  )
}

function ApplicationProgressStepper({ status }: { status: string }) {
  const activeIndex = getActiveStepIndex(status)
  const rejected = status === 'Rejected'

  return (
    <Box sx={{ py: 1 }}>
      <Box sx={{ display: 'grid', gridTemplateColumns: `repeat(${PROGRESS_STEPS.length}, 1fr)`, gap: 1 }}>
        {PROGRESS_STEPS.map((step, index) => {
          const completed = !rejected && index < activeIndex
          const active = !rejected && index === activeIndex
          const revisionActive = status === 'RevisionRequired' && index === 2

          return (
            <Box key={step.label} sx={{ textAlign: 'center', position: 'relative' }}>
              {index < PROGRESS_STEPS.length - 1 ? (
                <Box
                  sx={{
                    position: 'absolute',
                    top: 16,
                    left: '50%',
                    width: '100%',
                    height: 2,
                    bgcolor: completed ? portalColors.primary : portalColors.border,
                    zIndex: 0,
                  }}
                />
              ) : null}
              <Box
                sx={{
                  width: 32,
                  height: 32,
                  mx: 'auto',
                  mb: 1,
                  borderRadius: '50%',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  position: 'relative',
                  zIndex: 1,
                  bgcolor: completed || active ? portalColors.primary : portalColors.bgMuted,
                  color: completed || active ? '#fff' : portalColors.textMuted,
                  border: revisionActive ? '2px solid #f59e0b' : 'none',
                }}
              >
                {completed ? <CheckCircleOutlinedIcon sx={{ fontSize: 18 }} /> : <Typography sx={{ fontSize: '0.75rem', fontWeight: 700 }}>{index + 1}</Typography>}
              </Box>
              <Typography
                sx={{
                  fontSize: '0.75rem',
                  fontWeight: active || revisionActive ? 700 : 500,
                  color: active || revisionActive ? portalColors.textDark : portalColors.textMuted,
                }}
              >
                {step.label}
              </Typography>
            </Box>
          )
        })}
      </Box>
      {rejected ? (
        <Typography variant="caption" sx={{ color: '#b91c1c', display: 'block', mt: 1.5 }}>
          Application was not approved. Review the officer comments on your application details.
        </Typography>
      ) : null}
    </Box>
  )
}

function StatusGuidance({
  application,
  reviewComments,
  resubmitted,
}: {
  application: ListApplication
  reviewComments?: string
  resubmitted?: boolean
}) {
  const { status, uuid } = application

  if (status === 'Draft') {
    return (
      <Alert severity="info" icon={<EditOutlinedIcon />}>
        You have a draft application that has not been submitted yet. Continue filling out the form and submit when ready.
      </Alert>
    )
  }

  if (resubmitted) {
    return (
      <Alert severity="success" icon={<CheckCircleOutlinedIcon />}>
        Your revised documents were resubmitted successfully. Your application is now under review again. We will notify you when the accreditation team updates your status.
      </Alert>
    )
  }

  if (status === 'Submitted' || status === 'UnderReview') {
    return (
      <Alert severity="success" icon={<CheckCircleOutlinedIcon />}>
        Your application has been submitted and is under review. We will notify you when the accreditation team updates your status.
      </Alert>
    )
  }

  if (status === 'RevisionRequired') {
    return (
      <Alert
        severity="warning"
        icon={<WarningAmberOutlinedIcon />}
        action={
          <Button color="inherit" size="small" component={RouterLink} to={`/client/accreditation/${uuid}/compliance`}>
            Open compliance
          </Button>
        }
      >
        Document revisions are required before your application can proceed.
        {reviewComments ? ` ${reviewComments}` : ''}
      </Alert>
    )
  }

  if (status === 'Approved') {
    return (
      <Alert severity="success" icon={<VerifiedOutlinedIcon />}>
        Your accreditation is approved. You can now submit entries and use all client portal features.
      </Alert>
    )
  }

  if (status === 'Rejected') {
    return (
      <Alert severity="error" icon={<WarningAmberOutlinedIcon />}>
        Your application was not approved.
        {reviewComments ? ` ${reviewComments}` : ' Please review the details and contact support if you need assistance.'}
      </Alert>
    )
  }

  return (
    <Alert severity="info" icon={<HourglassEmptyOutlinedIcon />}>
      Current status: {formatStatus(status)}.
    </Alert>
  )
}

function NextStepsList({ status, resubmitted }: { status: string; resubmitted?: boolean }) {
  const items =
    status === 'Draft'
      ? [
          'Complete all required fields in the accreditation form.',
          'Upload all required supporting documents.',
          'Review your information, then submit for DA review.',
        ]
      : resubmitted
        ? [
            'Your revised documents are now with the accreditation team.',
            'The officer will review your updated files.',
            'You will be notified if further revisions are needed or when a decision is made.',
          ]
      : status === 'Submitted' || status === 'UnderReview'
        ? [
            'Our team will review your submitted documents and information.',
            'You may be contacted if additional information is needed.',
            'Once approved, you will receive full access to submit entries.',
            'The review process typically takes 5–10 business days.',
          ]
        : status === 'RevisionRequired'
          ? [
              'Review the officer comments for each document.',
              'Upload corrected versions through the compliance page.',
              'Resubmit compliance when all revisions are complete.',
            ]
          : status === 'Approved'
            ? [
                'Start submitting entries to DA agencies.',
                'Track inspections, bills, and certificates from your dashboard.',
                'Renew your accreditation before it expires.',
              ]
            : []

  if (items.length === 0) return null

  return (
    <Box>
      <Typography sx={{ fontWeight: 600, fontSize: '0.95rem', mb: 1.5 }}>
        {status === 'Draft' ? 'Before you submit' : 'What happens next'}
      </Typography>
      <Stack spacing={1}>
        {items.map((item) => (
          <Box key={item} sx={{ display: 'flex', gap: 1, alignItems: 'flex-start' }}>
            <CheckCircleOutlinedIcon sx={{ fontSize: 16, color: portalColors.primary, mt: 0.25, flexShrink: 0 }} />
            <Typography variant="body2" sx={{ color: portalColors.textDark }}>
              {item}
            </Typography>
          </Box>
        ))}
      </Stack>
    </Box>
  )
}

function StartApplicationPanel({
  creating,
  errorMessage,
  companyName,
  onCompanyNameChange,
  onSubmit,
}: {
  creating: boolean
  errorMessage: string | null
  companyName: string
  onCompanyNameChange: (value: string) => void
  onSubmit: (event: FormEvent) => void
}) {
  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1.4fr) minmax(280px, 0.8fr)' },
        gap: 3,
        alignItems: 'start',
      }}
    >
      <PortalPanel title="Why accreditation matters">
        <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
          <Typography sx={{ color: portalColors.textMuted, fontSize: '0.9375rem' }}>
            To access the full features of AgriCheck and submit entries to DA agencies, complete the DA accreditation process first.
          </Typography>
          <Typography sx={{ fontWeight: 600, fontSize: '0.9375rem' }}>After accreditation, you can:</Typography>
          <Stack spacing={1}>
            {ACCREDITATION_BENEFITS.map((item) => (
              <Box key={item} sx={{ display: 'flex', gap: 1, alignItems: 'flex-start' }}>
                <CheckCircleOutlinedIcon sx={{ fontSize: 16, color: portalColors.primary, mt: 0.25 }} />
                <Typography variant="body2">{item}</Typography>
              </Box>
            ))}
          </Stack>
          <Alert severity="info">
            You can only have one accreditation application per account. The review process typically takes 3–5 business days.
          </Alert>
        </Stack>
      </PortalPanel>

      <PortalPanel title="Start your application">
        <Box component="form" onSubmit={onSubmit} sx={{ px: 2.5, py: 2 }}>
          <Stack spacing={2}>
            <Box
              sx={{
                display: 'flex',
                flexDirection: 'column',
                alignItems: 'center',
                textAlign: 'center',
                p: 2,
                border: `1px dashed ${portalColors.borderStrong}`,
                borderRadius: '0.75rem',
                bgcolor: portalColors.bgMuted,
              }}
            >
              <ShieldOutlinedIcon sx={{ fontSize: 36, color: portalColors.primary, mb: 1 }} />
              <Typography sx={{ fontWeight: 600, mb: 0.5 }}>Ready to get started?</Typography>
              <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
                Enter your company name to create your single accreditation application.
              </Typography>
            </Box>
            {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : null}
            <TextField
              label="Company Name"
              value={companyName}
              onChange={(e) => onCompanyNameChange(e.target.value)}
              required
              fullWidth
            />
            <Button type="submit" variant="contained" fullWidth sx={portalPrimaryButtonSx} disabled={creating}>
              Start Application
            </Button>
          </Stack>
        </Box>
      </PortalPanel>
    </Box>
  )
}

function AccreditedApplicationHub({ application }: { application: ListApplication }) {
  const { data: detailData } = useGetAccreditationSubmissionQuery(application.uuid)
  const submission = detailData?.data
  const submittedAt = application.submittedAt ? new Date(application.submittedAt) : null
  const approvedAt = submission?.history
    ?.filter((item) => item.status === 'Approved')
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0]
  const approvedDate = approvedAt ? new Date(approvedAt.createdAt) : null

  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 2fr) minmax(280px, 1fr)' },
        gap: 3,
        alignItems: 'start',
      }}
    >
      <Stack spacing={3}>
        <Box
          sx={{
            display: 'flex',
            flexWrap: 'wrap',
            alignItems: 'center',
            gap: '0.75rem 1.25rem',
            px: 2,
            py: 1.75,
            border: `1px solid ${portalColors.border}`,
            borderRadius: '0.75rem',
            bgcolor: portalColors.bgWhite,
          }}
        >
          <Chip
            size="small"
            icon={<VerifiedOutlinedIcon sx={{ fontSize: '14px !important' }} />}
            label="Accredited"
            sx={{ bgcolor: '#dcfce7', color: portalColors.primary, fontWeight: 600 }}
          />
          {submission?.accreditationNumber ? (
            <Box>
              <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted }}>
                Accreditation Number
              </Typography>
              <Typography sx={{ fontSize: '0.9375rem', fontWeight: 600 }}>{submission.accreditationNumber}</Typography>
            </Box>
          ) : null}
          <Box>
            <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted }}>
              Company
            </Typography>
            <Typography sx={{ fontSize: '0.9375rem', fontWeight: 600 }}>{application.companyName}</Typography>
          </Box>
          {submission?.certificateNumber ? (
            <Box>
              <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted }}>
                Certificate
              </Typography>
              <Typography sx={{ fontSize: '0.9375rem', fontWeight: 600 }}>{submission.certificateNumber}</Typography>
            </Box>
          ) : null}
        </Box>

        <Alert severity="success" icon={<VerifiedOutlinedIcon />}>
          Your company is accredited with the Department of Agriculture. You can submit entries, track inspections, and access all client portal features.
        </Alert>

        <PortalPanel title="Accreditation Details">
          <Stack spacing={2.5} sx={{ px: 2.5, py: 2 }}>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
              <AccreditationStatusChip
                status={application.status}
                displayStatus={application.displayStatus}
                history={submission?.history}
              />
              <Chip size="small" label={formatSubmissionType(application.submissionType)} variant="outlined" />
            </Box>

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                gap: 1.5,
              }}
            >
              <MetaTile label="Company" value={application.companyName} />
              {submission?.accreditationNumber ? (
                <MetaTile label="Accreditation No." value={submission.accreditationNumber} />
              ) : null}
              <MetaTile
                label="Submitted On"
                value={submittedAt ? submittedAt.toLocaleDateString(undefined, { dateStyle: 'medium' }) : '—'}
                sub={submittedAt ? submittedAt.toLocaleTimeString(undefined, { timeStyle: 'short' }) : undefined}
              />
              <MetaTile
                label="Approved On"
                value={approvedDate ? approvedDate.toLocaleDateString(undefined, { dateStyle: 'medium' }) : '—'}
                sub={approvedDate ? approvedDate.toLocaleTimeString(undefined, { timeStyle: 'short' }) : undefined}
              />
            </Box>

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ flexWrap: 'wrap' }}>
              {submission?.certificateUuid ? (
                <>
                  <Button
                    component={RouterLink}
                    to={`/client/certificates/${submission.certificateUuid}`}
                    variant="contained"
                    sx={portalPrimaryButtonSx}
                    startIcon={<DescriptionOutlinedIcon />}
                  >
                    View Certificate
                  </Button>
                  <Button
                    variant="outlined"
                    sx={portalOutlinedButtonSx}
                    startIcon={<DownloadOutlinedIcon />}
                    onClick={() =>
                      downloadAuthenticatedFile(
                        `/certificates/${submission.certificateUuid}/pdf`,
                        `certificate-${submission.certificateNumber ?? submission.certificateUuid}.pdf`,
                      )
                    }
                  >
                    Download PDF
                  </Button>
                </>
              ) : null}
              <Button
                component={RouterLink}
                to={`/client/accreditation/${application.uuid}`}
                variant="outlined"
                sx={portalOutlinedButtonSx}
              >
                View Application Record
              </Button>
            </Stack>
          </Stack>
        </PortalPanel>
      </Stack>

      <Stack spacing={3}>
        <PortalPanel title="Quick Actions">
          <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
            <Button component={RouterLink} to="/client/entries/agencies" variant="contained" fullWidth sx={portalPrimaryButtonSx}>
              Submit New Entry
            </Button>
            {submission?.certificateUuid ? (
              <Button
                component={RouterLink}
                to={`/client/certificates/${submission.certificateUuid}`}
                variant="outlined"
                fullWidth
                sx={portalOutlinedButtonSx}
              >
                View Certificate
              </Button>
            ) : null}
            <Button
              component={RouterLink}
              to={`/client/accreditation/${application.uuid}`}
              variant="outlined"
              fullWidth
              sx={portalOutlinedButtonSx}
            >
              Application Record
            </Button>
            <Button component={RouterLink} to="/client" variant="outlined" fullWidth sx={portalOutlinedButtonSx}>
              Back to Dashboard
            </Button>
          </Stack>
        </PortalPanel>

        <PortalPanel title="Recent Activity">
          <Box sx={{ px: 2.5, py: 2 }}>
            {submission?.history?.length ? (
              <AccreditationStatusTimeline history={submission.history} emptyMessage="No activity recorded yet." />
            ) : (
              <Stack spacing={1.5} sx={{ alignItems: 'center', py: 2 }}>
                <SendOutlinedIcon sx={{ color: portalColors.textLight }} />
                <Typography variant="body2" sx={{ color: portalColors.textMuted, textAlign: 'center' }}>
                  Loading activity…
                </Typography>
              </Stack>
            )}
          </Box>
        </PortalPanel>
      </Stack>
    </Box>
  )
}

function ExistingApplicationHub({ application }: { application: ListApplication }) {
  const { data: detailData } = useGetAccreditationSubmissionQuery(application.uuid)
  const submission = detailData?.data
  const submittedAt = application.submittedAt ? new Date(application.submittedAt) : null
  const createdAt = new Date(application.createdAt)
  const resubmitted = isResubmittedForReview({
    status: application.status,
    displayStatus: application.displayStatus,
    history: submission?.history,
  })
  const primaryLabel =
    application.status === 'Draft'
      ? 'Continue Application'
      : application.status === 'RevisionRequired'
        ? 'Submit Compliance'
        : 'View Application'
  const primaryPath =
    application.status === 'RevisionRequired'
      ? `/client/accreditation/${application.uuid}/compliance`
      : `/client/accreditation/${application.uuid}`

  return (
    <Box
      sx={{
        display: 'grid',
        gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 2fr) minmax(280px, 1fr)' },
        gap: 3,
        alignItems: 'start',
      }}
    >
      <Stack spacing={3}>
        <PortalPanel title="Current Application">
          <Stack spacing={2.5} sx={{ px: 2.5, py: 2 }}>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, alignItems: 'center' }}>
              <AccreditationStatusChip
                status={application.status}
                displayStatus={application.displayStatus}
                history={submission?.history}
              />
              <Chip size="small" label={formatSubmissionType(application.submissionType)} variant="outlined" />
            </Box>

            <StatusGuidance application={application} reviewComments={submission?.reviewComments} resubmitted={resubmitted} />
            <ApplicationProgressStepper status={application.status} />

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' },
                gap: 1.5,
              }}
            >
              <MetaTile label="Company" value={application.companyName} />
              <MetaTile
                label="Started On"
                value={createdAt.toLocaleDateString(undefined, { dateStyle: 'medium' })}
                sub={createdAt.toLocaleTimeString(undefined, { timeStyle: 'short' })}
              />
              <MetaTile
                label="Submitted On"
                value={submittedAt ? submittedAt.toLocaleDateString(undefined, { dateStyle: 'medium' }) : 'Not yet submitted'}
                sub={submittedAt ? submittedAt.toLocaleTimeString(undefined, { timeStyle: 'short' }) : 'Draft in progress'}
              />
              {submission?.accreditationNumber ? (
                <MetaTile label="Accreditation No." value={submission.accreditationNumber} />
              ) : null}
            </Box>

            <NextStepsList status={application.status} resubmitted={resubmitted} />

            <Stack spacing={1} sx={{ alignItems: 'flex-start' }}>
              <Button component={RouterLink} to={primaryPath} variant="contained" sx={portalPrimaryButtonSx}>
                {primaryLabel}
              </Button>
              {application.status === 'RevisionRequired' ? (
                <Button
                  component={RouterLink}
                  to={`/client/accreditation/${application.uuid}`}
                  variant="outlined"
                  sx={portalOutlinedButtonSx}
                >
                  View Application
                </Button>
              ) : null}
            </Stack>
          </Stack>
        </PortalPanel>
      </Stack>

      <Stack spacing={3}>
        <PortalPanel title="Quick Actions">
          <Stack spacing={1.5} sx={{ px: 2.5, py: 2 }}>
            <Button component={RouterLink} to={primaryPath} variant="contained" fullWidth sx={portalPrimaryButtonSx}>
              {primaryLabel}
            </Button>
            {application.status === 'RevisionRequired' ? (
              <Button
                component={RouterLink}
                to={`/client/accreditation/${application.uuid}`}
                variant="outlined"
                fullWidth
                sx={portalOutlinedButtonSx}
              >
                View Application
              </Button>
            ) : null}
            <Button component={RouterLink} to="/client" variant="outlined" fullWidth sx={portalOutlinedButtonSx}>
              Back to Dashboard
            </Button>
          </Stack>
        </PortalPanel>

        <PortalPanel title="Recent Activity">
          <Box sx={{ px: 2.5, py: 2 }}>
            {submission?.history?.length ? (
              <AccreditationStatusTimeline history={submission.history} emptyMessage="No activity recorded yet." />
            ) : (
              <Stack spacing={1.5} sx={{ alignItems: 'center', py: 2 }}>
                <SendOutlinedIcon sx={{ color: portalColors.textLight }} />
                <Typography variant="body2" sx={{ color: portalColors.textMuted, textAlign: 'center' }}>
                  {application.status === 'Draft'
                    ? 'Activity will appear here after you submit your application.'
                    : 'Loading activity…'}
                </Typography>
              </Stack>
            )}
          </Box>
        </PortalPanel>
      </Stack>
    </Box>
  )
}

export function AccreditationListPage() {
  const navigate = useNavigate()
  const { data, isLoading, refetch } = useGetAccreditationSubmissionsQuery()
  const [createAccreditation, { isLoading: creating }] = useCreateAccreditationMutation()
  const [companyName, setCompanyName] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const submissions = data?.data?.items ?? []

  const application = useMemo(() => {
    return submissions.find((item) => item.status === 'Draft') ?? submissions[0] ?? null
  }, [submissions])

  const handleCreate = async (e: FormEvent) => {
    e.preventDefault()
    setErrorMessage(null)

    try {
      const result = await createAccreditation({ companyName, submissionType: 'NEW', formDataJson: '{}' }).unwrap()
      if (result.data && typeof result.data === 'object' && 'uuid' in result.data) {
        navigate(`/client/accreditation/${String((result.data as { uuid: string }).uuid)}`, { replace: true })
      }
    } catch (error) {
      const code = getApiErrorCode(error)
      if (code === 'DRAFT_EXISTS' || code === 'SUBMISSION_EXISTS') {
        const refreshed = await refetch()
        const existing =
          refreshed.data?.data?.items.find((item) => item.status === 'Draft') ??
          refreshed.data?.data?.items[0] ??
          null

        if (existing) {
          navigate(`/client/accreditation/${existing.uuid}`, { replace: true })
          return
        }
      }

      setErrorMessage(getApiErrorMessage(error) ?? 'Unable to start your application. Please try again.')
    }
  }

  const isAccredited = application?.status === 'Approved'

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={28} />
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Accreditation"
        title={isAccredited ? 'Accreditation' : 'Accreditation Application'}
        subtitle={
          isAccredited
            ? 'Your company is accredited. View your certificate and manage your accreditation here.'
            : application
              ? 'One application per account. Track your progress and continue your application here.'
              : 'One application per account. Start here, complete your form, and submit for DA review.'
        }
      />

      {application ? (
        isAccredited ? (
          <AccreditedApplicationHub application={application} />
        ) : (
          <ExistingApplicationHub application={application} />
        )
      ) : (
        <StartApplicationPanel
          creating={creating}
          errorMessage={errorMessage}
          companyName={companyName}
          onCompanyNameChange={setCompanyName}
          onSubmit={handleCreate}
        />
      )}
    </Box>
  )
}
