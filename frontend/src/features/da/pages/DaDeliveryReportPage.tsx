import ArrowBackOutlinedIcon from '@mui/icons-material/ArrowBackOutlined'
import { Box, Button, Chip, Stack } from '@mui/material'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { useGetDaContainerActualRouteQuery } from '../api/daApi'
import { DaDeliveryReportPanel } from '../components/DaDeliveryReportPanel'

export function DaDeliveryReportPage() {
  const { containerUuid = '' } = useParams()
  const { data, isLoading, isFetching, refetch } = useGetDaContainerActualRouteQuery(containerUuid, {
    skip: !containerUuid,
    pollingInterval: 30_000,
  })
  const route = data?.data

  useBreadcrumbLabel(route?.containerNumber ?? 'Delivery report')

  return (
    <Box>
      <PortalPageHeader
        eyebrow="DA Secretary · Logistics"
        title={route?.containerNumber ? `Delivery report · ${route.containerNumber}` : 'Delivery report'}
        subtitle="Full GPS path, coordinate log, and geotagged pickup/warehouse photos from AgriTrack."
        actions={
          <Stack direction="row" spacing={1}>
            <Button
              size="small"
              variant="outlined"
              component={RouterLink}
              to="/da/logistics/live"
              startIcon={<ArrowBackOutlinedIcon />}
            >
              Live map
            </Button>
            <Button size="small" variant="outlined" onClick={() => refetch()} disabled={isFetching}>
              Refresh
            </Button>
          </Stack>
        }
      />
      {route && (
        <Stack direction="row" spacing={1} sx={{ mb: 2, flexWrap: 'wrap' }}>
          <Chip size="small" label={route.status} />
          <Chip size="small" variant="outlined" label={route.entryReference} />
          {(route.proofPhotos?.length ?? 0) === 0 && (
            <Chip size="small" color="warning" label="No geotag photos in database" />
          )}
        </Stack>
      )}
      <DaDeliveryReportPanel route={route} loading={isLoading} />
    </Box>
  )
}
