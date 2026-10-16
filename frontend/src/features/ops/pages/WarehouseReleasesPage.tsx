import { useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import {
  useCreateReleaseAuthorizationMutation,
  useExecuteReleaseMutation,
  useGetReleaseAuthorizationsQuery,
  useGetWarehouseInventoryQuery,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { formatMavLibraryCommodity } from '../utils/mavCommodityDisplay'

export function WarehouseReleasesPage() {
  const { data: authData } = useGetReleaseAuthorizationsQuery()
  const { data: inventoryData } = useGetWarehouseInventoryQuery()
  const [createAuth, { isSuccess: authCreated }] = useCreateReleaseAuthorizationMutation()
  const [executeRelease, { isSuccess: releaseDone }] = useExecuteReleaseMutation()

  const [entryUuid, setEntryUuid] = useState('')
  const [recipientName, setRecipientName] = useState('')
  const [recipientId, setRecipientId] = useState('')
  const [inventoryUuid, setInventoryUuid] = useState('')
  const [authorizationUuid, setAuthorizationUuid] = useState('')

  const authorizations = authData?.data ?? []
  const storedInventory = (inventoryData?.data?.items ?? []).filter((i) => i.status === 'Stored')

  const entryOptions = useMemo(() => {
    const byEntry = new Map<string, { entryUuid: string; entryReference: string; mavLabel: string }>()
    for (const row of storedInventory) {
      if (!byEntry.has(row.entryUuid)) {
        const mav = formatMavLibraryCommodity(row)
        byEntry.set(row.entryUuid, {
          entryUuid: row.entryUuid,
          entryReference: row.entryReference,
          mavLabel: mav.primary,
        })
      }
    }
    return [...byEntry.values()]
  }, [storedInventory])

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Release Authorizations"
        subtitle="Create authorizations and release stored containers (MAV commodity from library)."
      />
      {authCreated && <Alert severity="success" sx={{ mb: 2 }}>Release authorization created.</Alert>}
      {releaseDone && <Alert severity="success" sx={{ mb: 2 }}>Container released successfully.</Alert>}

      <PortalPanel title="Create authorization">
        <Stack spacing={2} sx={{ maxWidth: 480, p: 2.5 }}>
          <TextField select label="Entry (stored inventory)" value={entryUuid} onChange={(e) => setEntryUuid(e.target.value)} required fullWidth>
            {entryOptions.length === 0 ? (
              <MenuItem value="" disabled>No stored entries</MenuItem>
            ) : (
              entryOptions.map((e) => (
                <MenuItem key={e.entryUuid} value={e.entryUuid}>
                  {e.entryReference} — {e.mavLabel}
                </MenuItem>
              ))
            )}
          </TextField>
          <TextField label="Recipient Name" value={recipientName} onChange={(e) => setRecipientName(e.target.value)} required fullWidth />
          <TextField label="Recipient ID Number" value={recipientId} onChange={(e) => setRecipientId(e.target.value)} required fullWidth />
          <Button
            variant="contained"
            sx={portalPrimaryButtonSx}
            onClick={() => createAuth({ entryUuid, recipientName, recipientIdNumber: recipientId })}
            disabled={!entryUuid || !recipientName || !recipientId}
          >
            Create Authorization
          </Button>
        </Stack>
      </PortalPanel>

      <Box sx={{ mt: 3 }}>
        <PortalPanel title="Execute release">
          <Stack spacing={2} sx={{ maxWidth: 480, p: 2.5 }}>
            <TextField select label="Inventory Item" value={inventoryUuid} onChange={(e) => setInventoryUuid(e.target.value)} required fullWidth>
              {storedInventory.map((i) => {
                const mav = formatMavLibraryCommodity(i)
                return (
                  <MenuItem key={i.uuid} value={i.uuid}>
                    {i.containerNumber} — {i.entryReference} — {mav.primary}
                  </MenuItem>
                )
              })}
            </TextField>
            <TextField select label="Authorization" value={authorizationUuid} onChange={(e) => setAuthorizationUuid(e.target.value)} required fullWidth>
              {authorizations.map((a) => (
                <MenuItem key={a.uuid} value={a.uuid}>
                  {a.entryReference} — {a.recipientName}
                </MenuItem>
              ))}
            </TextField>
            <Button
              variant="contained"
              sx={portalPrimaryButtonSx}
              onClick={() => executeRelease({ inventoryUuid, authorizationUuid })}
              disabled={!inventoryUuid || !authorizationUuid}
            >
              Release Container
            </Button>
          </Stack>
        </PortalPanel>
      </Box>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="All authorizations"
          columns={['Entry', 'MAV commodity', 'Recipient', 'ID Number', 'Authorized', 'Releases']}
          isEmpty={authorizations.length === 0}
          emptyMessage="No release authorizations yet."
        >
          {authorizations.map((a) => {
            const mav = formatMavLibraryCommodity(a)
            return (
              <TableRow key={a.uuid} hover>
                <TableCell>{a.entryReference}</TableCell>
                <TableCell>
                  <Typography sx={{ fontSize: '0.875rem' }}>{mav.primary}</Typography>
                  {mav.secondary && (
                    <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>{mav.secondary}</Typography>
                  )}
                </TableCell>
                <TableCell>{a.recipientName}</TableCell>
                <TableCell>{a.recipientIdNumber}</TableCell>
                <TableCell>{new Date(a.authorizedAt).toLocaleString()}</TableCell>
                <TableCell>{a.releaseCount}</TableCell>
              </TableRow>
            )
          })}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
