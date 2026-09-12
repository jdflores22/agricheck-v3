import PictureAsPdfOutlinedIcon from '@mui/icons-material/PictureAsPdfOutlined'
import { Alert, Box, CircularProgress } from '@mui/material'
import { useEffect, useState } from 'react'
import { portalColors } from '../../../components/portal/portalTheme'
import { fetchAuthenticatedFile, revokeAuthenticatedFileUrl } from '../utils/authenticatedFile'
import { ClientSectionCard } from './ClientSectionCard'

export function CertificatePdfPreview({ uuid }: { uuid: string }) {
  const [pdfUrl, setPdfUrl] = useState<string | null>(null)
  const [error, setError] = useState(false)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true
    let objectUrl: string | null = null

    setLoading(true)
    setError(false)
    setPdfUrl(null)

    fetchAuthenticatedFile(`/certificates/${uuid}/pdf`)
      .then((file) => {
        if (!active) {
          revokeAuthenticatedFileUrl(file.objectUrl)
          return
        }
        objectUrl = file.objectUrl
        setPdfUrl(file.objectUrl)
      })
      .catch(() => {
        if (active) setError(true)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
      if (objectUrl) revokeAuthenticatedFileUrl(objectUrl)
    }
  }, [uuid])

  return (
    <ClientSectionCard title="Certificate Preview" icon={<PictureAsPdfOutlinedIcon />}>
      <Box sx={{ borderRadius: '0.5rem', overflow: 'hidden', bgcolor: portalColors.bgMuted }}>
        {loading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 10 }}>
            <CircularProgress size={32} sx={{ color: '#166534' }} />
          </Box>
        )}
        {error && (
          <Alert severity="warning" sx={{ m: 0, borderRadius: 0 }}>
            Unable to load certificate preview. Use Download PDF instead.
          </Alert>
        )}
        {!loading && !error && pdfUrl && (
          <Box
            component="iframe"
            src={pdfUrl}
            title="Certificate PDF Preview"
            sx={{
              display: 'block',
              width: '100%',
              height: { xs: '28rem', md: '50rem' },
              maxHeight: '80vh',
              border: 0,
              bgcolor: portalColors.bgMuted,
            }}
          />
        )}
      </Box>
    </ClientSectionCard>
  )
}
