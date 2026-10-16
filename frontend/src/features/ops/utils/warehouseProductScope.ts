export const WAREHOUSE_COMMODITY_CLASS_OPTIONS = [
  { value: 'MeatAndMeatProducts', label: 'Meat & meat products (pork, poultry, beef — BAI)' },
  { value: 'FishAndAquaticProducts', label: 'Fish & aquatic products (BFAR)' },
  { value: 'PlantProducts', label: 'Plant products' },
  { value: 'EggsMilkAnimalByProducts', label: 'Eggs, milk & animal by-products' },
  { value: 'OtherAgri', label: 'Other agricultural' },
] as const

const MEAT = 'MeatAndMeatProducts'
const FISH = 'FishAndAquaticProducts'

export function parseFacilityScopeJson(productsJson?: string | null): string[] {
  if (!productsJson?.trim()) return []
  try {
    const parsed = JSON.parse(productsJson) as unknown
    if (!Array.isArray(parsed)) return []
    return parsed.filter((x): x is string => typeof x === 'string')
  } catch {
    return []
  }
}

export function scopeViolatesSegregation(selected: string[]): boolean {
  return selected.includes(MEAT) && selected.includes(FISH)
}

export function isClassAllowedAtFacility(classValue: string, facilityAllowed: string[]): boolean {
  if (facilityAllowed.length === 0) return true
  return facilityAllowed.includes(classValue)
}

export function toggleScopeClass(current: string[], classValue: string, facilityAllowed: string[]): string[] {
  if (!isClassAllowedAtFacility(classValue, facilityAllowed)) {
    return current
  }

  const next = current.includes(classValue)
    ? current.filter((c) => c !== classValue)
    : [...current, classValue]

  if (scopeViolatesSegregation(next)) {
    return current
  }

  return next
}

export function defaultContractScope(facilityAllowed: string[]): string[] {
  if (facilityAllowed.length === 1) return [...facilityAllowed]
  if (facilityAllowed.includes(FISH)) return [FISH]
  if (facilityAllowed.includes(MEAT)) return [MEAT]
  return facilityAllowed.length > 0 ? [facilityAllowed[0]] : []
}
