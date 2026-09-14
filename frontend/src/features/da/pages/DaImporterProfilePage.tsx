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
import { formatAddressSummary } from '../../addresses/AddressFieldRenderer'
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

const panelBodySx = { px: { xs: 2, md: 2.5 }, py: { xs: 2, md: 2.5 } }

function DetailField({ label, value }: { label: string; value?: string | null }) {
  return (
    <Box sx={{ minWidth: 0 }}>
      <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, color: portalColors.textMuted, mb: 0.5, lineHeight: 1.35 }}>
        {label}
      </Typography>
      <Typography sx={{ fontSize: '0.9375rem', fontWeight: 600, color: portalColors.textDark, lineHeight: 1.45, wordBreak: 'break-word' }}>
        {value?.trim() ? value : '—'}
      </Typography>
    </Box>
  )
}

function StatTile({ label, value }: { label: string; value: string | number }) {
  return (
    <Box
      sx={{
        borderRadius: '0.625rem',
        border: `1px solid ${portalColors.border}`,
        bgcolor: portalColors.bgMuted,
        px: 1.5,
        py: 1.25,
        minWidth: 0,
        height: '100%',
      }}
    >
      <Typography sx={{ fontSize: '0.72rem', fontWeight: 600, color: portalColors.textMuted, mb: 0.5, lineHeight: 1.35 }}>
        {label}
      </Typography>
      <Typography sx={{ fontSize: '1.35rem', fontWeight: 700, color: portalColors.textDark, fontVariantNumeric: 'tabular-nums' }}>
        {value}
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
  const resolvedAddress = useMemo(() => {
    if (profile?.address?.trim()) return profile.address
    const addressField = schemaFields.find((field) => field.type === 'address')
    if (addressField) {
      const formatted = formatAddressSummary(addressField.name, formValues)
      if (formatted.trim()) return formatted
    }
    return null
  }, [profile?.address, schemaFields, formValues])

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
        <Grid container spacing={2} sx={{ alignItems: 'stretch' }}>
          <Grid size={{ xs: 12, md: 6 }} sx={{ display: 'flex' }}>
            <Box sx={{ width: '100%', display: 'flex' }}>
              <PortalPanel title="Account details">
                <Box sx={panelBodySx}>
                  <Grid container spacing={2.5}>
                    <Grid size={{ xs: 12, sm: 6 }}><DetailField label="First name" value={profile.firstName} /></Grid>
                    <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Last name" value={profile.lastName} /></Grid>
                    <Grid size={{ xs: 12 }}><DetailField label="Company" value={profile.companyName} /></Grid>
                    <Grid size={{ xs: 12 }}><DetailField label="Address" value={resolvedAddress} /></Grid>
                    <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Registered" value={new Date(profile.createdAt).toLocaleString()} /></Grid>
                    <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Last login" value={profile.lastLoginAt ? new Date(profile.lastLoginAt).toLocaleString() : null} /></Grid>
                    <Grid size={{ xs: 12, sm: 6 }}><DetailField label="Email verified" value={profile.emailVerifiedAt ? new Date(profile.emailVerifiedAt).toLocaleString() : null} /></Grid>
                  </Grid>
                </Box>
              </PortalPanel>
            </Box>
          </Grid>
          <Grid size={{ xs: 12, md: 6 }} sx={{ display: 'flex' }}>
            <Box sx={{ width: '100%', display: 'flex' }}>
              <PortalPanel title="Entry & workflow summary">
                <Box
                  sx={{
                    ...panelBodySx,
                    display: 'grid',
                    gap: 1.5,
                    gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', sm: 'repeat(3, minmax(0, 1fr))' },
                  }}
                >
                  <StatTile label="Draft" value={profile.entryStats.draft} />
                  <StatTile label="Pending review" value={profile.entryStats.pendingReview} />
                  <StatTile label="For compliance" value={profile.entryStats.forCompliance} />
                  <StatTile label="In pipeline" value={profile.entryStats.inPipeline} />
                  <StatTile label="Rejected" value={profile.entryStats.rejected} />
                  <StatTile label="Cancelled" value={profile.entryStats.cancelled} />
                  <StatTile label="DA billing" value={profile.workflowStats.daIssueBilling} />
                  <StatTile label="For inspection" value={profile.workflowStats.forInspection} />
                  <StatTile label="In transit" value={profile.workflowStats.inTransit} />
                  <StatTile label="Open bills" value={profile.logisticsStats.openBills} />
                  <StatTile label="Overdue bills" value={profile.logisticsStats.overdueBills} />
                </Box>
              </PortalPanel>
            </Box>
          </Grid>
        </Grid>
      ) : null}

      {tab === 1 ? (
        profile.accreditation ? (
          <Stack spacing={2}>
            <PortalPanel title="Accreditation status">
              <Box sx={panelBodySx}>
                <Grid container spacing={2.5} sx={{ mb: 2.5 }}>
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
              </Box>
            </PortalPanel>

            {schemaFields.length > 0 ? (
              <PortalPanel title={profile.accreditation.formName ?? 'Accreditation application data'}>
                <Box sx={panelBodySx}>
                  <Grid container spacing={2.5}>
                    {visibleFormFields(schemaFields, formValues).map((field) => (
                      <Grid key={field.name} size={{ xs: 12, sm: field.type === 'address' ? 12 : 6 }}>
                        <DetailField
                          label={field.label}
                          value={field.type === 'address' ? formatAddressSummary(field.name, formValues) : formValues[field.name]}
                        />
                      </Grid>
                    ))}
                  </Grid>
                </Box>
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
