import {
  CheckCircleOutlined,
  EditOutlined,
  HourglassEmptyOutlined,
  SendOutlined,
  WarningAmberOutlined,
} from '@mui/icons-material'
import { Box, Stack, Typography } from '@mui/material'
import { portalColors } from '../../../components/portal/portalTheme'

export type AccreditationHistoryEntry = {
  status: string
  comment?: string
  createdAt: string
  actorName?: string
}

function formatStatusLabel(status: string) {
  return status.replace(/([A-Z])/g, ' $1').trim()
}

function getTimelineIcon(status: string) {
  switch (status) {
    case 'Draft':
      return EditOutlined
    case 'Submitted':
    case 'PhotoUploaded':
      return SendOutlined
    case 'UnderReview':
      return HourglassEmptyOutlined
    case 'Approved':
      return CheckCircleOutlined
    case 'RevisionRequired':
    case 'Rejected':
      return WarningAmberOutlined
    default:
      return HourglassEmptyOutlined
  }
}

function getTimelineAccent(status: string) {
  switch (status) {
    case 'Approved':
      return { bg: '#f0fdf4', color: '#15803d', line: '#bbf7d0' }
    case 'Rejected':
      return { bg: '#fef2f2', color: '#b91c1c', line: '#fecaca' }
    case 'RevisionRequired':
      return { bg: '#fffbeb', color: '#b45309', line: '#fde68a' }
    case 'Submitted':
      return { bg: '#eff6ff', color: '#1d4ed8', line: '#bfdbfe' }
    default:
      return { bg: portalColors.bgMuted, color: portalColors.textDark, line: portalColors.border }
  }
}

export function AccreditationStatusTimeline({
  history,
  emptyMessage = 'No activity recorded yet.',
  showActorName = false,
}: {
  history: AccreditationHistoryEntry[]
  emptyMessage?: string
  showActorName?: boolean
}) {
  const sorted = [...history].sort(
    (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
  )

  if (sorted.length === 0) {
    return (
      <Typography variant="body2" sx={{ color: portalColors.textMuted }}>
        {emptyMessage}
      </Typography>
    )
  }

  return (
    <Stack spacing={0}>
      {sorted.map((entry, index) => {
        const Icon = getTimelineIcon(entry.status)
        const accent = getTimelineAccent(entry.status)
        const isLast = index === sorted.length - 1

        return (
          <Box
            key={`${entry.status}-${entry.createdAt}-${index}`}
            sx={{ display: 'flex', gap: 2, minHeight: isLast ? 'auto' : 72 }}
          >
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', width: 36 }}>
              <Box
                sx={{
                  width: 36,
                  height: 36,
                  borderRadius: '50%',
                  bgcolor: accent.bg,
                  color: accent.color,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  border: `1px solid ${accent.line}`,
                  flexShrink: 0,
                }}
              >
                <Icon sx={{ fontSize: 18 }} />
              </Box>
              {!isLast && (
                <Box
                  sx={{
                    width: 2,
                    flex: 1,
                    bgcolor: accent.line,
                    mt: 0.75,
                    minHeight: 24,
                  }}
                />
              )}
            </Box>

            <Box sx={{ flex: 1, pb: isLast ? 0 : 2.5, pt: 0.25 }}>
              <Stack direction="row" spacing={1} sx={{ alignItems: 'center', flexWrap: 'wrap', mb: 0.5 }}>
                <Typography sx={{ fontWeight: 600, fontSize: '0.875rem' }}>
                  {formatStatusLabel(entry.status)}
                </Typography>
              </Stack>
              {entry.comment ? (
                <Typography variant="body2" sx={{ color: portalColors.textDark, mb: 0.75 }}>
                  {entry.comment}
                </Typography>
              ) : null}
              {showActorName && entry.actorName ? (
                <Typography variant="body2" sx={{ color: portalColors.textMuted, mb: 0.75 }}>
                  Officer: {entry.actorName}
                </Typography>
              ) : null}
              <Typography variant="caption" sx={{ color: portalColors.textMuted }}>
                {new Date(entry.createdAt).toLocaleString(undefined, {
                  dateStyle: 'medium',
                  timeStyle: 'short',
                })}
              </Typography>
            </Box>
          </Box>
        )
      })}
    </Stack>
  )
}
