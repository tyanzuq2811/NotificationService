import { ref, onMounted } from 'vue'
import { commandApi } from '../api/httpClient'
import { toast } from 'vue-sonner'

export function useWebPush(userIdRef) {
  const isSubscribed = ref(false)
  const isSupported = ref(false)
  const isRegistering = ref(false)

  const VAPID_PUBLIC_KEY = import.meta.env.VITE_VAPID_PUBLIC_KEY || 'BNjt2n6zUqd5yS9ORdDmRNxr16jH07LBTxdFjlj8m0eb167uR3uRC-uD_EAUean3NqbOoFTdU-uOzN1FiqQIf5E'

  function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4)
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/')
    const rawData = window.atob(base64)
    const outputArray = new Uint8Array(rawData.length)
    for (let i = 0; i < rawData.length; ++i) {
      outputArray[i] = rawData.charCodeAt(i)
    }
    return outputArray
  }

  async function checkSupport(userId) {
    isSupported.value = typeof window !== 'undefined' && 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window
    if (isSupported.value && Notification.permission === 'granted') {
      try {
        const reg = await navigator.serviceWorker.ready
        const sub = await reg.pushManager.getSubscription()
        if (sub) {
          isSubscribed.value = true
        }
      } catch (e) {
        console.warn('Check push subscription failed:', e)
      }
    }
  }

  async function checkStatus(userId) {
    return checkSupport(userId)
  }

  async function subscribe(overrideUserId) {
    if (!isSupported.value) {
      toast.error('Trình duyệt của bạn không hỗ trợ Web Push W3C.')
      return false
    }

    isRegistering.value = true
    try {
      const permission = await Notification.requestPermission()
      if (permission !== 'granted') {
        toast.error('Bạn đã từ chối quyền nhận thông báo trên trình duyệt. Vui lòng cấp quyền trong cài đặt site.')
        return false
      }

      const reg = await navigator.serviceWorker.register('/sw.js')
      await navigator.serviceWorker.ready

      let sub = await reg.pushManager.getSubscription()
      if (sub) {
        await sub.unsubscribe()
      }

      const convertedKey = urlBase64ToUint8Array(VAPID_PUBLIC_KEY)
      sub = await reg.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: convertedKey
      })

      const p256dh = btoa(String.fromCharCode.apply(null, new Uint8Array(sub.getKey('p256dh'))))
      const auth = btoa(String.fromCharCode.apply(null, new Uint8Array(sub.getKey('auth'))))

      const targetUserId = overrideUserId || (typeof userIdRef === 'function' ? userIdRef() : (userIdRef?.value || 'user_demo'))

      await commandApi.post('/webpush/subscribe', {
        userId: targetUserId,
        endpoint: sub.endpoint,
        p256dh,
        auth
      })

      isSubscribed.value = true
      toast.success('Đã bật Thông báo Đẩy Windows Action Center thành công!')

      // Pop immediate confirmation notification
      try {
        await reg.showNotification('Hệ Thống Thông Báo CQRS', {
          body: 'Đã kết nối thành công với Windows Action Center qua VAPID Service Worker!',
          icon: '/favicon.svg',
          badge: '/favicon.svg'
        })
      } catch (e) {
        console.warn('Local preview notification:', e)
      }

      return true
    } catch (err) {
      console.error('Web Push subscription error:', err)
      toast.error('Lỗi kích hoạt Web Push: ' + (err.message || 'Thất bại'))
      return false
    } finally {
      isRegistering.value = false
    }
  }

  onMounted(() => {
    checkSupport()
  })

  return {
    isSubscribed,
    isSupported,
    isRegistering,
    subscribe,
    checkSupport,
    checkStatus
  }
}
