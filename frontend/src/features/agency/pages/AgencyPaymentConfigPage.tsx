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
  useGetAgencyPaymentSettingsQuery,
  useUpdateAgencyPaymentSettingsMutation,
} from '../api/agencyApi'

export function AgencyPaymentConfigPage() {
  const { data, isLoading } = useGetAgencyPaymentSettingsQuery()
  const [updateSettings, { isLoading: saving, isSuccess, isError }] = useUpdateAgencyPaymentSettingsMutation()
  const settings = data?.data

  const [form, setForm] = useState({
    payMongoEnabled: false,
    cashPaymentEnabled: true,
    payMongoApiKey: '',
    payMongoWebhookSecret: '',
    payMongoPublicKey: '',
    cashPaymentInstructions: '',
  })

  useEffect(() => {
    if (!settings) return
    setForm({
      payMongoEnabled: settings.gateway.enabled,
      cashPaymentEnabled: settings.cashPaymentEnabled,
      payMongoApiKey: '',
      payMongoWebhookSecret: '',
      payMongoPublicKey: settings.gateway.publicKey ?? '',
      cashPaymentInstructions: settings.cashPaymentInstructions ?? '',
    })
  }, [settings])

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    await updateSettings({
      payMongoEnabled: form.payMongoEnabled,
      cashPaymentEnabled: form.cashPaymentEnabled,
      payMongoApiKey: form.payMongoApiKey || undefined,
      payMongoWebhookSecret: form.payMongoWebhookSecret || undefined,
      payMongoPublicKey: form.payMongoPublicKey || undefined,
      cashPaymentInstructions: form.cashPaymentInstructions || undefined,
    }).unwrap()
    setForm((prev) => ({ ...prev, payMongoApiKey: '', payMongoWebhookSecret: '' }))
  }

  const gatewayMode = settings?.gateway.mode ?? 'cash_only'

  return (
    <Box>
      <PortalPageHeader
        eyebrow={settings?.agencyCode ?? 'Agency'}
        title="Payment Configuration"
        subtitle={`PayMongo and cash payment settings for ${settings?.agencyName ?? 'your agency'}. Entry processing fees are managed by the system administrator.`}
      />

      {isSuccess && (
        <Alert severity="success" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Payment settings saved.
        </Alert>
      )}
      {isError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Unable to save payment settings.
        </Alert>
      )}

      <Box component="form" onSubmit={handleSubmit}>
        <Stack spacing={3}>
          <PortalPanel title="PayMongo Gateway">
            <Stack spacing={2.5} sx={{ px: 2.5, py: 2.5 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                <Chip
                  size="small"
                  label={gatewayMode === 'paymongo' ? 'Live PayMongo' : 'Cash / office payment'}
                  color={gatewayMode === 'paymongo' ? 'success' : 'default'}
                />
                <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                  Each agency uses its own PayMongo credentials. There is no shared platform fallback.
                </Typography>
              </Stack>

              <FormControlLabel
                control={
                  <Switch
                    checked={form.payMongoEnabled}
                    onChange={(e) => setForm({ ...form, payMongoEnabled: e.target.checked })}
                  />
                }
                label="Enable PayMongo for this agency"
              />

              <TextField
                label="Secret API key"
                value={form.payMongoApiKey}
                onChange={(e) => setForm({ ...form, payMongoApiKey: e.target.value })}
                placeholder={settings?.gateway.hasApiKey ? settings.gateway.apiKeyMasked : 'sk_live_...'}
                fullWidth
                size="small"
                type="password"
                autoComplete="new-password"
                required={form.payMongoEnabled && !settings?.gateway.hasApiKey}
                helperText="Required when PayMongo is enabled. Leave blank to keep the current key."
              />
              <Alert severity="info" sx={{ borderRadius: '0.75rem' }}>
                Webhook URL: <strong>{getApiV1Base()}/webhooks/paymongo</strong> — event:{' '}
                <strong>checkout_session.payment.paid</strong>
              </Alert>
              <TextField
                label="Webhook secret"
                value={form.payMongoWebhookSecret}
                onChange={(e) => setForm({ ...form, payMongoWebhookSecret: e.target.value })}
                placeholder={settings?.gateway.hasWebhookSecret ? '********' : 'whsec_...'}
                fullWidth
                size="small"
                type="password"
                autoComplete="new-password"
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

          <PortalPanel title="Cash / Office Payment (Kahera)">
            <Stack spacing={2.5} sx={{ px: 2.5, py: 2.5 }}>
              <FormControlLabel
                control={
                  <Switch
                    checked={form.cashPaymentEnabled}
                    onChange={(e) => setForm({ ...form, cashPaymentEnabled: e.target.checked })}
                  />
                }
                label="Allow clients to pay at agency cashier"
              />
              <TextField
                label="Cash payment instructions"
                value={form.cashPaymentInstructions}
                onChange={(e) => setForm({ ...form, cashPaymentInstructions: e.target.value })}
                fullWidth
                size="small"
                multiline
                minRows={3}
                helperText="Shown to clients when they choose office cash payment."
              />
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
