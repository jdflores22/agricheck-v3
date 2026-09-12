import { Alert, Box, Button, TableCell, TableRow } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { useGetCertificatesQuery } from '../api/clientApi'

export function CertificatesListPage() {
  const { data, isLoading } = useGetCertificatesQuery()
  const certs = data?.data?.items ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Management"
        title="My Certificates"
        subtitle="View certificates issued for your approved accreditations and entries."
      />
      {certs.length === 0 && !isLoading && (
        <Alert severity="info" sx={{ mb: 2 }}>
          No certificates yet. Certificates are issued automatically when your accreditation or entry application is approved.
        </Alert>
      )}
      <PortalTablePanel
        title="All certificates"
        columns={['Certificate No.', 'Title', 'Status', 'Entry', 'Issued', '']}
        isLoading={isLoading}
        isEmpty={!isLoading && certs.length === 0}
        emptyMessage="No certificates yet."
      >
        {certs.map((c) => (
          <TableRow key={c.uuid} hover>
            <TableCell>{c.certificateNumber}</TableCell>
            <TableCell>{c.title}</TableCell>
            <TableCell>{c.status}</TableCell>
            <TableCell>{c.entryReferenceNo ?? '—'}</TableCell>
            <TableCell>{new Date(c.issuedAt).toLocaleDateString()}</TableCell>
            <TableCell>
              <Button size="small" component={RouterLink} to={`/client/certificates/${c.uuid}`}>View</Button>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>
    </Box>
  )
}
