import QrCode2OutlinedIcon from '@mui/icons-material/QrCode2Outlined'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { useEffect, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  type AddTransportTagResult,
  type TransportTagQueueItem,
  useAddTransportTagMutation,
  useGetTransportTagQueueQuery,
} from '../api/agencyApi'

function todayDateValue() {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

function formatDisplayDate(value: string) {
  const parsed = new Date(`${value}T00:00:00`)
  return Number.isNaN(parsed.getTime()) ? value : parsed.toLocaleDateString()
}

export function TransportTagPage() {
  const { data, isLoading, refetch } = useGetTransportTagQueueQuery()
  const [addTransportTag, { isLoading: tagging }] = useAddTransportTagMutation()
  const [scheduledDates, setScheduledDates] = useState<Record<string, string>>({})
  const [message, setMessage] = useState<{ tone: 'success' | 'error'; text: string } | null>(null)
  const [taggedResult, setTaggedResult] = useState<AddTransportTagResult | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [confirmError, setConfirmError] = useState('')
  const [pendingContainer, setPendingContainer] = useState<TransportTagQueueItem | null>(null)

  const readyQueue = data?.data?.ready ?? []
  const taggedQueue = data?.data?.tagged ?? []

  useEffect(() => {
    const today = todayDateValue()
    setScheduledDates((current) => {
      const next = { ...current }
      for (const item of readyQueue) {
        if (!next[item.containerUuid]) {
          next[item.containerUuid] = today
        }
      }
      return next
    })
  }, [readyQueue])

  const getScheduledDate = (containerUuid: string) => scheduledDates[containerUuid] ?? todayDateValue()

  const setScheduledDate = (containerUuid: string, value: string) => {
    setScheduledDates((current) => ({ ...current, [containerUuid]: value }))
  }

  const pendingScheduledDate = pendingContainer
    ? getScheduledDate(pendingContainer.containerUuid)
    : ''

  const openConfirm = (container: TransportTagQueueItem) => {
    setConfirmError('')
    setPendingContainer(container)
    setConfirmOpen(true)
  }

  const closeConfirm = () => {
    if (tagging) return
    setConfirmOpen(false)
    setPendingContainer(null)
    setConfirmError('')
  }

  const handleConfirmTag = async () => {
    if (!pendingContainer) return
    const scheduledWarehouseDate = getScheduledDate(pendingContainer.containerUuid)
    if (!scheduledWarehouseDate) {
      setConfirmError('Select the scheduled warehouse inspection date before tagging.')
      return
    }

    setMessage(null)
    setConfirmError('')
    try {
      const result = await addTransportTag({
        containerUuid: pendingContainer.containerUuid,
        scheduledWarehouseDate,
      }).unwrap()
      setConfirmOpen(false)
      setPendingContainer(null)
      setTaggedResult(result.data ?? null)
      await refetch()
    } catch {
      setConfirmError('Unable to tag container. Ensure it is ready for transport and not already tagged.')
    }
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Workflow"
        title="Transport Tagging"
        subtitle="Tag inspected containers for warehouse transport and 2nd border inspection. Each tag generates a signed transport QR code for AgriTrack operators to scan and claim."
      />

      {message ? (
        <Alert severity={message.tone} sx={{ mb: 2 }} onClose={() => setMessage(null)}>
          {message.text}
        </Alert>
      ) : null}

      <PortalPanel title="Ready for transport">
        <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
          <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
            Containers appear here after inspector approval. Tagging assigns warehouse transport for 2nd border inspection, moves each container to awaiting confirmation, and exposes it in AgriTrack.
          </Typography>

          {isLoading ? (
            <Typography sx={{ color: portalColors.textMuted }}>Loading queue…</Typography>
          ) : readyQueue.length === 0 ? (
            <Alert severity="info">No containers are ready for transport tagging right now.</Alert>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>Container</TableCell>
                  <TableCell>Entry</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Last update</TableCell>
                  <TableCell>Scheduled date</TableCell>
                  <TableCell align="right">Action</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {readyQueue.map((item) => {
                  const rowScheduledDate = getScheduledDate(item.containerUuid)
                  return (
                  <TableRow key={item.containerUuid} hover>
                    <TableCell sx={{ fontWeight: 600 }}>{item.containerNumber}</TableCell>
                    <TableCell>{item.entryReference}</TableCell>
                    <TableCell>{item.containerStatus}</TableCell>
                    <TableCell>
                      {item.updatedAt ? new Date(item.updatedAt).toLocaleString() : '—'}
                    </TableCell>
                    <TableCell>
                      <TextField
                        type="date"
                        size="small"
                        value={rowScheduledDate}
                        onChange={(e) => setScheduledDate(item.containerUuid, e.target.value)}
                        slotProps={{ inputLabel: { shrink: true } }}
                        sx={{ minWidth: 150 }}
                      />
                    </TableCell>
                    <TableCell align="right">
                      <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                        <Button
                          component={RouterLink}
                          to={`/agency/transport-tags/${item.containerUuid}`}
                          size="small"
                          sx={portalOutlinedButtonSx}
                        >
                          View
                        </Button>
                        <Button
                          type="button"
                          variant="contained"
                          size="small"
                          sx={portalPrimaryButtonSx}
                          disabled={tagging || item.hasTransportTag || !rowScheduledDate}
                          onClick={() => openConfirm(item)}
                        >
                          Tag & generate QR
                        </Button>
                      </Stack>
                    </TableCell>
                  </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          )}
        </Stack>
      </PortalPanel>

      <Box sx={{ mt: 3 }}>
        <PortalPanel title="Tagged — awaiting operator claim">
          <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
            <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
              Tagged containers move here while waiting for an AgriTrack operator to scan the transport QR and claim the shipment.
            </Typography>

            {isLoading ? (
              <Typography sx={{ color: portalColors.textMuted }}>Loading tagged containers…</Typography>
            ) : taggedQueue.length === 0 ? (
              <Alert severity="info">No tagged containers are awaiting operator claim.</Alert>
            ) : (
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Container</TableCell>
                    <TableCell>Entry</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Tagged at</TableCell>
                    <TableCell>Scheduled date</TableCell>
                    <TableCell align="right">Action</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {taggedQueue.map((item) => (
                    <TableRow key={item.containerUuid} hover>
                      <TableCell sx={{ fontWeight: 600 }}>{item.containerNumber}</TableCell>
                      <TableCell>{item.entryReference}</TableCell>
                      <TableCell>{item.containerStatus}</TableCell>
                      <TableCell>{new Date(item.taggedAt).toLocaleString()}</TableCell>
                      <TableCell>
                        {item.scheduledWarehouseDate
                          ? formatDisplayDate(item.scheduledWarehouseDate.slice(0, 10))
                          : '—'}
                      </TableCell>
                      <TableCell align="right">
                        <Button
                          component={RouterLink}
                          to={`/agency/transport-tags/${item.containerUuid}`}
                          size="small"
                          sx={portalOutlinedButtonSx}
                        >
                          View & QR
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </Stack>
        </PortalPanel>
      </Box>

      <Dialog
        open={confirmOpen && Boolean(pendingContainer)}
        onClose={closeConfirm}
        maxWidth="sm"
        fullWidth
      >
        <DialogTitle>Tag container for warehouse transport?</DialogTitle>
        <DialogContent>
          <Typography sx={{ color: portalColors.textMuted }}>
            You are about to tag container <strong>{pendingContainer?.containerNumber}</strong>
            {pendingContainer?.entryReference ? ` for entry ${pendingContainer.entryReference}` : ''}.
            It will be scheduled for warehouse transport and 2nd border inspection on{' '}
            <strong>{formatDisplayDate(pendingScheduledDate)}</strong>.
            A signed transport QR will be generated for AgriTrack operators to scan and claim.
          </Typography>
          {confirmError ? <Alert severity="error" sx={{ mt: 2 }}>{confirmError}</Alert> : null}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button type="button" onClick={closeConfirm} disabled={tagging} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button
            type="button"
            onClick={() => void handleConfirmTag()}
            variant="contained"
            disabled={tagging || !pendingScheduledDate}
            sx={portalPrimaryButtonSx}
          >
            {tagging ? <CircularProgress size={20} color="inherit" /> : 'Yes, tag & generate QR'}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={Boolean(taggedResult)} onClose={() => setTaggedResult(null)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <QrCode2OutlinedIcon color="primary" />
          Transport QR generated
        </DialogTitle>
        <DialogContent>
          {taggedResult ? (
            <Stack spacing={2}>
              <Typography sx={{ color: portalColors.textMuted }}>
                Container <strong>{taggedResult.containerNumber}</strong> · Entry {taggedResult.entryReference}
              </Typography>
              {taggedResult.scheduledWarehouseDate ? (
                <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                  Scheduled warehouse inspection:{' '}
                  <strong>{formatDisplayDate(taggedResult.scheduledWarehouseDate.slice(0, 10))}</strong>
                </Typography>
              ) : null}
              <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                Operators can scan this QR in AgriTrack to claim the container. This is separate from the entry certificate verification QR.
              </Typography>
              {taggedResult.qrCodeData ? (
                <Box sx={{ textAlign: 'center' }}>
                  <Box
                    component="img"
                    src={taggedResult.qrCodeData}
                    alt="Transport QR code"
                    sx={{
                      width: 220,
                      height: 220,
                      objectFit: 'contain',
                      border: `1px solid ${portalColors.border}`,
                      borderRadius: 2,
                      bgcolor: '#fff',
                      p: 1,
                    }}
                  />
                </Box>
              ) : null}
            </Stack>
          ) : null}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button type="button" onClick={() => setTaggedResult(null)} sx={portalOutlinedButtonSx}>
            Close
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
