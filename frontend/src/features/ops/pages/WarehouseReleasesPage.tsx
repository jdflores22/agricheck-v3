import { useState } from 'react'
import {
  Alert,
  Box,
  Button,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import {
  useCreateReleaseAuthorizationMutation,
  useExecuteReleaseMutation,
  useGetReceivableContainersQuery,
  useGetReleaseAuthorizationsQuery,
  useGetWarehouseInventoryQuery,
} from '../api/opsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'

export function WarehouseReleasesPage() {
  const { data: authData } = useGetReleaseAuthorizationsQuery()
  const { data: inventoryData } = useGetWarehouseInventoryQuery()
  const { data: containersData } = useGetReceivableContainersQuery()
  const [createAuth, { isSuccess: authCreated }] = useCreateReleaseAuthorizationMutation()
  const [executeRelease, { isSuccess: releaseDone }] = useExecuteReleaseMutation()

  const [entryUuid, setEntryUuid] = useState('')
  const [recipientName, setRecipientName] = useState('')
  const [recipientId, setRecipientId] = useState('')
  const [inventoryUuid, setInventoryUuid] = useState('')
  const [authorizationUuid, setAuthorizationUuid] = useState('')

  const authorizations = authData?.data ?? []
  const storedInventory = (inventoryData?.data?.items ?? []).filter((i) => i.status === 'Stored')
  const entryOptions = containersData?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Release Authorizations"
        subtitle="Create authorizations and release stored containers."
      />
      {authCreated && <Alert severity="success" sx={{ mb: 2 }}>Release authorization created.</Alert>}
      {releaseDone && <Alert severity="success" sx={{ mb: 2 }}>Container released successfully.</Alert>}

      <PortalPanel title="Create authorization">
        <Stack spacing={2} sx={{ maxWidth: 480, p: 2.5 }}>
          <TextField select label="Entry" value={entryUuid} onChange={(e) => setEntryUuid(e.target.value)} required fullWidth>
            {entryOptions.map((c) => (
              <MenuItem key={c.entryUuid} value={c.entryUuid}>{c.entryReference} — {c.containerNumber}</MenuItem>
            ))}
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
              {storedInventory.map((i) => (
                <MenuItem key={i.uuid} value={i.uuid}>{i.containerNumber} — {i.entryReference}</MenuItem>
              ))}
            </TextField>
            <TextField select label="Authorization" value={authorizationUuid} onChange={(e) => setAuthorizationUuid(e.target.value)} required fullWidth>
              {authorizations.map((a) => (
                <MenuItem key={a.uuid} value={a.uuid}>{a.entryReference} — {a.recipientName}</MenuItem>
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
          columns={['Entry', 'Recipient', 'ID Number', 'Authorized', 'Releases']}
          isEmpty={authorizations.length === 0}
          emptyMessage="No release authorizations yet."
        >
          {authorizations.map((a) => (
            <TableRow key={a.uuid} hover>
              <TableCell>{a.entryReference}</TableCell>
              <TableCell>{a.recipientName}</TableCell>
              <TableCell>{a.recipientIdNumber}</TableCell>
              <TableCell>{new Date(a.authorizedAt).toLocaleString()}</TableCell>
              <TableCell>{a.releaseCount}</TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
