import { Box, Button, FormControl, InputLabel, MenuItem, Select, Stack, Typography } from '@mui/material'
import { useMemo, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { useGetClaimableContainersQuery, useGetClaimedContainersQuery } from '../api/opsApi'
import { OperatorRoutePinsPanel } from '../components/OperatorRoutePinsPanel'

export function OperatorRouteTestPage() {
  const { data: claimableData } = useGetClaimableContainersQuery()
  const { data: claimedData } = useGetClaimedContainersQuery()
  const [selectedUuid, setSelectedUuid] = useState('')

  const containers = useMemo(() => {
    const claimed = claimedData?.data ?? []
    const claimable = claimableData?.data ?? []
    const map = new Map<string, { uuid: string; containerNumber: string; status: string }>()
    for (const c of [...claimed, ...claimable]) {
      map.set(c.uuid, { uuid: c.uuid, containerNumber: c.containerNumber, status: c.status })
    }
    return [...map.values()]
  }, [claimableData, claimedData])

  const selected = containers.find((c) => c.uuid === selectedUuid)

  return (
    <Box>
      <PortalPageHeader
        eyebrow="AgriTrack"
        title="Manual route test"
        subtitle="Place pickup and warehouse pins on the map, preview the driving route, then save to a container for driver testing."
      />

      <PortalPanel title="Quick access">
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ p: 2.5 }}>
          <Typography variant="body2" color="text.secondary" sx={{ flex: 1 }}>
            Use this screen to test map pins and driving route preview before assigning a driver on AgriTrack mobile.
          </Typography>
          <Button component={RouterLink} to="/operator/containers" variant="outlined" sx={portalOutlinedButtonSx}>
            Back to containers
          </Button>
        </Stack>
      </PortalPanel>

      <PortalPanel title="Select container (optional until save)">
        <Stack spacing={2} sx={{ p: 2.5 }}>
          <FormControl fullWidth size="small">
            <InputLabel>Container</InputLabel>
            <Select
              label="Container"
              value={selectedUuid}
              onChange={(e) => setSelectedUuid(e.target.value)}
            >
              <MenuItem value="">
                <em>Choose a container…</em>
              </MenuItem>
              {containers.map((c) => (
                <MenuItem key={c.uuid} value={c.uuid}>
                  {c.containerNumber} · {c.status}
                </MenuItem>
              ))}
            </Select>
          </FormControl>
          {!selected && (
            <Typography variant="body2" color="text.secondary">
              Pick a container to load or save pins. You can also open route pins from the Containers list.
            </Typography>
          )}
        </Stack>
      </PortalPanel>

      {selected && (
        <PortalPanel title={`Route pins — ${selected.containerNumber}`}>
          <Box sx={{ p: 2.5 }}>
            <OperatorRoutePinsPanel containerUuid={selected.uuid} containerNumber={selected.containerNumber} />
          </Box>
        </PortalPanel>
      )}
    </Box>
  )
}
