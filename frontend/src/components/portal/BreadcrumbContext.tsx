import { createContext, PropsWithChildren, useContext, useEffect, useMemo, useState } from 'react'
import { useLocation } from 'react-router-dom'

interface BreadcrumbContextValue {
  currentLabel: string | null
  setCurrentLabel: (label: string | null) => void
}

const BreadcrumbContext = createContext<BreadcrumbContextValue | null>(null)

export function BreadcrumbProvider({ children }: PropsWithChildren) {
  const { pathname } = useLocation()
  const [currentLabel, setCurrentLabel] = useState<string | null>(null)

  useEffect(() => {
    setCurrentLabel(null)
  }, [pathname])

  const value = useMemo(
    () => ({ currentLabel, setCurrentLabel }),
    [currentLabel],
  )

  return <BreadcrumbContext.Provider value={value}>{children}</BreadcrumbContext.Provider>
}

export function useBreadcrumbLabel(label?: string | null) {
  const context = useContext(BreadcrumbContext)

  useEffect(() => {
    if (!context || !label) {
      return undefined
    }

    context.setCurrentLabel(label)
    return () => context.setCurrentLabel(null)
  }, [context, label])
}

export function useBreadcrumbContext() {
  return useContext(BreadcrumbContext)
}
