import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import {
  Alert,
  Box,
  Card,
  CardContent,
  Divider,
  Stack,
  Typography,
} from '@mui/material'
import type { ContainerDeliveryLegSummary, ContainerTrack } from '../api/opsApi'

const philippinesFormatter = new Intl.DateTimeFormat('en-PH', {
  timeZone: 'Asia/Manila',
  month: 'short',
  day: 'numeric',
  year: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
})

function formatLegTime(iso?: string | null) {
  if (!iso) return null
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return null
  return philippinesFormatter.format(d)
}

function findDaNmisLeg(summary?: ContainerDeliveryLegSummary[] | null) {
  return summary?.find((leg) => leg.leg === 'da_nmis' || leg.leg === 'nmis') ?? null
}

function isDriverTripComplete(status: string) {
  return status === 'Inspected' || status === 'Released'
}

export function DriverDeliveryTrackCards({ track }: { track: ContainerTrack }) {
  const summary = track.deliverySummary ?? []
  const tripLegs = summary.filter((leg) => leg.leg === 'pickup' || leg.leg === 'warehouse')
  const daNmis = findDaNmisLeg(summary)
  const tripComplete = isDriverTripComplete(track.status)

  return (
    <Stack spacing={2} sx={{ mt: 2 }}>
      {tripComplete && (
        <Alert severity="success">
          This delivery is completed. No further navigation is needed.
        </Alert>
      )}

      <Card variant="outlined">
        <CardContent>
          <Typography variant="overline" color="text.secondary">
            Trip summary
          </Typography>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }} gutterBottom>
            {track.containerNumber} · {track.entryReference}
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5 }}>
            {track.pickup.name} → {track.destination.name}
          </Typography>
          {tripLegs.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              Timestamps will appear after you confirm pickup and warehouse arrival.
            </Typography>
          ) : (
            tripLegs.map((leg) => (
              <Box key={leg.leg} sx={{ py: 1 }}>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>{leg.title}</Typography>
                {leg.statusLabel && (
                  <Typography variant="body2" color="text.secondary">{leg.statusLabel}</Typography>
                )}
                {formatLegTime(leg.arrivedAtUtc) && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                    Arrived · {formatLegTime(leg.arrivedAtUtc)}
                  </Typography>
                )}
                {leg.leg === 'pickup' && formatLegTime(leg.departedAtUtc) && (
                  <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                    Left · {formatLegTime(leg.departedAtUtc)}
                  </Typography>
                )}
              </Box>
            ))
          )}
        </CardContent>
      </Card>

      {daNmis && (
        <Card variant="outlined" sx={{ borderColor: 'primary.light' }}>
          <CardContent>
            <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center' }}>
              <VerifiedOutlinedIcon color="primary" fontSize="small" />
              <Typography variant="overline" color="primary">
                DA NMIS
              </Typography>
            </Stack>
            {daNmis.statusLabel && (
              <Typography variant="body2" sx={{ fontWeight: 600 }} gutterBottom>
                {daNmis.statusLabel}
              </Typography>
            )}
            <Divider sx={{ my: 1.5 }} />
            <Stack spacing={0.75}>
              <Typography variant="body2" color="text.secondary">
                {formatLegTime(daNmis.arrivedAtUtc)
                  ? `QR scan confirmed (arrived) · ${formatLegTime(daNmis.arrivedAtUtc)}`
                  : 'Awaiting DA NMIS / Doctor delivery QR scan'}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {formatLegTime(daNmis.departedAtUtc)
                  ? `Inspection completed · ${formatLegTime(daNmis.departedAtUtc)}`
                  : 'Inspection date & time will appear when the doctor completes inspection.'}
              </Typography>
              {tripComplete && (
                <Typography variant="body2" color="success.main" sx={{ fontWeight: 600 }}>
                  Container confirmed complete ({track.status})
                </Typography>
              )}
            </Stack>
          </CardContent>
        </Card>
      )}
    </Stack>
  )
}
