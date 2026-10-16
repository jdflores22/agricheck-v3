import { describe, expect, it } from 'vitest'
import {
  buildFileLabelMap,
  canResubmitCompliance,
  listComplianceDocuments,
  parseReviewerCommentBullets,
} from './accreditationComplianceUtils'

describe('accreditationComplianceUtils', () => {
  it('buildFileLabelMap maps file uuids to schema labels', () => {
    const map = buildFileLabelMap(
      { business_permit_file_uuid: 'abc-123' },
      [{ id: '1', name: 'business_permit', label: 'Business permit', type: 'file' }],
    )

    expect(map.get('abc-123')).toBe('Business permit')
  })

  it('listComplianceDocuments surfaces revision-required files', () => {
    const items = listComplianceDocuments(
      [
        {
          uuid: 'f1',
          originalFileName: 'doc.pdf',
          reviewDecision: 'RevisionRequired',
        },
        {
          uuid: 'f2',
          originalFileName: 'ok.pdf',
          reviewDecision: 'Approved',
        },
      ],
      { permit_file_uuid: 'f1' },
      [{ id: '2', name: 'permit', label: 'Permit', type: 'file' }],
    )

    expect(items).toHaveLength(1)
    expect(items[0].label).toBe('Permit')
    expect(items[0].needsUpload).toBe(true)
  })

  it('canResubmitCompliance requires revised upload and no pending revision', () => {
    expect(
      canResubmitCompliance([
        { uuid: 'a', originalFileName: 'a.pdf', reviewDecision: 'RevisionRequired' },
        { uuid: 'b', originalFileName: 'b.pdf', reviewDecision: 'Pending', versions: [{ versionNumber: 2, isCurrent: true }] },
      ]),
    ).toBe(false)

    expect(
      canResubmitCompliance([
        {
          uuid: 'b',
          originalFileName: 'b.pdf',
          reviewDecision: 'Pending',
          versions: [
            { versionNumber: 1, isCurrent: false },
            { versionNumber: 2, isCurrent: true },
          ],
        },
      ]),
    ).toBe(true)
  })

  it('parseReviewerCommentBullets extracts bullet lines', () => {
    const bullets = parseReviewerCommentBullets('Summary line\n• Fix permit\n• Update address')
    expect(bullets).toEqual(['Fix permit', 'Update address'])
  })
})
