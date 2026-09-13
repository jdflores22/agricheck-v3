import type { FormFieldSchema } from '../../forms/formSchema'
import { evaluateConditional, getCommodityHsValueKeys, parseFormDataJson, parseFormSchema, visibleFormFields } from '../../forms/formSchema'

export interface EntryFormCompletion {
  isComplete: boolean
  missingFields: string[]
  missingFiles: string[]
  requiredFieldCount: number
  completedFieldCount: number
  requiredFileCount: number
  uploadedFileCount: number
}

export interface EntryFormValuesSource {
  formDataJson?: string | null
  detail?: {
    commodityId?: number
    commodityName?: string
    description?: string
    quantity?: number
    unit?: string
    originCountry?: string
    destinationCountry?: string
    portOfEntry?: string
  }
  files?: Array<{ uuid: string; originalFileName: string; documentType?: string }>
}

function hasUploadedFile(
  fieldName: string,
  values: Record<string, string>,
  files?: EntryFormValuesSource['files'],
): boolean {
  if (values[`${fieldName}_file_uuid`]?.trim()) return true
  if (values[fieldName]?.trim()) return true
  return Boolean(files?.some((file) => file.documentType === fieldName))
}

function hasFieldValue(field: FormFieldSchema, values: Record<string, string>): boolean {
  if (field.type === 'checkbox') {
    const value = values[field.name] ?? ''
    return value === 'yes' || value === 'true' || value === '1'
  }

  if (field.type === 'commodity') {
    const keys = getCommodityHsValueKeys(field.name)
    return Boolean(values[keys.detailUuid]?.trim() || values[keys.commodityName]?.trim())
  }

  return Boolean(values[field.name]?.trim())
}

function isEmptyFormDataJson(formDataJson?: string | null): boolean {
  if (!formDataJson) return true
  const trimmed = formDataJson.trim()
  return trimmed === '' || trimmed === '{}' || trimmed === 'null'
}

function applyDetailFallbackValues(
  values: Record<string, string>,
  detail: NonNullable<EntryFormValuesSource['detail']>,
  schemaFields?: FormFieldSchema[],
): void {
  const commodityField = schemaFields?.find((field) => field.type === 'commodity')
  if (detail.commodityName) {
    if (commodityField && !values[commodityField.name]?.trim()) {
      values[commodityField.name] = detail.commodityName
    }
    if (!values.commodityName && !Object.keys(values).some((key) => key.startsWith('commodity_'))) {
      values.commodityName = detail.commodityName
    }
  }

  if (detail.quantity != null && detail.quantity > 0) {
    const quantity = String(detail.quantity)
    const weightField = schemaFields?.find(
      (field) => field.name === 'txt_volume_weight' || field.label.toLowerCase().includes('volume'),
    )
    if (weightField && !values[weightField.name]?.trim()) {
      values[weightField.name] = quantity
    }
    if (!values.quantity?.trim() && !values.txt_volume_weight?.trim()) {
      values.quantity = quantity
      values.txt_volume_weight = quantity
    }
  }

  if (detail.unit && !values.unit?.trim()) {
    values.unit = detail.unit
  }
  if (detail.description && !values.description?.trim()) {
    values.description = detail.description
  }
  if (detail.originCountry && !values.originCountry?.trim()) {
    values.originCountry = detail.originCountry
  }
  if (detail.destinationCountry && !values.destinationCountry?.trim()) {
    values.destinationCountry = detail.destinationCountry
  }
  if (detail.portOfEntry && !values.portOfEntry?.trim()) {
    values.portOfEntry = detail.portOfEntry
  }
}

export function buildEntryFormValues(
  source: EntryFormValuesSource,
  schemaFields?: FormFieldSchema[],
): Record<string, string> {
  const values = parseFormDataJson(source.formDataJson)
  const legacyDraft = isEmptyFormDataJson(source.formDataJson)

  for (const file of source.files ?? []) {
    if (!file.documentType) continue
    values[file.documentType] = file.originalFileName
    values[`${file.documentType}_file_uuid`] = file.uuid
  }

  const detail = source.detail
  if (detail) {
    if (legacyDraft) {
      applyDetailFallbackValues(values, detail, schemaFields)
    } else {
      if (detail.commodityName && !values.commodityName && !Object.keys(values).some((key) => key.startsWith('commodity_'))) {
        values.commodityName = detail.commodityName
      }
      if (detail.quantity && !values.quantity && !values.txt_volume_weight) {
        values.quantity = String(detail.quantity)
      }
      if (detail.unit && !values.unit) {
        values.unit = detail.unit
      }
      if (detail.originCountry && !values.originCountry) {
        values.originCountry = detail.originCountry
      }
      if (detail.destinationCountry && !values.destinationCountry) {
        values.destinationCountry = detail.destinationCountry
      }
      if (detail.portOfEntry && !values.portOfEntry) {
        values.portOfEntry = detail.portOfEntry
      }
    }
  }

  return values
}

export function getEntryFormCompletion(
  schemaFields: FormFieldSchema[],
  values: Record<string, string>,
  files?: EntryFormValuesSource['files'],
): EntryFormCompletion {
  const visible = visibleFormFields(schemaFields, values)
  const missingFields: string[] = []
  const missingFiles: string[] = []

  let requiredFieldCount = 0
  let completedFieldCount = 0
  let requiredFileCount = 0
  let uploadedFileCount = 0

  for (const field of visible) {
    if (field.type === 'section' || !field.required) {
      continue
    }

    if (field.type === 'file' || field.type === 'geotag_photo') {
      requiredFileCount += 1
      if (hasUploadedFile(field.name, values, files)) {
        uploadedFileCount += 1
      } else {
        missingFiles.push(field.label)
      }
      continue
    }

    requiredFieldCount += 1
    if (hasFieldValue(field, values)) {
      completedFieldCount += 1
    } else {
      missingFields.push(field.label)
    }
  }

  return {
    isComplete: missingFields.length === 0 && missingFiles.length === 0,
    missingFields,
    missingFiles,
    requiredFieldCount,
    completedFieldCount,
    requiredFileCount,
    uploadedFileCount,
  }
}

export function getEntryFormCompletionFromSchemaJson(
  schemaJson: string | undefined,
  source: EntryFormValuesSource,
): EntryFormCompletion {
  if (!schemaJson) {
    return {
      isComplete: true,
      missingFields: [],
      missingFiles: [],
      requiredFieldCount: 0,
      completedFieldCount: 0,
      requiredFileCount: 0,
      uploadedFileCount: 0,
    }
  }

  const schemaFields = parseFormSchema(schemaJson)
  const values = buildEntryFormValues(source, schemaFields)
  return getEntryFormCompletion(schemaFields, values, source.files)
}

export function isFieldVisible(field: FormFieldSchema, values: Record<string, string>): boolean {
  return evaluateConditional(field, values)
}

export function buildFormFieldLabelMap(schemaJson: string | undefined): Record<string, string> {
  if (!schemaJson) return {}

  return Object.fromEntries(parseFormSchema(schemaJson).map((field) => [field.name, field.label]))
}

export function humanizeFormFieldName(fieldName: string): string {
  return fieldName
    .replace(/^file_/, '')
    .replace(/^txt_/, '')
    .replace(/_/g, ' ')
    .replace(/\b\w/g, (char) => char.toUpperCase())
}

export function resolveFormFieldLabel(
  fieldName: string | undefined,
  labelMap: Record<string, string>,
): string {
  if (!fieldName) return '—'
  return labelMap[fieldName] ?? humanizeFormFieldName(fieldName)
}
