export const FORM_TYPES = ['ACCREDITATION', 'ENTRY', 'CONTAINER', 'MAV', 'INSPECTION'] as const
export type FormType = (typeof FORM_TYPES)[number]

export const FIELD_TYPES = [
  'section',
  'text',
  'textarea',
  'email',
  'number',
  'date',
  'file',
  'select',
  'checkbox',
  'radio',
  'commodity',
  'geotag_photo',
  'address',
  'warehouse',
] as const
export type FormFieldType = (typeof FIELD_TYPES)[number]

export interface FormFieldOption {
  label: string
  value: string
}

export interface ConditionalLogic {
  showIf?: {
    field: string
    operator: 'equals' | 'not_equals' | 'contains' | 'greater_than' | 'less_than' | 'is_empty' | 'is_not_empty'
    value?: string
  }
}

export interface FormFieldSchema {
  id: string
  name: string
  label: string
  type: FormFieldType
  required?: boolean
  placeholder?: string
  helpText?: string
  tooltip?: string
  columnWidth?: number
  displayOrder?: number
  options?: FormFieldOption[]
  validationRules?: Record<string, string | number | boolean>
  conditionalLogic?: ConditionalLogic
}

export interface FieldTypeDefinition {
  type: FormFieldType
  label: string
  description: string
  icon: string
}

export const FIELD_TYPE_DEFINITIONS: FieldTypeDefinition[] = [
  { type: 'section', label: 'Section', description: 'Section header / divider', icon: 'T' },
  { type: 'text', label: 'Text', description: 'Single-line text input', icon: 'Aa' },
  { type: 'textarea', label: 'Textarea', description: 'Multi-line text input', icon: '¶' },
  { type: 'email', label: 'Email', description: 'Email address input', icon: '@' },
  { type: 'number', label: 'Number', description: 'Numeric input', icon: '#' },
  { type: 'date', label: 'Date', description: 'Date picker', icon: '📅' },
  { type: 'file', label: 'File', description: 'File upload', icon: '📎' },
  { type: 'select', label: 'Select', description: 'Dropdown selection', icon: '▼' },
  { type: 'checkbox', label: 'Checkbox', description: 'Yes / no checkbox', icon: '☑' },
  { type: 'radio', label: 'Radio', description: 'Single choice from options', icon: '◉' },
  { type: 'commodity', label: 'Commodity', description: 'HS code / commodity selector', icon: '🌾' },
  { type: 'geotag_photo', label: 'Geotag Photo', description: 'Photo with GPS metadata', icon: '📷' },
  { type: 'address', label: 'Address', description: 'Region, province, city, barangay, zip, street', icon: '📍' },
  { type: 'warehouse', label: 'Warehouse', description: 'Registered warehouse selector with auto address', icon: '🏭' },
]

export const COLUMN_WIDTHS = [12, 9, 8, 6, 4, 3] as const

export function getWarehouseAutofillTarget(field: FormFieldSchema): string | undefined {
  const target = field.validationRules?.autofillTarget
  return typeof target === 'string' && target.trim() !== '' ? target : undefined
}

export function getAddressValueKeys(fieldName: string) {
  return {
    regionId: `${fieldName}_region_id`,
    regionName: `${fieldName}_region_name`,
    provinceId: `${fieldName}_province_id`,
    provinceName: `${fieldName}_province_name`,
    cityId: `${fieldName}_city_id`,
    cityName: `${fieldName}_city_name`,
    barangayId: `${fieldName}_barangay_id`,
    barangayName: `${fieldName}_barangay_name`,
    zipCode: `${fieldName}_zip_code`,
    street: `${fieldName}_street`,
  }
}

export function slugifyFieldName(label: string): string {
  return label
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '')
    .slice(0, 80) || 'field'
}

export function createField(type: FormFieldType, order: number, existingNames: Set<string>): FormFieldSchema {
  const def = FIELD_TYPE_DEFINITIONS.find((item) => item.type === type)!
  let baseName = slugifyFieldName(def.label)
  let name = baseName
  let counter = 1
  while (existingNames.has(name)) {
    counter += 1
    name = `${baseName}_${counter}`
  }

  const field: FormFieldSchema = {
    id: crypto.randomUUID(),
    name,
    label: def.label,
    type,
    columnWidth: type === 'section' || type === 'address' ? 12 : 6,
    displayOrder: order,
    required: type !== 'section',
  }

  if (type === 'address') {
    field.label = 'Address'
    field.name = existingNames.has('address') ? name : 'address'
    field.helpText = 'Select region, province, city/municipality, barangay (zip auto-fills), then enter street details.'
    field.placeholder = 'House no., street, building, unit'
  }

  if (type === 'warehouse') {
    field.label = 'Warehouse Name'
    field.name = existingNames.has('select_warehouse_name') ? name : 'select_warehouse_name'
    field.helpText = 'Choose from registered warehouses. Address auto-fills when linked.'
    field.placeholder = 'Select warehouse'
    field.validationRules = { autofillTarget: 'textarea_warehouse_address' }
  }

  if (type === 'select' || type === 'radio') {
    field.options = [
      { label: 'Option 1', value: 'option_1' },
      { label: 'Option 2', value: 'option_2' },
    ]
  }

  if (type === 'section') {
    field.required = false
  }

  return field
}

function normalizeOption(option: FormFieldOption | string): FormFieldOption {
  if (typeof option === 'string') {
    return { label: option, value: slugifyFieldName(option) || option }
  }
  return option
}

/** Strip HTML tags and whitespace-only placeholders from migrated V2 help text. */
export function sanitizeDisplayText(text?: string | null): string | undefined {
  if (!text) return undefined
  const stripped = text.replace(/<[^>]*>/g, '').trim()
  return stripped || undefined
}

export function parseFormSchema(schemaJson: string): FormFieldSchema[] {
  try {
    const parsed = JSON.parse(schemaJson) as Partial<FormFieldSchema>[]
    if (!Array.isArray(parsed)) return []

    return parsed
      .map((raw, index) => {
        const type = (raw.type ?? 'text') as FormFieldType
        const options = raw.options?.map(normalizeOption)
        return {
          id: raw.id ?? crypto.randomUUID(),
          name: raw.name ?? slugifyFieldName(raw.label ?? `field_${index + 1}`),
          label: raw.label ?? raw.name ?? `Field ${index + 1}`,
          type: FIELD_TYPES.includes(type) ? type : 'text',
          required: raw.required ?? false,
          placeholder: raw.placeholder,
          helpText: raw.helpText,
          tooltip: raw.tooltip,
          columnWidth: raw.columnWidth ?? 12,
          displayOrder: raw.displayOrder ?? index + 1,
          options,
          validationRules: raw.validationRules,
          conditionalLogic: raw.conditionalLogic,
        } satisfies FormFieldSchema
      })
      .sort((a, b) => (a.displayOrder ?? 0) - (b.displayOrder ?? 0))
  } catch {
    return []
  }
}

export function serializeFormSchema(fields: FormFieldSchema[]): string {
  const ordered = fields.map((field, index) => ({
    ...field,
    displayOrder: index + 1,
  }))
  return JSON.stringify(ordered, null, 2)
}

export function evaluateConditional(field: FormFieldSchema, values: Record<string, string>): boolean {
  const rule = field.conditionalLogic?.showIf
  if (!rule?.field) return true

  const current = values[rule.field] ?? ''
  switch (rule.operator) {
    case 'equals':
      return current === (rule.value ?? '')
    case 'not_equals':
      return current !== (rule.value ?? '')
    case 'contains':
      return current.includes(rule.value ?? '')
    case 'greater_than':
      return Number(current) > Number(rule.value ?? 0)
    case 'less_than':
      return Number(current) < Number(rule.value ?? 0)
    case 'is_empty':
      return current.trim() === ''
    case 'is_not_empty':
      return current.trim() !== ''
    default:
      return true
  }
}

export function visibleFormFields(fields: FormFieldSchema[], values: Record<string, string>): FormFieldSchema[] {
  return fields.filter((field) => evaluateConditional(field, values))
}

export function parseFormDataJson(formDataJson?: string | null): Record<string, string> {
  if (!formDataJson || formDataJson.trim() === '' || formDataJson.trim() === '{}') {
    return {}
  }

  try {
    const parsed = JSON.parse(formDataJson) as unknown
    if (typeof parsed === 'object' && parsed !== null && !Array.isArray(parsed)) {
      return Object.fromEntries(
        Object.entries(parsed as Record<string, unknown>).map(([key, value]) => [key, value == null ? '' : String(value)]),
      )
    }
  } catch {
    return { _legacyNotes: formDataJson }
  }

  return {}
}

export function serializeFormDataJson(values: Record<string, string>): string {
  return JSON.stringify(values)
}

export function resolveCompanyNameFromFormValues(values: Record<string, string>, fallback = ''): string {
  return values.company_name?.trim() || values.companyName?.trim() || fallback
}
