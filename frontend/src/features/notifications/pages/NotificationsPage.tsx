import { Box, Button, Chip, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import {
  useGetNotificationsQuery,
  useMarkAllNotificationsReadMutation,
  useMarkNotificationReadMutation,
} from '../api/notificationsApi'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { PortalPanel } from '../../../components/portal/PortalPanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'

export function NotificationsPage() {
  const { data, refetch } = useGetNotificationsQuery({ limit: 50 })
  const [markRead] = useMarkNotificationReadMutation()
  const [markAllRead, { isLoading: markingAll }] = useMarkAllNotificationsReadMutation()
  const notifications = data?.data ?? []

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Account"
        title="Notifications"
        subtitle="Stay updated on entries, payments, and compliance requests."
        actions={
          notifications.some((n) => !n.isRead) ? (
            <Button variant="outlined" sx={portalOutlinedButtonSx} disabled={markingAll} onClick={async () => { await markAllRead(undefined).unwrap(); refetch() }}>
              Mark all read
            </Button>
          ) : undefined
        }
      />

      <Stack spacing={2}>
        {notifications.length === 0 && (
          <Typography sx={{ color: portalColors.textMuted }}>No notifications yet.</Typography>
        )}
        {notifications.map((item) => (
          <PortalPanel key={item.uuid} title={item.title}>
            <Box sx={{ px: 2.5, py: 2 }}>
              <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
                {!item.isRead && <Chip size="small" label="New" color="warning" />}
                <Chip size="small" label={item.type} variant="outlined" />
              </Stack>
              <Typography variant="body2" sx={{ mb: 1 }}>{item.message}</Typography>
              <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
                {new Date(item.createdAt).toLocaleString()}
              </Typography>
              {!item.isRead && (
                <Button size="small" sx={{ mt: 1 }} onClick={async () => { await markRead(item.uuid).unwrap(); refetch() }}>
                  Mark as read
                </Button>
              )}
            </Box>
          </PortalPanel>
        ))}
      </Stack>

      <Button component={RouterLink} to="/profile" variant="outlined" sx={{ mt: 3, ...portalOutlinedButtonSx }}>
        Notification preferences
      </Button>
    </Box>
  )
}
