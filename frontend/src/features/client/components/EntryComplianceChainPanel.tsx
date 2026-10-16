import { Alert, Button, Chip, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import type { EntryComplianceChain } from '../api/clientApi'

function layerColor(status: string): 'default' | 'success' | 'warning' | 'error' {
  if (status === 'Complete' || status === 'Linked') return 'success'
  if (status === 'InProgress') return 'warning'
  if (status === 'NotApplicable') return 'default'
  return 'warning'
}

export function EntryComplianceChainPanel({ chain }: { chain: EntryComplianceChain }) {
  const layers = [
    chain.accreditation,
    chain.agencyLicense,
    chain.destinationFacility,
    chain.spsImportClearance,
    chain.erfi,
    chain.mav,
  ]

  return (
    <Stack spacing={2}>
      <PortalPanel title="Compliance spine">
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Phase: {chain.compliancePhase} · {chain.nextRecommendedAction ?? 'On track'}
        </Typography>
        <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
          {layers.map((layer) => (
            <Chip
              key={layer.layer}
              label={`${layer.layer}: ${layer.status}`}
              color={layerColor(layer.status)}
              size="small"
              variant="outlined"
            />
          ))}
        </Stack>
        <Stack spacing={1}>
          {layers.map((layer) => (
            <Typography key={layer.layer} variant="body2" color="text.secondary">
              <strong>{layer.layer}</strong> — {layer.summary}
            </Typography>
          ))}
        </Stack>
      </PortalPanel>
      {chain.blockers.length > 0 ? (
        <Alert severity="error">
          {chain.blockers.map((b) => (
            <Typography key={b.code} variant="body2">{b.message}</Typography>
          ))}
        </Alert>
      ) : null}
      {chain.warnings.length > 0 ? (
        <Alert severity="warning">
          {chain.warnings.map((w) => (
            <Typography key={w.code} variant="body2">{w.message}</Typography>
          ))}
        </Alert>
      ) : null}
      <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
        <Button size="small" component={RouterLink} to="/client/licenses" variant="outlined">
          Bureau licenses
        </Button>
        <Button size="small" component={RouterLink} to="/client/facilities" variant="outlined">
          RSAS / facilities
        </Button>
      </Stack>
    </Stack>
  )
}
