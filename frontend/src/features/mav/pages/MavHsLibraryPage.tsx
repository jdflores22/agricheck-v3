import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  MenuItem,
  Stack,
  TableCell,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useEffect, useMemo, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { PortalTablePanel } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import { useGetAgenciesQuery } from '../../client/api/clientApi'
import {
  useCreateMavHsCategoryMutation,
  useCreateMavHsDetailMutation,
  useCreateMavHsHeadingMutation,
  useGetMavHsCategoriesQuery,
  useGetMavHsCategoryQuery,
} from '../api/mavApi'

export function MavHsLibraryPage() {
  const [agencyFilter, setAgencyFilter] = useState('')
  const { data, isLoading } = useGetMavHsCategoriesQuery({})
  const allCategories = data?.data ?? []
  const categories = useMemo(
    () => (agencyFilter ? allCategories.filter((c) => c.agencyCode === agencyFilter) : allCategories),
    [agencyFilter, allCategories],
  )
  const [selectedUuid, setSelectedUuid] = useState<string | null>(null)
  const [selectedHeadingUuid, setSelectedHeadingUuid] = useState<string | null>(null)
  const { data: detailData } = useGetMavHsCategoryQuery(selectedUuid ?? '', { skip: !selectedUuid })
  const categoryDetail = detailData?.data

  const [createCategory] = useCreateMavHsCategoryMutation()
  const [createHeading] = useCreateMavHsHeadingMutation()
  const [createDetail] = useCreateMavHsDetailMutation()

  const [categoryOpen, setCategoryOpen] = useState(false)
  const [headingOpen, setHeadingOpen] = useState(false)
  const [detailOpen, setDetailOpen] = useState(false)
  const { data: agenciesData } = useGetAgenciesQuery()
  const agencies = agenciesData?.data ?? []
  const [categoryForm, setCategoryForm] = useState({ hsCode: '', description: '', notes: '', agencyId: '' })
  const [headingForm, setHeadingForm] = useState({ headingNumber: '', description: '', notes: '' })
  const [detailForm, setDetailForm] = useState({ headingUuid: '', description: '', notes: '' })

  useEffect(() => {
    if (categories.length === 0) {
      setSelectedUuid(null)
      return
    }

    if (!selectedUuid || !categories.some((category) => category.uuid === selectedUuid)) {
      setSelectedUuid(categories[0].uuid)
    }
  }, [categories, selectedUuid])

  useEffect(() => {
    const headings = categoryDetail?.headings ?? []
    if (headings.length === 0) {
      setSelectedHeadingUuid(null)
      return
    }

    if (!selectedHeadingUuid || !headings.some((heading) => heading.uuid === selectedHeadingUuid)) {
      setSelectedHeadingUuid(headings[0].uuid)
    }
  }, [categoryDetail?.headings, selectedHeadingUuid])

  const selectedHeading = useMemo(
    () => categoryDetail?.headings.find((heading) => heading.uuid === selectedHeadingUuid) ?? null,
    [categoryDetail?.headings, selectedHeadingUuid],
  )

  const handleCreateCategory = async (e: FormEvent) => {
    e.preventDefault()
    await createCategory({
      hsCode: categoryForm.hsCode,
      description: categoryForm.description,
      notes: categoryForm.notes || undefined,
      agencyId: categoryForm.agencyId ? Number(categoryForm.agencyId) : undefined,
    }).unwrap()
    setCategoryOpen(false)
    setCategoryForm({ hsCode: '', description: '', notes: '', agencyId: '' })
  }

  const handleCreateHeading = async (e: FormEvent) => {
    e.preventDefault()
    if (!selectedUuid) return
    await createHeading({ categoryUuid: selectedUuid, ...headingForm }).unwrap()
    setHeadingOpen(false)
    setHeadingForm({ headingNumber: '', description: '', notes: '' })
  }

  const handleCreateDetail = async (e: FormEvent) => {
    e.preventDefault()
    if (!detailForm.headingUuid) return
    await createDetail({ headingUuid: detailForm.headingUuid, description: detailForm.description, notes: detailForm.notes }).unwrap()
    setDetailOpen(false)
    setDetailForm({ headingUuid: '', description: '', notes: '' })
  }

  const agencyOptions = useMemo(() => {
    const codes = new Set(allCategories.map((c) => c.agencyCode).filter(Boolean) as string[])
    return Array.from(codes).sort()
  }, [allCategories])

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Administration"
        title="HS Code Library"
        subtitle="Full MAV and SPS import HS catalog classified by regulating agency (BAI, BPI, BFAR, SRA, NTA)."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={() => setCategoryOpen(true)}>
            Add Category
          </Button>
        }
      />

      <PortalPanel title="Filter by Agency">
        <Box sx={{ px: 2.5, py: 2 }}>
          <TextField
            select
            label="Agency"
            value={agencyFilter}
            onChange={(e) => {
              setAgencyFilter(e.target.value)
              setSelectedUuid(null)
              setSelectedHeadingUuid(null)
            }}
            sx={{ width: 280 }}
          >
            <MenuItem value="">All agencies</MenuItem>
            {agencyOptions.map((code) => (
              <MenuItem key={code} value={code}>
                {code}
              </MenuItem>
            ))}
          </TextField>
        </Box>
      </PortalPanel>

      <Grid container spacing={3} sx={{ mt: 0 }}>
        <Grid size={{ xs: 12, lg: 5 }}>
          <PortalTablePanel
            title="HS Categories"
            columns={['HS Code', 'Description', 'Agency', 'Headings', 'Status']}
            isLoading={isLoading}
            isEmpty={!isLoading && categories.length === 0}
            emptyMessage="No HS categories yet."
          >
            {categories.map((category) => (
              <TableRow
                key={category.uuid}
                hover
                selected={selectedUuid === category.uuid}
                onClick={() => {
                  setSelectedUuid(category.uuid)
                  setSelectedHeadingUuid(null)
                }}
                sx={{ cursor: 'pointer' }}
              >
                <TableCell>{category.hsCode}</TableCell>
                <TableCell>{category.description}</TableCell>
                <TableCell>{category.agencyCode ?? '—'}</TableCell>
                <TableCell>{category.headingCount}</TableCell>
                <TableCell>{category.isActive ? 'Active' : 'Inactive'}</TableCell>
              </TableRow>
            ))}
          </PortalTablePanel>
        </Grid>

        <Grid size={{ xs: 12, lg: 7 }}>
          {!categoryDetail ? (
            <PortalPanel title="Headings & Commodities">
              <Box sx={{ px: 2.5, py: 4 }}>
                <Typography color="text.secondary">Select an HS category to view headings and commodities.</Typography>
              </Box>
            </PortalPanel>
          ) : (
            <PortalPanel title={`${categoryDetail.hsCode} — Headings & Commodities`}>
              <Box
                sx={{
                  display: 'flex',
                  flexWrap: 'wrap',
                  gap: 1.5,
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  px: 2.5,
                  py: 1.5,
                  borderBottom: `1px solid ${portalColors.border}`,
                }}
              >
                <Box>
                  <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>{categoryDetail.description}</Typography>
                  {categoryDetail.agencyCode ? (
                    <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mt: 0.25 }}>
                      Agency: {categoryDetail.agencyCode}
                    </Typography>
                  ) : null}
                </Box>
                <Stack direction="row" spacing={1}>
                  <Button size="small" onClick={() => setHeadingOpen(true)}>
                    Add Heading
                  </Button>
                  <Button
                    size="small"
                    onClick={() => {
                      setDetailForm({
                        headingUuid: selectedHeadingUuid ?? categoryDetail.headings[0]?.uuid ?? '',
                        description: '',
                        notes: '',
                      })
                      setDetailOpen(true)
                    }}
                    disabled={categoryDetail.headings.length === 0}
                  >
                    Add Commodity
                  </Button>
                </Stack>
              </Box>
              {categoryDetail.notes ? (
                <Typography sx={{ px: 2.5, pt: 2, pb: 0, fontSize: '0.875rem', color: portalColors.textMuted }}>
                  {categoryDetail.notes}
                </Typography>
              ) : null}

              {categoryDetail.headings.length === 0 ? (
                <Box sx={{ px: 2.5, py: 3 }}>
                  <Typography color="text.secondary">No headings yet. Add a heading first.</Typography>
                </Box>
              ) : (
                <Grid container spacing={0} sx={{ minHeight: 360 }}>
                  <Grid
                    size={{ xs: 12, md: 6 }}
                    sx={{
                      borderRight: { md: `1px solid ${portalColors.border}` },
                      borderBottom: { xs: `1px solid ${portalColors.border}`, md: 'none' },
                    }}
                  >
                    <Box sx={{ px: 2, py: 1.5, bgcolor: portalColors.bgMuted, borderBottom: `1px solid ${portalColors.border}` }}>
                      <Typography sx={{ fontSize: '0.8125rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                        Headings
                      </Typography>
                    </Box>
                    <Box sx={{ maxHeight: 420, overflow: 'auto' }}>
                      {categoryDetail.headings.map((heading) => {
                        const isSelected = selectedHeadingUuid === heading.uuid
                        return (
                          <Box
                            key={heading.uuid}
                            onClick={() => setSelectedHeadingUuid(heading.uuid)}
                            sx={{
                              px: 2,
                              py: 1.5,
                              cursor: 'pointer',
                              borderBottom: `1px solid ${portalColors.border}`,
                              bgcolor: isSelected ? 'rgba(22, 101, 52, 0.08)' : 'transparent',
                              '&:hover': { bgcolor: isSelected ? 'rgba(22, 101, 52, 0.12)' : portalColors.bgMuted },
                            }}
                          >
                            <Typography sx={{ fontSize: '0.875rem', fontWeight: 700 }}>
                              {categoryDetail.hsCode}.{heading.headingNumber}
                            </Typography>
                            <Typography sx={{ fontSize: '0.8125rem', color: portalColors.textMuted, mt: 0.25 }}>
                              {heading.description}
                            </Typography>
                            <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.5 }}>
                              {heading.details.length} commodities
                            </Typography>
                          </Box>
                        )
                      })}
                    </Box>
                  </Grid>

                  <Grid size={{ xs: 12, md: 6 }}>
                    <Box sx={{ px: 2, py: 1.5, bgcolor: portalColors.bgMuted, borderBottom: `1px solid ${portalColors.border}` }}>
                      <Typography sx={{ fontSize: '0.8125rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
                        Commodities
                      </Typography>
                    </Box>
                    <Box sx={{ maxHeight: 420, overflow: 'auto' }}>
                      {!selectedHeading ? (
                        <Box sx={{ px: 2, py: 3 }}>
                          <Typography color="text.secondary">Select a heading to view commodities.</Typography>
                        </Box>
                      ) : selectedHeading.details.length === 0 ? (
                        <Box sx={{ px: 2, py: 3 }}>
                          <Typography color="text.secondary">No commodities under this heading yet.</Typography>
                        </Box>
                      ) : (
                        selectedHeading.details.map((detail) => (
                          <Box
                            key={detail.uuid}
                            sx={{
                              px: 2,
                              py: 1.5,
                              borderBottom: `1px solid ${portalColors.border}`,
                            }}
                          >
                            <Typography sx={{ fontSize: '0.875rem', fontWeight: 600 }}>{detail.displayLabel}</Typography>
                            {!detail.isActive ? (
                              <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.5 }}>Inactive</Typography>
                            ) : null}
                          </Box>
                        ))
                      )}
                    </Box>
                  </Grid>
                </Grid>
              )}
            </PortalPanel>
          )}
        </Grid>
      </Grid>

      <Dialog open={categoryOpen} onClose={() => setCategoryOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleCreateCategory}>
          <DialogTitle>Add HS Category</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="HS Code" value={categoryForm.hsCode} onChange={(e) => setCategoryForm({ ...categoryForm, hsCode: e.target.value })} required fullWidth />
              <TextField label="Description" value={categoryForm.description} onChange={(e) => setCategoryForm({ ...categoryForm, description: e.target.value })} required fullWidth />
              <TextField
                select
                label="Agency"
                value={categoryForm.agencyId}
                onChange={(e) => setCategoryForm({ ...categoryForm, agencyId: e.target.value })}
                fullWidth
                helperText="Leave blank for a shared HS code. Assign BAI, BFAR, or BPI so the entry form only shows that agency’s codes."
              >
                <MenuItem value="">Shared / all agencies</MenuItem>
                {agencies.map((agency) => (
                  <MenuItem key={agency.id} value={String(agency.id)}>
                    {agency.code} — {agency.name}
                  </MenuItem>
                ))}
              </TextField>
              <TextField label="Notes" value={categoryForm.notes} onChange={(e) => setCategoryForm({ ...categoryForm, notes: e.target.value })} fullWidth multiline minRows={2} />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setCategoryOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained">Save</Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={headingOpen} onClose={() => setHeadingOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleCreateHeading}>
          <DialogTitle>Add HS Heading</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Heading Number" value={headingForm.headingNumber} onChange={(e) => setHeadingForm({ ...headingForm, headingNumber: e.target.value })} required fullWidth />
              <TextField label="Description" value={headingForm.description} onChange={(e) => setHeadingForm({ ...headingForm, description: e.target.value })} required fullWidth />
              <TextField label="Notes" value={headingForm.notes} onChange={(e) => setHeadingForm({ ...headingForm, notes: e.target.value })} fullWidth multiline minRows={2} />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setHeadingOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained">Save</Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={detailOpen} onClose={() => setDetailOpen(false)} maxWidth="sm" fullWidth>
        <Box component="form" onSubmit={handleCreateDetail}>
          <DialogTitle>Add Commodity Detail</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                select
                label="Heading"
                value={detailForm.headingUuid}
                onChange={(e) => setDetailForm({ ...detailForm, headingUuid: e.target.value })}
                required
                fullWidth
                slotProps={{ select: { native: true } }}
              >
                <option value="" />
                {(categoryDetail?.headings ?? []).map((heading) => (
                  <option key={heading.uuid} value={heading.uuid}>
                    {heading.headingNumber} — {heading.description}
                  </option>
                ))}
              </TextField>
              <TextField label="Commodity Description" value={detailForm.description} onChange={(e) => setDetailForm({ ...detailForm, description: e.target.value })} required fullWidth />
              <TextField label="Notes" value={detailForm.notes} onChange={(e) => setDetailForm({ ...detailForm, notes: e.target.value })} fullWidth multiline minRows={2} />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDetailOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained">Save</Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
