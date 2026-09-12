import {
  Box,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'
import { PortalPanel } from './PortalPanel'
import { portalColors } from './portalTheme'
import { portalEmptyStateSx, portalTableHeadCellSx } from './portalStyles'

interface PortalTablePanelProps {
  title: string
  columns: string[]
  isLoading?: boolean
  isEmpty?: boolean
  emptyMessage?: string
  colSpan?: number
  children?: ReactNode
}

export function PortalTablePanel({
  title,
  columns,
  isLoading,
  isEmpty,
  emptyMessage = 'No records found.',
  colSpan,
  children,
}: PortalTablePanelProps) {
  const span = colSpan ?? columns.length

  return (
    <PortalPanel title={title}>
      <Box sx={{ overflowX: 'auto' }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              {columns.map((column) => (
                <TableCell key={column} sx={portalTableHeadCellSx}>
                  {column}
                </TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {isLoading && (
              <TableRow>
                <TableCell colSpan={span}>
                  <Typography sx={portalEmptyStateSx}>Loading…</Typography>
                </TableCell>
              </TableRow>
            )}
            {!isLoading && isEmpty && (
              <TableRow>
                <TableCell colSpan={span}>
                  <Typography sx={portalEmptyStateSx}>{emptyMessage}</Typography>
                </TableCell>
              </TableRow>
            )}
            {!isLoading && !isEmpty && children}
          </TableBody>
        </Table>
      </Box>
    </PortalPanel>
  )
}

export function portalStatusChipSx(status: string) {
  const normalized = status.toUpperCase()
  if (['APPROVED', 'ACTIVE', 'VERIFIED', 'PAID', 'COMPLETED'].includes(normalized)) {
    return { bgcolor: portalColors.successSoft, color: portalColors.successText }
  }
  if (['PENDING', 'DRAFT', 'SUBMITTED', 'REVIEW', 'PAYMENTPENDING', 'ISSUED', 'DAISSUEBILLING', 'AWAITING'].some((s) => normalized.includes(s))) {
    return { bgcolor: '#fffbeb', color: '#92400e' }
  }
  if (['PAYMENTSUBMITTED', 'PENDINGVERIFICATION'].some((s) => normalized.includes(s))) {
    return { bgcolor: '#eff6ff', color: '#1d4ed8' }
  }
  if (['REVISIONREQUIRED', 'REVISION', 'FORCOMPLIANCE'].some((s) => normalized.includes(s))) {
    return { bgcolor: '#fff7ed', color: '#c2410c' }
  }
  if (normalized.includes('RESUBMITTED')) {
    return { bgcolor: '#eff6ff', color: '#1d4ed8' }
  }
  if (['REJECTED', 'DENIED', 'SUSPENDED', 'FAILED'].some((s) => normalized.includes(s))) {
    return { bgcolor: '#fef2f2', color: '#991b1b' }
  }
  return { bgcolor: portalColors.bgMuted, color: portalColors.textDark }
}
