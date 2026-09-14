import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import PhoneOutlinedIcon from '@mui/icons-material/PhoneOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Grid,
  Stack,
  Tab,
  TableCell,
  TableRow,
  Tabs,
  Typography,
} from '@mui/material'
import { useMemo, useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { AccreditationStatusChip } from '../../accreditation/AccreditationStatusChip'
import { AccreditationStatusTimeline } from '../../client/components/AccreditationStatusTimeline'
import { parseFormDataJson, parseFormSchema, visibleFormFields } from '../../forms/formSchema'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { DaAnalyticsKpiCard } from '../components/DaAgencyAnalyticsPanels'
import {
  useGetDaImporterEntriesQuery,
  useGetDaImporterProfileQuery,
} from '../api/daApi'

function formatMt(kg: number) {
  return `${(kg / 1000).toLocaleString(undefined, { maximumFractionDigits: 2 })} MT`
}

function formatMoney(amount: number) {
  return `₱${amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function DetailField({ label, value }: { label: string; value?: string | null }) {
  return (
    <Box>
      <Typography sx={{ fontSize: '0.6875rem', fontWeight: 600, letterSpacing: '0.08em', textTransform: 'uppercase', color: portalColors.textMuted, mb: 0.25 }}>
        {label}
      </Typography>
      <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
        {value?.trim() ? value : '—'}
      </Typography>
    </Box>
  )
}

export function DaImporterProfilePage() {
  const { uuid = '' } = useParams()
  const [tab, setTab] = useState(0)
  const [entryPage, setEntryPage] = useState(1)
  const { data, isLoading, error } = useGetDaImporterProfileQuery(uuid, { skip: !uuid })
  const profile = data?.data
  const { data: entriesData, isFetching: entriesFetching } = useGetDaImporterEntriesQuery(
    { uuid, page: entryPage, pageSize: 25 },
    { skip: !uuid },
  )
  const entries = entriesData?.data

  const schemaFields = useMemo(
    () => (profile?.accreditation?.formSchemaJson ? parseFormSchema(profile.accreditation.formSchemaJson) : []),
    [profile?.accreditation?.formSchemaJson],
  )
  const formValues = useMemo(
    () => parseFormDataJson(profile?.accreditation?.formDataJson),
    [profile?.accreditation?.formDataJson],
  )

  if (!uuid) {
    return <Alert severity="error">Invalid importer id.</Alert>
  }

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  if (error || !profile) {
    return <Alert severity="error">Importer profile not found.</Alert>
  }

  const displayName = profile.companyName?.trim() || profile.fullName

  return (
    <Box>
      <Button
        component={RouterLink}
        to="/da/importers"
        startIcon={<ArrowBackIcon />}
        sx={{ ...portalOutlinedButtonSx, mb: 2 }}
      >
        Back to importers
      </Button>

      <Box
        sx={{
          mb: 3,
          borderRadius: '0.875rem',
          border: `1px solid ${portalColors.border}`,
          bgcolor: portalColors.bgWhite,
          overflow: 'hidden',
        }}
      >
        <Box
          sx={{
            px: { xs: 2, md: 3 },
            py: { xs: 2, md: 2.5 },
            background: `linear-gradient(135deg, ${portalColors.successSoft} 0%, ${portalColors.bgWhite} 55%)`,
            borderBottom: `1px solid ${portalColors.border}`,
          }}
        >
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ justifyContent: 'space-between' }}>
            <Stack direction="row" spacing={2} sx={{ alignItems: 'flex-start', minWidth: 0 }}>
              <Box
                sx={{
                  width: 52,
                  height: 52,
                  borderRadius: '0.875rem',
                  bgcolor: portalColors.primary,
                  color: '#fff',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  flexShrink: 0,
                }}
              >
                <BusinessOutlinedIcon />
              </Box>
              <Box sx={{ minWidth: 0 }}>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, letterSpacing: '0.14em', textTransform: 'uppercase', color: portalColors.primary, mb: 0.5 }}>
                  Importer profile
                </Typography>
                <Typography sx={{ fontSize: '1.5rem', fontWeight: 800, color: portalColors.textDark, lineHeight: 1.2 }}>
                  {displayName}
                </Typography>
                {profile.companyName?.trim() ? (
                  <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mt: 0.5 }}>
                    {profile.fullName}
                  </Typography>
                ) : null}
                <Stack direction="row" spacing={1} sx={{ mt: 1, flexWrap: 'wrap', gap: 1 }}>
                  <Chip size="small" label={profile.status} sx={portalStatusChipSx(profile.status)} />
                  {profile.accreditation ? (
                    <AccreditationStatusChip
                      status={profile.accreditation.status}
                      displayStatus={profile.accreditation.displayStatus}
                    />
                  ) : (
                    <Chip size="small" label="No accreditation" variant="outlined" />
                  )}
                  {profile.roles.map((role) => (
                    <Chip key={role} size="small" label={role.replace('ROLE_', '')} variant="outlined" />
                  ))}
                </Stack>
              </Box>
            </Stack>
            <Stack spacing={0.75} sx={{ minWidth: 220 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <EmailOutlinedIcon sx={{ fontSize: 16, color: portalColors.textMuted }} />
                <Typography sx={{ fontSize: '0.8125rem' }}>{profile.email}</Typography>
              </Stack>
              {profile.phone ? (
                <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                  <PhoneOutlinedIcon sx={{ fontSize: 16, color: portalColors.textMuted }} />
                  <Typography sx={{ fontSize: '0.8125rem' }}>{profile.phone}</Typography>
                </Stack>
              ) : null}
            </Stack>
          </Stack>
        </Box>
      </Box>

      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <DaAnalyticsKpiCard
            label="Total entries"
            value={String(profile.entryStats.total)}
            meta={`${profile.entryStats.inPipeline} in active pipeline`}
            icon={<LocalShippingOutlinedIcon />}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <DaAnalyticsKpiCard
            label="Expected pipeline"
            value={formatMt(profile.pipelineStats.expectedKg)}
            meta={`${profile.pipelineStats.pipelineEntries} active import entries`}
            icon={<LocalShippingOutlinedIcon />}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <DaAnalyticsKpiCard
            label="Warehoused volume"
            value={formatMt(profile.pipelineStats.actualKg)}
            meta={`${profile.logisticsStats.storedContainers} stored containers`}
            icon={<WarehouseOutlinedIcon />}
          />
        </Grid>
        <Grid size={{ xs: 12, sm: 6, md: 3 }}>
          <DaAnalyticsKpiCard
            label="Certificates"
            value={String(profile.certificates.length)}
            meta={`${profile.logisticsStats.activeMavLicenses} active MAV license(s)`}
            icon={<VerifiedOutlinedIcon />}
          />
        </Grid>
      </Grid>

      <Tabs value={tab} onChange={(_, value) => setTab(value)} sx={{ mb: 2 }}>
        <Tab label="Overview" />
        <Tab label="Accreditation" />
        <Tab label="Entries" />
        <Tab label="Certificates" />
        <Tab label="Bills" />
      </Tabs>

      {tab === 0 ? (
        <Grid container spacing={2}>
          <Grid size={{ xs: 12, md: 6 }}>
            <PortalPanel title="Account details">
              <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="First name" value={profile.firstName} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Last name" value={profile.lastName} /></Grid>
                <Grid size={{ xs: 12 }}><DetailField label="Company" value={profile.companyName} /></Grid>
                <Grid size={{ xs: 12 }}><DetailField label="Address" value={profile.address} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Registered" value={new Date(profile.createdAt).toLocaleString()} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Last login" value={profile.lastLoginAt ? new Date(profile.lastLoginAt).toLocaleString() : null} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Email verified" value={profile.emailVerifiedAt ? new Date(profile.emailVerifiedAt).toLocaleString() : null} /></Grid>
              </Grid>
            </PortalPanel>
          </Grid>
          <Grid size={{ xs: 12, md: 6 }}>
            <PortalPanel title="Entry & workflow summary">
              <Grid container spacing={2}>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="Draft" value={String(profile.entryStats.draft)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="Pending review" value={String(profile.entryStats.pendingReview)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="For compliance" value={String(profile.entryStats.forCompliance)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="In pipeline" value={String(profile.entryStats.inPipeline)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="Rejected" value={String(profile.entryStats.rejected)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="Cancelled" value={String(profile.entryStats.cancelled)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="DA billing" value={String(profile.workflowStats.daIssueBilling)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="For inspection" value={String(profile.workflowStats.forInspection)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="In transit" value={String(profile.workflowStats.inTransit)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="Open bills" value={String(profile.logisticsStats.openBills)} /></Grid>
                <Grid size={{ xs: 6, sm: 4 }}><DetailField label="Overdue bills" value={String(profile.logisticsStats.overdueBills)} /></Grid>
              </Grid>
            </PortalPanel>
          </Grid>
        </Grid>
      ) : null}

      {tab === 1 ? (
        profile.accreditation ? (
          <Stack spacing={2}>
            <PortalPanel title="Accreditation status">
              <Grid container spacing={2} sx={{ mb: 2 }}>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Company on file" value={profile.accreditation.companyName} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Application type" value={profile.accreditation.submissionType} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Accreditation no." value={profile.accreditation.accreditationNumber} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Certificate no." value={profile.accreditation.certificateNumber} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Submitted" value={profile.accreditation.submittedAt ? new Date(profile.accreditation.submittedAt).toLocaleString() : null} /></Grid>
                <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Assigned officer" value={profile.accreditation.assignedOfficerName} /></Grid>
                <Grid size={{ xs: 12 }}><DetailField label="Officer remarks" value={profile.accreditation.reviewComments} /></Grid>
              </Grid>
              <AccreditationStatusTimeline
                history={profile.accreditation.history.map((item) => ({
                  status: item.status,
                  comment: item.comment ?? undefined,
                  createdAt: item.createdAt,
                  actorName: item.actorName ?? undefined,
                }))}
                showActorName
              />
            </PortalPanel>

            {schemaFields.length > 0 ? (
              <PortalPanel title={profile.accreditation.formName ?? 'Accreditation application data'}>
                <Grid container spacing={2}>
                  {visibleFormFields(schemaFields, formValues).map((field) => (
                    <Grid key={field.name} size={{ xs: 12, sm: 6 }}>
                      <DetailField label={field.label} value={formValues[field.name]} />
                    </Grid>
                  ))}
                </Grid>
              </PortalPanel>
            ) : null}

            <PortalTablePanel
              title="Submitted documents"
              columns={['File', 'Size', 'Uploaded']}
              isEmpty={profile.accreditation.files.length === 0}
              emptyMessage="No accreditation documents on file."
            >
              {profile.accreditation.files.map((file) => (
                <TableRow key={file.uuid} hover>
                  <TableCell sx={{ fontWeight: 600 }}>{file.originalFileName}</TableCell>
                  <TableCell>{(file.fileSizeBytes / 1024).toFixed(1)} KB</TableCell>
                  <TableCell>{new Date(file.createdAt).toLocaleString()}</TableCell>
                </TableRow>
              ))}
            </PortalTablePanel>
          </Stack>
        ) : (
          <Alert severity="info">This importer has not submitted an accreditation application yet.</Alert>
        )
      ) : null}

      {tab === 2 ? (
        <Box>
          <PortalTablePanel
            title="All entries"
            columns={['Reference', 'Type', 'Agency', 'Commodity', 'Status', 'Volume', 'Containers', 'Submitted']}
            isEmpty={!entries?.items.length}
            emptyMessage="No entries recorded for this importer."
          >
            {(entries?.items ?? []).map((entry) => (
              <TableRow key={entry.uuid} hover>
                <TableCell sx={{ fontWeight: 600 }}>{entry.referenceNo}</TableCell>
                <TableCell>{entry.entryType}</TableCell>
                <TableCell>{entry.agencyCode}</TableCell>
                <TableCell>
                  {entry.commodityName ?? '—'}
                  {entry.hsCode ? (
                    <Typography sx={{ fontSize: '0.7rem', color: portalColors.textMuted }}>HS {entry.hsCode}</Typography>
                  ) : null}
                </TableCell>
                <TableCell><Chip size="small" label={entry.status} sx={portalStatusChipSx(entry.status)} /></TableCell>
                <TableCell>{formatMt(entry.volumeKg)}</TableCell>
                <TableCell>{entry.containerCount}</TableCell>
                <TableCell>{entry.submittedAt ? new Date(entry.submittedAt).toLocaleDateString() : '—'}</TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
          {entries && entries.totalCount > entries.pageSize ? (
            <Stack direction="row" spacing={1} sx={{ mt: 2, justifyContent: 'flex-end' }}>
              <Chip clickable label="Previous" disabled={entryPage <= 1} onClick={() => setEntryPage((p) => p - 1)} />
              <Chip label={`Page ${entryPage} of ${Math.ceil(entries.totalCount / entries.pageSize)}`} />
              <Chip
                clickable
                label="Next"
                disabled={entryPage * entries.pageSize >= entries.totalCount}
                onClick={() => setEntryPage((p) => p + 1)}
              />
            </Stack>
          ) : null}
          {entriesFetching ? <Typography sx={{ mt: 1, fontSize: '0.75rem', color: portalColors.textMuted }}>Loading entries…</Typography> : null}
        </Box>
      ) : null}

      {tab === 3 ? (
        <PortalTablePanel
          title="Issued certificates"
          columns={['Certificate', 'Title', 'Status', 'Entry', 'Issued', 'Expires']}
          isEmpty={profile.certificates.length === 0}
          emptyMessage="No certificates issued to this importer."
        >
          {profile.certificates.map((cert) => (
            <TableRow key={cert.uuid} hover>
              <TableCell sx={{ fontWeight: 600 }}>{cert.certificateNumber}</TableCell>
              <TableCell>{cert.title}</TableCell>
              <TableCell><Chip size="small" label={cert.status} sx={portalStatusChipSx(cert.status)} /></TableCell>
              <TableCell>{cert.entryReferenceNo ?? '—'}</TableCell>
              <TableCell>{new Date(cert.issuedAt).toLocaleDateString()}</TableCell>
              <TableCell>{cert.expiresAt ? new Date(cert.expiresAt).toLocaleDateString() : '—'}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      ) : null}

      {tab === 4 ? (
        <PortalTablePanel
          title="Bills & payments"
          columns={['Bill', 'Entry', 'Agency', 'Description', 'Amount', 'Status', 'Due']}
          isEmpty={profile.bills.length === 0}
          emptyMessage="No billing records for this importer."
        >
          {profile.bills.map((bill) => (
            <TableRow key={bill.uuid} hover>
              <TableCell sx={{ fontWeight: 600 }}>{bill.billNumber}</TableCell>
              <TableCell>{bill.entryReferenceNo ?? '—'}</TableCell>
              <TableCell>{bill.agencyCode ?? '—'}</TableCell>
              <TableCell>{bill.description}</TableCell>
              <TableCell>{formatMoney(bill.amount)}</TableCell>
              <TableCell>
                <Chip
                  size="small"
                  label={bill.status}
                  color={bill.isOverdue ? 'error' : 'default'}
                  sx={portalStatusChipSx(bill.status)}
                />
              </TableCell>
              <TableCell>{bill.dueDate ? new Date(bill.dueDate).toLocaleDateString() : '—'}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      ) : null}
    </Box>
  )
}
