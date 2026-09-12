import { Alert, Box, Button, Stack, TextField } from '@mui/material'
import { FormEvent, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useRunMavYearTransitionMutation } from '../api/mavApi'

export function MavYearTransitionPage() {
  const [runTransition, { isLoading, data, error }] = useRunMavYearTransitionMutation()
  const [form, setForm] = useState({ fromYear: 2026, toYear: 2027, poolType: 'BYP' })

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    await runTransition(form).unwrap()
  }

  const result = data?.data

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Administration"
        title="Year Transition"
        subtitle="Copy commodity allocation pools from one MAV year to the next as upcoming periods."
      />

      <PortalPanel title="Run transition">
        <Box component="form" onSubmit={handleSubmit} sx={{ maxWidth: 420, p: 2.5 }}>
          <Stack spacing={2}>
            {error && <Alert severity="error">Transition failed.</Alert>}
            {result && <Alert severity="success">Created {result.periodsCreated} period(s), copied {result.allocationsCopied} allocation(s).</Alert>}
            <TextField label="From Year" type="number" value={form.fromYear} onChange={(e) => setForm({ ...form, fromYear: Number(e.target.value) })} fullWidth />
            <TextField label="To Year" type="number" value={form.toYear} onChange={(e) => setForm({ ...form, toYear: Number(e.target.value) })} fullWidth />
            <TextField label="Pool Type" value={form.poolType} onChange={(e) => setForm({ ...form, poolType: e.target.value })} fullWidth />
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={isLoading}>Run Transition</Button>
          </Stack>
        </Box>
      </PortalPanel>
    </Box>
  )
}
