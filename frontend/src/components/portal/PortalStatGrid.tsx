import { Box } from '@mui/material'
import { PortalStatCard } from './PortalStatCard'

export interface PortalStatItem {
  key: string
  label: string
  caption?: string
}

interface PortalStatGridProps {
  items: readonly PortalStatItem[]
  stats?: Record<string, number | undefined>
  isLoading?: boolean
  columns?: { xs?: string; sm?: string; md?: string; xl?: string }
}

export function PortalStatGrid({
  items,
  stats,
  isLoading,
  columns = { xs: '1fr 1fr', md: 'repeat(3, 1fr)', xl: 'repeat(4, 1fr)' },
}: PortalStatGridProps) {
  return (
    <Box
      sx={{
        display: 'grid',
        gap: 2,
        gridTemplateColumns: columns,
      }}
    >
      {items.map((item) => (
        <PortalStatCard
          key={item.key}
          label={item.label}
          value={isLoading ? '…' : (stats?.[item.key] ?? 0)}
          caption={item.caption}
        />
      ))}
    </Box>
  )
}
