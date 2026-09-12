import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import BusinessOutlinedIcon from '@mui/icons-material/BusinessOutlined'
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined'
import CheckCircleOutlinedIcon from '@mui/icons-material/CheckCircleOutlined'
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined'
import QrCode2OutlinedIcon from '@mui/icons-material/QrCode2Outlined'
import SearchIcon from '@mui/icons-material/Search'
import ShieldOutlinedIcon from '@mui/icons-material/ShieldOutlined'
import TextSnippetOutlinedIcon from '@mui/icons-material/TextSnippetOutlined'
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined'
import {
  Box,
  Button,
  Chip,
  CircularProgress,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import type { ReactNode } from 'react'
import { FormEvent, useEffect, useMemo, useState } from 'react'
import { Link as RouterLink, useSearchParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { useSystemBranding } from '../../system/SystemBrandingProvider'
import { ClientSectionCard, clientOutlineButtonSx, clientPrimaryButtonSx } from '../components/ClientSectionCard'
import { formatCertificateDate, formatProcessTypeLabel } from '../utils/certificateUtils'
import { useVerifyCertificateQuery } from '../../public/api/publicApi'

function VerifyRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: { xs: 'column', sm: 'row' },
        alignItems: { sm: 'center' },
        gap: { xs: 0.5, sm: 2 },
        py: 1.5,
        borderBottom: '1px solid #f5f5f4',
        fontSize: '0.875rem',
        '&:last-of-type': { borderBottom: 0 },
      }}
    >
      <Typography sx={{ width: { sm: 192 }, flexShrink: 0, fontWeight: 600, color: portalColors.textMuted }}>
        {label}
      </Typography>
      <Box sx={{ color: portalColors.textDark }}>{children}</Box>
    </Box>
  )
}

function VerifyCallout({
  tone,
  title,
  children,
}: {
  tone: 'info' | 'warn' | 'danger'
  title: string
  children: ReactNode
}) {
  const styles = {
    info: { bgcolor: '#eff6ff', borderColor: '#bfdbfe', color: '#1e3a8a' },
    warn: { bgcolor: '#fffbeb', borderColor: '#fde68a', color: '#92400e' },
    danger: { bgcolor: '#fef2f2', borderColor: '#fecaca', color: '#991b1b' },
  }[tone]

  return (
    <Box
      sx={{
        border: `1px solid ${styles.borderColor}`,
        bgcolor: styles.bgcolor,
        color: styles.color,
        borderRadius: '0.625rem',
        p: 2,
        fontSize: '0.875rem',
        lineHeight: 1.45,
      }}
    >
      <Typography sx={{ mb: 0.5, fontWeight: 600 }}>{title}</Typography>
      <Box sx={{ fontSize: '0.875rem' }}>{children}</Box>
    </Box>
  )
}

function getStatusPresentation(effectiveStatus: string, isValid: boolean) {
  if (effectiveStatus === 'ACTIVE' && isValid) {
    return {
      icon: CheckCircleOutlinedIcon,
      iconBg: '#f0fdf4',
      iconColor: '#15803d',
      title: 'Certificate Verified',
      message: 'This certificate is authentic and currently valid',
      tone: 'success' as const,
    }
  }
  if (effectiveStatus === 'REVOKED') {
    return {
      icon: CancelOutlinedIcon,
      iconBg: '#fef2f2',
      iconColor: '#b91c1c',
      title: 'Certificate Revoked',
      message: 'This certificate has been revoked and is no longer valid',
      tone: 'danger' as const,
    }
  }
  if (effectiveStatus === 'EXPIRED') {
    return {
      icon: WarningAmberOutlinedIcon,
      iconBg: '#fffbeb',
      iconColor: '#b45309',
      title: 'Certificate Expired',
      message: 'This certificate has expired and is no longer valid',
      tone: 'warn' as const,
    }
  }
  return {
    icon: ErrorOutlineOutlinedIcon,
    iconBg: '#fef2f2',
    iconColor: '#b91c1c',
    title: 'Invalid Certificate',
    message: 'This certificate could not be verified',
    tone: 'danger' as const,
  }
}

export function VerifyCertificatePage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const initialCode = searchParams.get('code') ?? ''
  const [code, setCode] = useState(initialCode)
  const [searchCode, setSearchCode] = useState(initialCode.trim())
  const { systemName, systemLogoUrl } = useSystemBranding()

  useEffect(() => {
    const nextCode = (searchParams.get('code') ?? '').trim()
    setCode(nextCode)
    setSearchCode(nextCode)
  }, [searchParams])

  const { currentData, isFetching, isError, isSuccess } = useVerifyCertificateQuery(searchCode, {
    skip: !searchCode,
  })
  const result = isSuccess ? currentData?.data : undefined

  const statusPresentation = useMemo(() => {
    if (!result) return null
    return getStatusPresentation(result.effectiveStatus, result.isValid)
  }, [result])

  const verifiedAt = useMemo(
    () =>
      new Date().toLocaleString('en-US', {
        month: 'long',
        day: 'numeric',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
      }),
    [result?.certificateNumber],
  )

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    const trimmed = code.trim()
    setSearchCode(trimmed)
    if (trimmed) {
      setSearchParams({ code: trimmed })
    } else {
      setSearchParams({})
    }
  }

  const processTypeLabel = result
    ? formatProcessTypeLabel(result.processType, result.title)
    : null
  const StatusIcon = statusPresentation?.icon

  return (
    <Box sx={{ px: { xs: 2, sm: 2.5 }, py: { xs: 3, sm: 4 } }}>
      <Box sx={{ maxWidth: '42rem', mx: 'auto' }}>
        <PortalPageHeader
          eyebrow="Public Verify"
          title="Certificate Verification"
          subtitle={searchCode || 'Enter a verification code to confirm certificate authenticity'}
        />

        <ClientSectionCard title="Verification Lookup" icon={<SearchIcon />}>
          <Box component="form" onSubmit={handleSubmit}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
              <TextField
                label="Verification Code"
                value={code}
                onChange={(event) => setCode(event.target.value)}
                placeholder="e.g. FFFAB7027E964A92"
                fullWidth
                size="small"
              />
              <Button
                type="submit"
                variant="contained"
                disabled={isFetching}
                sx={{ ...clientPrimaryButtonSx, minWidth: { sm: 132 } }}
              >
                {isFetching ? <CircularProgress size={20} color="inherit" /> : 'Verify'}
              </Button>
            </Stack>
          </Box>
        </ClientSectionCard>

        {searchCode && isFetching && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
            <CircularProgress size={32} sx={{ color: portalColors.primary }} />
          </Box>
        )}

        {searchCode && !isFetching && isError && (
          <Box sx={{ mt: 2.5 }}>
            <ClientSectionCard title="Verification Result" icon={<ShieldOutlinedIcon />}>
              <Box sx={{ textAlign: 'center', py: 2.5, borderBottom: `1px solid ${portalColors.border}` }}>
                <Box
                  sx={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    width: 72,
                    height: 72,
                    borderRadius: '9999px',
                    mb: 2,
                    bgcolor: '#fef2f2',
                    color: '#b91c1c',
                  }}
                >
                  <CancelOutlinedIcon sx={{ fontSize: 36 }} />
                </Box>
                <Typography sx={{ fontSize: '1.25rem', fontWeight: 600, color: '#b91c1c', mb: 0.5 }}>
                  Certificate Not Found
                </Typography>
                <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                  No certificate found with verification code <strong>{searchCode}</strong>
                </Typography>
              </Box>
              <Box sx={{ pt: 2.5 }}>
                <VerifyCallout tone="warn" title="Certificate Not Found">
                  Please verify the code and try again. If you believe this is an error, contact the issuing authority
                  for assistance.
                </VerifyCallout>
              </Box>
            </ClientSectionCard>
          </Box>
        )}

        {searchCode && !isFetching && isSuccess && result && statusPresentation && StatusIcon && (
          <Box sx={{ mt: 2.5 }}>
            <ClientSectionCard title="Verification Result" icon={<ShieldOutlinedIcon />}>
              <Box sx={{ textAlign: 'center', py: 2.5, borderBottom: `1px solid ${portalColors.border}` }}>
                <Box
                  sx={{
                    display: 'inline-flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    width: 72,
                    height: 72,
                    borderRadius: '9999px',
                    mb: 2,
                    bgcolor: statusPresentation.iconBg,
                    color: statusPresentation.iconColor,
                  }}
                >
                  <StatusIcon sx={{ fontSize: 36 }} />
                </Box>
                <Typography sx={{ fontSize: '1.25rem', fontWeight: 600, color: portalColors.textDark, mb: 0.5 }}>
                  {statusPresentation.title}
                </Typography>
                <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, mb: 0 }}>
                  {statusPresentation.message}
                </Typography>
              </Box>

              <Stack spacing={3} sx={{ pt: 2.5 }}>
                <Box>
                  <Typography
                    sx={{
                      mb: 1.5,
                      fontSize: '1rem',
                      fontWeight: 600,
                      color: portalColors.textDark,
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1,
                    }}
                  >
                    <TextSnippetOutlinedIcon sx={{ fontSize: 18, color: '#16a34a' }} />
                    Certificate Details
                  </Typography>
                  <VerifyRow label="Certificate Number">
                    <Typography sx={{ fontWeight: 600 }}>{result.certificateNumber}</Typography>
                  </VerifyRow>
                  <VerifyRow label="Certificate Type">{processTypeLabel}</VerifyRow>
                  <VerifyRow label="Issue Date">{formatCertificateDate(result.issuedAt)}</VerifyRow>
                  {result.expiresAt && (
                    <VerifyRow label="Expiration Date">{formatCertificateDate(result.expiresAt)}</VerifyRow>
                  )}
                  <VerifyRow label="Status">
                    <Chip
                      size="small"
                      label={result.effectiveStatus.charAt(0) + result.effectiveStatus.slice(1).toLowerCase()}
                      sx={portalStatusChipSx(result.effectiveStatus)}
                    />
                  </VerifyRow>
                </Box>

                {result.companyName && (
                  <Box>
                    <Typography
                      sx={{
                        mb: 1.5,
                        fontSize: '1rem',
                        fontWeight: 600,
                        color: portalColors.textDark,
                        display: 'flex',
                        alignItems: 'center',
                        gap: 1,
                      }}
                    >
                      <BusinessOutlinedIcon sx={{ fontSize: 18, color: '#16a34a' }} />
                      Certificate Holder
                    </Typography>
                    <VerifyRow label="Company Name">
                      <Typography sx={{ fontWeight: 600 }}>{result.companyName}</Typography>
                    </VerifyRow>
                    {result.companyType && result.companyType !== 'N/A' && (
                      <VerifyRow label="Company Type">{result.companyType}</VerifyRow>
                    )}
                    {result.accreditationNumber && result.accreditationNumber !== 'N/A' && (
                      <VerifyRow label="Registration Number">{result.accreditationNumber}</VerifyRow>
                    )}
                  </Box>
                )}

                <Box>
                  <Typography
                    sx={{
                      mb: 1.5,
                      fontSize: '1rem',
                      fontWeight: 600,
                      color: portalColors.textDark,
                      display: 'flex',
                      alignItems: 'center',
                      gap: 1,
                    }}
                  >
                    <ShieldOutlinedIcon sx={{ fontSize: 18, color: '#16a34a' }} />
                    Issuing Authority
                  </Typography>
                  <Box
                    sx={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 2,
                      p: 2,
                      borderRadius: '0.5rem',
                      border: `1px solid ${portalColors.border}`,
                      bgcolor: portalColors.bgMuted,
                    }}
                  >
                    {systemLogoUrl ? (
                      <Box component="img" src={systemLogoUrl} alt={systemName} sx={{ width: 56, height: 56, objectFit: 'contain' }} />
                    ) : (
                      <Box
                        sx={{
                          width: 56,
                          height: 56,
                          borderRadius: '9999px',
                          bgcolor: portalColors.primary,
                          color: '#fff',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                        }}
                      >
                        <ShieldOutlinedIcon />
                      </Box>
                    )}
                    <Box>
                      <Typography sx={{ fontWeight: 600, color: portalColors.textDark }}>{systemName}</Typography>
                      <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted }}>
                        Official Certificate Issuing Authority
                      </Typography>
                    </Box>
                  </Box>
                </Box>

                {result.effectiveStatus === 'REVOKED' && (
                  <VerifyCallout tone="danger" title="Revocation Information">
                    {result.revokedAt && (
                      <Typography sx={{ mb: 1 }}>
                        <strong>Revoked On:</strong> {formatCertificateDate(result.revokedAt)}
                      </Typography>
                    )}
                    {result.revocationReason && (
                      <Typography sx={{ mb: 1 }}>
                        <strong>Reason:</strong> {result.revocationReason}
                      </Typography>
                    )}
                    <Typography sx={{ mb: 0 }}>
                      This certificate has been officially revoked and should not be accepted as valid.
                    </Typography>
                  </VerifyCallout>
                )}

                {result.qrCodeData && (
                  <Box sx={{ textAlign: 'center' }}>
                    <Typography
                      sx={{
                        mb: 1.5,
                        fontSize: '1rem',
                        fontWeight: 600,
                        color: portalColors.textDark,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        gap: 1,
                      }}
                    >
                      <QrCode2OutlinedIcon sx={{ fontSize: 18, color: '#16a34a' }} />
                      Verification QR Code
                    </Typography>
                    <Box
                      sx={{
                        display: 'inline-block',
                        p: 2,
                        border: `1px solid ${portalColors.border}`,
                        borderRadius: '0.75rem',
                        bgcolor: '#fff',
                      }}
                    >
                      <Box
                        component="img"
                        src={result.qrCodeData}
                        alt="Certificate QR Code"
                        sx={{ width: 192, height: 192, objectFit: 'contain' }}
                      />
                    </Box>
                    <Typography sx={{ mt: 2, fontSize: '0.875rem', color: portalColors.textMuted }}>
                      Scan this QR code to verify this certificate
                    </Typography>
                  </Box>
                )}

                <VerifyCallout tone="info" title="Privacy Notice">
                  For data privacy and security reasons, the full certificate document is only accessible to the
                  certificate holder. This verification page confirms authenticity and current status.
                </VerifyCallout>

                <VerifyCallout tone="info" title="Verification Notice">
                  This verification was performed on {verifiedAt}. Certificate status may change over time.
                </VerifyCallout>
              </Stack>
            </ClientSectionCard>
          </Box>
        )}

        <Box sx={{ textAlign: 'center', mt: 4 }}>
          <Button component={RouterLink} to="/" startIcon={<ArrowBackIcon />} sx={clientOutlineButtonSx}>
            Back to Home
          </Button>
        </Box>
      </Box>
    </Box>
  )
}
