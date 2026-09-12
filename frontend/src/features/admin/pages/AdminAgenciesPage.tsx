import AddCircleOutlinedIcon from '@mui/icons-material/AddCircleOutlined'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import InboxOutlinedIcon from '@mui/icons-material/InboxOutlined'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import {
  Box,
  Button,
  Checkbox,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  IconButton,
  Stack,
  TableCell,
  TableRow,
  Tooltip,
  Typography,
  Alert,
} from '@mui/material'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { getStatusBadgeStyle } from '../../../components/portal/portalUtils'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useDeleteAdminAgencyMutation, useGetAdminAgenciesQuery } from '../api/adminApi'

export function AdminAgenciesPage() {
  const { data, isLoading, isError } = useGetAdminAgenciesQuery()
  const [deleteAgency, { isLoading: deleting }] = useDeleteAdminAgencyMutation()
  const agencies = data?.data ?? []
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null)
  const [confirmDelete, setConfirmDelete] = useState(false)

  const handleDelete = async () => {
    if (!deleteTarget) return
    await deleteAgency(deleteTarget.id).unwrap()
    setDeleteTarget(null)
    setConfirmDelete(false)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Administration"
        title="Agency Management"
        subtitle="Manage government agencies and their configurations"
        actions={
          <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
            <Button
              component={RouterLink}
              to="/admin/agencies/create"
              variant="contained"
              startIcon={<AddCircleOutlinedIcon />}
              sx={portalPrimaryButtonSx}
            >
              Add New Agency
            </Button>
            <Button component={RouterLink} to="/admin" variant="outlined" startIcon={<ArrowBackIcon />} sx={portalOutlinedButtonSx}>
              Back
            </Button>
          </Stack>
        }
      />

      {isError ? (
        <Alert severity="error" sx={{ mb: 2 }}>
          Unable to load agencies. If you recently updated the app, restart the API so database migrations can apply, then refresh this page.
        </Alert>
      ) : null}

      <PortalTablePanel
        title="All Agencies"
        columns={['Code', 'Name', 'Parent Agency', 'Description', 'Actions']}
        isLoading={isLoading}
        isEmpty={!isLoading && agencies.length === 0}
        emptyMessage="No agencies found. Click Add New Agency to create one."
      >
        {agencies.map((agency) => (
          <TableRow key={agency.id} hover>
            <TableCell>
              <Chip size="small" label={agency.code} sx={getStatusBadgeStyle('Submitted')} />
            </TableCell>
            <TableCell>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
                {agency.parentId ? (
                  <Typography component="span" sx={{ color: portalColors.textMuted }}>
                    └─
                  </Typography>
                ) : null}
                <Typography sx={{ fontWeight: 600 }}>{agency.name}</Typography>
              </Box>
            </TableCell>
            <TableCell>
              {agency.parentName ? (
                <Chip size="small" label={agency.parentName} sx={getStatusBadgeStyle('Approved')} />
              ) : (
                <Chip size="small" label="Top Level" sx={getStatusBadgeStyle('Pending')} />
              )}
            </TableCell>
            <TableCell>{agency.description?.trim() || 'N/A'}</TableCell>
            <TableCell>
              <Stack direction="row" spacing={0.5}>
                <Tooltip title="View">
                  <IconButton
                    component={RouterLink}
                    to={`/admin/agencies/${agency.id}/view`}
                    size="small"
                    sx={{ border: '1px solid #93c5fd', color: '#1d4ed8', borderRadius: '0.375rem' }}
                  >
                    <VisibilityOutlinedIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Edit">
                  <IconButton
                    component={RouterLink}
                    to={`/admin/agencies/${agency.id}/edit`}
                    size="small"
                    sx={{ border: '1px solid #86efac', color: portalColors.primary, borderRadius: '0.375rem' }}
                  >
                    <EditOutlinedIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
                <Tooltip title="Delete">
                  <IconButton
                    size="small"
                    onClick={() => {
                      setDeleteTarget({ id: agency.id, name: agency.name })
                      setConfirmDelete(false)
                    }}
                    sx={{ border: '1px solid #fca5a5', color: '#b91c1c', borderRadius: '0.375rem' }}
                  >
                    <DeleteOutlinedIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              </Stack>
            </TableCell>
          </TableRow>
        ))}
      </PortalTablePanel>

      {!isLoading && agencies.length === 0 ? (
        <Box sx={{ textAlign: 'center', py: 6 }}>
          <InboxOutlinedIcon sx={{ fontSize: 48, color: portalColors.textMuted, mb: 1 }} />
          <Typography sx={{ fontWeight: 600, color: portalColors.textDark, mb: 0.5 }}>No agencies found</Typography>
          <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
            Click &quot;Add New Agency&quot; to create one.
          </Typography>
        </Box>
      ) : null}

      <Dialog open={Boolean(deleteTarget)} onClose={() => setDeleteTarget(null)} maxWidth="xs" fullWidth>
        <DialogTitle sx={{ bgcolor: '#dc2626', color: '#fff' }}>Delete Agency</DialogTitle>
        <DialogContent sx={{ pt: '20px !important' }}>
          <Box sx={{ border: '1px solid #fecaca', bgcolor: '#fef2f2', borderRadius: '0.5rem', p: 2, mb: 2 }}>
            <Typography sx={{ fontWeight: 600, color: '#991b1b', mb: 0.5 }}>Warning!</Typography>
            <Typography sx={{ fontSize: '0.875rem', color: '#991b1b' }}>
              You are about to delete <strong>{deleteTarget?.name}</strong>
            </Typography>
          </Box>
          <Typography sx={{ color: '#dc2626', fontWeight: 600, mb: 2 }}>This action cannot be undone!</Typography>
          <FormControlLabel
            control={<Checkbox checked={confirmDelete} onChange={(_, checked) => setConfirmDelete(checked)} />}
            label="I understand this action is permanent"
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setDeleteTarget(null)} sx={portalOutlinedButtonSx}>
            Cancel
          </Button>
          <Button
            variant="contained"
            color="error"
            disabled={!confirmDelete || deleting}
            onClick={() => void handleDelete()}
          >
            Delete Agency
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
