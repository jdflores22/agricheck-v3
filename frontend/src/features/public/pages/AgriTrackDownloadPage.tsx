import AndroidOutlinedIcon from '@mui/icons-material/AndroidOutlined'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import { Box, Button, Paper, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import {
  landingBtnPrimarySx,
  landingColors,
  landingContainerSx,
} from '../../home/landingTheme'

const APP_VERSION = '1.2.42'
const APK_FILE = `AgriTrack-${APP_VERSION}-release.apk`
const APK_URL = `/downloads/${APK_FILE}`

export function AgriTrackDownloadPage() {
  return (
    <Box sx={{ py: { xs: 4, md: 8 } }}>
      <Box sx={{ ...landingContainerSx, maxWidth: 560 }}>
        <Paper
          elevation={0}
          sx={{
            p: { xs: 3, md: 4 },
            borderRadius: 3,
            border: `1px solid ${landingColors.border}`,
            bgcolor: '#fff',
          }}
        >
          <Stack spacing={2.5} sx={{ alignItems: 'flex-start' }}>
            <Box
              sx={{
                width: 56,
                height: 56,
                borderRadius: 2,
                bgcolor: 'rgba(22, 101, 52, 0.08)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: landingColors.primary,
              }}
            >
              <AndroidOutlinedIcon fontSize="large" />
            </Box>

            <Typography variant="h4" component="h1" sx={{ fontWeight: 800, color: landingColors.textDark }}>
              AgriTrack for Android
            </Typography>

            <Typography color="text.secondary">
              Driver app for fleet operators — deliveries, QR scanning, navigation, and profile verification.
              Install this build for field testing against the production API.
            </Typography>

            <Typography variant="body2" color="text.secondary">
              Version <strong>{APP_VERSION}</strong> · Package <code>com.agricheck.agritrack</code>
            </Typography>

            <Button
              component="a"
              href={APK_URL}
              download={APK_FILE}
              size="large"
              startIcon={<DownloadOutlinedIcon />}
              sx={{ ...landingBtnPrimarySx, px: 3 }}
            >
              Download APK
            </Button>

            <Stack direction="row" spacing={1} sx={{ alignItems: 'flex-start', color: 'text.secondary' }}>
              <InfoOutlinedIcon fontSize="small" sx={{ mt: 0.25 }} />
              <Typography variant="body2">
                On your phone: allow installs from unknown sources if prompted, then open the downloaded file.
                You may need to uninstall an older test build first.
              </Typography>
            </Stack>

            <Button component={RouterLink} to="/" size="small" sx={{ mt: 1 }}>
              Back to AgriCheck home
            </Button>
          </Stack>
        </Paper>
      </Box>
    </Box>
  )
}
