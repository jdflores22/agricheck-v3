import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { useEffect, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx, portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import type { ApiEnvelope } from '../../auth/types'
import {
  useGetFacilityRegistrationsQuery,
  useLinkImporterWarehouseMutation,
  useSearchLicensedWarehousesForFacilityQuery,
} from '../api/clientApi'

function getApiErrorMessage(error: unknown): string | null {
  if (!error || typeof error !== 'object' || !('data' in error)) return null
  const data = (error as FetchBaseQueryError).data as ApiEnvelope<unknown> | undefined
  return data?.errors?.[0]?.message ?? null
}

function rsasChip(status: string) {
  if (status === 'Registered') return <Chip size="small" label="RSAS registered" color="success" />
  if (status === 'Submitted') return <Chip size="small" label="RSAS pending DA" color="warning" />
  if (status === 'Draft') return <Chip size="small" label="RSAS in progress (operator)" />
  return <Chip size="small" label="RSAS not started (operator)" variant="outlined" />
}

function cswChip(status?: string) {
  if (status === 'Active') return <Chip size="small" label="CSW active" color="success" />
  if (status) return <Chip size="small" label={`CSW ${status}`} />
  return <Chip size="small" label="CSW pending (DA)" variant="outlined" />
}

function destinationReady(facility: { registrationStatus: string; cswStatus?: string }) {
  return facility.registrationStatus === 'Registered' && facility.cswStatus === 'Active'
}

export function MyFacilitiesPage() {
  const { data, isLoading, isError: listError, refetch: refetchFacilities } = useGetFacilityRegistrationsQuery()
  const facilities = data?.data ?? []

  const [linkDialogOpen, setLinkDialogOpen] = useState(false)
  const [warehouseSearch, setWarehouseSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(warehouseSearch.trim()), 300)
    return () => window.clearTimeout(handle)
  }, [warehouseSearch])

  const { data: searchData, isFetching: searching } = useSearchLicensedWarehousesForFacilityQuery(
    debouncedSearch || undefined,
    { skip: !linkDialogOpen },
  )
  const searchResults = searchData?.data ?? []

  const [linkWarehouse, { isLoading: linking, error: linkError }] = useLinkImporterWarehouseMutation()

  return (
    <>
      <PortalPageHeader
        eyebrow="Compliance"
        title="My Facilities"
        subtitle="Link DA-licensed warehouses you use for import destinations. RSAS and CSW are completed by the warehouse operator and DA—not here."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setLinkDialogOpen(true)}>
            Add licensed warehouse
          </Button>
        }
      />

      <Alert severity="info" sx={{ mb: 3, borderRadius: '0.75rem' }}>
        Only warehouses with an active <strong>DA warehouse operating license (LTO)</strong> can be linked. The{' '}
        <strong>warehouse operator</strong> manages RSAS in the Warehouse Portal; DA records CSW after verification. Use a
        facility below as an entry destination only when RSAS and CSW show ready.
      </Alert>

      {listError ? (
        <Alert severity="error" sx={{ mb: 2 }} action={<Button size="small" onClick={() => refetchFacilities()}>Retry</Button>}>
          Could not load your facilities.
        </Alert>
      ) : null}

      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress size={32} sx={{ color: portalColors.primary }} />
        </Box>
      ) : facilities.length === 0 ? (
        <PortalPanel title="Your linked warehouses">
          <Box sx={{ px: 2.5, py: 3 }}>
            <Typography sx={{ color: portalColors.textMuted, mb: 2 }}>
              No warehouses linked yet. Search for a DA-licensed warehouse your operator has registered.
            </Typography>
            <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setLinkDialogOpen(true)}>
              Add licensed warehouse
            </Button>
          </Box>
        </PortalPanel>
      ) : (
        <PortalPanel title="Your linked warehouses">
          <Box sx={{ px: 2.5, py: 2 }}>
            <Stack spacing={1.5}>
              {facilities.map((facility) => (
                <Box
                  key={facility.warehouseFacilityId}
                  sx={{
                    display: 'flex',
                    flexWrap: 'wrap',
                    alignItems: 'center',
                    gap: 2,
                    p: 2,
                    borderRadius: '0.5rem',
                    border: `1px solid ${portalColors.border}`,
                  }}
                >
                  <WarehouseOutlinedIcon sx={{ color: portalColors.primary }} />
                  <Box sx={{ flex: 1, minWidth: 220 }}>
                    <Typography sx={{ fontWeight: 700 }}>
                      {facility.warehouseCode} — {facility.warehouseName}
                    </Typography>
                    {facility.warehouseLocation && (
                      <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>
                        {facility.warehouseLocation}
                      </Typography>
                    )}
                  </Box>
                  <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap' }}>
                    {rsasChip(facility.registrationStatus)}
                    {cswChip(facility.cswStatus)}
                    {destinationReady(facility) ? (
                      <Chip size="small" label="Destination ready" color="success" variant="outlined" />
                    ) : (
                      <Chip size="small" label="Not ready for entries" color="warning" variant="outlined" />
                    )}
                    {facility.renewalDue && facility.renewalWindowOpen ? (
                      <Chip size="small" label="Operator RSAS renewal due" color="warning" />
                    ) : null}
                  </Stack>
                </Box>
              ))}
            </Stack>
          </Box>
        </PortalPanel>
      )}

      {linkDialogOpen && (
        <Box sx={{ mt: 3 }}>
        <PortalPanel title="Add DA-licensed warehouse">
          <Box sx={{ px: 2.5, py: 2 }}>
            {getApiErrorMessage(linkError) && <Alert severity="error" sx={{ mb: 2 }}>{getApiErrorMessage(linkError)}</Alert>}
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Search by code or name. Warehouses without an active DA LTO are not listed.
            </Typography>
            <TextField
              label="Search warehouses"
              size="small"
              fullWidth
              value={warehouseSearch}
              onChange={(e) => setWarehouseSearch(e.target.value)}
              sx={{ mb: 2 }}
            />
            {searching ? (
              <CircularProgress size={24} sx={{ color: portalColors.primary }} />
            ) : (
              <Stack spacing={1}>
                {searchResults.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">
                    {debouncedSearch ? 'No matching licensed warehouses.' : 'Type to search licensed warehouses.'}
                  </Typography>
                ) : (
                  searchResults.map((w) => (
                    <Box
                      key={w.warehouseFacilityId}
                      sx={{
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                        gap: 2,
                        p: 1.5,
                        borderRadius: '0.5rem',
                        border: `1px solid ${portalColors.border}`,
                      }}
                    >
                      <Box>
                        <Typography sx={{ fontWeight: 600 }}>{w.warehouseCode} — {w.warehouseName}</Typography>
                        {w.warehouseLocation && (
                          <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted }}>{w.warehouseLocation}</Typography>
                        )}
                      </Box>
                      <Button
                        size="small"
                        variant="contained"
                        sx={portalPrimaryButtonSx}
                        disabled={linking}
                        onClick={async () => {
                          try {
                            await linkWarehouse(w.warehouseFacilityId).unwrap()
                            setLinkDialogOpen(false)
                            setWarehouseSearch('')
                          } catch {
                            /* linkError Alert */
                          }
                        }}
                      >
                        Link
                      </Button>
                    </Box>
                  ))
                )}
              </Stack>
            )}
            <Button onClick={() => setLinkDialogOpen(false)} sx={{ ...portalOutlinedButtonSx, mt: 2 }}>
              Close
            </Button>
          </Box>
        </PortalPanel>
        </Box>
      )}
    </>
  )
}
