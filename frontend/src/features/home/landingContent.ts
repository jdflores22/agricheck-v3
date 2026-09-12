export const heroStats = [
  { title: 'Accreditation', subtitle: 'Apply & renew' },
  { title: 'Entry management', subtitle: 'BAI · BFAR · BPI' },
  { title: 'Container tracking', subtitle: 'Port to warehouse' },
  { title: 'Warehouse ops', subtitle: 'Inventory & release' },
] as const

export const platformFeatures = [
  {
    title: 'Accreditation',
    description:
      'Apply for and manage business accreditation with government agencies in one guided workflow.',
  },
  {
    title: 'Entry management',
    description:
      'Submit and track import and export entries for BAI, BFAR, and BPI with real-time status updates.',
  },
  {
    title: 'Container tracking',
    description:
      'Monitor shipments from port arrival through warehouse delivery with a clear audit trail.',
  },
  {
    title: 'Warehouse management',
    description:
      'Run inventory, booking, and container release operations with agency-aligned controls.',
  },
] as const

export const highlights = [
  {
    title: 'Multi-role access',
    description: 'Separate portals for importers, agency staff, MAV administrators, and evaluators.',
  },
  {
    title: 'Secure & compliant',
    description: 'Built for regulatory workflows with audit logs, verification, and role-based permissions.',
  },
  {
    title: 'Real-time visibility',
    description: 'Track entries, documents, and compliance status from submission through approval.',
  },
  {
    title: 'Always available',
    description: 'Cloud-based access so teams can manage operations anytime, anywhere.',
  },
] as const

export const whyItems = [
  {
    title: 'Streamlined processes',
    description: 'Reduce manual steps across accreditation, evaluation, and release workflows.',
  },
  {
    title: 'Agency-specific portals',
    description: 'Each registered agency gets its own dashboard, navigation, and role tools.',
  },
  {
    title: 'Document verification',
    description: 'Upload, review, and approve supporting documents within the same system.',
  },
  {
    title: 'Integrated MAV compliance',
    description: 'Manage licenses, MICs, periods, and compliance reporting in one place.',
  },
] as const

export const agencies = [
  { code: 'BAI', name: 'Bureau of Animal Industry' },
  { code: 'BFAR', name: 'Bureau of Fisheries and Aquatic Resources' },
  { code: 'BPI', name: 'Bureau of Plant Industry' },
] as const

export const userTypeTags = ['Importers', 'Agencies', 'MAV', 'Evaluators'] as const
