<template>
  <div class="space-y-6">
    <!-- Action Bar -->
    <div class="flex justify-between items-center bg-white p-4 rounded-xl border border-gray-200 shadow-sm">
      <div class="flex items-center space-x-3">
        <h2 class="text-lg font-bold text-gray-900">Danh sách Thông báo</h2>
        <span class="text-xs bg-gray-100 text-gray-700 font-semibold px-2.5 py-1 rounded-full">
          {{ store.notifications.length }} thông báo
        </span>
      </div>

      <div class="flex items-center space-x-3">
        <button
          @click="filterUnread = !filterUnread"
          :class="filterUnread ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-700 hover:bg-gray-200'"
          class="px-3 py-1.5 rounded-lg text-xs font-semibold transition"
        >
          {{ filterUnread ? 'Hiển thị tất cả' : 'Chỉ hiện chưa đọc' }}
        </button>

        <button
          @click="refreshData"
          class="p-2 text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition"
          title="Làm mới dữ liệu"
        >
          🔄
        </button>
      </div>
    </div>

    <!-- Notification List -->
    <div v-if="filteredNotifications.length === 0" class="bg-white rounded-xl border border-gray-200 p-12 text-center">
      <div class="text-4xl mb-3">📭</div>
      <h3 class="text-base font-bold text-gray-800">Không có thông báo nào</h3>
      <p class="text-xs text-gray-500 mt-1">
        Hãy chuyển qua tab <strong>"Bộ Giả lập Sự kiện"</strong> để kích hoạt gửi thông báo đa kênh thử nghiệm!
      </p>
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="item in filteredNotifications"
        :key="item.notificationId"
        class="bg-white rounded-xl border p-5 transition shadow-sm hover:shadow-md"
        :class="item.isRead ? 'border-gray-200 opacity-90' : 'border-blue-300 bg-blue-50/20'"
      >
        <div class="flex items-start justify-between">
          <div class="flex items-start space-x-3">
            <span class="text-2xl mt-0.5">
              {{ getIcon(item.eventCode) }}
            </span>
            <div>
              <div class="flex items-center space-x-2">
                <h4 class="text-sm font-bold text-gray-900">
                  {{ item.title }}
                </h4>
                <span
                  v-if="!item.isRead"
                  class="bg-red-500 text-white text-[10px] font-bold px-2 py-0.5 rounded-full"
                >
                  Mới
                </span>
              </div>
              <p class="mt-1 text-xs text-gray-700 leading-relaxed">
                {{ item.content }}
              </p>

              <!-- Channels Delivered Badges -->
              <div class="mt-3 flex flex-wrap gap-2 items-center">
                <span class="text-[11px] font-semibold text-gray-500 mr-1">Các kênh:</span>
                <span
                  v-for="ch in item.channelsStatus"
                  :key="ch.channel"
                  class="inline-flex items-center space-x-1 px-2.5 py-1 rounded-md text-[11px] font-medium border"
                  :class="getChannelClass(ch.status)"
                >
                  <span>{{ getChannelIcon(ch.channel) }}</span>
                  <span class="font-bold">{{ ch.channel }}</span>
                  <span>: {{ ch.status }}</span>
                  <span v-if="ch.recipient" class="text-gray-500 text-[10px]">({{ ch.recipient }})</span>
                </span>
              </div>
            </div>
          </div>

          <!-- Mark As Read Button & Timestamp -->
          <div class="text-right flex-shrink-0 ml-4 space-y-2">
            <span class="block text-[11px] text-gray-400">
              {{ formatDate(item.createdAt) }}
            </span>
            <button
              v-if="!item.isRead"
              @click="store.markAsRead(item.notificationId)"
              class="px-2.5 py-1 bg-white hover:bg-blue-50 text-blue-600 border border-blue-200 rounded-md text-[11px] font-semibold shadow-sm transition"
            >
              ✓ Đánh dấu đã đọc
            </button>
            <span v-else class="text-[11px] text-gray-400 italic">
              Đã đọc
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useNotificationStore } from '../stores/notificationStore'

const store = useNotificationStore()
const filterUnread = ref(false)

const filteredNotifications = computed(() => {
  if (filterUnread.value) {
    return store.notifications.filter(n => !n.isRead)
  }
  return store.notifications
})

function refreshData() {
  store.fetchNotifications()
  store.fetchUnreadCount()
}

function getIcon(eventCode) {
  switch (eventCode) {
    case 'ORDER_SUCCESS': return '🛒'
    case 'SECURITY_OTP': return '🔐'
    case 'BALANCE_CHANGE': return '💰'
    default: return '🔔'
  }
}

function getChannelIcon(channel) {
  switch (channel) {
    case 'IN_APP': return '📱'
    case 'EMAIL': return '📧'
    case 'TELEGRAM': return '✈️'
    default: return '📢'
  }
}

function getChannelClass(status) {
  switch (status) {
    case 'DELIVERED':
    case 'SENT':
      return 'bg-emerald-50 text-emerald-700 border-emerald-200'
    case 'PENDING':
      return 'bg-amber-50 text-amber-700 border-amber-200'
    default:
      return 'bg-rose-50 text-rose-700 border-rose-200'
  }
}

function formatDate(isoStr) {
  if (!isoStr) return ''
  const d = new Date(isoStr)
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) + ' ' +
         d.toLocaleDateString('vi-VN')
}
</script>
