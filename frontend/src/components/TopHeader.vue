<template>
  <header class="h-16 bg-white/80 dark:bg-zinc-900/80 backdrop-blur-md border-b border-slate-200 dark:border-zinc-800 sticky top-0 z-20 px-6 sm:px-8 flex items-center justify-between transition-colors">
    <!-- Breadcrumb -->
    <div class="flex items-center space-x-2 text-xs">
      <span class="text-slate-400 dark:text-zinc-500 font-medium">NovuCQRS Hub</span>
      <ChevronRight class="w-3.5 h-3.5 text-slate-300 dark:text-zinc-600" />
      <span class="font-bold text-slate-800 dark:text-zinc-100">{{ currentTitle }}</span>
    </div>

    <!-- Actions & User Controls -->
    <div class="flex items-center space-x-3 sm:space-x-4">
      <!-- Windows Web Push Activation -->
      <button
        @click="handleWebPushToggle"
        :class="webPush.isSubscribed.value
          ? 'bg-emerald-50 text-emerald-700 border-emerald-300 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800'
          : 'bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border-indigo-200 dark:bg-indigo-950/40 dark:text-indigo-300 dark:border-indigo-800'"
        class="hidden md:flex px-3 py-1.5 rounded-xl text-xs font-bold border transition items-center space-x-1.5 shadow-sm"
        title="Kích hoạt thông báo Windows Action Center chuẩn W3C Service Worker"
      >
        <BellRing v-if="!webPush.isSubscribed.value" class="w-3.5 h-3.5" />
        <CheckCircle2 v-else class="w-3.5 h-3.5" />
        <span>{{ webPush.isSubscribed.value ? 'Windows Push Đã Bật' : 'Bật Push Windows' }}</span>
      </button>

      <!-- Fast Refresh Button -->
      <button
        @click="refreshAll"
        class="p-2 text-slate-500 dark:text-zinc-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-slate-100 dark:hover:bg-zinc-800 rounded-xl transition"
        title="Làm mới dữ liệu toàn hệ thống"
      >
        <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': store.loading }" />
      </button>

      <!-- Dark / Light Mode Switcher -->
      <button
        @click="themeStore.toggleTheme"
        class="p-2 text-slate-500 dark:text-zinc-400 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-slate-100 dark:hover:bg-zinc-800 rounded-xl transition"
        :title="themeStore.isDark ? 'Chuyển sang chế độ Sáng' : 'Chuyển sang chế độ Tối'"
      >
        <Sun v-if="themeStore.isDark" class="w-4 h-4 transition-transform duration-300 hover:rotate-45" />
        <Moon v-else class="w-4 h-4 transition-transform duration-300 hover:-rotate-12" />
      </button>

      <!-- Notification Center Popover Bell -->
      <div class="relative">
        <button
          @click="showPopover = !showPopover"
          class="relative p-2 text-slate-600 dark:text-zinc-300 hover:text-indigo-600 dark:hover:text-indigo-400 hover:bg-slate-100 dark:hover:bg-zinc-800 rounded-xl transition"
          title="Thông báo mới"
        >
          <Bell class="w-4 h-4" />
          <span
            v-if="store.unreadCount > 0"
            class="absolute top-1 right-1 w-2 h-2 rounded-full bg-rose-500 ring-2 ring-white dark:ring-zinc-900"
          ></span>
        </button>

        <!-- Notification Popover Menu -->
        <div
          v-if="showPopover"
          class="absolute right-0 mt-2 w-84 bg-white dark:bg-zinc-900 rounded-2xl shadow-2xl border border-slate-200 dark:border-zinc-800 py-3 z-50 animate-in fade-in zoom-in-95 duration-150"
        >
          <div class="px-4 pb-2 border-b border-slate-100 dark:border-zinc-800 flex items-center justify-between">
            <h3 class="text-xs font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-1.5">
              <span>Thông báo gần đây</span>
              <span v-if="store.unreadCount > 0" class="text-[10px] px-1.5 py-0.2 rounded-full bg-indigo-100 text-indigo-700 dark:bg-indigo-900/50 dark:text-indigo-300 font-mono font-bold">
                {{ store.unreadCount }}
              </span>
            </h3>

            <button
              v-if="store.unreadCount > 0"
              @click="store.markAllAsRead"
              class="text-[10px] text-indigo-600 dark:text-indigo-400 hover:underline font-bold"
            >
              Đánh dấu tất cả đã đọc
            </button>
          </div>

          <div class="max-h-64 overflow-y-auto divide-y divide-slate-100 dark:divide-zinc-800">
            <div
              v-for="item in store.notifications.slice(0, 5)"
              :key="item.notificationId"
              @click="openItem(item)"
              class="p-3 hover:bg-slate-50 dark:hover:bg-zinc-800/60 cursor-pointer transition text-left space-y-1"
            >
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-slate-800 dark:text-zinc-200 truncate max-w-[200px]">
                  {{ item.title }}
                </span>
                <span class="text-[9px] text-slate-400 dark:text-zinc-500 font-mono">
                  {{ formatTime(item.createdAt) }}
                </span>
              </div>
              <p class="text-[11px] text-slate-500 dark:text-zinc-400 line-clamp-1">{{ item.content }}</p>
            </div>

            <div v-if="store.notifications.length === 0" class="py-8 text-center text-xs text-slate-400 dark:text-zinc-500">
              Chưa có thông báo nào
            </div>
          </div>

          <div class="pt-2 px-4 border-t border-slate-100 dark:border-zinc-800 text-center">
            <button
              @click="goToInbox"
              class="text-xs text-indigo-600 dark:text-indigo-400 hover:text-indigo-700 dark:hover:text-indigo-300 font-bold flex items-center justify-center space-x-1 w-full py-1"
            >
              <span>Xem tất cả hộp thư</span>
              <ArrowRight class="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>

      <!-- Current Profile Avatar -->
      <div class="flex items-center space-x-2 pl-3 border-l border-slate-200 dark:border-zinc-800">
        <div class="w-8 h-8 rounded-full bg-gradient-to-tr from-indigo-600 to-blue-500 flex items-center justify-center text-white text-xs font-bold shadow-sm flex-shrink-0">
          TD
        </div>
        <div class="hidden lg:block text-left text-xs">
          <p class="font-bold text-slate-800 dark:text-zinc-200 leading-none">Lê Tuấn Dũng</p>
          <p class="text-[10px] text-slate-400 dark:text-zinc-500 font-mono mt-0.5">{{ store.userId }}</p>
        </div>
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useNotificationStore } from '../stores/notificationStore'
import { useOpsStore } from '../stores/opsStore'
import { useThemeStore } from '../stores/themeStore'
import { useWebPush } from '../composables/useWebPush'
import {
  Bell,
  BellRing,
  RefreshCw,
  Sun,
  Moon,
  ChevronRight,
  CheckCircle2,
  ArrowRight
} from 'lucide-vue-next'

const store = useNotificationStore()
const opsStore = useOpsStore()
const themeStore = useThemeStore()
const webPush = useWebPush()

const showPopover = ref(false)

const titles = {
  dashboard: 'Tổng quan & Thống kê hệ thống',
  inbox: 'Hộp thư & Truy vết độ trễ Waterfall',
  simulator: 'Phòng Giả lập Sự kiện & Stress Test Lab',
  templates: 'Studio Quản lý Mẫu thông báo (Dynamic Templates)',
  ops: 'Trung tâm Vận hành, Outbox & Dead-Letter Queue (DLQ)'
}

const currentTitle = computed(() => titles[store.activeTab] || 'Notification Hub')

onMounted(() => {
  webPush.checkStatus(store.userId)
})

function onUserChange(userId) {
  store.setUserId(userId)
  webPush.checkStatus(userId)
}

async function handleWebPushToggle() {
  await webPush.subscribe(store.userId)
}

function refreshAll() {
  store.fetchNotifications()
  store.fetchMetrics()
  store.fetchUnreadCount()
  opsStore.fetchOutboxEvents()
  opsStore.fetchDlqEvents()
}

function openItem(item) {
  showPopover.value = false
  store.activeTab = 'inbox'
  store.openTraceDrawer(item)
}

function goToInbox() {
  showPopover.value = false
  store.activeTab = 'inbox'
}

function formatTime(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}
</script>
