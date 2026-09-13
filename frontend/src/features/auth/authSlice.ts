import { createSlice, PayloadAction } from '@reduxjs/toolkit'
import type { AuthTokens, UserSummary } from './types'
import { clearOptimisticAccreditation } from '../client/optimisticAccreditation'

const ACCESS_KEY = 'agricheck_v3_access'
const REFRESH_KEY = 'agricheck_v3_refresh'
const ACCESS_EXPIRES_KEY = 'agricheck_v3_access_expires'
const REFRESH_EXPIRES_KEY = 'agricheck_v3_refresh_expires'

export interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  accessTokenExpiresAt: string | null
  refreshTokenExpiresAt: string | null
  user: UserSummary | null
  redirectPath: string | null
}

function loadToken(key: string): string | null {
  try {
    return localStorage.getItem(key)
  } catch {
    return null
  }
}

const initialState: AuthState = {
  accessToken: loadToken(ACCESS_KEY),
  refreshToken: loadToken(REFRESH_KEY),
  accessTokenExpiresAt: loadToken(ACCESS_EXPIRES_KEY),
  refreshTokenExpiresAt: loadToken(REFRESH_EXPIRES_KEY),
  user: null,
  redirectPath: null,
}

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setCredentials: (
      state,
      action: PayloadAction<{ tokens: AuthTokens; user: UserSummary; redirectPath: string }>,
    ) => {
      state.accessToken = action.payload.tokens.accessToken
      state.refreshToken = action.payload.tokens.refreshToken
      state.accessTokenExpiresAt = action.payload.tokens.accessTokenExpiresAt
      state.refreshTokenExpiresAt = action.payload.tokens.refreshTokenExpiresAt
      state.user = action.payload.user
      state.redirectPath = action.payload.redirectPath
      localStorage.setItem(ACCESS_KEY, action.payload.tokens.accessToken)
      localStorage.setItem(REFRESH_KEY, action.payload.tokens.refreshToken)
      localStorage.setItem(ACCESS_EXPIRES_KEY, action.payload.tokens.accessTokenExpiresAt)
      localStorage.setItem(REFRESH_EXPIRES_KEY, action.payload.tokens.refreshTokenExpiresAt)
    },
    setUser: (state, action: PayloadAction<UserSummary>) => {
      state.user = action.payload
    },
    logout: (state) => {
      state.accessToken = null
      state.refreshToken = null
      state.accessTokenExpiresAt = null
      state.refreshTokenExpiresAt = null
      state.user = null
      state.redirectPath = null
      localStorage.removeItem(ACCESS_KEY)
      localStorage.removeItem(REFRESH_KEY)
      localStorage.removeItem(ACCESS_EXPIRES_KEY)
      localStorage.removeItem(REFRESH_EXPIRES_KEY)
      clearOptimisticAccreditation()
    },
  },
})

export const { setCredentials, setUser, logout } = authSlice.actions
export default authSlice.reducer

export const selectIsAuthenticated = (state: { auth: AuthState }) => Boolean(state.auth.accessToken)
export const selectCurrentUser = (state: { auth: AuthState }) => state.auth.user
export const selectAuthRedirect = (state: { auth: AuthState }) => state.auth.redirectPath
