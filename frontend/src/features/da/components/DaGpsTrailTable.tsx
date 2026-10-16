import {
  Box,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useMemo } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaContainerTrailPoint } from '../api/daApi'

function formatCoord(value: number) {
  return value.toFixed(6)
}

interface DaGpsTrailTableProps {
  trail: DaContainerTrailPoint[]
  containerNumber?: string
}

export function DaGpsTrailTable({ trail, containerNumber }: DaGpsTrailTableProps) {
  const sorted = useMemo(
    () => [...trail].sort((a, b) => Date.parse(a.recordedAt) - Date.parse(b.recordedAt)),
    [trail],
  )

  const csv = useMemo(() => {
    const header = 'index,recorded_at_utc,latitude,longitude'
    const rows = sorted.map(
      (p, i) => `${i + 1},${p.recordedAt},${p.latitude},${p.longitude}`,
    )
    return [header, ...rows].join('\n')
  }, [sorted])

  if (sorted.length === 0) {
    return (
      <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted }}>
        No GPS coordinates recorded yet. Trail starts when the driver taps <strong>Start navigation</strong> in
        AgriTrack (foreground location service).
      </Typography>
    )
  }

  const first = sorted[0]
  const last = sorted[sorted.length - 1]

  return (
    <Box>
      <Typography sx={{ fontSize: '0.8rem', color: portalColors.textMuted, mb: 1 }}>
        {sorted.length} points · from {new Date(first.recordedAt).toLocaleString()} to{' '}
        {new Date(last.recordedAt).toLocaleString()}
      </Typography>
      <Box sx={{ mb: 1 }}>
        <Button
          size="small"
          variant="outlined"
          onClick={() => {
            const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' })
            const url = URL.createObjectURL(blob)
            const a = document.createElement('a')
            a.href = url
            a.download = `${containerNumber ?? 'delivery'}-gps-trail.csv`
            a.click()
            URL.revokeObjectURL(url)
          }}
        >
          Download CSV
        </Button>
      </Box>
      <TableContainer sx={{ maxHeight: 280, border: `1px solid ${portalColors.border}`, borderRadius: 1 }}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell sx={{ fontWeight: 700, width: 48 }}>#</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Time</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Latitude</TableCell>
              <TableCell sx={{ fontWeight: 700 }}>Longitude</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {sorted.map((p, index) => (
              <TableRow key={`${p.recordedAt}-${index}`}>
                <TableCell>{index + 1}</TableCell>
                <TableCell sx={{ fontSize: '0.75rem', whiteSpace: 'nowrap' }}>
                  {new Date(p.recordedAt).toLocaleString()}
                </TableCell>
                <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                  {formatCoord(Number(p.latitude))}
                </TableCell>
                <TableCell sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}>
                  {formatCoord(Number(p.longitude))}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  )
}
