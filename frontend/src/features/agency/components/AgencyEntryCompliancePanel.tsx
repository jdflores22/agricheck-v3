import {
  Alert,
  Button,
  Checkbox,
  Chip,
  FormControlLabel,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetAgencyEntryComplianceChainQuery,
  useRecordAgencyPortDabiMutation,
} from '../api/agencyApi'

function layerColor(status: string): 'default' | 'success' | 'warning' | 'error' {
  if (status === 'Complete' || status === 'Linked') return 'success'
  if (status === 'InProgress') return 'warning'
  if (status === 'NotApplicable') return 'default'
  return 'warning'
}

export function AgencyEntryCompliancePanel({ entryUuid }: { entryUuid: string }) {
  const { data, isLoading, refetch } = useGetAgencyEntryComplianceChainQuery(entryUuid, { skip: !entryUuid })
  const [recordDabi, { isLoading: recording }] = useRecordAgencyPortDabiMutation()
  const chain = data?.data
  const [findings, setFindings] = useState('')
  const [stampSps, setStampSps] = useState(true)
  const [error, setError] = useState('')

  if (isLoading) {
    return <Typography variant="body2">Loading compliance chain…</Typography>
  }

  if (!chain) {
    return null
  }

  const layers = [
    chain.accreditation,
    chain.agencyLicense,
    chain.destinationFacility,
    chain.spsImportClearance,
    chain.erfi,
    chain.mav,
  ]

  const handlePortDabi = async () => {
    setError('')
    try {
      await recordDabi({
        entryUuid,
        findings: findings.trim() || undefined,
        stampSpsUsed: stampSps,
      }).unwrap()
      setFindings('')
      await refetch()
    } catch {
      setError('Unable to record port DABI inspection.')
    }
  }

  return (
    <Stack spacing={2}>
      <PortalPanel title="Import compliance chain">
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

      <PortalPanel title="Port DABI (stamp SPSIC)">
        <Stack spacing={2}>
          <TextField
            label="Findings"
            size="small"
            multiline
            minRows={2}
            value={findings}
            onChange={(e) => setFindings(e.target.value)}
            fullWidth
          />
          <FormControlLabel
            control={<Checkbox checked={stampSps} onChange={(e) => setStampSps(e.target.checked)} />}
            label="Stamp SPSIC as USED at port"
          />
          {error ? <Alert severity="error">{error}</Alert> : null}
          <Button
            variant="contained"
            sx={portalPrimaryButtonSx}
            disabled={recording}
            onClick={() => void handlePortDabi()}
          >
            Record port DABI
          </Button>
        </Stack>
      </PortalPanel>
    </Stack>
  )
}
