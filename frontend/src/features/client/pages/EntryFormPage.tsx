import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import DescriptionOutlinedIcon from '@mui/icons-material/DescriptionOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Link as RouterLink, Navigate, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { resolveAgencyLogoUrl } from '../../admin/components/adminAgencyUtils'
import { parseFormSchema, serializeFormDataJson } from '../../forms/formSchema'
import {
  useCreateEntryMutation,
  useGetAgenciesQuery,
  useGetClientFormQuery,
  useGetClientFormsQuery,
  useGetCommoditiesQuery,
  useGetDashboardQuery,
  useGetEntryQuery,
  useUpdateEntryMutation,
  useUploadEntryFileMutation,
} from '../api/clientApi'
import { DynamicFormRenderer, mapDynamicValuesToEntryDetail } from '../components/DynamicFormRenderer'
import { ContainerInformationSection } from '../components/ContainerInformationSection'
import { EntryMavPanel } from '../components/EntryMavPanel'
import { buildEntryFormValues, getEntryFormCompletion } from '../utils/entryFormUtils'
import {
  buildContainersPayload,
  getNumContainersFromFormData,
  getContainerCompletion,
  parseContainerSchema,
  parseContainersFromEntry,
  resizeContainerDraft,
  validateContainers,
  type ContainerDraftState,
  type ContainerValidationResult,
} from '../utils/containerFormUtils'
import { downloadAuthenticatedFile } from '../utils/downloadFile'

function parseFormSchemaSafe(schemaJson: string) {
  return parseFormSchema(schemaJson)
}

export function EntryFormPage() {
  const { uuid: editUuid } = useParams()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const isEdit = Boolean(editUuid)
  const agencyIdParam = searchParams.get('agencyId')
  const { data: agenciesData } = useGetAgenciesQuery()
  const { data: dashboardData } = useGetDashboardQuery()
  const { data: commoditiesData } = useGetCommoditiesQuery()
  const { data: entryData, refetch: refetchEntry } = useGetEntryQuery(editUuid ?? '', { skip: !editUuid })
  const [createEntry, { isLoading: creating, error: createError }] = useCreateEntryMutation()
  const [updateEntry, { isLoading: updating, error: updateError }] = useUpdateEntryMutation()
  const [uploadEntryFile] = useUploadEntryFileMutation()
  const [form, setForm] = useState({
    agencyId: agencyIdParam ?? '',
    entryType: 'Import',
    commodityId: '',
    commodityName: '',
    quantity: '1',
    unit: 'kg',
    originCountry: '',
    destinationCountry: 'Philippines',
    portOfEntry: '',
    notes: '',
  })
  const [dynamicValues, setDynamicValues] = useState<Record<string, string>>({})
  const [numContainers, setNumContainers] = useState(0)
  const [containerDraft, setContainerDraft] = useState<ContainerDraftState>([])
  const [containerValidation, setContainerValidation] = useState<ContainerValidationResult | undefined>()
  const [uploadingField, setUploadingField] = useState<string | null>(null)
  const agencyIdNum = form.agencyId ? Number(form.agencyId) : 0
  const { data: formsData, isFetching: isLoadingForms } = useGetClientFormsQuery(
    { agencyId: agencyIdNum, formType: 'ENTRY' },
    { skip: !agencyIdNum },
  )
  const selectedFormUuid = formsData?.data?.[0]?.uuid
  const { data: formSchemaData, isFetching: isLoadingFormSchema } = useGetClientFormQuery(selectedFormUuid ?? '', {
    skip: !selectedFormUuid,
  })
  const { data: containerFormsData, isFetching: isLoadingContainerForms } = useGetClientFormsQuery(
    { agencyId: agencyIdNum, formType: 'CONTAINER' },
    { skip: !agencyIdNum },
  )
  const selectedContainerFormUuid = containerFormsData?.data?.[0]?.uuid
  const { data: containerFormSchemaData, isFetching: isLoadingContainerFormSchema } = useGetClientFormQuery(
    selectedContainerFormUuid ?? '',
    { skip: !selectedContainerFormUuid },
  )
  const schemaFields = useMemo(
    () => (formSchemaData?.data?.schemaJson ? parseFormSchemaSafe(formSchemaData.data.schemaJson) : []),
    [formSchemaData?.data?.schemaJson],
  )
  const containerSchemaFields = useMemo(
    () => parseContainerSchema(containerFormSchemaData?.data?.schemaJson),
    [containerFormSchemaData?.data?.schemaJson],
  )
  const hasContainerForm = containerSchemaFields.length > 0
  const useDynamicForm = schemaFields.length > 0
  const noFormConfigured = !isEdit && agencyIdNum > 0 && !isLoadingForms && (formsData?.data?.length ?? 0) === 0
  const selectedAgency = (agenciesData?.data ?? []).find((a) => String(a.id) === form.agencyId)
  const agencyLogo = resolveAgencyLogoUrl(dashboardData?.data?.agencies.find((a) => a.id === agencyIdNum)?.logoUrl)

  useBreadcrumbLabel(isEdit ? entryData?.data?.referenceNo : selectedAgency?.code ?? 'New')

  useEffect(() => {
    const entry = entryData?.data
    if (!entry) return
    setForm({
      agencyId: String(entry.agencyId),
      entryType: entry.entryType,
      commodityId: entry.detail?.commodityId ? String(entry.detail.commodityId) : '',
      commodityName: entry.detail?.commodityName ?? '',
      quantity: String(entry.detail?.quantity ?? 1),
      unit: entry.detail?.unit ?? 'kg',
      originCountry: entry.detail?.originCountry ?? '',
      destinationCountry: entry.detail?.destinationCountry ?? 'Philippines',
      portOfEntry: entry.detail?.portOfEntry ?? '',
      notes: entry.notes ?? '',
    })
    setDynamicValues(buildEntryFormValues(entry, schemaFields))
    const existingContainers = parseContainersFromEntry(entry.containers, containerSchemaFields)
    const resolvedCount = getNumContainersFromFormData(entry.formDataJson, existingContainers.length)
    setNumContainers(resolvedCount)
    setContainerDraft(resizeContainerDraft(existingContainers, resolvedCount, containerSchemaFields))
    setContainerValidation(undefined)
  }, [entryData, schemaFields, containerSchemaFields])

  useEffect(() => {
    if (agencyIdParam && !isEdit) {
      setForm((prev) => ({ ...prev, agencyId: agencyIdParam }))
    }
  }, [agencyIdParam, isEdit])

  if (!isEdit && !agencyIdParam) {
    return <Navigate to="/client/entries/agencies" replace />
  }

  const containerCompletion = useMemo(
    () => getContainerCompletion(containerSchemaFields, containerDraft),
    [containerSchemaFields, containerDraft],
  )
  const formCompletion = useMemo(
    () => getEntryFormCompletion(schemaFields, dynamicValues, entryData?.data?.files),
    [schemaFields, dynamicValues, entryData?.data?.files],
  )
  const requiredFileCount = formCompletion.requiredFileCount
  const uploadedFileCount = formCompletion.uploadedFileCount

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!isEdit && !useDynamicForm) return

    if (hasContainerForm && numContainers > 0) {
      const validation = validateContainers(containerSchemaFields, containerDraft)
      setContainerValidation(validation)
      if (!validation.isValid) return
    } else {
      setContainerValidation(undefined)
    }

    const commodity = commoditiesData?.data?.find((c) => String(c.id) === form.commodityId)
    const detail = useDynamicForm && formSchemaData?.data
      ? mapDynamicValuesToEntryDetail(dynamicValues, form.commodityId, form.unit)
      : {
        commodityId: form.commodityId ? Number(form.commodityId) : null,
        commodityName: commodity?.name ?? form.commodityName,
        quantity: Number(form.quantity),
        unit: form.unit,
        originCountry: form.originCountry,
        destinationCountry: form.destinationCountry,
        portOfEntry: form.portOfEntry,
      }
    const containerPayload = hasContainerForm
      ? buildContainersPayload(dynamicValues, numContainers, containerDraft)
      : null
    const payload = {
      agencyId: Number(form.agencyId),
      entryType: form.entryType,
      notes: form.notes,
      formDataJson: containerPayload
        ? serializeFormDataJson(containerPayload.formDataJsonValues)
        : useDynamicForm
          ? serializeFormDataJson(dynamicValues)
          : undefined,
      numContainers: containerPayload?.numContainers ?? 0,
      containersJson: containerPayload?.containersJson ?? '[]',
      detail,
      mavNo: dynamicValues.mav_no?.trim() || undefined,
    }

    try {
      if (isEdit && editUuid) {
        const result = await updateEntry({
          uuid: editUuid,
          body: {
            notes: form.notes,
            formDataJson: payload.formDataJson,
            numContainers: payload.numContainers,
            containersJson: payload.containersJson,
            detail: payload.detail,
            mavNo: dynamicValues.mav_no?.trim() || entryData?.data?.mav?.mavNo || undefined,
          },
        }).unwrap()
        if (result.success) navigate(`/client/entries/${editUuid}`)
        return
      }

      const result = await createEntry(payload).unwrap()
      if (result.success) navigate(`/client/entries/${result.data.uuid}/edit`)
    } catch {
      // RTK mutation error is surfaced via createError / updateError.
    }
  }

  const handleFormFileUpload = async (fieldName: string, file: File) => {
    if (!editUuid) return
    setUploadingField(fieldName)
    try {
      const result = await uploadEntryFile({ uuid: editUuid, file, documentType: fieldName }).unwrap()
      const uploaded = result.data as { uuid: string; originalFileName: string }
      const nextValues = {
        ...dynamicValues,
        [fieldName]: uploaded.originalFileName,
        [`${fieldName}_file_uuid`]: uploaded.uuid,
      }
      setDynamicValues(nextValues)

      if (useDynamicForm) {
        await updateEntry({
          uuid: editUuid,
          body: {
            notes: form.notes,
            formDataJson: serializeFormDataJson(nextValues),
            detail: mapDynamicValuesToEntryDetail(nextValues, form.commodityId, form.unit),
          },
        }).unwrap()
      }

      refetchEntry()
    } finally {
      setUploadingField(null)
    }
  }

  const handleDownloadFile = async (fileUuid: string, fileName: string) => {
    if (!editUuid) return
    await downloadAuthenticatedFile(`/entries/${editUuid}/files/${fileUuid}/download`, fileName)
  }

  const error = createError || updateError
  const isLoading = creating || updating
  const isFormLoading = !isEdit && agencyIdNum > 0 && (isLoadingForms || (Boolean(selectedFormUuid) && isLoadingFormSchema))
  const isContainerFormLoading = hasContainerForm && (isLoadingContainerForms || (Boolean(selectedContainerFormUuid) && isLoadingContainerFormSchema))
  const canSubmit = !isLoading && !isFormLoading && !isContainerFormLoading && (isEdit || (useDynamicForm && !noFormConfigured))
  const filesPendingDraft = !isEdit && useDynamicForm

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Entries"
        title={isEdit ? 'Edit Draft Entry' : 'Submit New Entry'}
        subtitle={
          isEdit
            ? 'Update your draft and attach required documents before submission.'
            : selectedAgency
              ? `Complete the ${selectedAgency.code} entry form, then save draft to upload documents.`
              : 'Complete the agency entry form.'
        }
        actions={
          <Button
            component={RouterLink}
            to={isEdit && editUuid ? `/client/entries/${editUuid}` : '/client/entries/agencies'}
            variant="outlined"
            startIcon={<ArrowBackOutlinedIcon />}
            sx={portalOutlinedButtonSx}
          >
            {isEdit ? 'Back to entry' : 'Choose agency'}
          </Button>
        }
      />

      {error && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: '0.75rem' }}>
          Unable to save entry. Ensure you are accredited for new entries.
        </Alert>
      )}
      {noFormConfigured && (
        <Alert severity="warning" sx={{ mb: 3, borderRadius: '0.75rem' }}>
          No entry form is configured for this agency yet. Please choose another agency or contact support.
        </Alert>
      )}

      <Box
        component="form"
        onSubmit={handleSubmit}
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1fr) 320px' },
          gap: 3,
          alignItems: 'start',
        }}
      >
        <Stack spacing={3}>
          {(selectedAgency || entryData?.data) && (
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 2,
                p: 2.5,
                borderRadius: '0.75rem',
                border: `1px solid ${portalColors.border}`,
                bgcolor: portalColors.bgWhite,
              }}
            >
              <Box
                sx={{
                  width: 56,
                  height: 56,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  borderRadius: '0.75rem',
                  border: `1px solid ${portalColors.border}`,
                  bgcolor: portalColors.bgMuted,
                  flexShrink: 0,
                }}
              >
                {agencyLogo ? (
                  <Box
                    component="img"
                    src={agencyLogo}
                    alt={selectedAgency?.name ?? entryData?.data?.agencyName}
                    sx={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }}
                  />
                ) : (
                  <BusinessOutlinedIcon sx={{ color: portalColors.primary, fontSize: 28 }} />
                )}
              </Box>
              <Box sx={{ minWidth: 0 }}>
                <Typography sx={{ fontSize: '0.75rem', fontWeight: 600, color: portalColors.textMuted, textTransform: 'uppercase', letterSpacing: '0.08em' }}>
                  Submitting to
                </Typography>
                <Typography sx={{ fontSize: '1.0625rem', fontWeight: 700, color: portalColors.textDark }}>
                  {selectedAgency
                    ? `${selectedAgency.code} — ${selectedAgency.name}`
                    : `${entryData?.data?.agencyCode} — ${entryData?.data?.agencyName}`}
                </Typography>
                {formSchemaData?.data?.name && (
                  <Typography sx={{ mt: 0.35, fontSize: '0.8125rem', color: portalColors.textMuted }}>
                    Form: {formSchemaData.data.name}
                  </Typography>
                )}
              </Box>
            </Box>
          )}

          <PortalPanel title="Entry setup">
            <Box sx={{ px: 2.5, py: 2.5 }}>
              <TextField
                select
                fullWidth
                size="small"
                label="Entry type"
                value={form.entryType}
                onChange={(e) => setForm({ ...form, entryType: e.target.value })}
                disabled={isEdit}
                sx={{
                  maxWidth: 360,
                  '& .MuiOutlinedInput-root': { borderRadius: '0.5rem' },
                }}
              >
                <MenuItem value="Import">Import</MenuItem>
                <MenuItem value="Export">Export</MenuItem>
              </TextField>
            </Box>
          </PortalPanel>

          <PortalPanel title={formSchemaData?.data?.name ?? 'Entry details'}>
            <Box sx={{ px: 2.5, py: 2.5 }}>
              {isFormLoading ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
                  <CircularProgress size={28} sx={{ color: portalColors.primary }} />
                </Box>
              ) : useDynamicForm && formSchemaData?.data ? (
                <DynamicFormRenderer
                  schemaJson={formSchemaData.data.schemaJson}
                  values={dynamicValues}
                  onChange={(name, value) => setDynamicValues((prev) => ({ ...prev, [name]: value }))}
                  onFileUpload={isEdit ? handleFormFileUpload : undefined}
                  onFileDownload={isEdit ? handleDownloadFile : undefined}
                  uploadingField={uploadingField}
                  filesPendingDraft={filesPendingDraft}
                />
              ) : isEdit ? (
                <Stack spacing={2}>
                  <TextField select label="Commodity" value={form.commodityId} onChange={(e) => setForm({ ...form, commodityId: e.target.value })} fullWidth size="small">
                    {(commoditiesData?.data ?? []).map((c) => <MenuItem key={c.id} value={String(c.id)}>{c.name}</MenuItem>)}
                  </TextField>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                    <TextField label="Quantity" type="number" value={form.quantity} onChange={(e) => setForm({ ...form, quantity: e.target.value })} required fullWidth size="small" />
                    <TextField label="Unit" value={form.unit} onChange={(e) => setForm({ ...form, unit: e.target.value })} fullWidth size="small" />
                  </Stack>
                  <TextField label="Origin Country" value={form.originCountry} onChange={(e) => setForm({ ...form, originCountry: e.target.value })} fullWidth size="small" />
                  <TextField label="Destination Country" value={form.destinationCountry} onChange={(e) => setForm({ ...form, destinationCountry: e.target.value })} fullWidth size="small" />
                  <TextField label="Port of Entry" value={form.portOfEntry} onChange={(e) => setForm({ ...form, portOfEntry: e.target.value })} fullWidth size="small" />
                </Stack>
              ) : null}

              {(useDynamicForm || isEdit) && (
                <Box sx={{ mt: 3, pt: 3, borderTop: `1px solid ${portalColors.border}` }}>
                  <TextField
                    label="Additional notes"
                    value={form.notes}
                    onChange={(e) => setForm({ ...form, notes: e.target.value })}
                    multiline
                    rows={3}
                    fullWidth
                    size="small"
                    placeholder="Optional remarks for the evaluating agency"
                    sx={{ '& .MuiOutlinedInput-root': { borderRadius: '0.5rem' } }}
                  />
                </Box>
              )}
            </Box>
          </PortalPanel>

          {isContainerFormLoading ? (
            <PortalPanel title="Container Information">
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
                <CircularProgress size={28} sx={{ color: portalColors.primary }} />
              </Box>
            </PortalPanel>
          ) : hasContainerForm ? (
            <ContainerInformationSection
              schemaFields={containerSchemaFields}
              numContainers={numContainers}
              containers={containerDraft}
              validation={containerValidation}
              disabled={isLoading}
              onNumContainersChange={(count) => {
                setNumContainers(count)
                setContainerDraft((current) => resizeContainerDraft(current, count, containerSchemaFields))
                setContainerValidation(undefined)
              }}
              onContainersChange={(next) => {
                setContainerDraft(next)
                setContainerValidation(undefined)
              }}
              onClearDraft={() => {
                setNumContainers(0)
                setContainerDraft([])
                setContainerValidation(undefined)
              }}
            />
          ) : (
            <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
              Container tracking is not available for this agency.
            </Alert>
          )}

          {isEdit && form.entryType === 'Import' && entryData?.data && (
            <EntryMavPanel entry={entryData.data} editable onUpdated={() => refetchEntry()} />
          )}
        </Stack>

        <Box sx={{ position: { lg: 'sticky' }, top: 24 }}>
          <PortalPanel title="Summary">
            <Box sx={{ px: 2.5, py: 2.5 }}>
              <Stack spacing={2}>
                <Stack spacing={1.25}>
                  <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>Agency</Typography>
                    <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
                      {selectedAgency?.code ?? entryData?.data?.agencyCode ?? '—'}
                    </Typography>
                  </Stack>
                  <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>Entry type</Typography>
                    <Chip size="small" label={form.entryType} sx={getStatusBadgeStyle('Approved')} />
                  </Stack>
                  {useDynamicForm && requiredFileCount > 0 && (
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>Documents</Typography>
                      <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
                        {uploadedFileCount} / {requiredFileCount} uploaded
                      </Typography>
                    </Stack>
                  )}
                  {hasContainerForm && numContainers > 0 && (
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>Containers</Typography>
                      <Typography sx={{ fontSize: '0.8125rem', fontWeight: 600, color: portalColors.textDark }}>
                        {containerCompletion.completed} / {containerCompletion.total} completed
                      </Typography>
                    </Stack>
                  )}
                </Stack>

                {filesPendingDraft && requiredFileCount > 0 && (
                  <Alert severity="info" icon={<DescriptionOutlinedIcon fontSize="inherit" />} sx={{ borderRadius: '0.625rem', py: 0.5 }}>
                    Save draft first, then return here to upload required PDFs.
                  </Alert>
                )}

                <Divider />

                <Stack spacing={1.25}>
                  <Button type="submit" variant="contained" fullWidth sx={portalPrimaryButtonSx} disabled={!canSubmit}>
                    {isLoading ? 'Saving…' : isEdit ? 'Save changes' : 'Save draft'}
                  </Button>
                  <Button
                    component={RouterLink}
                    to={isEdit && editUuid ? `/client/entries/${editUuid}` : '/client/entries/agencies'}
                    variant="outlined"
                    fullWidth
                    sx={portalOutlinedButtonSx}
                  >
                    Cancel
                  </Button>
                </Stack>

                {!isEdit && (
                  <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, lineHeight: 1.5 }}>
                    After saving, you can attach documents and submit the entry for agency review.
                  </Typography>
                )}
              </Stack>
            </Box>
          </PortalPanel>
        </Box>
      </Box>
    </Box>
  )
}
