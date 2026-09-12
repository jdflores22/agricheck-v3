import { configureStore } from '@reduxjs/toolkit'
import { healthApi } from '../features/health/api/healthApi'
import { authApi } from '../features/auth/api/authApi'
import { clientApi } from '../features/client/api/clientApi'
import { notificationsApi } from '../features/notifications/api/notificationsApi'
import { agencyApi } from '../features/agency/api/agencyApi'
import { adminApi } from '../features/admin/api/adminApi'
import { addressApi } from '../features/addresses/addressApi'
import { mavApi } from '../features/mav/api/mavApi'
import { opsApi } from '../features/ops/api/opsApi'
import { accreditationOfficerApi } from '../features/accreditation-officer/api/accreditationOfficerApi'
import { daApi } from '../features/da/api/daApi'
import { systemBrandingApi } from '../features/system/systemBrandingApi'
import { publicApi } from '../features/public/api/publicApi'
import authReducer from '../features/auth/authSlice'

export const store = configureStore({
  reducer: {
    auth: authReducer,
    [healthApi.reducerPath]: healthApi.reducer,
    [authApi.reducerPath]: authApi.reducer,
    [clientApi.reducerPath]: clientApi.reducer,
    [notificationsApi.reducerPath]: notificationsApi.reducer,
    [agencyApi.reducerPath]: agencyApi.reducer,
    [adminApi.reducerPath]: adminApi.reducer,
    [addressApi.reducerPath]: addressApi.reducer,
    [mavApi.reducerPath]: mavApi.reducer,
    [accreditationOfficerApi.reducerPath]: accreditationOfficerApi.reducer,
    [daApi.reducerPath]: daApi.reducer,
    [opsApi.reducerPath]: opsApi.reducer,
    [systemBrandingApi.reducerPath]: systemBrandingApi.reducer,
    [publicApi.reducerPath]: publicApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      healthApi.middleware,
      authApi.middleware,
      clientApi.middleware,
      notificationsApi.middleware,
      agencyApi.middleware,
      adminApi.middleware,
      addressApi.middleware,
      mavApi.middleware,
      accreditationOfficerApi.middleware,
      daApi.middleware,
      opsApi.middleware,
      systemBrandingApi.middleware,
      publicApi.middleware,
    ),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
