import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import type { AppDispatch } from '../../app/store'
import { getHubUrl } from '../../app/apiBase'
import { notificationsApi, type NotificationItem } from './api/notificationsApi'

type NotificationReceivedPayload = {
  notification: NotificationItem
  unreadCount: number
}

let connection: HubConnection | null = null
let activeToken: string | null = null
let currentToken: string | null = null
let dispatchRef: AppDispatch | null = null
let starting = false

function attachHandlers(conn: HubConnection) {
  conn.off('notificationReceived')
  conn.on('notificationReceived', (payload: NotificationReceivedPayload) => {
    if (!dispatchRef) return
    dispatchRef(
      notificationsApi.util.updateQueryData('getUnreadCount', undefined, (draft) => {
        if (draft?.data) {
          draft.data.count = payload.unreadCount
        }
      }),
    )
    dispatchRef(notificationsApi.util.invalidateTags(['Notifications']))
  })
}

async function startConnection(token: string) {
  if (starting) return
  if (
    connection
    && activeToken === token
    && (connection.state === HubConnectionState.Connected || connection.state === HubConnectionState.Connecting)
  ) {
    return
  }

  starting = true
  try {
    if (connection) {
      const previous = connection
      connection = null
      activeToken = null
      try {
        await previous.stop()
      } catch {
        // Ignore stop errors while replacing an existing connection.
      }
    }

    activeToken = token
    currentToken = token
    const next = new HubConnectionBuilder()
      .withUrl(getHubUrl('/hubs/notifications'), {
        accessTokenFactory: () => currentToken ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
      .build()

    attachHandlers(next)
    connection = next
    await next.start()
  } catch {
    if (connection?.state !== HubConnectionState.Connected) {
      connection = null
      activeToken = null
    }
  } finally {
    starting = false
  }
}

export function ensureNotificationRealtime(token: string, dispatch: AppDispatch) {
  dispatchRef = dispatch
  currentToken = token
  void startConnection(token)
}

export async function stopNotificationRealtime() {
  activeToken = null
  currentToken = null
  dispatchRef = null
  starting = false

  if (!connection) return

  const current = connection
  connection = null
  try {
    await current.stop()
  } catch {
    // Ignore stop errors during logout or token rotation.
  }
}
