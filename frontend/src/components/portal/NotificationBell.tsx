import NotificationsNoneOutlinedIcon from '@mui/icons-material/NotificationsNoneOutlined'
import { Badge, Box, Button, Divider, IconButton, Menu, MenuItem, Typography } from '@mui/material'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import {
  useGetNotificationsQuery,
  useGetUnreadCountQuery,
  useMarkAllNotificationsReadMutation,
  useMarkNotificationReadMutation,
} from '../../features/notifications/api/notificationsApi'
import { portalColors } from './portalTheme'

function resolveNotificationLink(type: string, relatedEntityType?: string, relatedEntityUuid?: string) {
  if (relatedEntityType === 'Entry' && relatedEntityUuid) return `/client/entries/${relatedEntityUuid}`
  if (relatedEntityType === 'Bill' && relatedEntityUuid) return `/client/bills/${relatedEntityUuid}`
  if (type.includes('payment')) return '/client/bills'
  if (type.includes('compliance') || type.includes('entry')) return '/client/entries'
  return '/notifications'
}

export function NotificationBell() {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const open = Boolean(anchorEl)
  const { data: countData } = useGetUnreadCountQuery(undefined, { pollingInterval: 30000 })
  const { data: listData, refetch } = useGetNotificationsQuery({ limit: 10, unreadOnly: false }, { skip: !open })
  const [markRead] = useMarkNotificationReadMutation()
  const [markAllRead] = useMarkAllNotificationsReadMutation()

  const unreadCount = countData?.data?.count ?? 0
  const notifications = listData?.data ?? []

  return (
    <>
      <IconButton color="inherit" aria-label="Notifications" onClick={(e) => setAnchorEl(e.currentTarget)}>
        <Badge badgeContent={unreadCount} color="error" max={99}>
          <NotificationsNoneOutlinedIcon />
        </Badge>
      </IconButton>

      <Menu
        anchorEl={anchorEl}
        open={open}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{ paper: { sx: { width: 360, maxWidth: '92vw' } } }}
      >
        <Box sx={{ px: 2, py: 1.5, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Typography sx={{ fontWeight: 600 }}>Notifications</Typography>
          {unreadCount > 0 && (
            <Button size="small" onClick={async () => { await markAllRead(undefined).unwrap(); refetch() }}>
              Mark all read
            </Button>
          )}
        </Box>
        <Divider />
        {notifications.length === 0 ? (
          <Box sx={{ px: 2, py: 3 }}>
            <Typography variant="body2" sx={{ color: portalColors.textMuted }}>No notifications yet.</Typography>
          </Box>
        ) : (
          notifications.map((item) => (
            <MenuItem
              key={item.uuid}
              component={RouterLink}
              to={resolveNotificationLink(item.type, item.relatedEntityType, item.relatedEntityUuid)}
              onClick={async () => {
                if (!item.isRead) await markRead(item.uuid)
                setAnchorEl(null)
              }}
              sx={{ alignItems: 'flex-start', whiteSpace: 'normal', py: 1.25, bgcolor: item.isRead ? undefined : 'rgba(22,101,52,0.06)' }}
            >
              <Box>
                <Typography sx={{ fontSize: '0.875rem', fontWeight: item.isRead ? 500 : 700 }}>{item.title}</Typography>
                <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.25 }}>{item.message}</Typography>
                <Typography sx={{ fontSize: '0.6875rem', color: portalColors.textLight, mt: 0.5 }}>
                  {new Date(item.createdAt).toLocaleString()}
                </Typography>
              </Box>
            </MenuItem>
          ))
        )}
        <Divider />
        <MenuItem component={RouterLink} to="/notifications" onClick={() => setAnchorEl(null)} sx={{ justifyContent: 'center' }}>
          View all notifications
        </MenuItem>
      </Menu>
    </>
  )
}
