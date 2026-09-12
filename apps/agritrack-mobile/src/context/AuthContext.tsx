import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { AppState } from 'react-native'
import { login as loginRequest, syncPull } from '../api/agritrackApi'
import {
  detectNewAssignments,
  registerPushNotifications,
  unregisterPushNotifications,
} from '../services/pushNotifications'
import { clearSession, loadSession, saveSession } from '../storage/session'
import type { AuthUser } from '../types/api'

interface AuthContextValue {
  user: AuthUser | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  hasRole: (role: string) => boolean
  isOperator: boolean
  isDriver: boolean
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadSession()
      .then(async (session) => {
        setUser(session.user)
        if (session.user) {
          await registerPushNotifications()
        }
      })
      .finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    if (!user) {
      return
    }

    let initial = true
    const pollAssignments = async () => {
      try {
        const pull = await syncPull()
        await detectNewAssignments(pull.containers, initial)
        initial = false
      } catch {
        // Ignore polling failures while offline.
      }
    }

    void pollAssignments()
    const timer = setInterval(() => {
      void pollAssignments()
    }, 60_000)

    const appStateSub = AppState.addEventListener('change', (state) => {
      if (state === 'active') {
        void pollAssignments()
      }
    })

    return () => {
      clearInterval(timer)
      appStateSub.remove()
    }
  }, [user])

  const login = useCallback(async (email: string, password: string) => {
    const result = await loginRequest(email, password)
    await saveSession(result.tokens, result.user)
    setUser(result.user)
    await registerPushNotifications()
  }, [])

  const logout = useCallback(async () => {
    await unregisterPushNotifications()
    await clearSession()
    setUser(null)
  }, [])

  const hasRole = useCallback((role: string) => user?.roles.includes(role) ?? false, [user])

  const value = useMemo(
    () => ({
      user,
      loading,
      login,
      logout,
      hasRole,
      isOperator: hasRole('ROLE_OPERATOR') || hasRole('ROLE_ADMIN'),
      isDriver: hasRole('ROLE_DRIVER') || hasRole('ROLE_OPERATOR') || hasRole('ROLE_ADMIN'),
    }),
    [user, loading, login, logout, hasRole],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
