import AssignmentTurnedInOutlinedIcon from '@mui/icons-material/AssignmentTurnedInOutlined'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import EmojiEventsOutlinedIcon from '@mui/icons-material/EmojiEventsOutlined'
import GroupsOutlinedIcon from '@mui/icons-material/GroupsOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import LoginIcon from '@mui/icons-material/Login'
import PersonAddOutlinedIcon from '@mui/icons-material/PersonAddOutlined'
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined'
import TrendingUpOutlinedIcon from '@mui/icons-material/TrendingUpOutlined'
import VerifiedUserOutlinedIcon from '@mui/icons-material/VerifiedUserOutlined'
import { Box, Button, Typography } from '@mui/material'
import { Link as RouterLink, Navigate } from 'react-router-dom'
import { useAppSelector } from '../../../app/hooks'
import { selectAuthRedirect, selectCurrentUser, selectIsAuthenticated } from '../../../features/auth/authSlice'
import { resolveDashboardPath } from '../../../components/portal/portalUtils'
import { useSystemBranding } from '../../system/SystemBrandingProvider'
import {
  agencies,
  heroStats,
  highlights,
  platformFeatures,
  userTypeTags,
  whyItems,
} from '../landingContent'
import {
  landingBtnPrimarySx,
  landingBtnSecondarySx,
  landingColors,
  landingContainerSx,
} from '../landingTheme'

const featureIcons = [
  EmojiEventsOutlinedIcon,
  AssignmentTurnedInOutlinedIcon,
  Inventory2OutlinedIcon,
  BusinessOutlinedIcon,
] as const

const highlightIcons = [
  GroupsOutlinedIcon,
  VerifiedUserOutlinedIcon,
  TrendingUpOutlinedIcon,
  ScheduleOutlinedIcon,
] as const

const heroIcons = [
  EmojiEventsOutlinedIcon,
  AssignmentTurnedInOutlinedIcon,
  Inventory2OutlinedIcon,
  BusinessOutlinedIcon,
] as const

export function HomePage() {
  const isAuthenticated = useAppSelector(selectIsAuthenticated)
  const user = useAppSelector(selectCurrentUser)
  const redirectPath = useAppSelector(selectAuthRedirect)
  const dashboardPath = resolveDashboardPath(redirectPath, user?.roles ?? [])
  const { systemName } = useSystemBranding()

  if (isAuthenticated) {
    return <Navigate to={dashboardPath} replace />
  }

  return (
    <Box>
      <Box
        component="section"
        sx={{
          position: 'relative',
          overflow: 'hidden',
          py: { xs: 6, md: 8 },
          background: landingColors.heroGradient,
        }}
      >
        <Box
          sx={{
            ...landingContainerSx,
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', lg: '1.05fr 0.95fr' },
            gap: { xs: 5, lg: 6 },
            alignItems: 'center',
          }}
        >
          <Box>
            <Typography
              sx={{
                mb: 1.5,
                fontSize: '0.6875rem',
                fontWeight: 600,
                letterSpacing: '0.12em',
                textTransform: 'uppercase',
                color: landingColors.primary,
              }}
            >
              Agricultural trade platform
            </Typography>
            <Typography
              component="h1"
              sx={{
                m: 0,
                fontSize: { xs: '2rem', md: 'clamp(2rem, 4vw, 3.25rem)' },
                lineHeight: 1.1,
                fontWeight: 700,
                letterSpacing: '-0.03em',
                color: landingColors.textDark,
              }}
            >
              Welcome to {systemName}
            </Typography>
            <Typography
              sx={{
                mt: 2,
                maxWidth: '38rem',
                fontSize: '1.0625rem',
                lineHeight: 1.7,
                color: landingColors.textMuted,
              }}
            >
              Operational platform for importers, agencies, MAV partners, and evaluators — accreditation,
              compliance, and agricultural trade management in one secure workspace.
            </Typography>

            <Box
              sx={{
                display: 'flex',
                flexWrap: 'wrap',
                gap: '0.75rem 1.25rem',
                mt: 3,
              }}
              aria-label="Supported user types"
            >
              {userTypeTags.map((tag, index) => (
                <Typography
                  key={tag}
                  sx={{
                    fontSize: '0.6875rem',
                    fontWeight: 600,
                    letterSpacing: '0.08em',
                    textTransform: 'uppercase',
                    color: landingColors.textMuted,
                    '&::after': index < userTypeTags.length - 1 ? {
                      content: '"·"',
                      ml: 2.5,
                      opacity: 0.45,
                    } : undefined,
                  }}
                >
                  {tag}
                </Typography>
              ))}
            </Box>

            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, mt: 4 }}>
              <Button
                component={RouterLink}
                to="/login"
                startIcon={<LoginIcon />}
                sx={landingBtnPrimarySx}
              >
                Sign in
              </Button>
              <Button
                component={RouterLink}
                to="/register"
                startIcon={<PersonAddOutlinedIcon />}
                sx={landingBtnSecondarySx}
              >
                Create account
              </Button>
            </Box>
          </Box>

          <Box sx={{ display: 'flex', justifyContent: 'center' }} aria-hidden="true">
            <Box
              sx={{
                width: '100%',
                maxWidth: '28rem',
                border: `1px solid ${landingColors.border}`,
                borderRadius: '1rem',
                bgcolor: landingColors.bgWhite,
                boxShadow: '0 20px 45px rgba(28, 25, 23, 0.08)',
                overflow: 'hidden',
              }}
            >
              <Box
                sx={{
                  display: 'flex',
                  gap: 0.75,
                  px: 2,
                  py: 1.75,
                  borderBottom: `1px solid ${landingColors.bgMuted}`,
                  bgcolor: landingColors.bgPage,
                }}
              >
                {['#fca5a5', '#fde047', '#86efac'].map((color) => (
                  <Box
                    key={color}
                    sx={{ width: 10, height: 10, borderRadius: '9999px', bgcolor: color }}
                  />
                ))}
              </Box>
              <Box sx={{ display: 'grid', gap: 1.75, p: 2 }}>
                {heroStats.map((stat, index) => {
                  const Icon = heroIcons[index]
                  return (
                    <Box
                      key={stat.title}
                      sx={{
                        display: 'flex',
                        alignItems: 'flex-start',
                        gap: 1.75,
                        p: 1.75,
                        border: `1px solid ${landingColors.bgMuted}`,
                        borderRadius: '0.75rem',
                        bgcolor: landingColors.bgPage,
                      }}
                    >
                      <Box
                        sx={{
                          display: 'inline-flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          width: 40,
                          height: 40,
                          borderRadius: '0.625rem',
                          bgcolor: 'rgba(22, 101, 52, 0.1)',
                          color: landingColors.primary,
                          flexShrink: 0,
                        }}
                      >
                        <Icon sx={{ fontSize: 22 }} />
                      </Box>
                      <Box>
                        <Typography sx={{ fontSize: '0.9375rem', fontWeight: 600, color: landingColors.textDark }}>
                          {stat.title}
                        </Typography>
                        <Typography sx={{ mt: 0.25, fontSize: '0.8125rem', color: landingColors.textMuted }}>
                          {stat.subtitle}
                        </Typography>
                      </Box>
                    </Box>
                  )
                })}
              </Box>
            </Box>
          </Box>
        </Box>
      </Box>

      <Box component="section" sx={{ py: { xs: 6, md: 8 } }}>
        <Box sx={landingContainerSx}>
          <Box sx={{ maxWidth: '42rem', mb: 5 }}>
            <Typography
              sx={{
                mb: 1.5,
                fontSize: '0.6875rem',
                fontWeight: 600,
                letterSpacing: '0.12em',
                textTransform: 'uppercase',
                color: landingColors.primary,
              }}
            >
              Platform services
            </Typography>
            <Typography
              component="h2"
              sx={{
                m: 0,
                fontSize: { xs: '1.75rem', md: 'clamp(1.75rem, 3vw, 2.5rem)' },
                lineHeight: 1.15,
                fontWeight: 700,
                letterSpacing: '-0.02em',
              }}
            >
              Everything you need for agricultural trade
            </Typography>
            <Typography sx={{ mt: 1.5, fontSize: '1rem', lineHeight: 1.7, color: landingColors.textMuted }}>
              Comprehensive tools for accreditation, regulatory submissions, logistics, and warehouse operations.
            </Typography>
          </Box>

          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', lg: 'repeat(4, minmax(0, 1fr))' },
              gap: 2,
            }}
          >
            {platformFeatures.map((feature, index) => {
              const Icon = featureIcons[index]
              return (
                <Box
                  key={feature.title}
                  component="article"
                  sx={{
                    height: '100%',
                    p: 2.5,
                    border: `1px solid ${landingColors.border}`,
                    borderRadius: '0.875rem',
                    bgcolor: landingColors.bgWhite,
                    transition: 'box-shadow 0.15s ease, border-color 0.15s ease',
                    '&:hover': {
                      borderColor: 'rgba(22, 101, 52, 0.25)',
                      boxShadow: '0 10px 24px rgba(28, 25, 23, 0.06)',
                    },
                  }}
                >
                  <Box
                    sx={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      width: 44,
                      height: 44,
                      mb: 2,
                      borderRadius: '0.75rem',
                      bgcolor: 'rgba(22, 101, 52, 0.1)',
                      color: landingColors.primary,
                    }}
                  >
                    <Icon sx={{ fontSize: 24 }} />
                  </Box>
                  <Typography sx={{ mb: 1, fontSize: '1.0625rem', fontWeight: 600 }}>{feature.title}</Typography>
                  <Typography sx={{ fontSize: '0.9375rem', lineHeight: 1.6, color: landingColors.textMuted }}>
                    {feature.description}
                  </Typography>
                </Box>
              )
            })}
          </Box>
        </Box>
      </Box>

      <Box
        component="section"
        sx={{
          py: { xs: 6, md: 8 },
          bgcolor: landingColors.bgMuted,
          borderTop: `1px solid ${landingColors.border}`,
          borderBottom: `1px solid ${landingColors.border}`,
        }}
      >
        <Box sx={landingContainerSx}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', lg: 'repeat(4, minmax(0, 1fr))' },
              gap: 2,
            }}
          >
            {highlights.map((item, index) => {
              const Icon = highlightIcons[index]
              return (
                <Box
                  key={item.title}
                  component="article"
                  sx={{
                    height: '100%',
                    p: 2.5,
                    border: `1px solid ${landingColors.border}`,
                    borderRadius: '0.875rem',
                    bgcolor: landingColors.bgWhite,
                  }}
                >
                  <Icon sx={{ mb: 1.75, fontSize: 28, color: landingColors.primary }} />
                  <Typography sx={{ mb: 1, fontSize: '1.0625rem', fontWeight: 600 }}>{item.title}</Typography>
                  <Typography sx={{ fontSize: '0.9375rem', lineHeight: 1.6, color: landingColors.textMuted }}>
                    {item.description}
                  </Typography>
                </Box>
              )
            })}
          </Box>
        </Box>
      </Box>

      <Box component="section" sx={{ py: { xs: 6, md: 8 } }}>
        <Box
          sx={{
            ...landingContainerSx,
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', lg: '1.1fr 0.9fr' },
            gap: { xs: 4, lg: 6 },
            alignItems: 'center',
          }}
        >
          <Box>
            <Typography
              sx={{
                mb: 1.5,
                fontSize: '0.6875rem',
                fontWeight: 600,
                letterSpacing: '0.12em',
                textTransform: 'uppercase',
                color: landingColors.primary,
              }}
            >
              Why AgriCheck
            </Typography>
            <Typography
              component="h2"
              sx={{
                m: 0,
                fontSize: { xs: '1.75rem', md: 'clamp(1.75rem, 3vw, 2.5rem)' },
                lineHeight: 1.15,
                fontWeight: 700,
                letterSpacing: '-0.02em',
              }}
            >
              Built for agencies and trade partners
            </Typography>
            <Box component="ul" sx={{ m: 0, mt: 3, p: 0, listStyle: 'none', display: 'grid', gap: 2 }}>
              {whyItems.map((item) => (
                <Box component="li" key={item.title} sx={{ display: 'flex', gap: 1.75 }}>
                  <CheckCircleIcon sx={{ mt: 0.25, fontSize: 20, color: landingColors.primary, flexShrink: 0 }} />
                  <Box>
                    <Typography sx={{ mb: 0.25, fontSize: '0.9375rem', fontWeight: 700 }}>{item.title}</Typography>
                    <Typography sx={{ fontSize: '0.875rem', lineHeight: 1.6, color: landingColors.textMuted }}>
                      {item.description}
                    </Typography>
                  </Box>
                </Box>
              ))}
            </Box>
          </Box>

          <Box sx={{ display: 'grid', gap: 1.75 }}>
            {agencies.map((agency) => (
              <Box
                key={agency.code}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.75,
                  p: 2,
                  border: `1px solid ${landingColors.border}`,
                  borderRadius: '0.875rem',
                  bgcolor: landingColors.bgWhite,
                  boxShadow: '0 8px 20px rgba(28, 25, 23, 0.05)',
                }}
              >
                <Box
                  component="span"
                  sx={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    minWidth: 48,
                    px: 1,
                    py: 0.5,
                    borderRadius: '0.5rem',
                    bgcolor: landingColors.primary,
                    color: '#fff',
                    fontSize: '0.75rem',
                    fontWeight: 700,
                    letterSpacing: '0.04em',
                  }}
                >
                  {agency.code}
                </Box>
                <Typography sx={{ fontSize: '0.9375rem', color: landingColors.textDark }}>{agency.name}</Typography>
              </Box>
            ))}
          </Box>
        </Box>
      </Box>

      <Box
        component="section"
        sx={{
          py: { xs: 6, md: 9 },
          background: landingColors.ctaGradient,
          color: '#fff',
        }}
      >
        <Box sx={{ ...landingContainerSx, textAlign: 'center' }}>
          <Typography
            component="h2"
            sx={{
              m: 0,
              fontSize: { xs: '1.75rem', md: 'clamp(1.75rem, 3vw, 2.5rem)' },
              lineHeight: 1.15,
              fontWeight: 700,
              letterSpacing: '-0.02em',
            }}
          >
            Ready to get started?
          </Typography>
          <Typography
            sx={{
              mt: 1.75,
              mx: 'auto',
              maxWidth: '38rem',
              fontSize: '1.0625rem',
              lineHeight: 1.7,
              color: 'rgba(255, 255, 255, 0.88)',
            }}
          >
            Create your account and begin managing agricultural trade operations with {systemName}.
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'center', gap: 1.5, mt: 4 }}>
            <Button
              component={RouterLink}
              to="/register"
              startIcon={<PersonAddOutlinedIcon />}
              sx={{
                ...landingBtnPrimarySx,
                bgcolor: '#fff',
                color: landingColors.primary,
                borderColor: '#fff',
                '&:hover': { bgcolor: landingColors.bgMuted, color: landingColors.primaryDark, borderColor: '#fff' },
              }}
            >
              Create your account
            </Button>
            <Button
              component={RouterLink}
              to="/login"
              sx={{
                ...landingBtnPrimarySx,
                bgcolor: 'transparent',
                color: '#fff',
                borderColor: 'rgba(255, 255, 255, 0.35)',
                '&:hover': { bgcolor: 'rgba(255, 255, 255, 0.12)', borderColor: 'rgba(255, 255, 255, 0.35)' },
              }}
            >
              Sign in
            </Button>
          </Box>
        </Box>
      </Box>
    </Box>
  )
}
