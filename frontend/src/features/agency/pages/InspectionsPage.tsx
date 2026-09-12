import {
  Box,
  Button,
  Chip,
  Stack,
  TableCell,
  TableRow,
  Typography,
} from '@mui/material'
import {
  useCompleteInspectionMutation,
  useCreateInspectionMutation,
  useGetApprovedEntriesQuery,
  useGetInspectionsQuery,
} from '../api/agencyApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel, portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'

export function InspectionsPage() {
  const { data, isLoading } = useGetInspectionsQuery({})
  const { data: approvedData } = useGetApprovedEntriesQuery({})
  const [createInspection] = useCreateInspectionMutation()
  const [completeInspection] = useCompleteInspectionMutation()
  const items = data?.data?.items ?? []
  const approvedEntries = approvedData?.data?.items ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Operations"
        title="Inspections"
        subtitle="Schedule and complete entry inspections."
      />

      <PortalPanel title="Approved entries ready for inspection">
        <Box sx={{ px: 2.5, py: 2 }}>
          {approvedEntries.length === 0 ? (
            <Typography variant="body2" sx={{ color: portalColors.textMuted }}>No approved entries pending inspection.</Typography>
          ) : (
            <Stack spacing={1}>
              {approvedEntries.map((entry) => (
                <Box key={entry.uuid} sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
                  <Typography variant="body2" sx={{ minWidth: 120 }}>{entry.referenceNo}</Typography>
                  <Typography variant="body2">{entry.applicantName}</Typography>
                  <Typography variant="body2" sx={{ color: portalColors.textMuted }}>{entry.commodityName ?? '—'}</Typography>
                  <Button size="small" variant="outlined" sx={portalOutlinedButtonSx} onClick={() => createInspection({ entryUuid: entry.uuid })}>
                    Schedule
                  </Button>
                </Box>
              ))}
            </Stack>
          )}
        </Box>
      </PortalPanel>

      <Box sx={{ mt: 3 }}>
        <PortalTablePanel
          title="All inspections"
          columns={['Entry', 'Applicant', 'Status', 'Scheduled', 'Actions']}
          isLoading={isLoading}
          isEmpty={!isLoading && items.length === 0}
          emptyMessage="No inspections yet."
        >
          {items.map((item) => (
            <TableRow key={item.uuid} hover>
              <TableCell>{item.entryReferenceNo}</TableCell>
              <TableCell>{item.applicantName}</TableCell>
              <TableCell>
                <Chip size="small" label={item.status} sx={portalStatusChipSx(item.status)} />
              </TableCell>
              <TableCell>{item.scheduledAt ? new Date(item.scheduledAt).toLocaleDateString() : '—'}</TableCell>
              <TableCell align="right">
                {item.status !== 'Completed' && item.status !== 'Failed' && (
                  <>
                    <Button size="small" onClick={() => completeInspection({ uuid: item.uuid, result: 'pass' })}>Pass</Button>
                    <Button size="small" color="error" onClick={() => completeInspection({ uuid: item.uuid, result: 'fail' })}>Fail</Button>
                  </>
                )}
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      </Box>
    </Box>
  )
}
