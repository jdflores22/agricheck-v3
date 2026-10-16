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
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { FormEvent, useEffect, useMemo, useRef, useState } from 'react'
import { Link as RouterLink, Navigate, useLocation, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import type { ApiEnvelope } from '../../auth/types'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { resolveAgencyLogoUrl } from '../../admin/components/adminAgencyUtils'
import { applyMavEntrySchema, getCommodityHsValueKeys, parseFormSchema, serializeFormDataJson } from '../../forms/formSchema'

function getApiErrorMessage(error: unknown): string | null {
  if (!error || typeof error !== 'object' || !('data' in error)) {
    return null
  }

  const data = (error as FetchBaseQueryError).data as ApiEnvelope<unknown> | undefined
  return data?.errors?.[0]?.message ?? null
}
import {
  useCreateEntryMutation,
  useGetAgenciesQuery,
  useGetClientFormQuery,
  useGetClientFormsQuery,
  useGetCommoditiesQuery,
  useGetDashboardQuery,
  useGetEntryQuery,
  useGetFacilityRegistrationsQuery,
  useUpdateEntryMutation,
  useUploadEntryFileMutation,
  useUtilizeEntryMicMutation,
  useValidateEntryDestinationMutation,
} from '../api/clientApi'
import { useGetMavAgencyContextQuery } from '../../mav/api/mavApi'
import { DynamicFormRenderer, mapDynamicValuesToEntryDetail } from '../components/DynamicFormRenderer'
import { ContainerInformationSection } from '../components/ContainerInformationSection'
import { EntryMavPanel } from '../components/EntryMavPanel'
import { EntryMavUtilizeDraft } from '../components/EntryMavUtilizeDraft'
import { ImportTrackSelector, type ImportTrack } from '../components/ImportTrackSelector'
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
import { ENTRY_CONTAINER_EDIT_HASH } from '../utils/entrySubmitValidation'
import {
  DESTINATION_SCOPE_CHECK_CODES,
  entryAgencyWarehouseFilterHint,
  resolveEffectiveWarehouseFacilityId,
  resolveWarehouseIdFromFormFields,
} from '../utils/entryDestinationUtils'
import { downloadAuthenticatedFile } from '../utils/downloadFile'

function parseFormSchemaSafe(schemaJson: string) {
  return parseFormSchema(schemaJson)
}

export function EntryFormPage() {
  const { uuid: editUuid } = useParams()
  const [searchParams] = useSearchParams()
  const location = useLocation()
  const navigate = useNavigate()
  const micLinkFailed = Boolean((location.state as { micLinkFailed?: boolean } | null)?.micLinkFailed)
  const isEdit = Boolean(editUuid)
  const agencyIdParam = searchParams.get('agencyId')
  const { data: agenciesData } = useGetAgenciesQuery()
  const { data: dashboardData } = useGetDashboardQuery()
  const { data: commoditiesData } = useGetCommoditiesQuery()
  const { data: entryData, refetch: refetchEntry } = useGetEntryQuery(editUuid ?? '', { skip: !editUuid })
  const [createEntry, { isLoading: creating, error: createError }] = useCreateEntryMutation()
  const [updateEntry, { isLoading: updating, error: updateError }] = useUpdateEntryMutation()
  const [uploadEntryFile] = useUploadEntryFileMutation()
  const [utilizeMic] = useUtilizeEntryMicMutation()
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
  const [utilizeDraft, setUtilizeDraft] = useState({ micUuid: '', volume: '' })
  const [importTrack, setImportTrack] = useState<ImportTrack>('Regular')
  const [finalWarehouseFacilityId, setFinalWarehouseFacilityId] = useState('')
  const [validateDestination] = useValidateEntryDestinationMutation()
  const [destinationValidation, setDestinationValidation] = useState<{
    isValid: boolean
    checks: Array<{ code: string; severity: string; message: string; detail?: string | null }>
  } | null>(null)
  const [validatingDestination, setValidatingDestination] = useState(false)
  const agencyIdNum = form.agencyId ? Number(form.agencyId) : 0
  const { data: linkedFacilitiesData } = useGetFacilityRegistrationsQuery(agencyIdNum, {
    skip: form.entryType !== 'Import' || !agencyIdNum,
  })
  const linkedDestinationWarehouses = (linkedFacilitiesData?.data ?? []).map((f) => ({
    id: f.warehouseFacilityId,
    code: f.warehouseCode,
    name: f.warehouseName,
    scopeHint: f.facilityScopeHint,
    leadAgency: f.cswLeadAgencyCode,
  }))
  const allowedWarehouseIds = useMemo(
    () => linkedDestinationWarehouses.map((w) => w.id),
    [linkedDestinationWarehouses],
  )
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
  const { data: mavContextData } = useGetMavAgencyContextQuery(agencyIdNum, {
    skip: !agencyIdNum || form.entryType !== 'Import',
  })
  const mavProgramAvailable = form.entryType === 'Import' && Boolean(mavContextData?.data?.isProgramAvailable ?? mavContextData?.data?.isActive)
  const importerHasMavAccess = Boolean(
    mavContextData?.data?.importerHasMavAccess
    ?? ((mavContextData?.data?.activeLicenseCount ?? 0) > 0 || (mavContextData?.data?.availableMicCount ?? 0) > 0),
  )
  const mavTrackSelected = importTrack === 'Mav'
  const trackInitialized = useRef(isEdit)
  const effectiveSchemaFields = useMemo(
    () => applyMavEntrySchema(schemaFields, {
      showMavFields: mavTrackSelected,
      requireCommodityHs: mavProgramAvailable,
    }),
    [schemaFields, mavTrackSelected, mavProgramAvailable],
  )
  const commodityField = effectiveSchemaFields.find((field) => field.type === 'commodity')
  const commodityHsKeys = commodityField ? getCommodityHsValueKeys(commodityField.name) : null
  const selectedHsCode = commodityHsKeys ? dynamicValues[commodityHsKeys.hsCode] : ''
  const selectedCommodityName = commodityHsKeys ? dynamicValues[commodityHsKeys.commodityName] : ''
  const entryQuantity = Number(dynamicValues.quantity || dynamicValues.txt_volume_weight || form.quantity || 0)
  const warehouseIdFromDynamicForm = useMemo(
    () => resolveWarehouseIdFromFormFields(effectiveSchemaFields, dynamicValues),
    [effectiveSchemaFields, dynamicValues],
  )
  const effectiveWarehouseFacilityId = useMemo(
    () => resolveEffectiveWarehouseFacilityId(effectiveSchemaFields, dynamicValues, finalWarehouseFacilityId),
    [effectiveSchemaFields, dynamicValues, finalWarehouseFacilityId],
  )
  const hasExistingMav = Boolean(
    entryData?.data?.mav?.mavNo
    || (entryData?.data?.mav?.micUtilizations?.length ?? 0) > 0,
  )

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
    setFinalWarehouseFacilityId(
      entry.finalWarehouseFacilityId ? String(entry.finalWarehouseFacilityId) : '',
    )
    setDynamicValues(buildEntryFormValues(entry, schemaFields))
    const existingContainers = parseContainersFromEntry(entry.containers, containerSchemaFields)
    const resolvedCount = getNumContainersFromFormData(entry.formDataJson, existingContainers.length)
    setNumContainers(resolvedCount)
    setContainerDraft(resizeContainerDraft(existingContainers, resolvedCount, containerSchemaFields))
    setContainerValidation(undefined)
    const savedTrack = entry.mav?.importTrack === 'Mav'
      || Boolean(entry.mav?.mavNo || (entry.mav?.micUtilizations?.length ?? 0) > 0)
      ? 'Mav'
      : 'Regular'
    setImportTrack(savedTrack)
    trackInitialized.current = true
  }, [entryData, schemaFields, containerSchemaFields])

  useEffect(() => {
    if (isEdit || trackInitialized.current || !mavContextData?.data) return
    trackInitialized.current = true
    const access = Boolean(
      mavContextData.data.importerHasMavAccess
      ?? ((mavContextData.data.activeLicenseCount ?? 0) > 0 || (mavContextData.data.availableMicCount ?? 0) > 0),
    )
    setImportTrack(access ? 'Mav' : 'Regular')
  }, [isEdit, mavContextData])

  useEffect(() => {
    if (location.hash !== `#${ENTRY_CONTAINER_EDIT_HASH}`) return
    if (!hasContainerForm) return
    const node = document.getElementById(ENTRY_CONTAINER_EDIT_HASH)
    node?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [hasContainerForm, location.hash])

  useEffect(() => {
    if (agencyIdParam && !isEdit) {
      setForm((prev) => ({ ...prev, agencyId: agencyIdParam }))
    }
  }, [agencyIdParam, isEdit])

  useEffect(() => {
    if (!warehouseIdFromDynamicForm || warehouseIdFromDynamicForm === finalWarehouseFacilityId) {
      return
    }
    if (!allowedWarehouseIds.includes(Number(warehouseIdFromDynamicForm))) {
      return
    }
    setFinalWarehouseFacilityId(warehouseIdFromDynamicForm)
  }, [warehouseIdFromDynamicForm, finalWarehouseFacilityId, allowedWarehouseIds])

  useEffect(() => {
    if (!finalWarehouseFacilityId) {
      return
    }
    if (!allowedWarehouseIds.includes(Number(finalWarehouseFacilityId))) {
      setFinalWarehouseFacilityId('')
    }
  }, [allowedWarehouseIds, finalWarehouseFacilityId])

  const containerCompletion = useMemo(
    () => getContainerCompletion(containerSchemaFields, containerDraft),
    [containerSchemaFields, containerDraft],
  )
  const formCompletion = useMemo(
    () => getEntryFormCompletion(effectiveSchemaFields, dynamicValues, entryData?.data?.files),
    [effectiveSchemaFields, dynamicValues, entryData?.data?.files],
  )
  const requiredFileCount = formCompletion.requiredFileCount
  const uploadedFileCount = formCompletion.uploadedFileCount

  const destinationValidationPayload = useMemo(() => {
    if (form.entryType !== 'Import' || !effectiveWarehouseFacilityId) {
      return null
    }
    const detail = useDynamicForm && formSchemaData?.data
      ? mapDynamicValuesToEntryDetail(dynamicValues, form.commodityId, form.unit)
      : {
          quantity: Number(form.quantity),
          unit: form.unit,
        }
    const valuesForSave = { ...dynamicValues, import_track: importTrack }
    const containerPayload = hasContainerForm
      ? buildContainersPayload(valuesForSave, numContainers, containerDraft)
      : null
    const formDataJson = containerPayload
      ? serializeFormDataJson(containerPayload.formDataJsonValues)
      : useDynamicForm
        ? serializeFormDataJson(valuesForSave)
        : serializeFormDataJson({ import_track: importTrack })
    return {
      warehouseFacilityId: Number(effectiveWarehouseFacilityId),
      shipmentQuantity: detail.quantity,
      shipmentUnit: detail.unit,
      excludeEntryUuid: editUuid,
      formDataJson,
      entryAgencyId: agencyIdNum,
    }
  }, [
    form.entryType,
    effectiveWarehouseFacilityId,
    useDynamicForm,
    formSchemaData?.data,
    dynamicValues,
    form.commodityId,
    form.unit,
    form.quantity,
    importTrack,
    hasContainerForm,
    numContainers,
    containerDraft,
    editUuid,
    agencyIdNum,
  ])

  useEffect(() => {
    if (!destinationValidationPayload) {
      setDestinationValidation(null)
      setValidatingDestination(false)
      return
    }

    setDestinationValidation(null)
    setValidatingDestination(true)

    let cancelled = false
    const handle = window.setTimeout(() => {
      validateDestination(destinationValidationPayload)
        .unwrap()
        .then((res) => {
          if (!cancelled && res.success && res.data) {
            setDestinationValidation({ isValid: res.data.isValid, checks: res.data.checks })
          }
        })
        .catch((err) => {
          if (!cancelled) {
            setDestinationValidation({
              isValid: false,
              checks: [{
                code: 'VALIDATION_REQUEST_FAILED',
                severity: 'warning',
                message: getApiErrorMessage(err) ?? 'Could not verify warehouse destination. Try again.',
                detail: null,
              }],
            })
          }
        })
        .finally(() => {
          if (!cancelled) {
            setValidatingDestination(false)
          }
        })
    }, 450)

    return () => {
      cancelled = true
      window.clearTimeout(handle)
    }
  }, [destinationValidationPayload, validateDestination])

  const destinationHasBlockingError = Boolean(
    destinationValidation?.checks.some((c) => c.severity === 'error'),
  )

  const destinationChecksToShow = useMemo(() => {
    const checks = destinationValidation?.checks ?? []
    return checks.filter((c) => c.severity !== 'ok')
  }, [destinationValidation?.checks])

  const commodityScopeChecks = useMemo(
    () => destinationChecksToShow.filter((c) => DESTINATION_SCOPE_CHECK_CODES.has(c.code)),
    [destinationChecksToShow],
  )

  if (!isEdit && !agencyIdParam) {
    return <Navigate to="/client/entries/agencies" replace />
  }

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

    if (
      form.entryType === 'Import'
      && effectiveWarehouseFacilityId
      && destinationHasBlockingError
    ) {
      return
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
    const valuesForSave = {
      ...dynamicValues,
      import_track: importTrack,
    }
    const containerPayload = hasContainerForm
      ? buildContainersPayload(valuesForSave, numContainers, containerDraft)
      : null
    const payload = {
      agencyId: Number(form.agencyId),
      entryType: form.entryType,
      notes: form.notes,
      formDataJson: containerPayload
        ? serializeFormDataJson(containerPayload.formDataJsonValues)
        : useDynamicForm
          ? serializeFormDataJson(valuesForSave)
          : serializeFormDataJson({ import_track: importTrack }),
      numContainers: containerPayload?.numContainers ?? 0,
      containersJson: containerPayload?.containersJson ?? '[]',
      detail,
      importTrack,
      mavNo: mavTrackSelected ? (dynamicValues.mav_no?.trim() || undefined) : undefined,
      finalWarehouseFacilityId:
        form.entryType === 'Import' && effectiveWarehouseFacilityId
          ? Number(effectiveWarehouseFacilityId)
          : undefined,
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
            importTrack,
            mavNo: mavTrackSelected
              ? (dynamicValues.mav_no?.trim() || entryData?.data?.mav?.mavNo || undefined)
              : undefined,
            finalWarehouseFacilityId:
              form.entryType === 'Import' && effectiveWarehouseFacilityId
                ? Number(effectiveWarehouseFacilityId)
                : undefined,
          },
        }).unwrap()
        if (result.success) navigate(`/client/entries/${editUuid}`)
        return
      }

      const result = await createEntry(payload).unwrap()
      if (result.success) {
        const volume = Number(utilizeDraft.volume)
        if (mavTrackSelected && utilizeDraft.micUuid && volume > 0) {
          try {
            await utilizeMic({
              uuid: result.data.uuid,
              micUuid: utilizeDraft.micUuid,
              volume,
            }).unwrap()
          } catch {
            navigate(`/client/entries/${result.data.uuid}/edit`, { state: { micLinkFailed: true } })
            return
          }
        }
        navigate(`/client/entries/${result.data.uuid}/edit`)
      }
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
        const valuesForSave = { ...nextValues, import_track: importTrack }
        const containerPayload = hasContainerForm
          ? buildContainersPayload(valuesForSave, numContainers, containerDraft)
          : null
        await updateEntry({
          uuid: editUuid,
          body: {
            notes: form.notes,
            formDataJson: containerPayload
              ? serializeFormDataJson(containerPayload.formDataJsonValues)
              : serializeFormDataJson(valuesForSave),
            importTrack,
            numContainers: containerPayload?.numContainers ?? numContainers,
            containersJson: containerPayload?.containersJson,
            detail: mapDynamicValuesToEntryDetail(nextValues, form.commodityId, form.unit),
            mavNo: mavTrackSelected
              ? (nextValues.mav_no?.trim() || entryData?.data?.mav?.mavNo || undefined)
              : undefined,
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

  const mutationError = createError || updateError
  const errorMessage = getApiErrorMessage(mutationError)
    ?? (mutationError && typeof mutationError === 'object' && 'status' in mutationError
      ? `Unable to save entry (HTTP ${String((mutationError as FetchBaseQueryError).status)}).`
      : null)
  const isLoading = creating || updating
  const isFormLoading = !isEdit && agencyIdNum > 0 && (isLoadingForms || (Boolean(selectedFormUuid) && isLoadingFormSchema))
  const isContainerFormLoading = hasContainerForm && (isLoadingContainerForms || (Boolean(selectedContainerFormUuid) && isLoadingContainerFormSchema))
  const destinationBlocksSave = form.entryType === 'Import'
    && Boolean(effectiveWarehouseFacilityId)
    && (validatingDestination || destinationHasBlockingError)
  const canSubmit = !isLoading
    && !isFormLoading
    && !isContainerFormLoading
    && !destinationBlocksSave
    && (isEdit || (useDynamicForm && !noFormConfigured))
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

      {micLinkFailed && (
        <Alert severity="warning" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Draft saved, but MIC volume was not linked. Use MAV & MIC below to add utilization.
        </Alert>
      )}

      {errorMessage && (
        <Alert severity="error" sx={{ mb: 3, borderRadius: '0.75rem' }}>
          {errorMessage}
          {errorMessage.includes('Accreditation') ? null : (
            <Typography component="span" sx={{ display: 'block', mt: 0.5, fontSize: '0.8125rem' }}>
              If you are accredited, this may be a server/database issue — try again after a minute or contact support.
            </Typography>
          )}
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
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                spacing={2.5}
                sx={{ alignItems: { sm: 'flex-start' } }}
              >
                <TextField
                  select
                  size="small"
                  label="Entry type"
                  value={form.entryType}
                  onChange={(e) => setForm({ ...form, entryType: e.target.value })}
                  disabled={isEdit}
                  sx={{
                    minWidth: 180,
                    maxWidth: { sm: 220 },
                    width: { xs: '100%', sm: 'auto' },
                    '& .MuiOutlinedInput-root': { borderRadius: '0.5rem' },
                  }}
                >
                  <MenuItem value="Import">Import</MenuItem>
                  <MenuItem value="Export">Export</MenuItem>
                </TextField>
                {form.entryType === 'Import' && (
                  <TextField
                    select
                    size="small"
                    label="Final warehouse (CSW destination)"
                    value={finalWarehouseFacilityId}
                    onChange={(e) => setFinalWarehouseFacilityId(e.target.value)}
                    helperText={entryAgencyWarehouseFilterHint(selectedAgency?.code)}
                    sx={{
                      minWidth: 260,
                      flex: 1,
                      width: { xs: '100%', sm: 'auto' },
                      '& .MuiOutlinedInput-root': { borderRadius: '0.5rem' },
                    }}
                  >
                    <MenuItem value="">
                      <em>Not selected yet</em>
                    </MenuItem>
                    {linkedDestinationWarehouses.length === 0 ? (
                      <MenuItem value="" disabled>
                        No {selectedAgency?.code ?? 'agency'}-compatible warehouses — link an LTO facility under My Facilities
                        with matching CSW/RSAS scope
                      </MenuItem>
                    ) : (
                      linkedDestinationWarehouses.map((warehouse) => (
                        <MenuItem key={warehouse.id} value={String(warehouse.id)}>
                          {warehouse.code} — {warehouse.name}
                          {warehouse.leadAgency ? ` (${warehouse.leadAgency})` : ''}
                        </MenuItem>
                      ))
                    )}
                  </TextField>
                )}
                {mavProgramAvailable && (
                  <ImportTrackSelector
                    value={importTrack}
                    importerHasMavAccess={importerHasMavAccess || hasExistingMav}
                    disabled={hasExistingMav}
                    onChange={(track) => {
                      setImportTrack(track)
                      setDynamicValues((prev) => {
                        const next: Record<string, string> = { ...prev, import_track: track }
                        if (track === 'Regular') {
                          delete next.mav_no
                          delete next.mav_certificate
                          delete next.mav_certificate_file_uuid
                        }
                        return next
                      })
                    }}
                  />
                )}
              </Stack>
              {form.entryType === 'Import' && effectiveWarehouseFacilityId && (
                <Box sx={{ mt: 2 }}>
                  {validatingDestination && (
                    <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center' }}>
                      <CircularProgress size={16} />
                      <Typography variant="body2" color="text.secondary">
                        Checking warehouse destination…
                      </Typography>
                    </Stack>
                  )}
                  {destinationChecksToShow.map((check) => (
                      <Alert
                        key={check.code}
                        severity={
                          check.severity === 'error'
                            ? 'error'
                            : check.severity === 'warning'
                              ? 'warning'
                              : 'info'
                        }
                        sx={{ mb: 1, borderRadius: '0.75rem' }}
                      >
                        {check.message}
                        {check.detail ? (
                          <Typography component="span" sx={{ display: 'block', mt: 0.5, fontSize: '0.8125rem' }}>
                            {check.detail}
                          </Typography>
                        ) : null}
                      </Alert>
                    ))}
                  {destinationValidation?.isValid && !destinationHasBlockingError && !validatingDestination && (
                    <Alert severity="success" sx={{ borderRadius: '0.75rem' }}>
                      Destination checks passed for this warehouse and shipment volume.
                    </Alert>
                  )}
                </Box>
              )}
            </Box>
          </PortalPanel>

          <PortalPanel title={formSchemaData?.data?.name ?? 'Entry details'}>
            <Box sx={{ px: 2.5, py: 2.5 }}>
              {isFormLoading ? (
                <Box sx={{ display: 'flex', justifyContent: 'center', py: 5 }}>
                  <CircularProgress size={28} sx={{ color: portalColors.primary }} />
                </Box>
              ) : useDynamicForm && formSchemaData?.data ? (
                <>
                  <DynamicFormRenderer
                    schemaJson={formSchemaData.data.schemaJson}
                    values={dynamicValues}
                    onChange={(name, value) => setDynamicValues((prev) => ({ ...prev, [name]: value }))}
                    onBatchChange={(updates) => setDynamicValues((prev) => ({ ...prev, ...updates }))}
                    agencyId={agencyIdNum || undefined}
                    showMavFields={mavTrackSelected}
                    showCommodityHs={mavProgramAvailable}
                    onFileUpload={isEdit ? handleFormFileUpload : undefined}
                    onFileDownload={isEdit ? handleDownloadFile : undefined}
                    uploadingField={uploadingField}
                    filesPendingDraft={filesPendingDraft}
                    allowedWarehouseIds={allowedWarehouseIds}
                  />
                  {form.entryType === 'Import' && effectiveWarehouseFacilityId && selectedHsCode && (
                    <Box sx={{ mt: 2.5 }}>
                      <Typography variant="subtitle2" sx={{ mb: 1 }}>
                        Warehouse product scope (LTO / RSAS)
                      </Typography>
                      {validatingDestination && (
                        <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center' }}>
                          <CircularProgress size={16} />
                          <Typography variant="body2" color="text.secondary">
                            Checking meat vs fish segregation and your contract scope…
                          </Typography>
                        </Stack>
                      )}
                      {commodityScopeChecks.map((check) => (
                        <Alert
                          key={`scope-${check.code}`}
                          severity={
                            check.severity === 'error'
                              ? 'error'
                              : check.severity === 'warning'
                                ? 'warning'
                                : 'info'
                          }
                          sx={{ mb: 1, borderRadius: '0.75rem' }}
                        >
                          {check.message}
                          {check.detail ? (
                            <Typography component="span" sx={{ display: 'block', mt: 0.5, fontSize: '0.8125rem' }}>
                              {check.detail}
                            </Typography>
                          ) : null}
                        </Alert>
                      ))}
                      {!validatingDestination
                        && commodityScopeChecks.length === 0
                        && destinationValidation
                        && !destinationHasBlockingError && (
                        <Alert severity="success" sx={{ borderRadius: '0.75rem' }}>
                          This HS commodity matches the warehouse LTO scope (e.g. BAI meat-only vs BFAR fish-only).
                        </Alert>
                      )}
                    </Box>
                  )}
                </>
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

          {!isEdit && mavTrackSelected && (
            <EntryMavUtilizeDraft
              hsCode={selectedHsCode}
              commodityName={selectedCommodityName}
              quantity={entryQuantity}
              value={utilizeDraft}
              onChange={setUtilizeDraft}
            />
          )}

          {isEdit && form.entryType === 'Import' && entryData?.data && mavTrackSelected && (
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
                  {form.entryType === 'Import' && mavProgramAvailable && (
                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                      <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>Import track</Typography>
                      <Chip
                        size="small"
                        label={mavTrackSelected ? 'MAV (in-quota)' : 'Regular (out-quota)'}
                        sx={getStatusBadgeStyle(mavTrackSelected ? 'Pending' : 'Approved')}
                      />
                    </Stack>
                  )}
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

                {form.entryType === 'Import' && effectiveWarehouseFacilityId && destinationHasBlockingError && (
                  <Alert severity="error" sx={{ borderRadius: '0.75rem' }}>
                    Fix warehouse destination checks (scope, contract, or capacity) before saving this draft.
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
