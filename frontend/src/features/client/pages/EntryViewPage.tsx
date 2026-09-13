import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Tab,
  Tabs,
  TableCell,
  TableRow,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx, portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetClientFormQuery,
  useGetClientFormsQuery,
  useGetEntryQuery,
  useGetContainerInspectionsQuery,
  useGetInspectionsQuery,
  useSubmitEntryMutation,
} from '../api/clientApi'
import { applyMavEntrySchema, parseFormSchema } from '../../forms/formSchema'
import { useGetMavAgencyContextQuery } from '../../mav/api/mavApi'
import { buildFormFieldLabelMap, getEntryFormCompletion, buildEntryFormValues, resolveFormFieldLabel } from '../utils/entryFormUtils'
import { downloadAuthenticatedFile } from '../utils/downloadFile'
import { EntryOverviewPanel } from '../components/EntryOverviewPanel'
import { EntryMavPanel } from '../components/EntryMavPanel'
import { EntrySubmitValidationDialog } from '../components/EntrySubmitValidationDialog'
import { getNumContainersFromFormData, parseContainerSchema } from '../utils/containerFormUtils'
import {
  getEntryEditPath,
  validateEntryForSubmit,
  type EntrySubmitValidationIssue,
} from '../utils/entrySubmitValidation'
import { getContainerInspectionProgress } from '../utils/entryInspectionUtils'

export function EntryViewPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const viewEntryDetails = searchParams.get('view') === 'entry'
  const [tab, setTab] = useState(0)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [submitIssue, setSubmitIssue] = useState<EntrySubmitValidationIssue | null>(null)
  const { data, isLoading, refetch } = useGetEntryQuery(uuid, { skip: !uuid })
  const entry = data?.data
  const { data: inspectionsData } = useGetInspectionsQuery({ entryUuid: uuid }, { skip: !uuid })
  const { data: containerInspectionsData } = useGetContainerInspectionsQuery(uuid, {
    skip: !uuid || entry?.status !== 'ForInspection',
  })
  const [submitEntry, { isLoading: submitting, error: submitError }] = useSubmitEntryMutation()

  const containerInspections = containerInspectionsData?.data ?? []
  const inspectionProgress = getContainerInspectionProgress(containerInspections)

  useEffect(() => {
    if (entry?.status !== 'ForInspection' || viewEntryDetails) return
    if (inspectionProgress.allUploadsComplete) return
    navigate(`/client/entries/${uuid}/inspection`, { replace: true })
  }, [entry?.status, inspectionProgress.allUploadsComplete, navigate, uuid, viewEntryDetails])

  const { data: formsData } = useGetClientFormsQuery(
    { agencyId: entry?.agencyId, formType: 'ENTRY' },
    { skip: !entry?.agencyId },
  )
  const selectedFormUuid = formsData?.data?.[0]?.uuid
  const { data: formSchemaData } = useGetClientFormQuery(selectedFormUuid ?? '', { skip: !selectedFormUuid })

  const { data: containerFormsData, isLoading: isLoadingContainerForms } = useGetClientFormsQuery(
    { agencyId: entry?.agencyId, formType: 'CONTAINER' },
    { skip: !entry?.agencyId },
  )
  const selectedContainerFormUuid = containerFormsData?.data?.[0]?.uuid
  const { data: containerFormSchemaData } = useGetClientFormQuery(selectedContainerFormUuid ?? '', {
    skip: !selectedContainerFormUuid,
  })
  const containerSchemaFields = useMemo(
    () => parseContainerSchema(containerFormSchemaData?.data?.schemaJson),
    [containerFormSchemaData?.data?.schemaJson],
  )

  const { data: mavContextData } = useGetMavAgencyContextQuery(entry?.agencyId ?? 0, {
    skip: !entry?.agencyId || entry.entryType !== 'Import',
  })
  const mavProgramAvailable = entry?.entryType === 'Import' && Boolean(mavContextData?.data?.isProgramAvailable ?? mavContextData?.data?.isActive)
  const hasExistingMav = Boolean(entry?.mav?.mavNo || (entry?.mav?.micUtilizations?.length ?? 0) > 0)
  const isMavTrack = entry?.entryType === 'Import' && (entry.mav?.importTrack === 'Mav' || (!entry.mav?.importTrack && hasExistingMav))

  const formCompletion = useMemo(() => {
    const schemaFields = applyMavEntrySchema(parseFormSchema(formSchemaData?.data?.schemaJson ?? ''), {
      showMavFields: isMavTrack,
      requireCommodityHs: mavProgramAvailable,
    })
    const values = buildEntryFormValues({
      formDataJson: entry?.formDataJson,
      detail: entry?.detail,
      files: entry?.files,
    }, schemaFields)
    return getEntryFormCompletion(schemaFields, values, entry?.files)
  }, [formSchemaData?.data?.schemaJson, entry?.formDataJson, entry?.detail, entry?.files, isMavTrack, mavProgramAvailable])

  const documentLabelMap = useMemo(
    () => buildFormFieldLabelMap(formSchemaData?.data?.schemaJson),
    [formSchemaData?.data?.schemaJson],
  )

  useBreadcrumbLabel(entry?.referenceNo)

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading entry…</Typography>
  if (!entry) return <Alert severity="error">Entry not found.</Alert>
  if (entry.status === 'ForInspection' && !viewEntryDetails) {
    return <Typography sx={{ color: portalColors.textMuted }}>Opening inspection uploads…</Typography>
  }

  const canSubmitDraft = entry.status === 'Draft' && formCompletion.isComplete
  const missingSummary = [...formCompletion.missingFields, ...formCompletion.missingFiles]
  const savedContainerCount = entry.containers?.length ?? 0
  const expectedContainerCount = getNumContainersFromFormData(entry.formDataJson, savedContainerCount)
  const submitValidationIssue = validateEntryForSubmit({
    hasContainerForm: Boolean(selectedContainerFormUuid),
    savedContainerCount,
    expectedContainerCount,
  })

  const handleSubmitClick = () => {
    if (!canSubmitDraft || isLoadingContainerForms) return
    if (submitValidationIssue) {
      setSubmitIssue(submitValidationIssue)
      return
    }
    setConfirmOpen(true)
  }

  const handleSubmit = async () => {
    if (!canSubmitDraft) return
    if (submitValidationIssue) {
      setConfirmOpen(false)
      setSubmitIssue(submitValidationIssue)
      return
    }
    try {
      await submitEntry(uuid).unwrap()
      setConfirmOpen(false)
      navigate(`/client/entries/${uuid}/confirmation`)
    } catch {
      // submitError surfaces via RTK mutation state
    }
  }

  const entryInspections = inspectionsData?.data ?? []

  const handleDownloadFile = async (fileUuid: string, fileName: string) => {
    await downloadAuthenticatedFile(`/entries/${uuid}/files/${fileUuid}/download`, fileName)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Entry"
        title={entry.referenceNo}
        subtitle={`${entry.entryType} · ${entry.agencyCode}`}
        actions={
          <Stack direction="row" spacing={1}>
            {entry.status === 'Draft' && (
              <Button component={RouterLink} to={`/client/entries/${uuid}/edit`} variant="outlined" sx={portalOutlinedButtonSx}>
                Edit Draft
              </Button>
            )}
            {canSubmitDraft && (
              <Button variant="contained" sx={portalPrimaryButtonSx} onClick={handleSubmitClick} disabled={submitting || isLoadingContainerForms}>
                Submit Entry
              </Button>
            )}
          </Stack>
        }
      />
      <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
        <Chip size="small" label={entry.status} sx={portalStatusChipSx(entry.status)} />
        <Chip size="small" label={entry.entryType} variant="outlined" />
        <Chip size="small" label={entry.agencyCode} variant="outlined" />
      </Stack>

      {entry.status === 'Draft' && submitValidationIssue && (
        <Alert severity="warning" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          {submitValidationIssue.message}{' '}
          <Button
            component={RouterLink}
            to={getEntryEditPath(uuid)}
            size="small"
            sx={{ px: 0, minWidth: 0, verticalAlign: 'baseline', textTransform: 'none' }}
          >
            Edit container details
          </Button>
        </Alert>
      )}

      {entry.status === 'Draft' && !formCompletion.isComplete && (
        <Alert severity="warning" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Complete all required fields and upload required documents before submitting.
          {formCompletion.requiredFileCount > 0 && (
            <> Documents: {formCompletion.uploadedFileCount} / {formCompletion.requiredFileCount} uploaded.</>
          )}
          {missingSummary.length > 0 && (
            <> Still missing: {missingSummary.slice(0, 6).join(', ')}{missingSummary.length > 6 ? '…' : ''}.</>
          )}
          {' '}Use <strong>Edit Draft</strong> to finish the entry form.
        </Alert>
      )}

      {submitError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Unable to submit entry. Ensure all required fields and documents are complete.
        </Alert>
      )}

      {entry.status === 'ForCompliance' && (
        <Alert severity="warning" sx={{ mb: 2 }} action={
          <Button color="inherit" size="small" component={RouterLink} to={`/client/entries/${uuid}/compliance`}>
            Open compliance
          </Button>
        }>
          This entry requires document revisions.
          {entry.complianceDeadlineAt && ` Deadline: ${new Date(entry.complianceDeadlineAt).toLocaleDateString()}.`}
          {' '}Upload revised files and resubmit for review.
        </Alert>
      )}

      {entry.status === 'DaIssueBilling' && (
        <Alert
          severity="info"
          sx={{ mb: 2, borderRadius: '0.75rem' }}
          action={
            <Button color="inherit" size="small" component={RouterLink} to="/client/bills">
              Pay bill
            </Button>
          }
        >
          Agency billing has been issued for this entry. Status: <strong>DA Issue Billing</strong>. Go to{' '}
          <strong>Bills &amp; Payments</strong> to pay online (PayMongo) or submit a cash (OR) payment.
        </Alert>
      )}

      {entry.status === 'ForInspection' && inspectionProgress.awaitingInspectorReview && (
        <Alert severity="info" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          All container photos have been uploaded. Your entry is now <strong>waiting for inspector review</strong>.
          The agency inspector will verify each photo before transport tagging can begin.
        </Alert>
      )}

      {entry.status === 'ForInspection' && !inspectionProgress.allUploadsComplete && (
        <Alert
          severity="warning"
          sx={{ mb: 2, borderRadius: '0.75rem' }}
          action={
            <Button
              color="inherit"
              size="small"
              component={RouterLink}
              to={`/client/entries/${uuid}/inspection`}
            >
              Upload photos
            </Button>
          }
        >
          Container inspection photos are required.
          {containerInspections.length > 0 && (
            <>
              {' '}
              Progress: {inspectionProgress.completeContainers}/{containerInspections.length} containers complete.
            </>
          )}
        </Alert>
      )}

      {entry.entryType === 'Import' && isMavTrack && (
        <Box sx={{ mb: 2 }}>
          <EntryMavPanel entry={entry} editable={entry.status === 'Draft'} onUpdated={() => refetch()} />
        </Box>
      )}

      {entry.entryType === 'Import' && mavProgramAvailable && !isMavTrack && (
        <Alert severity="info" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Regular import (out-quota). This shipment does not use a MAV license or MIC and will not reduce remaining MAV quota.
        </Alert>
      )}

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Overview" />
        <Tab label="Documents" />
        <Tab label="History" />
        <Tab label="Payment" />
        <Tab label="Inspection" />
      </Tabs>

      {tab === 0 && (
        <EntryOverviewPanel
          entry={entry}
          containerSchemaFields={containerSchemaFields}
          showImportTrack={mavProgramAvailable || isMavTrack}
        />
      )}

      {tab === 1 && (
        <PortalPanel title="Documents">
          <Box sx={{ px: 2.5, py: 2 }}>
            {entry.files.length === 0 ? (
              <Stack spacing={1.5}>
                <Typography sx={{ color: portalColors.textMuted }}>No documents uploaded yet.</Typography>
                {entry.status === 'Draft' && (
                  <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
                    Upload required documents from{' '}
                    <Button
                      component={RouterLink}
                      to={`/client/entries/${uuid}/edit`}
                      size="small"
                      sx={{ px: 0, minWidth: 0, verticalAlign: 'baseline', textTransform: 'none' }}
                    >
                      Edit Draft
                    </Button>
                    .
                  </Typography>
                )}
              </Stack>
            ) : (
              <PortalTablePanel
                title=""
                columns={['Document', 'File name', 'Evaluation', 'Size', 'Uploaded', '']}
                isEmpty={false}
              >
                {entry.files.map((file) => (
                  <TableRow key={file.uuid}>
                    <TableCell sx={{ fontWeight: 600 }}>
                      {resolveFormFieldLabel(file.documentType, documentLabelMap)}
                    </TableCell>
                    <TableCell>{file.originalFileName}</TableCell>
                    <TableCell>
                      {file.evaluationDecision ? (
                        <Chip size="small" label={file.evaluationDecision} sx={portalStatusChipSx(file.evaluationDecision)} />
                      ) : '—'}
                    </TableCell>
                    <TableCell>{Math.round(file.fileSizeBytes / 1024)} KB</TableCell>
                    <TableCell>{new Date(file.createdAt).toLocaleString()}</TableCell>
                    <TableCell>
                      <Button size="small" onClick={() => handleDownloadFile(file.uuid, file.originalFileName)}>
                        Download
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </PortalTablePanel>
            )}
          </Box>
        </PortalPanel>
      )}

      {tab === 2 && (
        <Stack spacing={2}>
          {entry.timeline.map((event) => (
            <PortalPanel key={`${event.eventType}-${event.createdAt}`} title={event.title}>
              <Box sx={{ px: 2.5, py: 2 }}>
                <Typography variant="body2" color="text.secondary">{event.description}</Typography>
                <Typography variant="caption">{new Date(event.createdAt).toLocaleString()}</Typography>
              </Box>
            </PortalPanel>
          ))}
        </Stack>
      )}

      {tab === 3 && (
        <Stack spacing={2}>
          {entry.bills.length === 0 && (
            <Typography sx={{ color: portalColors.textMuted }}>
              {entry.status === 'DaIssueBilling'
                ? 'Your agency bill is being prepared. Open Bills & Payments to pay once it appears.'
                : 'No bills yet. Submit the entry to generate a processing fee bill.'}
            </Typography>
          )}
          {entry.bills.map((bill) => (
            <PortalPanel key={bill.uuid} title={bill.billNumber}>
              <Box sx={{ px: 2.5, py: 2 }}>
                <Typography variant="body2">{bill.description}</Typography>
                <Typography variant="body2">Amount: PHP {bill.amount.toLocaleString()}</Typography>
                <Typography variant="body2">Status: {bill.status}</Typography>
                {bill.status !== 'Paid' && (
                  <Button
                    component={RouterLink}
                    to={`/client/bills/${bill.uuid}`}
                    variant="contained"
                    sx={{ mt: 1.5, ...portalPrimaryButtonSx }}
                  >
                    Pay bill
                  </Button>
                )}
              </Box>
            </PortalPanel>
          ))}
          {entry.status === 'DaIssueBilling' && (
            <Button component={RouterLink} to="/client/bills" variant="outlined" sx={portalOutlinedButtonSx}>
              Open Bills &amp; Payments
            </Button>
          )}
        </Stack>
      )}

      {tab === 4 && (
        <Stack spacing={2}>
          {entry.status === 'ForInspection' && (
            <PortalPanel title="Container photo uploads">
              <Box sx={{ px: 2.5, py: 2 }}>
                {inspectionProgress.awaitingInspectorReview ? (
                  <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
                    All required photos were submitted for {containerInspections.length} container(s).
                    Status: <strong>waiting for inspector review</strong>.
                  </Typography>
                ) : (
                  <>
                    <Typography variant="body2" sx={{ color: portalColors.textMuted, mb: 1.5 }}>
                      {containerInspections.length > 0
                        ? `${inspectionProgress.completeContainers} of ${containerInspections.length} containers have all required photos.`
                        : 'Prepare container inspection photos for agency review.'}
                    </Typography>
                    <Button
                      component={RouterLink}
                      to={`/client/entries/${uuid}/inspection`}
                      variant="contained"
                      sx={portalPrimaryButtonSx}
                    >
                      Open inspection uploads
                    </Button>
                  </>
                )}
              </Box>
            </PortalPanel>
          )}
          {entryInspections.length === 0 ? (
            entry.status !== 'ForInspection' && (
              <Alert severity="info">Inspection details will appear here after agency review.</Alert>
            )
          ) : (
            <PortalTablePanel
              title="Agency inspections"
              columns={['Agency', 'Status', 'Scheduled', 'Completed', '']}
              isEmpty={false}
            >
              {entryInspections.map((inspection) => (
                <TableRow key={inspection.uuid}>
                  <TableCell>{inspection.agencyCode}</TableCell>
                  <TableCell>
                    <Chip size="small" label={inspection.status} sx={portalStatusChipSx(inspection.status)} />
                  </TableCell>
                  <TableCell>{inspection.scheduledAt ? new Date(inspection.scheduledAt).toLocaleString() : '—'}</TableCell>
                  <TableCell>{inspection.completedAt ? new Date(inspection.completedAt).toLocaleString() : '—'}</TableCell>
                  <TableCell>
                    <Button size="small" component={RouterLink} to={`/client/inspections/${inspection.uuid}`}>
                      View
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </PortalTablePanel>
          )}
        </Stack>
      )}

      <Button sx={{ mt: 3, ...portalOutlinedButtonSx }} variant="outlined" onClick={() => navigate('/client/entries')}>
        Back to entries
      </Button>

      <EntrySubmitValidationDialog
        open={Boolean(submitIssue)}
        issue={submitIssue}
        onCancel={() => setSubmitIssue(null)}
        onConfirmEdit={() => {
          setSubmitIssue(null)
          navigate(getEntryEditPath(uuid))
        }}
      />

      <Dialog open={confirmOpen} onClose={() => !submitting && setConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Submit Entry?</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted }}>
            You are about to submit entry <strong>{entry.referenceNo}</strong> to{' '}
            <strong>{entry.agencyName}</strong> for review.
          </Typography>
          <Typography sx={{ color: portalColors.textMuted, mt: 1.5 }}>
            After submission, you will not be able to edit this entry unless the agency requests revisions.
            A processing fee bill may be generated for payment.
          </Typography>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setConfirmOpen(false)} disabled={submitting} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} variant="contained" disabled={submitting} sx={portalPrimaryButtonSx}>
            {submitting ? 'Submitting…' : 'Yes, Submit Entry'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
