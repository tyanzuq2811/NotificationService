import { ref } from 'vue'
import * as signalR from '@microsoft/signalr'
import { SIGNALR_HUB_URL } from '../api/httpClient'
import { toast } from 'vue-sonner'

export function useSignalR() {
  const isConnected = ref(false)
  const isConnecting = ref(false)
  const activeUserId = ref('user_demo')
  let hubConnection = null

  async function connect(userId, onNotificationReceived, onStateChange) {
    if (hubConnection) {
      try {
        await hubConnection.stop()
      } catch (e) {
        console.warn('Stopping previous connection:', e)
      }
    }

    activeUserId.value = userId || 'user_demo'
    isConnecting.value = true

    hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${SIGNALR_HUB_URL}?userId=${activeUserId.value}`)
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    hubConnection.on('ReceiveNotification', (notification) => {
      if (onNotificationReceived) {
        onNotificationReceived(notification)
      }
    })

    hubConnection.onreconnecting((error) => {
      isConnected.value = false
      if (onStateChange) onStateChange(false)
      console.warn('SignalR reconnecting...', error)
    })

    hubConnection.onreconnected((connectionId) => {
      isConnected.value = true
      if (onStateChange) onStateChange(true)
      toast.success('SignalR đã tự động kết nối lại thành công!')
      console.log('SignalR reconnected:', connectionId)
    })

    hubConnection.onclose((error) => {
      isConnected.value = false
      if (onStateChange) onStateChange(false)
      console.warn('SignalR closed:', error)
    })

    try {
      await hubConnection.start()
      isConnected.value = true
      isConnecting.value = false
      if (onStateChange) onStateChange(true)
      console.log(`SignalR connected to group 'user_${activeUserId.value}'`)
    } catch (err) {
      isConnected.value = false
      isConnecting.value = false
      if (onStateChange) onStateChange(false)
      console.error('SignalR start failed:', err)
    }
  }

  async function disconnect() {
    if (hubConnection) {
      try {
        await hubConnection.stop()
      } catch (e) {
        console.warn('Disconnect error:', e)
      } finally {
        isConnected.value = false
      }
    }
  }

  return {
    isConnected,
    isConnecting,
    activeUserId,
    connect,
    disconnect
  }
}
