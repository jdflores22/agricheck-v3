import KeyboardArrowDownIcon from '@mui/icons-material/KeyboardArrowDown'
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import {
  Avatar,
  Box,
  Divider,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import { Link as RouterLink } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../../app/hooks'
import { logout, selectCurrentUser } from '../../features/auth/authSlice'
import { portalColors } from './portalTheme'

export function PortalUserMenu() {
  const dispatch = useAppDispatch()
  const user = useAppSelector(selectCurrentUser)
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null)
  const open = Boolean(anchorEl)

  if (!user) return null

  const initials = `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase() || 'U'

  return (
    <>
      <Box
        component="button"
        type="button"
        onClick={(event) => setAnchorEl(event.currentTarget)}
        aria-controls={open ? 'portal-user-menu' : undefined}
        aria-haspopup="true"
        aria-expanded={open ? 'true' : undefined}
        aria-label="Open user menu"
        sx={{
          display: 'flex',
          alignItems: 'center',
          gap: 1,
          border: 'none',
          borderRadius: '0.5rem',
          bgcolor: open ? 'rgba(255,255,255,0.12)' : 'transparent',
          color: '#fff',
          cursor: 'pointer',
          px: { xs: 0.5, sm: 1 },
          py: 0.5,
          transition: 'background-color 0.15s ease',
          '&:hover': { bgcolor: 'rgba(255,255,255,0.12)' },
        }}
      >
        <Avatar
          sx={{
            width: 32,
            height: 32,
            bgcolor: 'rgba(255,255,255,0.15)',
            fontSize: '0.75rem',
            fontWeight: 600,
          }}
        >
          {initials}
        </Avatar>
        <Box sx={{ display: { xs: 'none', sm: 'block' }, textAlign: 'left', minWidth: 0 }}>
          <Typography
            sx={{
              fontSize: '0.8125rem',
              fontWeight: 600,
              lineHeight: 1.2,
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              maxWidth: 180,
            }}
          >
            {user.firstName} {user.lastName}
          </Typography>
          <Typography
            sx={{
              fontSize: '0.6875rem',
              opacity: 0.8,
              lineHeight: 1.2,
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              maxWidth: 180,
            }}
          >
            {user.email}
          </Typography>
        </Box>
        <KeyboardArrowDownIcon
          sx={{
            fontSize: 18,
            opacity: 0.85,
            transform: open ? 'rotate(180deg)' : 'none',
            transition: 'transform 0.15s ease',
          }}
        />
      </Box>

      <Menu
        id="portal-user-menu"
        anchorEl={anchorEl}
        open={open}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
        transformOrigin={{ vertical: 'top', horizontal: 'right' }}
        slotProps={{
          paper: {
            elevation: 3,
            sx: {
              mt: 1,
              minWidth: 220,
              borderRadius: '0.625rem',
              border: `1px solid ${portalColors.border}`,
              overflow: 'hidden',
            },
          },
        }}
      >
        <Box sx={{ px: 2, py: 1.5, bgcolor: portalColors.bgMuted }}>
          <Typography sx={{ fontSize: '0.875rem', fontWeight: 600, color: portalColors.textDark }}>
            {user.firstName} {user.lastName}
          </Typography>
          <Typography sx={{ fontSize: '0.75rem', color: portalColors.textMuted, mt: 0.25 }}>
            {user.email}
          </Typography>
        </Box>
        <Divider />
        <MenuItem
          component={RouterLink}
          to="/profile"
          onClick={() => setAnchorEl(null)}
          sx={{ py: 1.25 }}
        >
          <ListItemIcon sx={{ minWidth: 36 }}>
            <PersonOutlineOutlinedIcon fontSize="small" sx={{ color: portalColors.textMuted }} />
          </ListItemIcon>
          <ListItemText primary="Profile" slotProps={{ primary: { sx: { fontSize: '0.875rem' } } }} />
        </MenuItem>
        <Divider />
        <MenuItem
          onClick={() => {
            setAnchorEl(null)
            dispatch(logout())
          }}
          sx={{ py: 1.25, color: '#991b1b' }}
        >
          <ListItemIcon sx={{ minWidth: 36 }}>
            <LogoutOutlinedIcon fontSize="small" sx={{ color: '#991b1b' }} />
          </ListItemIcon>
          <ListItemText primary="Sign out" slotProps={{ primary: { sx: { fontSize: '0.875rem' } } }} />
        </MenuItem>
      </Menu>
    </>
  )
}
