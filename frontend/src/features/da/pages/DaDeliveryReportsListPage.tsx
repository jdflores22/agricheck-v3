import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import { useGetDaLogisticsLiveQuery } from '../api/daApi'
import { resolveDriverGpsDisplay } from '../utils/daDriverGpsStatus'

function reportHref(containerUuid: string) {
  return `/da/logistics/containers/${containerUuid}/report`
}

export function DaDeliveryReportsListPage() {
  const { data, isLoading, isFetching, refetch } = useGetDaLogisticsLiveQuery(undefined, {
    pollingInterval: 30_000,
  })
  const snapshot = data?.data
  const rows = [...(snapshot?.activeDeliveries ?? []), ...(snapshot?.recentlyArrived ?? [])]

  if (isLoading && !snapshot) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="DA Secretary · Logistics"
        title="Delivery reports"
        subtitle="GPS trail, coordinate log, and geotagged pickup/warehouse photos per container. Open a row to view the full report."
        actions={
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" component={RouterLink} to="/da/logistics/live">
              Live map
            </Button>
            <Button size="small" variant="outlined" onClick={() => refetch()} disabled={isFetching}>
              Refresh
            </Button>
          </Stack>
        }
      />

      {rows.length === 0 ? (
        <Typography sx={{ color: portalColors.textMuted }}>
          No active or recent deliveries with a driver. Assign a container in AgriTrack first.
        </Typography>
      ) : (
        <TableContainer sx={{ border: `1px solid ${portalColors.border}`, borderRadius: 2 }}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell sx={{ fontWeight: 700 }}>Container</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Entry</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Status</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>Driver</TableCell>
                <TableCell sx={{ fontWeight: 700 }}>GPS</TableCell>
                <TableCell align="right" sx={{ fontWeight: 700 }} />
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((d) => {
                const gps = resolveDriverGpsDisplay(d, snapshot?.serverTime)
                return (
                  <TableRow key={d.containerUuid} hover>
                    <TableCell sx={{ fontWeight: 700 }}>{d.containerNumber}</TableCell>
                    <TableCell>{d.entryReference}</TableCell>
                    <TableCell>
                      <Chip size="small" label={d.status} />
                    </TableCell>
                    <TableCell>{d.driverName ?? '—'}</TableCell>
                    <TableCell sx={{ fontSize: '0.8rem' }}>{gps.label}</TableCell>
                    <TableCell align="right">
                      <Button
                        size="small"
                        variant="contained"
                        component={RouterLink}
                        to={reportHref(d.containerUuid)}
                        startIcon={<AssessmentOutlinedIcon />}
                      >
                        View report
                      </Button>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  )
}
