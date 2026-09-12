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
import { useMemo, useState } from 'react'
import { Link as RouterLink, useNavigate, useParams } from 'react-router-dom'
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
  useGetInspectionsQuery,
  useInitiateBillPaymentMutation,
  useSubmitEntryMutation,
} from '../api/clientApi'
import { redirectToBillPayment } from '../utils/billPayment'
import { buildFormFieldLabelMap, getEntryFormCompletionFromSchemaJson, resolveFormFieldLabel } from '../utils/entryFormUtils'
import { downloadAuthenticatedFile } from '../utils/downloadFile'
import { EntryOverviewPanel } from '../components/EntryOverviewPanel'
import { EntryMavPanel } from '../components/EntryMavPanel'
import { EntryDaBillingPanel } from '../components/EntryDaBillingPanel'
import { EntryContainerInspectionPanel } from '../components/EntryContainerInspectionPanel'
import { parseContainerSchema } from '../utils/containerFormUtils'

export function EntryViewPage() {
  const { uuid = '' } = useParams()
  const navigate = useNavigate()
  const [tab, setTab] = useState(0)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const { data, isLoading, refetch } = useGetEntryQuery(uuid, { skip: !uuid })
  const { data: inspectionsData } = useGetInspectionsQuery({ entryUuid: uuid }, { skip: !uuid })
  const [submitEntry, { isLoading: submitting, error: submitError }] = useSubmitEntryMutation()
  const [initiateBillPayment, { isLoading: paying }] = useInitiateBillPaymentMutation()
  const entry = data?.data

  const { data: formsData } = useGetClientFormsQuery(
    { agencyId: entry?.agencyId, formType: 'ENTRY' },
    { skip: !entry?.agencyId },
  )
  const selectedFormUuid = formsData?.data?.[0]?.uuid
  const { data: formSchemaData } = useGetClientFormQuery(selectedFormUuid ?? '', { skip: !selectedFormUuid })

  const { data: containerFormsData } = useGetClientFormsQuery(
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

  const formCompletion = useMemo(
    () => getEntryFormCompletionFromSchemaJson(formSchemaData?.data?.schemaJson, {
      formDataJson: entry?.formDataJson,
      detail: entry?.detail,
      files: entry?.files,
    }),
    [formSchemaData?.data?.schemaJson, entry?.formDataJson, entry?.detail, entry?.files],
  )

  const documentLabelMap = useMemo(
    () => buildFormFieldLabelMap(formSchemaData?.data?.schemaJson),
    [formSchemaData?.data?.schemaJson],
  )

  useBreadcrumbLabel(entry?.referenceNo)

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading entry…</Typography>
  if (!entry) return <Alert severity="error">Entry not found.</Alert>

  const canSubmitDraft = entry.status === 'Draft' && formCompletion.isComplete
  const missingSummary = [...formCompletion.missingFields, ...formCompletion.missingFiles]

  const handleSubmit = async () => {
    if (!canSubmitDraft) return
    try {
      await submitEntry(uuid).unwrap()
      setConfirmOpen(false)
      navigate(`/client/entries/${uuid}/confirmation`)
    } catch {
      // submitError surfaces via RTK mutation state
    }
  }

  const handlePay = async (billUuid: string) => {
    const result = await redirectToBillPayment(initiateBillPayment, billUuid)
    if (result === 'completed') {
      refetch()
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
              <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setConfirmOpen(true)} disabled={submitting}>
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
        <Box sx={{ mb: 2 }}>
          <EntryDaBillingPanel entryUuid={uuid} />
        </Box>
      )}

      {entry.status === 'ForInspection' && (
        <Box sx={{ mb: 2 }}>
          <EntryContainerInspectionPanel entryUuid={uuid} />
        </Box>
      )}

      {entry.entryType === 'Import' && (
        <Box sx={{ mb: 2 }}>
          <EntryMavPanel entry={entry} editable={entry.status === 'Draft'} onUpdated={() => refetch()} />
        </Box>
      )}

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Overview" />
        <Tab label="Documents" />
        <Tab label="History" />
        <Tab label="Payment" />
        <Tab label="Inspection" />
        {entry.status === 'DaIssueBilling' && <Tab label="DA Billing" />}
      </Tabs>

      {tab === 0 && <EntryOverviewPanel entry={entry} containerSchemaFields={containerSchemaFields} />}

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
            <Typography sx={{ color: portalColors.textMuted }}>No bills yet. Submit the entry to generate a processing fee bill.</Typography>
          )}
          {entry.bills.map((bill) => (
            <PortalPanel key={bill.uuid} title={bill.billNumber}>
              <Box sx={{ px: 2.5, py: 2 }}>
                <Typography variant="body2">{bill.description}</Typography>
                <Typography variant="body2">Amount: PHP {bill.amount.toLocaleString()}</Typography>
                <Typography variant="body2">Status: {bill.status}</Typography>
                {bill.status !== 'Paid' && (
                  <>
                    <Button variant="contained" sx={{ mt: 1, mr: 1, ...portalPrimaryButtonSx }} disabled={paying} onClick={() => handlePay(bill.uuid)}>
                      Pay Now (Simulated)
                    </Button>
                    <Button component={RouterLink} to={`/client/bills/${bill.uuid}`} variant="outlined" sx={portalOutlinedButtonSx}>
                      View Bill
                    </Button>
                  </>
                )}
              </Box>
            </PortalPanel>
          ))}
        </Stack>
      )}

      {tab === 4 && (
        entryInspections.length === 0 ? (
          <Alert severity="info">Inspection details will appear here after agency review.</Alert>
        ) : (
          <PortalTablePanel
            title="Inspections"
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
        )
      )}

      {entry.status === 'DaIssueBilling' && tab === 5 && (
        <EntryDaBillingPanel entryUuid={uuid} />
      )}

      <Button sx={{ mt: 3, ...portalOutlinedButtonSx }} variant="outlined" onClick={() => navigate('/client/entries')}>
        Back to entries
      </Button>

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
