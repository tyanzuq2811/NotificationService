<template>
  <div class="space-y-6 text-left">
    <!-- Filter & Search Toolbar -->
    <div class="bg-white dark:bg-zinc-900 p-4 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm flex flex-col md:flex-row gap-3 items-center justify-between">
      <div class="flex-1 w-full flex flex-wrap items-center gap-3">
        <!-- Search Input -->
        <div class="relative flex-1 min-w-[200px]">
          <Search class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 dark:text-zinc-500" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Tìm theo tiêu đề, nội dung, mã đơn hoặc OTP..."
            class="w-full text-xs pl-9 pr-4 py-2 border border-slate-200 dark:border-zinc-700 rounded-xl focus:ring-2 focus:ring-indigo-500 bg-slate-50/50 dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 placeholder-slate-400 dark:placeholder-zinc-500"
          />
        </div>

        <!-- Filter Event Code -->
        <select
          v-model="selectedEvent"
          class="text-xs px-3 py-2 border border-slate-200 dark:border-zinc-700 rounded-xl focus:ring-2 focus:ring-indigo-500 bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-200"
        >
          <option value="ALL">Tất cả Sự kiện</option>
          <option value="ORDER_SUCCESS">Đơn hàng (ORDER_SUCCESS)</option>
          <option value="SECURITY_OTP">Bảo mật OTP (SECURITY_OTP)</option>
          <option value="BALANCE_CHANGE">Biến động số dư (BALANCE_CHANGE)</option>
          <option value="SYSTEM_ALERT">Cảnh báo hệ thống (SYSTEM_ALERT)</option>
        </select>

        <!-- Filter Status -->
        <select
          v-model="selectedStatus"
          class="text-xs px-3 py-2 border border-slate-200 dark:border-zinc-700 rounded-xl focus:ring-2 focus:ring-indigo-500 bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-200"
        >
          <option value="ALL">Tất cả Trạng thái</option>
          <option value="DELIVERED">DELIVERED</option>
          <option value="SENT">SENT</option>
          <option value="FALLBACK_SUCCESS">FALLBACK_SUCCESS</option>
          <option value="PENDING">PENDING</option>
          <option value="FAILED_DLQ">FAILED_DLQ</option>
          <option value="EXPIRED">EXPIRED</option>
        </select>
      </div>

      <!-- Quick Actions -->
      <div class="flex items-center space-x-2">
        <button
          @click="filterUnreadOnly = !filterUnreadOnly"
          :class="filterUnreadOnly
            ? 'bg-indigo-600 text-white shadow-sm'
            : 'bg-slate-100 dark:bg-zinc-800 text-slate-700 dark:text-zinc-300 hover:bg-slate-200 dark:hover:bg-zinc-700'"
          class="px-3.5 py-2 rounded-xl text-xs font-semibold transition"
        >
          {{ filterUnreadOnly ? 'Đang lọc chưa đọc' : 'Chỉ chưa đọc' }}
        </button>

        <button
          v-if="store.unreadCount > 0"
          @click="store.markAllAsRead"
          class="px-3 py-2 rounded-xl text-xs font-semibold bg-slate-100 dark:bg-zinc-800 hover:bg-slate-200 dark:hover:bg-zinc-700 text-slate-700 dark:text-zinc-300 transition flex items-center space-x-1"
          title="Đánh dấu tất cả thông báo là đã đọc"
        >
          <CheckCheck class="w-3.5 h-3.5" />
          <span class="hidden sm:inline">Đọc tất cả</span>
        </button>

        <button
          @click="store.fetchNotifications()"
          class="p-2 text-slate-500 dark:text-zinc-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-slate-100 dark:hover:bg-zinc-800 rounded-xl transition"
          title="Tải lại danh sách"
        >
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': store.loading }" />
        </button>
      </div>
    </div>

    <!-- Skeleton Loading -->
    <div v-if="store.loading" class="space-y-3">
      <SkeletonRow v-for="n in 5" :key="n" />
    </div>

    <!-- Empty State -->
    <div
      v-else-if="filteredNotifications.length === 0"
      class="bg-white dark:bg-zinc-900 rounded-2xl border border-slate-200/80 dark:border-zinc-800 p-16 text-center space-y-3"
    >
      <div class="w-12 h-12 rounded-2xl bg-indigo-50 dark:bg-indigo-950/60 text-indigo-600 dark:text-indigo-400 flex items-center justify-center mx-auto">
        <Inbox class="w-6 h-6" />
      </div>
      <h3 class="text-sm font-bold text-slate-800 dark:text-zinc-100">Không tìm thấy thông báo phù hợp</h3>
      <p class="text-xs text-slate-500 dark:text-zinc-400 max-w-sm mx-auto">
        Hãy chuyển qua tab <strong>Phòng Giả lập Sự kiện</strong> để kích hoạt gửi thông báo đa kênh mới hoặc xóa bộ lọc tìm kiếm!
      </p>
    </div>

    <!-- Notification Cards List -->
    <div v-else class="space-y-3">
      <div
        v-for="item in filteredNotifications"
        :key="item.notificationId"
        @click="store.openDrawer(item)"
        class="bg-white dark:bg-zinc-900 p-5 rounded-2xl border transition duration-150 cursor-pointer shadow-sm hover:shadow-md text-left flex items-start justify-between group"
        :class="item.isRead
          ? 'border-slate-200/80 dark:border-zinc-800 opacity-90'
          : 'border-indigo-300 dark:border-indigo-800 bg-indigo-50/15 dark:bg-indigo-950/20 ring-1 ring-indigo-500/20'"
      >
        <div class="flex items-start space-x-4 flex-1 pr-4">
          <!-- Event Icon -->
          <div class="w-10 h-10 rounded-xl bg-slate-100 dark:bg-zinc-800 text-slate-700 dark:text-zinc-300 flex items-center justify-center flex-shrink-0 mt-0.5 group-hover:scale-105 transition">
            <component :is="getEventIcon(item.eventCode)" class="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
          </div>

          <div class="space-y-1.5 flex-1 min-w-0">
            <!-- Header Line -->
            <div class="flex flex-wrap items-center gap-2">
              <span class="font-bold text-sm text-slate-900 dark:text-zinc-100">{{ item.title }}</span>
              <span
                v-if="!item.isRead"
                class="w-2 h-2 rounded-full bg-indigo-500 flex-shrink-0"
                title="Chưa đọc"
              ></span>
              <span
                class="text-[10px] font-mono px-2 py-0.5 rounded"
                :class="item.priority === 'HIGH' ? 'bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-300' : 'bg-slate-100 text-slate-600 dark:bg-zinc-800 dark:text-zinc-400'"
              >
                {{ item.priority || 'STANDARD' }}
              </span>
            </div>

            <!-- Content preview -->
            <p class="text-xs text-slate-600 dark:text-zinc-400 line-clamp-2 leading-relaxed">
              {{ item.content }}
            </p>

            <!-- Channels Status Line -->
            <div class="flex flex-wrap items-center gap-2 pt-1">
              <div
                v-for="ch in item.channelsStatus"
                :key="ch.channel"
                class="inline-flex items-center space-x-1.5 px-2 py-0.5 rounded-lg bg-slate-50 dark:bg-zinc-800 border border-slate-200 dark:border-zinc-700"
              >
                <BrandIcon :name="ch.channel" size="xs" />
                <span class="text-[10px] font-bold text-slate-700 dark:text-zinc-300">{{ ch.channel }}</span>
                <StatusBadge :status="ch.status" />
              </div>
            </div>
          </div>
        </div>

        <!-- Right Side: Time & Actions -->
        <div class="flex flex-col items-end space-y-3 flex-shrink-0">
          <span class="text-[11px] text-slate-400 dark:text-zinc-500 font-mono tabular-nums">
            {{ formatTime(item.createdAt) }}
          </span>

          <div class="flex items-center space-x-1.5">
            <button
              v-if="!item.isRead"
              @click.stop="store.markAsRead(item.notificationId)"
              class="px-2.5 py-1 text-[11px] font-bold text-indigo-600 dark:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-950/60 rounded-lg transition"
              title="Đánh dấu đã đọc"
            >
              Đánh dấu đã đọc
            </button>
            <ChevronRight class="w-4 h-4 text-slate-400 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 group-hover:translate-x-0.5 transition" />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useNotificationStore } from '../stores/notificationStore'
import BrandIcon from '../components/icons/BrandIcon.vue'
import StatusBadge from '../components/ui/StatusBadge.vue'
import SkeletonRow from '../components/ui/SkeletonRow.vue'
import {
  Search,
  RefreshCw,
  Inbox,
  ShoppingBag,
  ShieldCheck,
  Wallet,
  AlertOctagon,
  Bell,
  ChevronRight,
  CheckCheck
} from 'lucide-vue-next'

const store = useNotificationStore()

onMounted(() => {
  store.fetchNotifications()
  store.fetchUnreadCount()
})

const searchQuery = ref('')
const selectedEvent = ref('ALL')
const selectedStatus = ref('ALL')
const filterUnreadOnly = ref(false)

const filteredNotifications = computed(() => {
  return store.notifications.filter(item => {
    // 1. Search Query
    if (searchQuery.value.trim() !== '') {
      const q = searchQuery.value.toLowerCase().trim()
      const matchTitle = item.title?.toLowerCase().includes(q)
      const matchContent = item.content?.toLowerCase().includes(q)
      if (!matchTitle && !matchContent) return false
    }

    // 2. Event Code
    if (selectedEvent.value !== 'ALL' && item.eventCode !== selectedEvent.value) {
      return false
    }

    // 3. Status Filter (check in channelsStatus)
    if (selectedStatus.value !== 'ALL') {
      const hasStatus = item.channelsStatus?.some(cs => cs.status === selectedStatus.value)
      if (!hasStatus) return false
    }

    // 4. Unread only
    if (filterUnreadOnly.value && item.isRead) {
      return false
    }

    return true
  })
})

function getEventIcon(code) {
  switch (code) {
    case 'ORDER_SUCCESS': return ShoppingBag
    case 'SECURITY_OTP': return ShieldCheck
    case 'BALANCE_CHANGE': return Wallet
    case 'SYSTEM_ALERT': return AlertOctagon
    default: return Bell
  }
}

function formatTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) + ' ' +
         d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })
}
</script>
