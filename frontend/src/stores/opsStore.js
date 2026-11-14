import { defineStore } from 'pinia'
import { commandApi, queryApi } from '../api/httpClient'
import { toast } from 'vue-sonner'

export const useOpsStore = defineStore('ops', {
  state: () => ({
    outboxEvents: [],
    dlqEvents: [],
    preferences: [],
    templates: [],
    loadingOutbox: false,
    loadingDlq: false,
    loadingPrefs: false,
    loadingTemplates: false,
    retryingIds: [] // list of notificationIds currently retrying
  }),

  actions: {
    async fetchOutboxEvents() {
      this.loadingOutbox = true
      try {
        const res = await queryApi.get('/admin/outbox')
        if (res.data?.success) {
          this.outboxEvents = res.data.data || []
          return
        }
      } catch (err) {
        // Fallback: Try /admin/outbox-events or /notifications/statistics
        try {
          const alt = await queryApi.get('/admin/outbox-events')
          if (alt.data?.success) {
            this.outboxEvents = alt.data.data || []
            return
          }
        } catch (e2) {
          try {
            const stats = await queryApi.get('/notifications/statistics')
            if (stats.data?.outbox) {
              this.outboxEvents = stats.data.outbox
            }
          } catch (e3) {
            console.warn('Fetch outbox failed:', e3)
          }
        }
      } finally {
        this.loadingOutbox = false
      }
    },

    async fetchDlqEvents() {
      this.loadingDlq = true
      try {
        const res = await queryApi.get('/notifications/dlq')
        if (res.data?.success) {
          this.dlqEvents = res.data.data || []
        }
      } catch (err) {
        console.error('Fetch DLQ failed:', err)
      } finally {
        this.loadingDlq = false
      }
    },

    async retryDlq(notificationId) {
      if (!notificationId) return
      if (this.retryingIds.includes(notificationId)) return

      this.retryingIds.push(notificationId)
      try {
        let res
        try {
          res = await commandApi.post(`/preferences/dlq/${notificationId}/retry`)
        } catch (err1) {
          res = await commandApi.post(`/admin/dlq/${notificationId}/retry`)
        }

        if (res.data?.success) {
          toast.success(`Đã đưa bản tin #${notificationId.substring(0, 8)} trở lại hàng đợi Kafka thành công!`)
          await this.fetchDlqEvents()
        }
      } catch (err) {
        console.error('Retry DLQ failed:', err)
        toast.error(`Thử lại thất bại: ${err.response?.data?.message || err.message}`)
      } finally {
        this.retryingIds = this.retryingIds.filter(id => id !== notificationId)
      }
    },

    async fetchPreferences(userId = 'user_demo') {
      this.loadingPrefs = true
      try {
        const res = await queryApi.get(`/preferences/${userId}`)
        if (res.data?.success) {
          this.preferences = res.data.data || []
        }
      } catch (err) {
        console.error('Fetch preferences failed:', err)
      } finally {
        this.loadingPrefs = false
      }
    },

    async updatePreference(pref) {
      try {
        // Normalize telegramChatId to avoid 400 Bad Request
        const normalized = {
          userId: pref.userId || 'user_demo',
          eventCode: pref.eventCode,
          enableInApp: Boolean(pref.enableInApp),
          enableEmail: Boolean(pref.enableEmail),
          enableTelegram: Boolean(pref.enableTelegram),
          enableZalo: Boolean(pref.enableZalo),
          enableWebPush: Boolean(pref.enableWebPush),
          enableWebhook: Boolean(pref.enableWebhook),
          telegramChatId: pref.telegramChatId ? Number(pref.telegramChatId) : null,
          email: pref.email || null,
          phoneNumber: pref.phoneNumber || null,
          webhookUrl: pref.webhookUrl || null,
          quietHoursStart: pref.quietHoursStart || null,
          quietHoursEnd: pref.quietHoursEnd || null
        }

        const res = await commandApi.put(`/preferences/${normalized.userId}`, normalized)
        if (res.data?.success) {
          toast.success(`Đã lưu cấu hình cho sự kiện ${pref.eventCode}!`)
          return true
        }
      } catch (err) {
        console.error('Update preference failed:', err)
        toast.error(`Lỗi cập nhật cấu hình: ${err.response?.data?.message || err.message}`)
        throw err
      }
      return false
    },

    async fetchTemplates() {
      this.loadingTemplates = true
      try {
        const res = await commandApi.get('/templates')
        if (res.data?.success) {
          this.templates = res.data.data || []
        }
      } catch (err) {
        console.error('Fetch templates failed:', err)
      } finally {
        this.loadingTemplates = false
      }
    },

    async updateTemplate(id, data) {
      try {
        const res = await commandApi.put(`/templates/${id}`, data)
        if (res.data?.success) {
          toast.success(`Đã lưu mẫu #${id} & đồng bộ cache Kafka (system-config-events)!`)
          await this.fetchTemplates()
          return true
        }
      } catch (err) {
        console.error('Update template failed:', err)
        toast.error(`Lỗi lưu mẫu: ${err.response?.data?.message || err.message}`)
        throw err
      }
      return false
    }
  }
})
