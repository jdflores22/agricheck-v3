declare global {
  interface Window {
    __AGRICHECK_GOOGLE_MAPS_API_KEY__?: string
  }
}

let loadPromise: Promise<void> | null = null

export function getGoogleMapsApiKey(): string {
  if (typeof window !== 'undefined' && window.__AGRICHECK_GOOGLE_MAPS_API_KEY__) {
    return window.__AGRICHECK_GOOGLE_MAPS_API_KEY__.trim()
  }
  return (import.meta.env.VITE_GOOGLE_MAPS_API_KEY ?? '').trim()
}

export function loadGoogleMapsScript(): Promise<void> {
  const key = getGoogleMapsApiKey()
  if (!key) {
    return Promise.reject(new Error('GOOGLE_MAPS_KEY_MISSING'))
  }
  if (typeof google !== 'undefined' && google.maps) {
    return Promise.resolve()
  }
  if (loadPromise) return loadPromise

  loadPromise = new Promise((resolve, reject) => {
    const existing = document.querySelector<HTMLScriptElement>('script[data-agricheck-gmaps]')
    if (existing) {
      existing.addEventListener('load', () => resolve())
      existing.addEventListener('error', () => reject(new Error('GOOGLE_MAPS_LOAD_FAILED')))
      return
    }
    const script = document.createElement('script')
    script.dataset.agricheckGmaps = '1'
    script.async = true
    script.defer = true
    script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(key)}&v=weekly`
    script.onload = () => resolve()
    script.onerror = () => reject(new Error('GOOGLE_MAPS_LOAD_FAILED'))
    document.head.appendChild(script)
  })

  return loadPromise
}
