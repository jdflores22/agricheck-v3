import { useMemo } from 'react'
import { Alert, Box, Button, Chip, Typography } from '@mui/material'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'
import { useGetClientFormQuery, useGetClientFormsQuery, useGetContainerQuery, useGetEntryQuery } from '../api/clientApi'
import { ContainerProcessPanel } from '../components/ContainerProcessPanel'
import { parseContainerSchema } from '../utils/containerFormUtils'

export function ContainerDetailPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading } = useGetContainerQuery(uuid, { skip: !uuid })
  const container = data?.data
  const { data: entryData } = useGetEntryQuery(container?.entryUuid ?? '', { skip: !container?.entryUuid })

  const { data: containerFormsData } = useGetClientFormsQuery(
    { agencyId: entryData?.data?.agencyId, formType: 'CONTAINER' },
    { skip: !entryData?.data?.agencyId },
  )
  const selectedContainerFormUuid = containerFormsData?.data?.[0]?.uuid
  const { data: containerFormSchemaData } = useGetClientFormQuery(selectedContainerFormUuid ?? '', {
    skip: !selectedContainerFormUuid,
  })

  const containerSchemaFields = useMemo(
    () => parseContainerSchema(containerFormSchemaData?.data?.schemaJson),
    [containerFormSchemaData?.data?.schemaJson],
  )

  useBreadcrumbLabel(container?.containerNumber)

  if (isLoading) return <Typography sx={{ color: portalColors.textMuted }}>Loading container…</Typography>
  if (!container) return <Alert severity="error">Container not found.</Alert>

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Logistics"
        title={container.containerNumber}
        subtitle={`Entry ${container.entryReferenceNo} · ${container.agencyCode}`}
        actions={
          <Button component={RouterLink} to="/client/containers" variant="outlined" sx={portalOutlinedButtonSx}>
            Back
          </Button>
        }
      />
      <Chip size="small" label={container.status} sx={{ mb: 2, ...portalStatusChipSx(container.status) }} />
      <ContainerProcessPanel container={container} schemaFields={containerSchemaFields} />
    </Box>
  )
}
