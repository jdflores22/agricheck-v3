import { Alert, Button, Stack, Typography } from '@mui/material'
import { useRef, useState } from 'react'
import {
  useGetDoctorBorderPhotoCountQuery,
  useUploadDoctorBorderInspectionPhotoMutation,
} from '../api/opsApi'
import { portalOutlinedButtonSx } from '../../../components/portal/portalStyles'

const MIN_PHOTOS = 5

type DoctorBorderInspectionPhotosProps = {
  containerUuid: string
}

export function DoctorBorderInspectionPhotos({ containerUuid }: DoctorBorderInspectionPhotosProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [uploading, setUploading] = useState(false)
  const { data, refetch } = useGetDoctorBorderPhotoCountQuery(containerUuid)
  const [uploadPhoto] = useUploadDoctorBorderInspectionPhotoMutation()

  const count = data?.data ?? 0

  const handleFiles = async (files: FileList | null) => {
    if (!files?.length) return
    setUploadError(null)
    setUploading(true)
    try {
      const position = await new Promise<GeolocationPosition>((resolve, reject) => {
        navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: true, timeout: 15000 })
      })
      for (const file of Array.from(files)) {
        await uploadPhoto({
          uuid: containerUuid,
          file,
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        }).unwrap()
      }
      await refetch()
    } catch {
      setUploadError('Could not upload photos. Allow location access and ensure the container is Under Inspection.')
    } finally {
      setUploading(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  return (
    <Stack spacing={1}>
      <Typography variant="subtitle2">Border inspection geotag photos</Typography>
      <Typography variant="body2" color="text.secondary">
        Upload at least {MIN_PHOTOS} geotagged photos (same as AgriTrack driver proofs). Required before approving inspection.
      </Typography>
      <Typography variant="body2" sx={{ fontWeight: 600 }}>
        Uploaded: {count} / {MIN_PHOTOS}
      </Typography>
      {uploadError && <Alert severity="error">{uploadError}</Alert>}
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        capture="environment"
        multiple
        hidden
        onChange={(e) => void handleFiles(e.target.files)}
      />
      <Button
        variant="outlined"
        sx={portalOutlinedButtonSx}
        disabled={uploading}
        onClick={() => inputRef.current?.click()}
      >
        {uploading ? 'Uploading…' : 'Add geotagged photos'}
      </Button>
    </Stack>
  )
}
