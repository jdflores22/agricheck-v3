import type { FormFieldSchema } from '../../forms/formSchema'

export function resolveWarehouseIdFromFormFields(
  schemaFields: FormFieldSchema[],
  values: Record<string, string>,
): string {
  for (const field of schemaFields) {
    if (field.type !== 'warehouse') {
      continue
    }
    const id = values[field.name]?.trim()
    if (id) {
      return id
    }
  }
  const legacy = values.select_warehouse_name?.trim()
  return legacy ?? ''
}

export function resolveEffectiveWarehouseFacilityId(
  schemaFields: FormFieldSchema[],
  dynamicValues: Record<string, string>,
  finalWarehouseFacilityId: string,
): string {
  return finalWarehouseFacilityId.trim() || resolveWarehouseIdFromFormFields(schemaFields, dynamicValues)
}

export function entryAgencyWarehouseFilterHint(agencyCode?: string | null): string {
  const code = agencyCode?.trim().toUpperCase()
  switch (code) {
    case 'BAI':
      return 'Only meat cold-chain warehouses (BAI / NMIS CSW or RSAS meat scope). Fish facilities are not listed.'
    case 'NMIS':
      return 'Only meat cold-chain destinations (NMIS / BAI). Fish facilities are not listed.'
    case 'BFAR':
      return 'Only fish and aquatic warehouses (BFAR scope). Meat facilities are not listed.'
    case 'BPI':
      return 'Only plant-product warehouses (BPI scope). Meat and fish facilities are not listed.'
    default:
      return 'Destinations match this agency’s regulatory scope (LTO / RSAS / CSW).'
  }
}

export const DESTINATION_SCOPE_CHECK_CODES = new Set([
  'DESTINATION_AGENCY_SCOPE',
  'DESTINATION_COMMODITY_SCOPE',
  'DESTINATION_CONTRACT_COMMODITY',
  'DESTINATION_SEGREGATION',
  'DESTINATION_COMMODITY_UNKNOWN',
])
