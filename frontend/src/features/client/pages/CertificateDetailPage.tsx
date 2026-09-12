import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import DownloadOutlinedIcon from '@mui/icons-material/DownloadOutlined'
import EmojiEventsOutlinedIcon from '@mui/icons-material/EmojiEventsOutlined'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import QrCode2OutlinedIcon from '@mui/icons-material/QrCode2Outlined'
import ShieldOutlinedIcon from '@mui/icons-material/ShieldOutlined'
import TextSnippetOutlinedIcon from '@mui/icons-material/TextSnippetOutlined'
import { Alert, Box, Button, Chip, Stack, Typography } from '@mui/material'
import { Link as RouterLink, useParams } from 'react-router-dom'
import { PortalPageHeader } from '../../../components/portal/PortalPageHeader'
import { portalStatusChipSx } from '../../../components/portal/PortalTablePanel'
import { portalColors } from '../../../components/portal/portalTheme'
import { useBreadcrumbLabel } from '../../../components/portal/BreadcrumbContext'
import { useGetCertificateQuery } from '../api/clientApi'
import {
  ClientDetailGrid,
  ClientDetailItem,
  ClientSectionCard,
  clientBadgeSx,
  clientOutlineButtonSx,
  clientPrimaryButtonSx,
} from '../components/ClientSectionCard'
import { CertificatePdfPreview } from '../components/CertificatePdfPreview'
import { downloadAuthenticatedFile } from '../utils/downloadFile'
import {
  formatCertificateDate,
  formatProcessTypeLabel,
  getCertificateStatusLabel,
  getEffectiveCertificateStatus,
  parseCertificateSummary,
} from '../utils/certificateUtils'

export function CertificateDetailPage() {
  const { uuid = '' } = useParams()
  const { data, isLoading } = useGetCertificateQuery(uuid, { skip: !uuid })
  const cert = data?.data

  useBreadcrumbLabel(cert?.certificateNumber)

  if (isLoading) {
    return <Typography sx={{ color: portalColors.textMuted }}>Loading certificate…</Typography>
  }

  if (!cert) {
    return <Alert severity="error">Certificate not found.</Alert>
  }

  const summary = parseCertificateSummary(cert.summaryJson)
  const effectiveStatus = getEffectiveCertificateStatus(cert.status, cert.expiresAt)
  const statusLabel = getCertificateStatusLabel(effectiveStatus)
  const processTypeLabel = formatProcessTypeLabel(summary?.processType, cert.title)
  const issuedLabel = formatCertificateDate(cert.issuedAt)
  const expiresLabel = formatCertificateDate(cert.expiresAt)
  const isExpired = effectiveStatus === 'EXPIRED'
  const verifyUrl = `/verify?code=${encodeURIComponent(cert.verificationCode)}`
  const hasSidebarDetails = Boolean(summary?.companyName || summary?.accreditationNumber || cert.entryReferenceNo)

  return (
    <Box>
      <PortalPageHeader
        eyebrow="Certificates"
        title="Certificate Details"
        subtitle={cert.certificateNumber}
        actions={
          <Button
            component={RouterLink}
            to="/client/certificates"
            variant="outlined"
            startIcon={<ArrowBackIcon />}
            sx={clientOutlineButtonSx}
          >
            Back to List
          </Button>
        }
      />

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', lg: '2fr 1fr' },
          gap: 2.5,
          alignItems: 'start',
        }}
      >
        <Stack spacing={2.5}>
          <ClientSectionCard
            title="Certificate Information"
            icon={<InfoOutlinedIcon />}
            headerAction={
              <Chip size="small" label={statusLabel} sx={portalStatusChipSx(effectiveStatus)} />
            }
          >
            <ClientDetailGrid>
              <ClientDetailItem label="Certificate Number">
                <Box component="span" sx={clientBadgeSx}>
                  {cert.certificateNumber}
                </Box>
              </ClientDetailItem>
              <ClientDetailItem label="Certificate Type">
                <Box component="span" sx={clientBadgeSx}>
                  {processTypeLabel}
                </Box>
              </ClientDetailItem>
              <ClientDetailItem label="Issue Date">
                <Typography sx={{ m: 0, color: portalColors.textDark }}>{issuedLabel}</Typography>
              </ClientDetailItem>
              {expiresLabel && (
                <ClientDetailItem label="Expiration Date">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                    <Typography sx={{ m: 0, color: portalColors.textDark }}>{expiresLabel}</Typography>
                    {isExpired && (
                      <Chip size="small" label="Expired" sx={portalStatusChipSx('Expired')} />
                    )}
                  </Box>
                </ClientDetailItem>
              )}
              {cert.title && (
                <ClientDetailItem label="Title">
                  <Typography sx={{ m: 0, color: portalColors.textDark }}>{cert.title}</Typography>
                </ClientDetailItem>
              )}
              <ClientDetailItem label="Verification Code">
                <Typography sx={{ m: 0, fontWeight: 600, color: portalColors.textDark }}>
                  {cert.verificationCode}
                </Typography>
              </ClientDetailItem>
            </ClientDetailGrid>

            {effectiveStatus === 'REVOKED' && (
              <Alert severity="error" sx={{ mt: 2.5 }}>
                This certificate has been revoked and is no longer valid.
              </Alert>
            )}

            <Stack
              direction={{ xs: 'column', sm: 'row' }}
              spacing={1.5}
              sx={{
                mt: 3,
                pt: 2.5,
                borderTop: '1px solid #f5f5f4',
              }}
            >
              <Button
                variant="contained"
                startIcon={<DownloadOutlinedIcon />}
                sx={{ ...clientPrimaryButtonSx, flex: { sm: 1 } }}
                onClick={() =>
                  downloadAuthenticatedFile(
                    `/certificates/${uuid}/pdf`,
                    `Certificate_${processTypeLabel.replace(/\s+/g, '_')}_${cert.certificateNumber}.pdf`,
                  )
                }
              >
                Download PDF
              </Button>
              <Button
                component="a"
                href={verifyUrl}
                target="_blank"
                rel="noopener noreferrer"
                variant="outlined"
                startIcon={<ShieldOutlinedIcon />}
                sx={{ ...clientOutlineButtonSx, flex: { sm: 1 } }}
              >
                Verify Certificate
              </Button>
            </Stack>
          </ClientSectionCard>

          <CertificatePdfPreview uuid={uuid} />
        </Stack>

        <Stack spacing={2.5}>
          {cert.qrCodeData && (
            <ClientSectionCard title="Verification QR Code" icon={<QrCode2OutlinedIcon />}>
              <Box sx={{ textAlign: 'center' }}>
                <Box
                  sx={{
                    display: 'inline-block',
                    p: 2,
                    border: `1px solid ${portalColors.border}`,
                    borderRadius: '0.75rem',
                    bgcolor: portalColors.bgWhite,
                  }}
                >
                  <Box
                    component="img"
                    src={cert.qrCodeData}
                    alt="Certificate QR Code"
                    sx={{ width: 192, height: 192, objectFit: 'contain' }}
                  />
                </Box>
                <Typography sx={{ mt: 2, mb: 0, fontSize: '0.875rem', color: portalColors.textMuted }}>
                  Scan this QR code to verify the certificate authenticity
                </Typography>
              </Box>
            </ClientSectionCard>
          )}

          {hasSidebarDetails && (
            <ClientSectionCard title="Certificate Details" icon={<TextSnippetOutlinedIcon />}>
              <Stack spacing={2.5}>
                {summary?.companyName && (
                  <Box>
                    <Typography sx={{ mb: 1.5, fontSize: '0.75rem', fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase', color: portalColors.textMuted }}>
                      Company Information
                    </Typography>
                    <Box>
                      <Typography sx={{ fontSize: '0.875rem', fontWeight: 500, color: portalColors.textDark }}>
                        Name
                      </Typography>
                      <Typography sx={{ mt: 0.25, mb: 0, fontSize: '0.875rem', color: portalColors.textDark }}>
                        {summary.companyName}
                      </Typography>
                    </Box>
                  </Box>
                )}

                {(summary?.accreditationNumber || summary?.processType === 'Accreditation') && (
                  <Box sx={{ pt: summary?.companyName ? 2 : 0, borderTop: summary?.companyName ? '1px solid #f5f5f4' : undefined }}>
                    <Typography sx={{ mb: 1.5, fontSize: '0.75rem', fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase', color: portalColors.textMuted }}>
                      Accreditation Information
                    </Typography>
                    <Stack spacing={1.5}>
                      <Box>
                        <Typography sx={{ fontSize: '0.875rem', fontWeight: 500, color: '#44403c' }}>Type</Typography>
                        <Typography sx={{ mt: 0.25, mb: 0, fontSize: '0.875rem' }}>{processTypeLabel}</Typography>
                      </Box>
                      {summary?.accreditationNumber && (
                        <Box>
                          <Typography sx={{ fontSize: '0.875rem', fontWeight: 500, color: '#44403c' }}>Number</Typography>
                          <Typography sx={{ mt: 0.25, mb: 0, fontSize: '0.875rem' }}>{summary.accreditationNumber}</Typography>
                        </Box>
                      )}
                    </Stack>
                  </Box>
                )}

                {cert.entryReferenceNo && (
                  <Box sx={{ pt: 2, borderTop: '1px solid #f5f5f4' }}>
                    <Typography sx={{ mb: 1.5, fontSize: '0.75rem', fontWeight: 600, letterSpacing: '0.06em', textTransform: 'uppercase', color: portalColors.textMuted }}>
                      Entry Information
                    </Typography>
                    <Box>
                      <Typography sx={{ fontSize: '0.875rem', fontWeight: 500, color: '#44403c' }}>Entry Reference</Typography>
                      <Typography sx={{ mt: 0.25, mb: 0, fontSize: '0.875rem' }}>{cert.entryReferenceNo}</Typography>
                    </Box>
                  </Box>
                )}
              </Stack>
            </ClientSectionCard>
          )}

          {!cert.qrCodeData && !hasSidebarDetails && (
            <ClientSectionCard title="Certificate" icon={<EmojiEventsOutlinedIcon />}>
              <Typography sx={{ fontSize: '0.875rem', color: portalColors.textMuted, m: 0 }}>
                Use Download PDF or Verify Certificate to share this document with agencies and partners.
              </Typography>
            </ClientSectionCard>
          )}
        </Stack>
      </Box>
    </Box>
  )
}
