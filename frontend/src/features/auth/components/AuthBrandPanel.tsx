import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { Box, Typography } from '@mui/material'
import { authColors } from './authTheme'
import { useSystemBranding } from '../../system/SystemBrandingProvider'

const tags = ['Importers', 'Agencies', 'MAV', 'Evaluators']

export function AuthBrandPanel() {
  const { systemName, systemLogoUrl } = useSystemBranding()

  return (
    <Box
      aria-hidden
      sx={{
        display: { xs: 'none', md: 'flex' },
        position: 'relative',
        alignItems: 'center',
        justifyContent: 'flex-start',
        overflow: 'hidden',
        p: { md: '2.5rem 2.5rem 2.5rem 3rem', lg: '2.5rem 3.5rem 2.5rem 4rem' },
        bgcolor: authColors.brandBg,
        backgroundImage: `
          radial-gradient(circle at 18% 82%, rgba(22, 101, 52, 0.1) 0%, transparent 42%),
          radial-gradient(circle at 82% 18%, rgba(22, 101, 52, 0.08) 0%, transparent 40%),
          linear-gradient(160deg, #fafaf9 0%, #f5f5f4 55%, #ececea 100%)
        `,
      }}
    >
      <Box
        sx={{
          position: 'absolute',
          top: '-4rem',
          right: '-3rem',
          width: '18rem',
          height: '18rem',
          borderRadius: '9999px',
          bgcolor: 'rgba(22, 101, 52, 0.06)',
          pointerEvents: 'none',
        }}
      />
      <Box
        sx={{
          position: 'absolute',
          bottom: '-5rem',
          left: '-4rem',
          width: '22rem',
          height: '22rem',
          borderRadius: '9999px',
          bgcolor: 'rgba(22, 163, 74, 0.05)',
          pointerEvents: 'none',
        }}
      />

      <Box sx={{ position: 'relative', zIndex: 1, maxWidth: '30rem', display: 'flex', flexDirection: 'column', gap: 3.5 }}>
        <Box
          sx={{
            width: 64,
            height: 64,
            borderRadius: '1rem',
            bgcolor: systemLogoUrl ? 'transparent' : '#166534',
            color: '#fff',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: systemLogoUrl ? 'none' : '0 10px 15px -3px rgba(20, 83, 45, 0.2)',
            overflow: 'hidden',
          }}
        >
          {systemLogoUrl ? (
            <Box component="img" src={systemLogoUrl} alt={systemName} sx={{ maxWidth: '100%', maxHeight: '100%', objectFit: 'contain' }} />
          ) : (
            <CheckCircleIcon sx={{ fontSize: 36 }} />
          )}
        </Box>

        <Box>
          <Typography
            component="h2"
            sx={{
              m: 0,
              fontSize: { md: 'clamp(2rem, 3vw, 2.75rem)' },
              lineHeight: 1.15,
              fontWeight: 700,
              letterSpacing: '-0.02em',
              color: authColors.textDark,
            }}
          >
            Welcome to {systemName}
          </Typography>
          <Typography
            sx={{
              mt: 1.5,
              fontSize: '1rem',
              lineHeight: 1.7,
              color: authColors.textMuted,
            }}
          >
            Operational platform for importers, agencies, MAV partners, and evaluators — accreditation,
            compliance, and agricultural trade management in one secure workspace.
          </Typography>
        </Box>

        <Box
          aria-label="Supported user types"
          sx={{
            display: 'flex',
            flexWrap: 'wrap',
            gap: '0.75rem 1.25rem',
            pt: 0.5,
          }}
        >
          {tags.map((tag, index) => (
            <Typography
              key={tag}
              component="span"
              sx={{
                fontSize: '0.6875rem',
                fontWeight: 600,
                letterSpacing: '0.08em',
                textTransform: 'uppercase',
                color: authColors.textMuted,
                opacity: 0.85,
                '&::after': index < tags.length - 1 ? {
                  content: '"·"',
                  ml: '1.25rem',
                  opacity: 0.45,
                } : undefined,
              }}
            >
              {tag}
            </Typography>
          ))}
        </Box>
      </Box>
    </Box>
  )
}
