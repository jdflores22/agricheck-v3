import OpenInNewOutlinedIcon from '@mui/icons-material/OpenInNewOutlined'
import ReportProblemOutlinedIcon from '@mui/icons-material/ReportProblemOutlined'
import { Box, Button, Chip, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { portalColors } from '../../../components/portal/portalTheme'
import type { DaDeliveryComplianceAlert } from '../api/daApi'
import {
  complianceAlertSeverity,
  formatComplianceDetails,
  formatComplianceEventType,
} from '../utils/complianceAlertLabels'

function severityChipColor(severity: ReturnType<typeof complianceAlertSeverity>) {
  if (severity === 'critical') return 'error' as const
  if (severity === 'warning') return 'warning' as const
  return 'default' as const
}

interface DaLiveComplianceAlertsProps {
  alerts: DaDeliveryComplianceAlert[]
  selectedContainerUuid: string | null
  onSelectContainer: (uuid: string) => void
}

export function DaLiveComplianceAlerts({
  alerts,
  selectedContainerUuid,
  onSelectContainer,
}: DaLiveComplianceAlertsProps) {
  if (alerts.length === 0) return null

  return (
    <Box
      sx={{
        mb: 2,
        p: 2,
        borderRadius: 2,
        border: `1px solid ${portalColors.border}`,
        bgcolor: '#FFFBEB',
      }}
    >
      <Stack direction="row" spacing={1} sx={{ alignItems: 'center', mb: 1.5 }}>
        <ReportProblemOutlinedIcon sx={{ color: '#B45309', fontSize: 22 }} />
        <Typography sx={{ fontWeight: 800, fontSize: '0.95rem' }}>
          Compliance alerts (24h) · {alerts.length}
        </Typography>
      </Stack>
      <Typography sx={{ fontSize: '0.78rem', color: portalColors.textMuted, mb: 1.5 }}>
        Driver TAG reports and route flags. Piliin ang container para makita sa mapa; buong trail at geotag sa
        delivery report.
      </Typography>
      <Stack spacing={1}>
        {alerts.slice(0, 12).map((a) => {
          const severity = complianceAlertSeverity(a.eventType)
          const selected = selectedContainerUuid === a.containerUuid
          const details = formatComplianceDetails(a.details)
          return (
            <Box
              key={`${a.containerUuid}-${a.eventType}-${a.recordedAt}`}
              sx={{
                p: 1.25,
                borderRadius: 1.5,
                border: `1px solid ${selected ? portalColors.primary : portalColors.border}`,
                bgcolor: selected ? '#ECFDF5' : portalColors.bgWhite,
                cursor: 'pointer',
              }}
              onClick={() => onSelectContainer(a.containerUuid)}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === 'Enter' || e.key === ' ') onSelectContainer(a.containerUuid)
              }}
            >
              <Stack
                direction={{ xs: 'column', sm: 'row' }}
                spacing={1}
                sx={{ alignItems: { sm: 'center' } }}
              >
                <Stack direction="row" spacing={0.75} sx={{ flex: 1, flexWrap: 'wrap', alignItems: 'center' }}>
                  <Chip
                    size="small"
                    label={formatComplianceEventType(a.eventType)}
                    color={severityChipColor(severity)}
                    sx={{ height: 22, fontSize: '0.7rem', fontWeight: 700 }}
                  />
                  <Typography component="span" sx={{ fontWeight: 800, fontSize: '0.85rem' }}>
                    {a.containerNumber}
                  </Typography>
                  {details ? (
                    <Typography component="span" sx={{ fontSize: '0.8rem', color: portalColors.textMuted }}>
                      {details}
                    </Typography>
                  ) : null}
                  <Typography component="span" sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
                    {new Date(a.recordedAt).toLocaleString()}
                  </Typography>
                </Stack>
                <Button
                  size="small"
                  variant="outlined"
                  component={RouterLink}
                  to={`/da/logistics/containers/${a.containerUuid}/report`}
                  target="_blank"
                  rel="noopener noreferrer"
                  startIcon={<OpenInNewOutlinedIcon />}
                  onClick={(e) => e.stopPropagation()}
                >
                  Delivery report
                </Button>
              </Stack>
            </Box>
          )
        })}
      </Stack>
    </Box>
  )
}
