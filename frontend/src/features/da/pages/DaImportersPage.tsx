import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import {
  Box,
  Chip,
  CircularProgress,
  LinearProgress,
  TableCell,
  TableRow,
  TextField,
} from '@mui/material'
import { useNavigate } from 'react-router-dom'
import { useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { useGetDaImportersQuery } from '../api/daApi'

function importerLabel(fullName: string, companyName?: string | null) {
  return companyName?.trim() ? companyName.trim() : fullName
}

export function DaImportersPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const { data, isLoading, isFetching } = useGetDaImportersQuery({ page, pageSize: 25, search })

  const result = data?.data
  const rows = useMemo(() => result?.items ?? [], [result])

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Import oversight"
        title="Registered importers"
        subtitle="Browse accredited and active importers — open a profile for accreditation history, entries, certificates, and pipeline volume."
      />

      {isFetching && <LinearProgress sx={{ mb: 2 }} />}

      <TextField
        label="Search company, name, or email"
        value={search}
        onChange={(e) => {
          setSearch(e.target.value)
          setPage(1)
        }}
        size="small"
        sx={{ mb: 2, maxWidth: 420 }}
      />

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress size={32} sx={{ color: portalColors.primary }} />
        </Box>
      ) : (
        <PortalTablePanel
          title="Importers"
          columns={['Importer', 'Contact', 'Accreditation', 'Entries', 'Last login']}
          isEmpty={rows.length === 0}
          emptyMessage="No importers match your search."
        >
          {rows.map((row) => (
            <TableRow
              key={row.uuid}
              hover
              onClick={() => navigate(`/da/importers/${row.uuid}`)}
              sx={{ cursor: 'pointer' }}
            >
              <TableCell sx={{ fontWeight: 600 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <BusinessOutlinedIcon sx={{ fontSize: 18, color: portalColors.textMuted }} />
                  {importerLabel(row.fullName, row.companyName)}
                </Box>
              </TableCell>
              <TableCell>
                {row.companyName?.trim() ? row.fullName : row.email}
                {row.companyName?.trim() ? (
                  <Box component="span" sx={{ display: 'block', fontSize: '0.7rem', color: portalColors.textMuted }}>
                    {row.email}
                  </Box>
                ) : null}
              </TableCell>
              <TableCell>
                {row.isAccredited ? (
                  <Chip
                    size="small"
                    icon={<CheckCircleOutlinedIcon />}
                    label={row.accreditationDisplayStatus ?? 'Accredited'}
                    color="success"
                    sx={{ fontWeight: 600 }}
                  />
                ) : (
                  <Chip
                    size="small"
                    label={row.accreditationDisplayStatus ?? row.accreditationStatus ?? 'No submission'}
                    variant="outlined"
                    sx={{ fontWeight: 600 }}
                  />
                )}
              </TableCell>
              <TableCell>
                {row.importEntries} import · {row.totalEntries} total
              </TableCell>
              <TableCell>
                {row.lastLoginAt ? new Date(row.lastLoginAt).toLocaleDateString() : '—'}
              </TableCell>
            </TableRow>
          ))}
        </PortalTablePanel>
      )}

      {result && result.totalCount > result.pageSize ? (
        <Box sx={{ mt: 2, display: 'flex', gap: 1, justifyContent: 'flex-end' }}>
          <Chip
            clickable
            label="Previous"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1}
          />
          <Chip label={`Page ${page} of ${Math.ceil(result.totalCount / result.pageSize)}`} />
          <Chip
            clickable
            label="Next"
            onClick={() => setPage((p) => p + 1)}
            disabled={page * result.pageSize >= result.totalCount}
          />
        </Box>
      ) : null}
    </Box>
  )
}
