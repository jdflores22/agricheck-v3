import type { FormFieldSchema } from '../../forms/formSchema'
import { parseFormDataJson, parseFormSchema, visibleFormFields } from '../../forms/formSchema'

export const MAX_CONTAINERS = 50
export const NUM_CONTAINERS_FIELD = 'num_containers'

export type ContainerFormValues = Record<string, string>
export type ContainerDraftState = ContainerFormValues[]

export interface ContainerValidationResult {
  isValid: boolean
  errors: Record<number, Record<string, string>>
  duplicateContainerNumbers: string[]
}

function hasFieldValue(field: FormFieldSchema, values: ContainerFormValues): boolean {
  if (field.type === 'checkbox') {
    const value = values[field.name] ?? ''
    return value === 'yes' || value === 'true' || value === '1'
  }

  return Boolean(values[field.name]?.trim())
}

export function createEmptyContainerValues(schemaFields: FormFieldSchema[]): ContainerFormValues {
  const values: ContainerFormValues = {}
  for (const field of visibleFormFields(schemaFields, values)) {
    if (field.type === 'checkbox') {
      values[field.name] = 'no'
    }
  }
  return values
}

export function resizeContainerDraft(
  current: ContainerDraftState,
  count: number,
  schemaFields: FormFieldSchema[],
): ContainerDraftState {
  const nextCount = Math.max(0, Math.min(MAX_CONTAINERS, count))
  const next = current.slice(0, nextCount)
  while (next.length < nextCount) {
    next.push(createEmptyContainerValues(schemaFields))
  }
  return next
}

export function parseContainersFromEntry(
  containers: Array<{ formDataJson?: string | null }> | undefined,
  schemaFields: FormFieldSchema[],
): ContainerDraftState {
  if (!containers?.length) return []

  return containers.map((container) => {
    const values = parseFormDataJson(container.formDataJson)
    return { ...createEmptyContainerValues(schemaFields), ...values }
  })
}

export function serializeContainersJson(containers: ContainerDraftState): string {
  return JSON.stringify(containers)
}

export function getContainerType(values: ContainerFormValues): string {
  return (
    values.select_container_type?.trim()
    || values.container_type?.trim()
    || values.containerType?.trim()
    || ''
  )
}

export function getContainerNumber(values: ContainerFormValues): string {
  return (
    values.container_number?.trim()
    || values.containerNumber?.trim()
    || values.txt_container_number?.trim()
    || values.text_container_number?.trim()
    || ''
  )
}

export function isContainerComplete(schemaFields: FormFieldSchema[], values: ContainerFormValues): boolean {
  for (const field of visibleFormFields(schemaFields, values)) {
    if (!field.required) continue
    if (field.type === 'section' || field.type === 'file' || field.type === 'geotag_photo') continue
    if (!hasFieldValue(field, values)) return false
  }
  return true
}

export function getContainerCompletion(schemaFields: FormFieldSchema[], containers: ContainerDraftState) {
  const completed = containers.filter((values) => isContainerComplete(schemaFields, values)).length
  const total = containers.length
  const percent = total === 0 ? 0 : Math.round((completed / total) * 100)
  return { completed, total, percent }
}

export function validateContainers(
  schemaFields: FormFieldSchema[],
  containers: ContainerDraftState,
): ContainerValidationResult {
  const errors: Record<number, Record<string, string>> = {}
  const duplicateContainerNumbers: string[] = []
  const seenNumbers = new Map<string, number>()

  containers.forEach((values, index) => {
    for (const field of visibleFormFields(schemaFields, values)) {
      if (!field.required) continue
      if (field.type === 'section' || field.type === 'file' || field.type === 'geotag_photo') continue
      if (hasFieldValue(field, values)) continue

      if (!errors[index]) errors[index] = {}
      errors[index][field.name] = 'This field is required'
    }

    const containerNumber = getContainerNumber(values)
    if (containerNumber) {
      const normalized = containerNumber.toLowerCase()
      if (seenNumbers.has(normalized)) {
        duplicateContainerNumbers.push(containerNumber)
        if (!errors[index]) errors[index] = {}
        errors[index].container_number = 'Duplicate container number'
        const firstIndex = seenNumbers.get(normalized)!
        if (!errors[firstIndex]) errors[firstIndex] = {}
        errors[firstIndex].container_number = 'Duplicate container number'
      } else {
        seenNumbers.set(normalized, index)
      }
    }
  })

  return {
    isValid: Object.keys(errors).length === 0,
    errors,
    duplicateContainerNumbers,
  }
}

export function copyContainerValues(source: ContainerFormValues): ContainerFormValues {
  return { ...source }
}

export function buildContainersPayload(
  dynamicValues: Record<string, string>,
  numContainers: number,
  containers: ContainerDraftState,
) {
  const formDataJsonValues = {
    ...dynamicValues,
    [NUM_CONTAINERS_FIELD]: String(numContainers),
  }

  return {
    formDataJsonValues,
    numContainers,
    containersJson: serializeContainersJson(containers),
  }
}

export function parseContainerSchema(schemaJson?: string): FormFieldSchema[] {
  if (!schemaJson) return []
  return parseFormSchema(schemaJson)
}

export function getNumContainersFromFormData(formDataJson?: string | null, fallbackCount = 0): number {
  const values = parseFormDataJson(formDataJson)
  const raw = values[NUM_CONTAINERS_FIELD]
  if (raw === undefined || raw === '') return fallbackCount
  const parsed = Number.parseInt(raw, 10)
  if (Number.isNaN(parsed)) return fallbackCount
  return Math.max(0, Math.min(MAX_CONTAINERS, parsed))
}
