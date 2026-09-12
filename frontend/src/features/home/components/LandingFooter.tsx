import EmailOutlinedIcon from '@mui/icons-material/EmailOutlined'
import { Box, Link, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import { useSystemBranding } from '../../system/SystemBrandingProvider'
import { landingColors, landingContainerSx } from '../landingTheme'

export function LandingFooter() {
  const { systemName, footerText } = useSystemBranding()
  const year = new Date().getFullYear()
  const copyright = footerText || `© ${year} ${systemName}`

  return (
    <Box
      component="footer"
      sx={{
        borderTop: `1px solid ${landingColors.border}`,
        bgcolor: landingColors.bgWhite,
        py: { xs: 4, md: 6 },
        pb: 3,
      }}
    >
      <Box sx={landingContainerSx}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: 'repeat(3, minmax(0, 1fr))' },
            gap: { xs: 3, md: 4 },
          }}
        >
          <Box>
            <Typography
              sx={{
                mb: 1.5,
                fontSize: '0.8125rem',
                fontWeight: 700,
                letterSpacing: '0.06em',
                textTransform: 'uppercase',
                color: landingColors.primary,
              }}
            >
              About
            </Typography>
            <Typography sx={{ fontSize: '0.875rem', lineHeight: 1.6, color: landingColors.textMuted }}>
              {systemName} is a comprehensive platform for managing agricultural import and export operations,
              accreditation, and compliance.
            </Typography>
          </Box>

          <Box>
            <Typography
              sx={{
                mb: 1.5,
                fontSize: '0.8125rem',
                fontWeight: 700,
                letterSpacing: '0.06em',
                textTransform: 'uppercase',
                color: landingColors.primary,
              }}
            >
              Quick links
            </Typography>
            <Box component="ul" sx={{ m: 0, p: 0, listStyle: 'none', display: 'grid', gap: 1 }}>
              {[
                { label: 'Sign in', to: '/login' },
                { label: 'Register', to: '/register' },
                { label: 'Verify certificate', to: '/verify' },
                { label: 'Reset password', to: '/reset-password' },
              ].map((item) => (
                <Box component="li" key={item.to}>
                  <Link
                    component={RouterLink}
                    to={item.to}
                    sx={{
                      fontSize: '0.875rem',
                      color: landingColors.textMuted,
                      textDecoration: 'none',
                      '&:hover': { color: landingColors.primary },
                    }}
                  >
                    {item.label}
                  </Link>
                </Box>
              ))}
            </Box>
          </Box>

          <Box>
            <Typography
              sx={{
                mb: 1.5,
                fontSize: '0.8125rem',
                fontWeight: 700,
                letterSpacing: '0.06em',
                textTransform: 'uppercase',
                color: landingColors.primary,
              }}
            >
              Contact
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <EmailOutlinedIcon sx={{ fontSize: 18, color: landingColors.textMuted }} />
              <Typography sx={{ fontSize: '0.875rem', lineHeight: 1.6, color: landingColors.textMuted }}>
                Reach out through your agency administrator for support.
              </Typography>
            </Box>
          </Box>
        </Box>

        <Box
          sx={{
            mt: 4,
            pt: 2.5,
            borderTop: `1px solid ${landingColors.bgMuted}`,
            textAlign: 'center',
          }}
        >
          <Typography sx={{ fontSize: '0.8125rem', color: landingColors.textLight }}>
            {copyright}
          </Typography>
        </Box>
      </Box>
    </Box>
  )
}
