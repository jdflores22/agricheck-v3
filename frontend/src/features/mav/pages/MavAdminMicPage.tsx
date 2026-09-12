import { Box, TableCell, TableRow } from '@mui/material'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { useGetMavAdminMicListQuery } from '../api/mavApi'

export function MavAdminMicPage() {
  const { data, isLoading } = useGetMavAdminMicListQuery()
  const mics = data?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Administration"
        title="Import Certificates (MIC)"
        subtitle="All issued MICs across MAV licenses."
      />

      <PortalTablePanel
        title="All MICs"
        columns={['Certificate No.', 'HS Code', 'Commodity', 'Authorized', 'Utilized', 'Available', 'Status', 'Expires']}
        isLoading={isLoading}
        isEmpty={!isLoading && mics.length === 0}
        emptyMessage="No MICs issued yet."
      >
        {mics.map((mic) => (
          <TableRow key={mic.uuid} hover>
            <TableCell>{mic.certificateNumber}</TableCell>
            <TableCell>{mic.hsCode}</TableCell>
            <TableCell>{mic.commodityName}</TableCell>
            <TableCell>{mic.authorizedVolume}</TableCell>
            <TableCell>{mic.utilizedVolume}</TableCell>
            <TableCell>{mic.availableVolume}</TableCell>
            <TableCell>{mic.status}</TableCell>
            <TableCell>{new Date(mic.expiresAt).toLocaleDateString()}</TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
