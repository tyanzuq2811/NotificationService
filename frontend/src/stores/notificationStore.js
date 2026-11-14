import { defineStore } from 'pinia'
import { commandApi, queryApi } from '../api/httpClient'
import { toast } from 'vue-sonner'
import { useOpsStore } from './opsStore'
import { useSignalR } from '../composables/useSignalR'

const signalRService = useSignalR()

export const useNotificationStore = defineStore('notification', {
  state: () => ({
    userId: localStorage.getItem('active_user_id') || 'user_demo',
    userName: 'Lê Tuấn Dũng',
    unreadCount: 0,
    notifications: [],
    totalCount: 0,
    page: 1,
    pageSize: 20,
    selectedNotification: null,
    isDrawerOpen: false,
    loading: false,
    activeTab: 'dashboard',
    signalRConnected: false,
    metrics: {
      totalNotifications: 0,
      successRate: 100,
      avgOutboxLatencyMs: 25,
      dlqCount: 0,
      channelBreakdown: { inApp: 0, email: 0, telegram: 0, zalo: 0, webPush: 0, webhook: 0 },
      statusBreakdown: { delivered: 0, sent: 0, pending: 0, failedDlq: 0 }
    },
    liveLogs: [
      { id: 1, time: new Date().toLocaleTimeString('vi-VN'), type: 'SYS', message: 'Hệ thống Microservices Enterprise sẵn sàng. Kafka KRaft + CQRS Stack initialized.' }
    ]
  }),

  getters: {
    currentTab(state) {
      return state.activeTab
    },
    // Expose ops state for backwards compatibility
    outboxEvents() {
      const ops = useOpsStore()
      return ops.outboxEvents
    },
    dlqEvents() {
      const ops = useOpsStore()
      return ops.dlqEvents
    },
    userPreferences() {
      const ops = useOpsStore()
      return ops.preferences
    },
    templates() {
      const ops = useOpsStore()
      return ops.templates
    },
    retryingIds() {
      const ops = useOpsStore()
      return ops.retryingIds
    }
  },

  actions: {
    setCurrentTab(tab) {
      this.activeTab = tab
    },

    setUserId(newUserId) {
      if (!newUserId || newUserId.trim() === '') return
      this.userId = newUserId.trim()
      localStorage.setItem('active_user_id', this.userId)
      toast.info(`Đã chuyển phiên làm việc sang: ${this.userId}`)
      this.initSignalR()
      this.fetchNotifications()
      this.fetchUnreadCount()
      this.fetchMetrics()
      const ops = useOpsStore()
      ops.fetchPreferences(this.userId)
    },

    addLog(type, message) {
      const time = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
      this.liveLogs.unshift({
        id: Date.now() + Math.random(),
        time,
        type,
        message
      })
      if (this.liveLogs.length > 100) {
        this.liveLogs.pop()
      }
    },

    openDrawer(notification) {
      this.selectedNotification = notification
      this.isDrawerOpen = true
    },

    openTraceDrawer(notification) {
      this.openDrawer(notification)
    },

    closeDrawer() {
      this.isDrawerOpen = false
      this.selectedNotification = null
    },

    async initSignalR() {
      try {
        await signalRService.connect(
          this.userId,
          (notification) => {
            this.handleIncomingNotification(notification)
          },
          (connected) => {
            this.signalRConnected = connected
          }
        )
        this.signalRConnected = signalRService.isConnected.value
        this.addLog('SIGNALR', `Kết nối Hub Realtime cho nhóm: user_${this.userId}`)
      } catch (err) {
        console.warn('Init SignalR error:', err)
        this.signalRConnected = false
      }
    },

    handleIncomingNotification(notification) {
      this.unreadCount++
      const item = {
        notificationId: notification.notificationId || notification.NotificationId,
        userId: notification.userId || notification.UserId,
        eventCode: notification.eventCode || notification.EventCode || 'SYSTEM_ALERT',
        title: notification.title || notification.Title,
        content: notification.content || notification.Content,
        isRead: false,
        priority: notification.priority || notification.Priority || 'STANDARD',
        channelsStatus: notification.channelsStatus || [
          { channel: 'IN_APP', status: 'DELIVERED', recipient: `SignalR:user_${this.userId}`, timestamp: new Date() }
        ],
        traceLogs: notification.traceLogs || [
          { step: '1. Command API Accepted', durationMs: 5, timestamp: new Date() },
          { step: '2. Outbox Relay -> Kafka', durationMs: 400, timestamp: new Date() },
          { step: '3. Consumer Strategy Dispatch', durationMs: 1, timestamp: new Date() },
          { step: '4. All Channels Delivered', durationMs: 450, timestamp: new Date() }
        ],
        createdAt: notification.createdAt || new Date()
      }

      this.notifications.unshift(item)
      this.addLog('IN_APP', `[SignalR Push] Nhận tin nhắn mới: "${item.title}"`)
      toast.success(item.title, {
        description: item.content
      })
      this.fetchMetrics()
    },

    async fetchNotifications(filters = {}) {
      this.loading = true
      try {
        const res = await queryApi.get('/notifications', {
          params: {
            userId: this.userId,
            page: filters.page || this.page,
            pageSize: filters.pageSize || this.pageSize,
            isRead: filters.isRead !== undefined ? filters.isRead : null
          }
        })

        if (res.data?.success) {
          this.notifications = res.data.data || []
          this.totalCount = res.data.pagination?.totalCount || this.notifications.length
        }
      } catch (err) {
        console.error('Fetch notifications error:', err)
      } finally {
        this.loading = false
      }
    },

    async fetchUnreadCount() {
      try {
        const res = await queryApi.get('/notifications/unread-count', {
          params: { userId: this.userId }
        })
        if (res.data?.success) {
          this.unreadCount = res.data.unreadCount || 0
        }
      } catch (err) {
        console.warn('Fetch unread count failed:', err)
      }
    },

    async markAsRead(notificationId) {
      const target = this.notifications.find(n => n.notificationId === notificationId)
      if (target && !target.isRead) {
        target.isRead = true
        if (this.unreadCount > 0) this.unreadCount--
      }

      try {
        await commandApi.put(`/notifications/${notificationId}/read`, {
          userId: this.userId
        })
        this.addLog('CMD', `Đánh dấu đã đọc #${notificationId.substring(0, 8)}`)
      } catch (err) {
        console.error('Mark read failed:', err)
        if (target) target.isRead = false
        this.fetchUnreadCount()
      }
    },

    async markAllAsRead() {
      this.notifications.forEach(n => { n.isRead = true })
      this.unreadCount = 0

      try {
        await queryApi.put('/notifications/read-all', null, {
          params: { userId: this.userId }
        })
        toast.success('Đã đánh dấu tất cả thông báo là đã đọc!')
        this.addLog('CMD', `Đã đánh dấu đã đọc toàn bộ hộp thư của ${this.userId}`)
      } catch (err) {
        console.error('Mark all read failed:', err)
        this.fetchNotifications()
        this.fetchUnreadCount()
      }
    },

    async fetchMetrics() {
      try {
        const res = await queryApi.get('/notifications/metrics')
        if (res.data?.success) {
          this.metrics = res.data.data
        }
      } catch (err) {
        console.warn('Fetch metrics failed:', err)
      }
    },

    async sendNotification(payload) {
      this.addLog('CMD', `Gửi yêu cầu phát tán sự kiện [${payload.eventCode}] (Priority: ${payload.priority || 'STANDARD'})`)
      try {
        const res = await commandApi.post('/notifications/send', payload)
        if (res.data?.success) {
          const notifId = res.data.data?.notificationId
          toast.success(`Đã ghi nhận sự kiện Outbox: #${notifId?.substring(0, 8)}`)
          this.addLog('OUTBOX', `Ghi sự kiện thành công vào SQL Server (IsPublished=0) #${notifId?.substring(0, 8)}`)
          return res.data
        }
      } catch (err) {
        this.addLog('ERR', `Thất bại khi gửi lệnh: ${err.response?.data?.message || err.message}`)
        throw err
      }
    },

    async sendBulkNotification(payload) {
      this.addLog('CMD', `Kích hoạt Stress Test phát tán đồng loạt cho ${payload.userIds?.length} người dùng`)
      try {
        const res = await commandApi.post('/notifications/send-bulk', payload)
        if (res.data?.success) {
          toast.success(`Stress Test: Đã đẩy ${res.data.data?.total} bản tin vào Transactional Outbox!`)
          this.addLog('OUTBOX', `Cụm Outbox Relay đã tiếp nhận ${res.data.data?.total} bản ghi song song`)
          return res.data
        }
      } catch (err) {
        this.addLog('ERR', `Stress test thất bại: ${err.message}`)
        throw err
      }
    },

    // Delegators to OpsStore
    async fetchOutboxEvents() {
      const ops = useOpsStore()
      return await ops.fetchOutboxEvents()
    },

    async fetchDlqEvents() {
      const ops = useOpsStore()
      return await ops.fetchDlqEvents()
    },

    async retryDlqNotification(id) {
      const ops = useOpsStore()
      return await ops.retryDlq(id)
    },

    async fetchPreferences() {
      const ops = useOpsStore()
      return await ops.fetchPreferences(this.userId)
    },

    async updatePreference(pref) {
      const ops = useOpsStore()
      return await ops.updatePreference(pref)
    },

    async fetchTemplates() {
      const ops = useOpsStore()
      return await ops.fetchTemplates()
    },

    async updateTemplate(id, data) {
      const ops = useOpsStore()
      return await ops.updateTemplate(id, data)
    }
  }
})
