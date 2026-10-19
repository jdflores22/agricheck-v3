const EVENT_LABELS: Record<string, string> = {
  misroute: 'Misroute',
  wrong_warehouse: 'Wrong warehouse',
  off_route_blocked: 'Off-route blocked',
  handoff_qr_replay: 'QR replay',
  driver_incident: 'Driver incident (TAG)',
  navigation_not_started: 'Navigation not started',
  route_distance_regression: 'Route distance regression',
  driver_stationary_15: 'Driver stationary (15 min)',
  driver_stationary_30: 'Driver stationary (30 min)',
}

const INCIDENT_CATEGORY_LABELS: Record<string, string> = {
  accident: 'Road accident / collision',
  flat_tire: 'Flat tire',
  engine_problem: 'Engine problem',
  mechanical_breakdown: 'Mechanical breakdown',
  road_blocked: 'Road blocked / obstruction',
  weather_hazard: 'Weather hazard',
  cargo_shift: 'Cargo shift / load issue',
  medical_emergency: 'Medical emergency',
  traffic_enforcement: 'Traffic enforcement / checkpoint delay',
  fuel_issue: 'Fuel issue',
  other: 'Other incident',
}

export type ComplianceAlertSeverity = 'critical' | 'warning' | 'info'

export function formatComplianceEventType(eventType: string): string {
  const key = eventType.trim().toLowerCase()
  return EVENT_LABELS[key] ?? eventType.replace(/_/g, ' ')
}

export function complianceAlertSeverity(eventType: string): ComplianceAlertSeverity {
  const key = eventType.trim().toLowerCase()
  if (key === 'driver_incident') return 'warning'
  if (key === 'driver_stationary_30') return 'warning'
  if (key === 'misroute' || key === 'wrong_warehouse' || key === 'off_route_blocked') return 'critical'
  if (key === 'navigation_not_started' || key === 'route_distance_regression' || key === 'driver_stationary_15') {
    return 'warning'
  }
  return 'info'
}

function parseDetailParts(details: string) {
  const parts = details.split('|').map((p) => p.trim()).filter(Boolean)
  const read = (prefix: string) => parts.find((p) => p.startsWith(prefix))?.slice(prefix.length)
  return {
    label: read('label='),
    notes: read('notes='),
    category: read('category='),
  }
}

export function formatComplianceDetails(details: string | null | undefined): string {
  if (!details) return ''
  const { label, notes, category } = parseDetailParts(details)
  const categoryLabel =
    label ?? (category ? INCIDENT_CATEGORY_LABELS[category.toLowerCase()] ?? category.replace(/_/g, ' ') : '')
  if (categoryLabel && notes) return `${categoryLabel} — ${notes}`
  if (categoryLabel) return categoryLabel
  if (notes) return notes
  return details
}
