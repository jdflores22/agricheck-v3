import { Box, CircularProgress } from '@mui/material'
import { createContext, PropsWithChildren, useCallback, useContext, useMemo, useState } from 'react'
import { resolveBrandingAssetUrl, useGetSystemBrandingQuery } from './systemBrandingApi'

interface SystemBrandingContextValue {
  systemName: string
  systemLogoUrl: string | null
  spinnerLogoUrl: string | null
  spinnerColor: string
  primaryColor: string
  footerText: string
  showLoadingSpinner: () => void
  hideLoadingSpinner: () => void
  loadingVisible: boolean
}

const SystemBrandingContext = createContext<SystemBrandingContextValue | null>(null)

function GlobalLoadingSpinner({
  visible,
  spinnerColor,
  spinnerLogoUrl,
}: {
  visible: boolean
  spinnerColor: string
  spinnerLogoUrl: string | null
}) {
  if (!visible) return null

  return (
    <Box
      role="status"
      aria-live="polite"
      aria-label="Loading"
      sx={{
        position: 'fixed',
        inset: 0,
        zIndex: 9999,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: 'rgba(0, 0, 0, 0.7)',
      }}
    >
      <Box sx={{ position: 'relative', width: 120, height: 120 }}>
        <CircularProgress
          size={120}
          thickness={3}
          sx={{
            color: spinnerColor,
            position: 'absolute',
            inset: 0,
          }}
        />
        {spinnerLogoUrl ? (
          <Box
            component="img"
            src={spinnerLogoUrl}
            alt="Loading"
            sx={{
              position: 'absolute',
              top: '50%',
              left: '50%',
              transform: 'translate(-50%, -50%)',
              maxWidth: 70,
              maxHeight: 70,
              animation: 'brandingSpinnerPulse 1.5s ease-in-out infinite',
              '@keyframes brandingSpinnerPulse': {
                '0%, 100%': { opacity: 1 },
                '50%': { opacity: 0.65 },
              },
            }}
          />
        ) : null}
      </Box>
    </Box>
  )
}

export function SystemBrandingProvider({ children }: PropsWithChildren) {
  const { data } = useGetSystemBrandingQuery()
  const branding = data?.data
  const [loadingVisible, setLoadingVisible] = useState(false)

  const showLoadingSpinner = useCallback(() => setLoadingVisible(true), [])
  const hideLoadingSpinner = useCallback(() => setLoadingVisible(false), [])

  const value = useMemo<SystemBrandingContextValue>(() => ({
    systemName: branding?.systemName ?? 'AgriCheck',
    systemLogoUrl: resolveBrandingAssetUrl(branding?.systemLogoUrl),
    spinnerLogoUrl: resolveBrandingAssetUrl(branding?.spinnerLogoUrl),
    spinnerColor: branding?.spinnerColor ?? '#166534',
    primaryColor: branding?.primaryColor ?? '#166534',
    footerText: branding?.footerText ?? '',
    showLoadingSpinner,
    hideLoadingSpinner,
    loadingVisible,
  }), [branding, hideLoadingSpinner, loadingVisible, showLoadingSpinner])

  return (
    <SystemBrandingContext.Provider value={value}>
      {children}
      <GlobalLoadingSpinner
        visible={loadingVisible}
        spinnerColor={value.spinnerColor}
        spinnerLogoUrl={value.spinnerLogoUrl}
      />
    </SystemBrandingContext.Provider>
  )
}

export function useSystemBranding() {
  const context = useContext(SystemBrandingContext)
  if (!context) {
    throw new Error('useSystemBranding must be used within SystemBrandingProvider')
  }
  return context
}

export function useLoadingSpinner() {
  const { showLoadingSpinner, hideLoadingSpinner } = useSystemBranding()
  return { showLoadingSpinner, hideLoadingSpinner }
}
