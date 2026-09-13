import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import QrCode2OutlinedIcon from '@mui/icons-material/QrCode2Outlined'
import {
  Alert,
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
import { useState } from 'react'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { ContainerInspectionReviewSummary } from '../components/ContainerInspectionReviewSummary'
import {
  type AddTransportTagResult,
  useAddTransportTagMutation,
  useGetTransportTagContainerDetailQuery,
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

export function TransportTagDetailPage() {
  const { containerUuid = '' } = useParams()
  const { data, isLoading, isError, refetch } = useGetTransportTagContainerDetailQuery(containerUuid, {
    skip: !containerUuid,
  })
  const [addTransportTag, { isLoading: tagging }] = useAddTransportTagMutation()
  const [scheduledWarehouseDate, setScheduledWarehouseDate] = useState(todayDateValue)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [confirmError, setConfirmError] = useState('')
  const [taggedResult, setTaggedResult] = useState<AddTransportTagResult | null>(null)

  const detail = data?.data
  const container = detail?.container
  const transportTag = detail?.transportTag
  const canTag = container?.containerStatus === 'ReadyForTransport' && !transportTag

  useBreadcrumbLabel(container?.containerNumber)

  const handleConfirmTag = async () => {
    if (!containerUuid || !scheduledWarehouseDate) {
      setConfirmError('Select the scheduled warehouse inspection date before tagging.')
      return
    }

    setConfirmError('')
    try {
      const result = await addTransportTag({
        containerUuid,
        scheduledWarehouseDate,
      }).unwrap()
      setConfirmOpen(false)
      setTaggedResult(result.data ?? null)
      await refetch()
    } catch {
      setConfirmError('Unable to tag container. Ensure it is ready for transport and not already tagged.')
    }
  }

  if (isLoading) {
    return <Typography sx={{ color: portalColors.textMuted }}>Loading container details…</Typography>
  }

  if (isError || !container) {
    return (
      <Box>
        <Alert severity="error" sx={{ mb: 2 }}>
          Container details are unavailable or this container is not ready for transport tagging.
        </Alert>
        <Button component={RouterLink} to="/agency/transport-tags" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
          Back to transport tags
        </Button>
      </Box>
    )
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Transport Tagging"
        title={container.containerNumber}
        subtitle={`Entry ${container.entry.referenceNo} · ${container.entry.agencyCode}`}
        actions={
          <Stack direction="row" spacing={1}>
            <Button component={RouterLink} to="/agency/transport-tags" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
              Back
            </Button>
            {canTag ? (
              <Button type="button" variant="contained" sx={portalPrimaryButtonSx} onClick={() => setConfirmOpen(true)}>
                Tag & generate QR
              </Button>
            ) : null}
          </Stack>
        }
      />

      <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
        <Chip size="small" label={container.containerStatus} sx={portalStatusChipSx(container.containerStatus)} />
        {container.inspectionOutcome ? (
          <Chip size="small" label={`Inspection: ${container.inspectionOutcome}`} sx={portalStatusChipSx(container.inspectionOutcome)} />
        ) : null}
        {detail.updatedAt ? (
          <Chip
            size="small"
            variant="outlined"
            label={`Last update: ${new Date(detail.updatedAt).toLocaleString()}`}
          />
        ) : null}
      </Stack>

      {transportTag ? (
        <PortalPanel title="Transport tag">
          <Stack spacing={2} sx={{ px: 2.5, py: 2 }}>
            <Typography sx={{ color: portalColors.textMuted }}>
              Tagged on {new Date(transportTag.taggedAt).toLocaleString()}
              {transportTag.scheduledWarehouseDate
                ? ` · Scheduled warehouse inspection: ${formatDisplayDate(transportTag.scheduledWarehouseDate.slice(0, 10))}`
                : ''}
            </Typography>
            {transportTag.qrCodeData ? (
              <Box sx={{ textAlign: 'center' }}>
                <Box
                  component="img"
                  src={transportTag.qrCodeData}
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
        </PortalPanel>
      ) : null}

      <Box sx={{ mt: 3 }}>
        <ContainerInspectionReviewSummary
          detail={container}
          disablePhotoReviews
          photoGridColumns={3}
          onReviewPhoto={async () => undefined}
        />
      </Box>

      <Dialog open={confirmOpen} onClose={() => !tagging && setConfirmOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Tag container for warehouse transport?</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ pt: 0.5 }}>
            <Typography sx={{ color: portalColors.textMuted }}>
              Tag container <strong>{container.containerNumber}</strong> for warehouse transport and 2nd border inspection.
            </Typography>
            <TextField
              label="Scheduled warehouse inspection date"
              type="date"
              value={scheduledWarehouseDate}
              onChange={(e) => setScheduledWarehouseDate(e.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              fullWidth
            />
            {confirmError ? <Alert severity="error">{confirmError}</Alert> : null}
          </Stack>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button type="button" onClick={() => setConfirmOpen(false)} disabled={tagging} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button
            type="button"
            onClick={() => void handleConfirmTag()}
            variant="contained"
            disabled={tagging || !scheduledWarehouseDate}
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
          {taggedResult?.qrCodeData ? (
            <Box sx={{ textAlign: 'center', pt: 1 }}>
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
