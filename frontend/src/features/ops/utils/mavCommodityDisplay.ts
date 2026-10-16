export function formatMavLibraryCommodity(item: {
  mavLibraryLinked?: boolean
  mavHsCode?: string | null
  mavCommodityName?: string | null
  mavHsLabel?: string | null
  mavCategoryName?: string | null
  volumeKg?: number | null
}): { primary: string; secondary?: string } {
  if (!item.mavLibraryLinked) {
    return {
      primary: 'Not in MAV library',
      secondary: 'Entry must use MAV HS picker on the import form',
    }
  }

  const primary = [item.mavHsCode, item.mavCommodityName || item.mavHsLabel].filter(Boolean).join(' · ')
  const secondaryParts = [item.mavCategoryName, item.volumeKg != null ? `${item.volumeKg} kg` : null].filter(Boolean)
  return {
    primary: primary || '—',
    secondary: secondaryParts.length ? secondaryParts.join(' · ') : undefined,
  }
}
