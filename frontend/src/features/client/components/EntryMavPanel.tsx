import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import LinkOutlinedIcon from '@mui/icons-material/LinkOutlined'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
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
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { useGetAvailableMicsQuery } from '../../mav/api/mavApi'
import {
  useCheckMavNoQuery,
  useUpdateEntryMavMutation,
  useUploadEntryFileMutation,
  useUtilizeEntryMicMutation,
  type Entry,
} from '../api/clientApi'

const MAV_CERT_DOCUMENT_TYPE = 'mav_certificate'

function mavStatusLabel(status: string) {
  switch (status) {
    case 'Approved':
      return 'Approved'
    case 'Rejected':
      return 'Rejected'
    case 'RevisionRequired':
      return 'Revision Required'
    case 'PendingReview':
      return 'Pending Review'
    default:
      return 'Not Provided'
  }
}

export function EntryMavPanel({
  entry,
  editable,
  onUpdated,
}: {
  entry: Entry
  editable: boolean
  onUpdated?: () => void
}) {
  const [mavNo, setMavNo] = useState(entry.mav?.mavNo ?? '')
  const [debouncedMavNo, setDebouncedMavNo] = useState(mavNo)
  const [selectedMicUuid, setSelectedMicUuid] = useState('')
  const [utilizeVolume, setUtilizeVolume] = useState('')
  const [confirmUtilizeOpen, setConfirmUtilizeOpen] = useState(false)
  const [uploadSuccessOpen, setUploadSuccessOpen] = useState(false)

  const { data: checkData, isFetching: checkingMavNo } = useCheckMavNoQuery(
    { mavNo: debouncedMavNo, excludeUuid: entry.uuid },
    { skip: !debouncedMavNo.trim() || debouncedMavNo.trim().length < 3 },
  )
  const { data: micsData, isLoading: loadingMics } = useGetAvailableMicsQuery(undefined, { skip: !editable })
  const [updateMav, { isLoading: savingMavNo }] = useUpdateEntryMavMutation()
  const [uploadFile, { isLoading: uploadingCert }] = useUploadEntryFileMutation()
  const [utilizeMic, { isLoading: utilizing, error: utilizeError }] = useUtilizeEntryMicMutation()

  useEffect(() => {
    setMavNo(entry.mav?.mavNo ?? '')
  }, [entry.mav?.mavNo])

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedMavNo(mavNo.trim()), 400)
    return () => window.clearTimeout(timer)
  }, [mavNo])

  useEffect(() => {
    const required = entry.mav?.requiredVolume ?? entry.detail?.quantity
    const remaining = Math.max(0, (required ?? 0) - (entry.mav?.totalUtilizedVolume ?? 0))
    if (remaining > 0) {
      setUtilizeVolume(String(remaining))
    }
  }, [entry.mav?.requiredVolume, entry.mav?.totalUtilizedVolume, entry.detail?.quantity])

  const availableMics = micsData?.data ?? []
  const selectedMic = availableMics.find((m) => m.uuid === selectedMicUuid)
  const mavCheck = checkData?.data
  const mavNoChanged = mavNo.trim().toUpperCase() !== (entry.mav?.mavNo ?? '').toUpperCase()
  const requiredVolume = entry.mav?.requiredVolume ?? entry.detail?.quantity ?? 0
  const utilizedVolume = entry.mav?.totalUtilizedVolume ?? 0
  const volumeComplete = requiredVolume > 0 && utilizedVolume >= requiredVolume

  const mavNoValid = useMemo(() => {
    if (!mavNo.trim()) return false
    if (!mavNoChanged && entry.mav?.mavNo) return true
    return mavCheck?.isAvailable === true
  }, [mavNo, mavNoChanged, entry.mav?.mavNo, mavCheck?.isAvailable])

  const handleSaveMavNo = async () => {
    if (!mavNoValid) return
    await updateMav({ uuid: entry.uuid, mavNo: mavNo.trim() }).unwrap()
    onUpdated?.()
  }

  const handleUploadCertificate = async (file: File) => {
    await uploadFile({ uuid: entry.uuid, file, documentType: MAV_CERT_DOCUMENT_TYPE }).unwrap()
    setUploadSuccessOpen(true)
    onUpdated?.()
  }

  const handleUtilizeMic = async () => {
    if (!selectedMicUuid || !utilizeVolume) return
    await utilizeMic({
      uuid: entry.uuid,
      micUuid: selectedMicUuid,
      volume: Number(utilizeVolume),
    }).unwrap()
    setConfirmUtilizeOpen(false)
    onUpdated?.()
  }

  if (entry.entryType !== 'Import') {
    return null
  }

  return (
    <>
      <PortalPanel title="MAV & MIC">
        <Stack spacing={2.5} sx={{ p: 2.5 }}>
          <Stack direction={{ xs: 'column', md: 'row' }} spacing={1} sx={{ alignItems: { md: 'center' } }}>
            <Chip size="small" label={mavStatusLabel(entry.mav?.mavDocumentStatus ?? 'NotProvided')} sx={portalStatusChipSx(entry.mav?.mavDocumentStatus ?? 'Draft')} />
            {volumeComplete && <Chip size="small" color="success" label="MIC volume linked" icon={<CheckCircleOutlinedIcon />} />}
            {!volumeComplete && requiredVolume > 0 && (
              <Chip size="small" color="warning" variant="outlined" label={`${utilizedVolume}/${requiredVolume} volume utilized`} />
            )}
            <Button
              component={RouterLink}
              to="/mav/licenses"
              size="small"
              startIcon={<LinkOutlinedIcon />}
              sx={{ ml: { md: 'auto' } }}
            >
              Manage MAV Licenses
            </Button>
          </Stack>

          {entry.mav?.mavRemarks && entry.mav.mavDocumentStatus === 'RevisionRequired' && (
            <Alert severity="warning" sx={{ borderRadius: '0.75rem' }}>
              Agency remarks: {entry.mav.mavRemarks}
            </Alert>
          )}

          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1, color: portalColors.textDark }}>
              MAV No.
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <TextField
                fullWidth
                size="small"
                value={mavNo}
                disabled={!editable}
                onChange={(e) => setMavNo(e.target.value.toUpperCase())}
                placeholder="Enter MAV number"
                error={Boolean(debouncedMavNo) && mavCheck?.isAvailable === false}
                helperText={
                  checkingMavNo
                    ? 'Checking availability…'
                    : mavCheck?.message ?? 'Unique MAV number required before submit.'
                }
              />
              {editable && (
                <Button
                  variant="contained"
                  sx={{ ...portalPrimaryButtonSx, minWidth: 120 }}
                  disabled={!mavNoValid || !mavNoChanged || savingMavNo}
                  onClick={handleSaveMavNo}
                >
                  Save
                </Button>
              )}
            </Stack>
          </Box>

          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1, color: portalColors.textDark }}>
              MAV Certificate (PDF)
            </Typography>
            {entry.mav?.mavCertificateFileName ? (
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <CheckCircleOutlinedIcon color="success" fontSize="small" />
                <Typography variant="body2">{entry.mav.mavCertificateFileName}</Typography>
              </Stack>
            ) : (
              <Typography variant="body2" sx={{ color: portalColors.textMuted, mb: 1 }}>
                Upload the signed MAV certificate document.
              </Typography>
            )}
            {editable && (
              <Button component="label" variant="outlined" sx={portalOutlinedButtonSx} disabled={uploadingCert} startIcon={uploadingCert ? <CircularProgress size={16} /> : <UploadFileOutlinedIcon />}>
                {entry.mav?.mavCertificateFileName ? 'Replace Certificate' : 'Upload Certificate'}
                <input
                  hidden
                  type="file"
                  accept=".pdf,application/pdf"
                  onChange={(e) => {
                    const file = e.target.files?.[0]
                    if (file) void handleUploadCertificate(file)
                    e.target.value = ''
                  }}
                />
              </Button>
            )}
          </Box>

          <Box>
            <Typography variant="subtitle2" sx={{ mb: 1, color: portalColors.textDark }}>
              Link MIC Volume
            </Typography>
            <Typography variant="body2" sx={{ color: portalColors.textMuted, mb: 1.5 }}>
              Select an active MIC from your MAV license and allocate volume matching this entry quantity.
            </Typography>

            {(entry.mav?.micUtilizations?.length ?? 0) > 0 && (
              <Stack spacing={0.75} sx={{ mb: 1.5 }}>
                {entry.mav?.micUtilizations.map((u) => (
                  <Box key={`${u.micUuid}-${u.utilizedAt}`} sx={{ px: 1.5, py: 1, bgcolor: portalColors.bgMuted, borderRadius: 1.5 }}>
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {u.certificateNumber} · {u.volume} {entry.detail?.unit ?? 'kg'}
                    </Typography>
                    <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
                      {u.commodityName} ({u.hsCode})
                    </Typography>
                  </Box>
                ))}
              </Stack>
            )}

            {editable && (
              <>
                {loadingMics ? (
                  <CircularProgress size={20} />
                ) : availableMics.length === 0 ? (
                  <Alert severity="info" icon={<ErrorOutlineOutlinedIcon />} sx={{ borderRadius: '0.75rem' }}>
                    No active MICs available. Issue a MIC from your{' '}
                    <RouterLink to="/mav/licenses">MAV license</RouterLink> first.
                  </Alert>
                ) : (
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                    <FormControl fullWidth size="small">
                      <InputLabel>Active MIC</InputLabel>
                      <Select
                        label="Active MIC"
                        value={selectedMicUuid}
                        onChange={(e) => setSelectedMicUuid(e.target.value)}
                      >
                        {availableMics.map((mic) => (
                          <MenuItem key={mic.uuid} value={mic.uuid}>
                            {mic.certificateNumber} · {mic.availableVolume} available · {mic.commodityName}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    <TextField
                      size="small"
                      label="Volume"
                      type="number"
                      value={utilizeVolume}
                      onChange={(e) => setUtilizeVolume(e.target.value)}
                      sx={{ minWidth: 120 }}
                      slotProps={{ htmlInput: { min: 0, step: '0.001', max: selectedMic?.availableVolume } }}
                    />
                    <Button
                      variant="contained"
                      sx={{ ...portalPrimaryButtonSx, minWidth: 120 }}
                      disabled={!selectedMicUuid || !utilizeVolume || utilizing || volumeComplete}
                      onClick={() => setConfirmUtilizeOpen(true)}
                    >
                      Link MIC
                    </Button>
                  </Stack>
                )}
                {utilizeError && (
                  <Alert severity="error" sx={{ mt: 1, borderRadius: '0.75rem' }}>
                    Unable to link MIC. Check available volume and try again.
                  </Alert>
                )}
              </>
            )}
          </Box>
        </Stack>
      </PortalPanel>

      <Dialog open={confirmUtilizeOpen} onClose={() => setConfirmUtilizeOpen(false)}>
        <DialogTitle>Link MIC to entry?</DialogTitle>
        <DialogContent>
          <Typography>
            Utilize <strong>{utilizeVolume}</strong> from MIC{' '}
            <strong>{selectedMic?.certificateNumber}</strong> for entry {entry.referenceNo}?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setConfirmUtilizeOpen(false)}>Cancel</Button>
          <Button variant="contained" sx={portalPrimaryButtonSx} disabled={utilizing} onClick={() => void handleUtilizeMic()}>
            Confirm
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={uploadSuccessOpen} onClose={() => setUploadSuccessOpen(false)}>
        <DialogTitle>MAV certificate uploaded</DialogTitle>
        <DialogContent>
          <Typography>Your MAV certificate was saved and is ready for agency review after submit.</Typography>
        </DialogContent>
        <DialogActions>
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setUploadSuccessOpen(false)}>
            OK
          </Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
