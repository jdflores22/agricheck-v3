import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined'
import PhoneOutlinedIcon from '@mui/icons-material/PhoneOutlined'
import RefreshOutlinedIcon from '@mui/icons-material/RefreshOutlined'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalColors } from '../../../components/portal/portalTheme'
import {
  useAcknowledgeDaComplianceAlertMutation,
  useClearDaDeliveryComplianceHoldMutation,
  useGetDaLogisticsAlertsQuery,
  type DaDeliveryComplianceAlert,
} from '../api/daApi'
import {
  complianceAlertSeverity,
  formatComplianceDetails,
  formatComplianceEventType,
} from '../utils/complianceAlertLabels'

function formatHoldReason(reason: string) {
  if (reason === 'off_route_blocked') return 'misroute'
  return reason.replace(/_/g, ' ')
}

function severityChipColor(severity: ReturnType<typeof complianceAlertSeverity>) {
  if (severity === 'critical') return 'error' as const
  if (severity === 'warning') return 'warning' as const
  return 'default' as const
}

function AlertCard({
  alert,
  onAcknowledge,
}: {
  alert: DaDeliveryComplianceAlert
  onAcknowledge?: () => void
}) {
  const severity = complianceAlertSeverity(alert.eventType)
  const details = formatComplianceDetails(alert.details)
  const phone = alert.driverPhone?.trim()

  return (
    <Box
      sx={{
        p: 1.5,
        borderRadius: 2,
        border: `1px solid ${alert.isOpen ? '#F59E0B' : portalColors.border}`,
        bgcolor: alert.isOpen ? '#FFFBEB' : portalColors.bgWhite,
        mb: 1,
      }}
    >
      <Stack direction="row" spacing={0.75} sx={{ flexWrap: 'wrap', alignItems: 'center', mb: 0.75 }}>
        <Chip
          size="small"
          label={formatComplianceEventType(alert.eventType)}
          color={severityChipColor(severity)}
          sx={{ height: 22, fontSize: '0.7rem', fontWeight: 700 }}
        />
        {alert.isOpen ? (
          <Chip size="small" label="Open" color="warning" sx={{ height: 20, fontSize: '0.65rem' }} />
        ) : (
          <Chip size="small" label="Cleared" variant="outlined" sx={{ height: 20, fontSize: '0.65rem' }} />
        )}
        <Typography sx={{ fontWeight: 800, fontSize: '0.9rem' }}>{alert.containerNumber}</Typography>
        <Typography sx={{ fontSize: '0.8rem', color: portalColors.textMuted }}>{alert.entryReference}</Typography>
      </Stack>
      {details ? (
        <Typography sx={{ fontSize: '0.85rem', mb: 0.75 }}>{details}</Typography>
      ) : null}
      <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mb: 1 }}>
        {new Date(alert.recordedAt).toLocaleString()}
      </Typography>
      {(alert.driverName || phone) && (
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ mb: 1, alignItems: { sm: 'center' } }}>
          <Typography sx={{ fontSize: '0.85rem', fontWeight: 700 }}>
            Driver: {alert.driverName ?? '—'}
            {phone ? ` · ${phone}` : ''}
          </Typography>
          {phone ? (
            <Button
              size="small"
              variant="outlined"
              startIcon={<PhoneOutlinedIcon />}
              href={`tel:${phone.replace(/\s/g, '')}`}
              component="a"
            >
              Call driver
            </Button>
          ) : null}
        </Stack>
      )}
      {!alert.isOpen && alert.daReviewNotes ? (
        <Box sx={{ p: 1, borderRadius: 1, bgcolor: '#F3F4F6', mb: 1 }}>
          <Typography sx={{ fontSize: '0.75rem', fontWeight: 700, color: portalColors.textMuted }}>DA remarks</Typography>
          <Typography sx={{ fontSize: '0.85rem' }}>{alert.daReviewNotes}</Typography>
          {alert.daReviewedAtUtc ? (
            <Typography sx={{ fontSize: '0.72rem', color: portalColors.textMuted, mt: 0.5 }}>
              Cleared {new Date(alert.daReviewedAtUtc).toLocaleString()}
            </Typography>
          ) : null}
        </Box>
      ) : null}
      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
        <Button
          size="small"
          variant="outlined"
          component={RouterLink}
          to={`/da/logistics/containers/${alert.containerUuid}/report`}
          target="_blank"
          rel="noopener noreferrer"
          startIcon={<OpenInNewOutlinedIcon />}
        >
          Delivery report
        </Button>
        {alert.isOpen && onAcknowledge ? (
          <Button size="small" variant="contained" color="primary" onClick={onAcknowledge}>
            Clear alert (log note)
          </Button>
        ) : null}
      </Stack>
    </Box>
  )
}

export function DaLogisticsAlertsPage() {
  const { data, isLoading, isFetching, refetch } = useGetDaLogisticsAlertsQuery(undefined, {
    pollingInterval: 20_000,
  })
  const snapshot = data?.data
  const [clearHold] = useClearDaDeliveryComplianceHoldMutation()
  const [acknowledge, { isLoading: acknowledging }] = useAcknowledgeDaComplianceAlertMutation()
  const [ackTarget, setAckTarget] = useState<DaDeliveryComplianceAlert | null>(null)
  const [ackNotes, setAckNotes] = useState('')

  const submitAcknowledge = async () => {
    if (!ackTarget) return
    const notes = ackNotes.trim()
    if (!notes) return
    await acknowledge({ alertId: ackTarget.alertId, daReviewNotes: notes }).unwrap()
    setAckTarget(null)
    setAckNotes('')
  }

  if (isLoading && !snapshot) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
        <CircularProgress size={32} sx={{ color: portalColors.primary }} />
      </Box>
    )
  }

  const openAlerts = snapshot?.openAlerts ?? []
  const history = snapshot?.alertHistory ?? []
  const holds = snapshot?.activeComplianceHolds ?? []
  const arrived = snapshot?.recentlyArrived ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="DA Secretary · Logistics"
        title="Delivery alerts & compliance"
        subtitle="Hiwalay sa live map: open compliance events, active holds, warehouse check-in (48h), at cleared alert log."
        actions={
          <Stack direction="row" spacing={1}>
            <Button size="small" variant="outlined" component={RouterLink} to="/da/logistics/live">
              Live map
            </Button>
            <Button
              size="small"
              variant="outlined"
              startIcon={<RefreshOutlinedIcon />}
              onClick={() => refetch()}
              disabled={isFetching}
            >
              Refresh
            </Button>
          </Stack>
        }
      />

      {holds.length > 0 && (
        <Box sx={{ mb: 3, p: 2, borderRadius: 2, border: '2px solid #DC2626', bgcolor: '#FEF2F2' }}>
          <Typography sx={{ fontWeight: 800, mb: 1 }}>Active compliance holds — trip cannot end</Typography>
          {holds.map((h) => (
            <Stack
              key={h.containerUuid}
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1}
              sx={{ mb: 1, alignItems: { sm: 'center' } }}
            >
              <Typography sx={{ flex: 1, fontSize: '0.85rem' }}>
                <strong>{h.containerNumber}</strong> · {formatHoldReason(h.holdReason)} ·{' '}
                {new Date(h.holdAt).toLocaleString()}
                {h.driverName ? ` · ${h.driverName}` : ''}
              </Typography>
              <Button
                size="small"
                variant="contained"
                onClick={() =>
                  clearHold({
                    uuid: h.containerUuid,
                    clearanceNotes: 'DA authority reviewed route/warehouse compliance.',
                  })
                }
              >
                Clear hold
              </Button>
            </Stack>
          ))}
        </Box>
      )}

      <Typography sx={{ fontWeight: 800, mb: 1 }}>Open alerts ({openAlerts.length})</Typography>
      {openAlerts.length === 0 ? (
        <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted, mb: 3 }}>
          Walang open compliance alert sa nakalipas na 72 oras.
        </Typography>
      ) : (
        <Box sx={{ mb: 3 }}>
          {openAlerts.map((a) => (
            <AlertCard key={a.alertId} alert={a} onAcknowledge={() => setAckTarget(a)} />
          ))}
        </Box>
      )}

      <Typography sx={{ fontWeight: 800, mb: 1 }}>Warehouse check-in (48h) — {arrived.length}</Typography>
      {arrived.length === 0 ? (
        <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted, mb: 3 }}>
          Walang recent warehouse arrival.
        </Typography>
      ) : (
        <Box sx={{ mb: 3 }}>
          {arrived.map((d) => (
            <Box
              key={d.containerUuid}
              sx={{ p: 1.25, mb: 1, borderRadius: 1.5, border: `1px solid ${portalColors.border}` }}
            >
              <Typography sx={{ fontWeight: 700 }}>{d.containerNumber}</Typography>
              <Typography sx={{ fontSize: '0.8rem', color: portalColors.textMuted }}>
                {d.entryReference} · {d.status}
                {d.driverName ? ` · ${d.driverName}` : ''}
              </Typography>
            </Box>
          ))}
        </Box>
      )}

      <Typography sx={{ fontWeight: 800, mb: 1 }}>Cleared alerts (log)</Typography>
      {history.length === 0 ? (
        <Typography sx={{ fontSize: '0.85rem', color: portalColors.textMuted }}>Wala pang na-clear na alert.</Typography>
      ) : (
        history.map((a) => <AlertCard key={a.alertId} alert={a} />)
      )}

      <Dialog open={ackTarget != null} onClose={() => setAckTarget(null)} maxWidth="sm" fullWidth>
        <DialogTitle>Clear alert — DA remarks</DialogTitle>
        <DialogContent>
          <Typography sx={{ fontSize: '0.85rem', mb: 2, color: portalColors.textMuted }}>
            I-log ang resulta ng tawag o action (required). Mananatili sa history ang alert; tatanggal lang ang open
            status.
          </Typography>
          {ackTarget ? (
            <Typography sx={{ fontWeight: 700, mb: 1 }}>
              {ackTarget.containerNumber} · {formatComplianceEventType(ackTarget.eventType)}
            </Typography>
          ) : null}
          <TextField
            label="DA note / remarks"
            multiline
            minRows={3}
            fullWidth
            value={ackNotes}
            onChange={(e) => setAckNotes(e.target.value)}
            placeholder="Hal: Natawagan ang driver, traffic sa SLEX, ipinagpatuloy ang biyahe."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setAckTarget(null)}>Cancel</Button>
          <Button
            variant="contained"
            disabled={!ackNotes.trim() || acknowledging}
            onClick={() => void submitAcknowledge()}
          >
            Save & clear alert
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
