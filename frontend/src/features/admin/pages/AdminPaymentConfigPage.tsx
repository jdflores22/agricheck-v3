import {
  Alert,
  Box,
  Button,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useEffect, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  useGetAdminPaymentSettingsQuery,
  useUpdateAdminPaymentSettingsMutation,
} from '../api/adminApi'

export function AdminPaymentConfigPage() {
  const { data, isLoading } = useGetAdminPaymentSettingsQuery()
  const [updateSettings, { isLoading: saving, isSuccess, isError }] = useUpdateAdminPaymentSettingsMutation()
  const settings = data?.data

  const [form, setForm] = useState({
    importFeeAmount: 2500,
    exportFeeAmount: 2500,
    currency: 'PHP',
  })

  useEffect(() => {
    if (!settings) return
    const importFee = settings.processingFees.find((fee) => fee.entryType === 'Import')?.amount ?? 2500
    const exportFee = settings.processingFees.find((fee) => fee.entryType === 'Export')?.amount ?? 2500
    const currency = settings.processingFees[0]?.currency ?? 'PHP'
    setForm({
      importFeeAmount: importFee,
      exportFeeAmount: exportFee,
      currency,
    })
  }, [settings])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    await updateSettings({
      importFeeAmount: form.importFeeAmount,
      exportFeeAmount: form.exportFeeAmount,
      currency: form.currency,
    }).unwrap()
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="Entry Processing Fees"
        subtitle="Set the platform fee charged when a client submits an import or export entry. PayMongo and agency billing are configured per agency."
      />

      {isSuccess && (
        <Alert severity="success" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Entry processing fees saved.
        </Alert>
      )}
      {isError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Unable to save entry processing fees.
        </Alert>
      )}

      <Box component="form" onSubmit={handleSubmit}>
        <Stack spacing={3}>
          <PortalPanel title="Global Entry Processing Fees">
            <Stack spacing={2.5} sx={{ px: 2.5, py: 2.5 }}>
              <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                Default fee applied when a client submits an entry. Agencies may override these amounts
                through per-agency fee configuration in the admin payment configs list.
              </Typography>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  label="Import entry fee"
                  type="number"
                  value={form.importFeeAmount}
                  onChange={(e) => setForm({ ...form, importFeeAmount: Number(e.target.value) })}
                  required
                  fullWidth
                  size="small"
                />
                <TextField
                  label="Export entry fee"
                  type="number"
                  value={form.exportFeeAmount}
                  onChange={(e) => setForm({ ...form, exportFeeAmount: Number(e.target.value) })}
                  required
                  fullWidth
                  size="small"
                />
                <TextField
                  label="Currency"
                  value={form.currency}
                  onChange={(e) => setForm({ ...form, currency: e.target.value.toUpperCase() })}
                  required
                  sx={{ maxWidth: 120 }}
                  size="small"
                />
              </Stack>
            </Stack>
          </PortalPanel>

          <Box>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={saving || isLoading}>
              {saving ? 'Saving…' : 'Save entry fees'}
            </Button>
          </Box>
        </Stack>
      </Box>
    </Box>
  )
}
