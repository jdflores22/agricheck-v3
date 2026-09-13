import { useEffect } from 'react'
import { useAppDispatch, useAppSelector } from '../../app/hooks'
import { selectCurrentUser } from '../auth/authSlice'
import { ensureNotificationRealtime, stopNotificationRealtime } from './notificationRealtimeManager'

export function useNotificationRealtime() {
  const dispatch = useAppDispatch()
  const user = useAppSelector(selectCurrentUser)
  const accessToken = useAppSelector((state) => state.auth.accessToken)

  useEffect(() => {
    if (!user || !accessToken) {
      void stopNotificationRealtime()
      return
    }

    ensureNotificationRealtime(accessToken, dispatch)
  }, [accessToken, dispatch, user])
}
