import {
  Alert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  MenuItem,
  Stack,
  Switch,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import { FormEvent, useState } from 'react'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalPrimaryButtonSx } from '../../../components/portal/portalStyles'
import {
  type AgencyNewsManageDetail,
  type CreateAgencyNewsRequest,
  useCreateAgencyNewsPostMutation,
  useDeleteAgencyNewsPostMutation,
  useGetAgencyNewsPostsQuery,
  useLazyGetAgencyNewsPostQuery,
  usePublishAgencyNewsPostMutation,
  useUnpublishAgencyNewsPostMutation,
  useUpdateAgencyNewsPostMutation,
} from '../api/agencyApi'

const categories = ['News', 'Announcement', 'Article'] as const

const emptyForm: CreateAgencyNewsRequest = {
  title: '',
  summary: '',
  body: '',
  imageUrl: '',
  category: 'News',
  isRegulatory: true,
}

function formatDate(value?: string | null) {
  if (!value) return '—'
  const d = new Date(value)
  return Number.isNaN(d.getTime()) ? value : d.toLocaleDateString()
}

export function AgencyNewsPage() {
  const { data, isLoading, isError } = useGetAgencyNewsPostsQuery()
  const posts = data?.data ?? []

  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<AgencyNewsManageDetail | null>(null)
  const [form, setForm] = useState<CreateAgencyNewsRequest>(emptyForm)

  const [createPost, { isLoading: creating }] = useCreateAgencyNewsPostMutation()
  const [updatePost, { isLoading: updating }] = useUpdateAgencyNewsPostMutation()
  const [publishPost] = usePublishAgencyNewsPostMutation()
  const [unpublishPost] = useUnpublishAgencyNewsPostMutation()
  const [deletePost] = useDeleteAgencyNewsPostMutation()
  const [fetchPost] = useLazyGetAgencyNewsPostQuery()

  const openCreate = () => {
    setEditing(null)
    setForm(emptyForm)
    setDialogOpen(true)
  }

  const openEdit = async (uuid: string) => {
    const result = await fetchPost(uuid).unwrap()
    const row = result.data
    if (!row) return
    setEditing(row)
    setForm({
      title: row.title,
      summary: row.summary ?? '',
      body: row.body,
      imageUrl: row.imageUrl ?? '',
      category: row.category,
      isRegulatory: row.isRegulatory,
      displayOrder: row.displayOrder,
    })
    setDialogOpen(true)
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const payload: CreateAgencyNewsRequest = {
      ...form,
      title: form.title.trim(),
      body: form.body.trim(),
      summary: form.summary?.trim() || undefined,
      imageUrl: form.imageUrl?.trim() || undefined,
    }
    if (editing) {
      await updatePost({ uuid: editing.uuid, body: payload }).unwrap()
    } else {
      await createPost(payload).unwrap()
    }
    setDialogOpen(false)
  }

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Agency"
        title="News & announcements"
        subtitle="Publish updates for AgriTrack drivers and clients. Posts show your agency badge (e.g. DA, BAI). Lower display order appears first in the app."
        actions={
          <Button variant="contained" sx={portalPrimaryButtonSx} onClick={openCreate}>
            New post
          </Button>
        }
      />

      {isError && (
        <Alert severity="error" sx={{ mb: 2, borderRadius: '0.75rem' }}>
          Unable to load news posts. Ensure you have agency publisher access.
        </Alert>
      )}

      <PortalPanel title="Your posts">
        {isLoading ? (
          <Typography sx={{ p: 2.5, color: portalColors.textMuted }}>Loading…</Typography>
        ) : posts.length === 0 ? (
          <Typography sx={{ p: 2.5, color: portalColors.textMuted }}>
            No posts yet. Create a draft, then publish when ready.
          </Typography>
        ) : (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Title</TableCell>
                <TableCell>Category</TableCell>
                <TableCell>Tags</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Order</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {posts.map((post) => (
                <TableRow key={post.uuid} hover>
                  <TableCell>
                    <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>{post.title}</Typography>
                    <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted }}>
                      Updated {formatDate(post.updatedAt)}
                    </Typography>
                  </TableCell>
                  <TableCell>{post.category}</TableCell>
                  <TableCell>
                    {post.isRegulatory && <Chip size="small" label="Regulatory" color="warning" variant="outlined" />}
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={post.isPublished ? 'Published' : 'Draft'}
                      color={post.isPublished ? 'success' : 'default'}
                    />
                  </TableCell>
                  <TableCell>{post.displayOrder}</TableCell>
                  <TableCell align="right">
                    <Stack direction="row" spacing={1} sx={{ justifyContent: 'flex-end' }}>
                      <Button size="small" onClick={() => openEdit(post.uuid)}>
                        Edit
                      </Button>
                      {post.isPublished ? (
                        <Button size="small" onClick={() => unpublishPost(post.uuid)}>
                          Unpublish
                        </Button>
                      ) : (
                        <Button size="small" color="primary" onClick={() => publishPost(post.uuid)}>
                          Publish
                        </Button>
                      )}
                      <Button size="small" color="error" onClick={() => deletePost(post.uuid)}>
                        Delete
                      </Button>
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </PortalPanel>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} fullWidth maxWidth="md">
        <Box component="form" onSubmit={handleSubmit}>
          <DialogTitle>{editing ? 'Edit post' : 'New post'}</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <TextField
                label="Title"
                required
                fullWidth
                value={form.title}
                onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
              />
              <TextField
                label="Summary"
                fullWidth
                multiline
                minRows={2}
                value={form.summary}
                onChange={(e) => setForm((f) => ({ ...f, summary: e.target.value }))}
              />
              <TextField
                label="Body"
                required
                fullWidth
                multiline
                minRows={6}
                value={form.body}
                onChange={(e) => setForm((f) => ({ ...f, body: e.target.value }))}
              />
              <TextField
                label="Image URL (optional)"
                fullWidth
                value={form.imageUrl}
                onChange={(e) => setForm((f) => ({ ...f, imageUrl: e.target.value }))}
              />
              <TextField
                select
                label="Category"
                fullWidth
                value={form.category}
                onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))}
              >
                {categories.map((c) => (
                  <MenuItem key={c} value={c}>{c}</MenuItem>
                ))}
              </TextField>
              <TextField
                label="Display order (lower = higher in feed)"
                type="number"
                fullWidth
                value={form.displayOrder ?? ''}
                onChange={(e) =>
                  setForm((f) => ({
                    ...f,
                    displayOrder: e.target.value === '' ? undefined : Number(e.target.value),
                  }))
                }
              />
              <FormControlLabel
                control={
                  <Switch
                    checked={form.isRegulatory}
                    onChange={(e) => setForm((f) => ({ ...f, isRegulatory: e.target.checked }))}
                  />
                }
                label="Regulatory classification (shows regulatory badge in AgriTrack)"
              />
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button type="submit" variant="contained" sx={portalPrimaryButtonSx} disabled={creating || updating}>
              {editing ? 'Save' : 'Create draft'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
