import {
  Alert,
  Box,
  Button,
  Chip,
  FormControlLabel,
  Stack,
  Switch,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useEffect, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { getApiV1Base } from '../../../app/apiBase'
import {
  useGetAdminPaymentSettingsQuery,
  useUpdateAdminPaymentSettingsMutation,
} from '../api/adminApi'

export function AdminPaymentConfigPage() {
  const { data, isLoading } = useGetAdminPaymentSettingsQuery()
  const [updateSettings, { isLoading: saving, isSuccess, isError }] = useUpdateAdminPaymentSettingsMutation()
  const settings = data?.data

  const [form, setForm] = useState({
    payMongoEnabled: false,
    payMongoApiKey: '',
    payMongoWebhookSecret: '',
    payMongoPublicKey: '',
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
      payMongoEnabled: settings.gateway.enabled,
      payMongoApiKey: '',
      payMongoWebhookSecret: '',
      payMongoPublicKey: settings.gateway.publicKey ?? '',
      importFeeAmount: importFee,
      exportFeeAmount: exportFee,
      currency,
    })
  }, [settings])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    await updateSettings({
      payMongoEnabled: form.payMongoEnabled,
      payMongoApiKey: form.payMongoApiKey || undefined,
      payMongoWebhookSecret: form.payMongoWebhookSecret || undefined,
      payMongoPublicKey: form.payMongoPublicKey || undefined,
      importFeeAmount: form.importFeeAmount,
      exportFeeAmount: form.exportFeeAmount,
      currency: form.currency,
    }).unwrap()
    setForm((prev) => ({ ...prev, payMongoApiKey: '', payMongoWebhookSecret: '' }))
  }

  const gatewayMode = settings?.gateway.mode ?? 'simulated'

  return (
    <Box>
      <PortalPageHeader
        eyebrow="System Provider"
        title="Payment Configuration"
        subtitle="Configure how AgriCheck collects system entry processing fees. Agency billing (inspection charges, certificates, etc.) is configured separately by each agency."
      />

      {isSuccess && (
        <Alert severity="success" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          System payment settings saved.
        </Alert>
      )}
      {isError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Unable to save system payment settings.
        </Alert>
      )}

      <Box component="form" onSubmit={handleSubmit}>
        <Stack spacing={3}>
          <PortalPanel title="System Provider PayMongo">
            <Stack spacing={2.5} sx={{ px: 2.5, py: 2.5 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Chip
                  size="small"
                  label={gatewayMode === 'paymongo' ? 'Live PayMongo' : 'Simulated mode'}
                  color={gatewayMode === 'paymongo' ? 'success' : 'default'}
                />
                <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                  Used only for entry processing fee payments collected by the system provider.
                </Typography>
              </Stack>

              <FormControlLabel
                control={
                  <Switch
                    checked={form.payMongoEnabled}
                    onChange={(e) => setForm({ ...form, payMongoEnabled: e.target.checked })}
                  />
                }
                label="Enable PayMongo for entry processing fees"
              />

              <TextField
                label="Secret API key"
                value={form.payMongoApiKey}
                onChange={(e) => setForm({ ...form, payMongoApiKey: e.target.value })}
                placeholder={settings?.gateway.hasApiKey ? settings.gateway.apiKeyMasked : 'sk_live_...'}
                fullWidth
                size="small"
                type="password"
                helperText="Leave blank to keep the current key."
              />
              <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
                In the PayMongo dashboard, add a webhook to{' '}
                <strong>{getApiV1Base()}/webhooks/paymongo</strong> and subscribe to{' '}
                <strong>checkout_session.payment.paid</strong>.
              </Alert>
              <TextField
                label="Webhook secret"
                value={form.payMongoWebhookSecret}
                onChange={(e) => setForm({ ...form, payMongoWebhookSecret: e.target.value })}
                placeholder={settings?.gateway.hasWebhookSecret ? '********' : 'whsec_...'}
                fullWidth
                size="small"
                type="password"
              />
              <TextField
                label="Public key (optional)"
                value={form.payMongoPublicKey}
                onChange={(e) => setForm({ ...form, payMongoPublicKey: e.target.value })}
                placeholder="pk_live_..."
                fullWidth
                size="small"
              />
            </Stack>
          </PortalPanel>

          <PortalPanel title="System Entry Processing Fees">
            <Stack spacing={2.5} sx={{ px: 2.5, py: 2.5 }}>
              <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                Platform fee charged to clients when they submit an import or export entry. This revenue
                belongs to the system provider, not the agency.
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
              {saving ? 'Saving…' : 'Save payment settings'}
            </Button>
          </Box>
        </Stack>
      </Box>
    </Box>
  )
}
