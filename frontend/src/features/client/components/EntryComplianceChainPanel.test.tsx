import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import type { EntryComplianceChain } from '../api/clientApi'
import { EntryComplianceChainPanel } from './EntryComplianceChainPanel'

function sampleChain(overrides?: Partial<EntryComplianceChain>): EntryComplianceChain {
  const layer = (layer: string, status: string, summary: string) => ({
    layer,
    status,
    summary,
    reference: undefined,
  })

  return {
    entryUuid: '00000000-0000-0000-0000-000000000001',
    referenceNo: 'ENT-TEST-001',
    entryStatus: 'Approved',
    compliancePhase: 'PortCleared',
    agencyId: 1,
    agencyCode: 'BAI',
    accreditation: layer('Accreditation', 'Complete', 'DA accreditation approved.'),
    agencyLicense: layer('AgencyLicense', 'Complete', 'LTO valid.'),
    destinationFacility: layer('DestinationFacility', 'Complete', 'RSAS + CSW ready.'),
    spsImportClearance: layer('SpsImportClearance', 'Complete', 'SPSIC issued.'),
    erfi: layer('Erfi', 'Complete', 'e-RFI filed.'),
    mav: layer('Mav', 'NotApplicable', 'Regular import track.'),
    blockers: [],
    warnings: [],
    nextRecommendedAction: 'Haul to CSW',
    ...overrides,
  }
}

describe('EntryComplianceChainPanel', () => {
  it('renders compliance spine layers and phase', () => {
    render(
      <MemoryRouter>
        <EntryComplianceChainPanel chain={sampleChain()} />
      </MemoryRouter>,
    )

    expect(screen.getByText(/Compliance spine/i)).toBeInTheDocument()
    expect(screen.getByText(/Phase: PortCleared/i)).toBeInTheDocument()
    expect(screen.getByText(/Accreditation: Complete/i)).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Bureau licenses/i })).toHaveAttribute('href', '/client/licenses')
  })

  it('shows blockers when present', () => {
    render(
      <MemoryRouter>
        <EntryComplianceChainPanel
          chain={sampleChain({
            blockers: [{ code: 'COMPLIANCE_AGENCY_LICENSE', message: 'License required.', layer: 'AgencyLicense' }],
          })}
        />
      </MemoryRouter>,
    )

    expect(screen.getByText('License required.')).toBeInTheDocument()
  })
})
