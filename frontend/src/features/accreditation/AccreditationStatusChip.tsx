import { Chip } from '@mui/material'
import { portalStatusChipSx } from '../../components/portal/PortalTablePanel'
import { resolveAccreditationListItemStatus } from './accreditationStatusUtils'

type AccreditationStatusChipProps = {
  status: string
  displayStatus?: string
  history?: Array<{ status: string; comment?: string; createdAt: string }>
}

export function AccreditationStatusChip({ status, displayStatus, history }: AccreditationStatusChipProps) {
  const { label, chipKey } = resolveAccreditationListItemStatus({ status, displayStatus, history })
  return <Chip size="small" label={label} sx={portalStatusChipSx(chipKey)} />
}
