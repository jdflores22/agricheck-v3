/** Truck marker styling aligned with AgriTrack mobile (blue delivery truck). */
export function createDaTruckMapIcon(googleMaps: typeof google.maps, offline = false): google.maps.Icon {
  const fill = offline ? '#94A3B8' : '#2563EB'
  const svg = `
<svg xmlns="http://www.w3.org/2000/svg" width="44" height="44" viewBox="0 0 44 44">
  <circle cx="22" cy="22" r="20" fill="#ffffff" stroke="${fill}" stroke-width="3"/>
  <g transform="translate(8,10)" fill="${fill}">
    <path d="M0 12h11.5l2.4-4H16V6H9.2L6.8 12H0v8h2.2a3.3 3.3 0 0 0 6.4 0h7a3.3 3.3 0 0 0 6.4 0H26V12H0zm4 10.5a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm13 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zM14 10h5.8l1.6 2H14V10z"/>
  </g>
</svg>`.trim()

  return {
    url: `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`,
    scaledSize: new googleMaps.Size(44, 44),
    anchor: new googleMaps.Point(22, 22),
  }
}

export function createDaStopMapIcon(
  googleMaps: typeof google.maps,
  label: string,
  color: string,
): google.maps.Icon {
  const text = label.slice(0, 1).toUpperCase()
  const svg = `
<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 32 32">
  <circle cx="16" cy="16" r="14" fill="${color}" stroke="#ffffff" stroke-width="3"/>
  <text x="16" y="21" text-anchor="middle" font-size="14" font-weight="800" fill="#ffffff" font-family="Arial,sans-serif">${text}</text>
</svg>`.trim()

  return {
    url: `data:image/svg+xml;charset=UTF-8,${encodeURIComponent(svg)}`,
    scaledSize: new googleMaps.Size(32, 32),
    anchor: new googleMaps.Point(16, 16),
  }
}
