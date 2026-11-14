<template>
  <aside
    :class="[
      isCollapsed ? 'w-20' : 'w-64',
      'bg-slate-900 dark:bg-zinc-950 border-r border-slate-800 dark:border-zinc-800 flex flex-col justify-between h-screen fixed left-0 top-0 z-30 select-none transition-all duration-300 ease-in-out'
    ]"
  >
    <!-- Brand & Navigation -->
    <div class="flex-1 flex flex-col min-h-0">
      <!-- Logo Header -->
      <div class="h-16 flex items-center px-4 border-b border-slate-800 dark:border-zinc-800 justify-between">
        <div class="flex items-center space-x-3 overflow-hidden">
          <div class="w-10 h-10 rounded-xl bg-gradient-to-tr from-indigo-600 via-indigo-500 to-blue-500 flex items-center justify-center text-white shadow-lg shadow-indigo-500/20 flex-shrink-0">
            <Radio class="w-5 h-5" :stroke-width="2" />
          </div>
          <div v-if="!isCollapsed" class="transition-opacity duration-200">
            <h1 class="text-sm font-bold text-white tracking-tight flex items-center space-x-1.5">
              <span>NovuCQRS</span>
              <span class="text-[9px] px-1.5 py-0.5 rounded bg-indigo-500/20 text-indigo-400 font-mono border border-indigo-500/30">v2.0</span>
            </h1>
            <p class="text-[10px] text-slate-400 dark:text-zinc-500 font-mono flex items-center space-x-1">
              <span class="w-1.5 h-1.5 rounded-full bg-emerald-400"></span>
              <span>.NET 8 &bull; Kafka KRaft</span>
            </p>
          </div>
        </div>

        <!-- Collapse Toggle Button -->
        <button
          @click="isCollapsed = !isCollapsed"
          class="p-1.5 text-slate-400 hover:text-white hover:bg-slate-800 dark:hover:bg-zinc-800 rounded-lg transition"
          :title="isCollapsed ? 'Mở rộng thanh điều hướng' : 'Thu gọn thanh điều hướng'"
        >
          <ChevronLeft v-if="!isCollapsed" class="w-4 h-4" />
          <ChevronRight v-else class="w-4 h-4" />
        </button>
      </div>

      <!-- Navigation Links -->
      <nav class="p-3 space-y-1.5 flex-1 overflow-y-auto">
        <button
          v-for="item in navItems"
          :key="item.id"
          @click="store.activeTab = item.id"
          :class="[
            store.activeTab === item.id
              ? 'bg-indigo-600 text-white font-semibold shadow-md shadow-indigo-600/30'
              : 'text-slate-400 hover:text-slate-100 hover:bg-slate-800/60 dark:hover:bg-zinc-900 font-medium',
            isCollapsed ? 'justify-center px-2' : 'justify-between px-3.5',
            'w-full flex items-center py-2.5 rounded-xl text-xs transition duration-150 group relative'
          ]"
          :title="isCollapsed ? item.label : undefined"
        >
          <div class="flex items-center space-x-3">
            <component :is="item.icon" class="w-4 h-4 flex-shrink-0" :stroke-width="1.75" />
            <span v-if="!isCollapsed" class="truncate">{{ item.label }}</span>
          </div>

          <!-- Badges when expanded -->
          <div v-if="!isCollapsed" class="flex items-center space-x-1">
            <span
              v-if="item.id === 'inbox' && store.unreadCount > 0"
              class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-indigo-400 text-slate-950 font-mono"
            >
              {{ store.unreadCount }}
            </span>

            <span
              v-if="item.id === 'ops' && store.metrics.dlqCount > 0"
              class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-rose-500/20 text-rose-300 border border-rose-500/30 font-mono"
            >
              {{ store.metrics.dlqCount }} DLQ
            </span>
          </div>

          <!-- Little dot badge when collapsed -->
          <span
            v-if="isCollapsed && item.id === 'inbox' && store.unreadCount > 0"
            class="absolute top-2 right-2 w-2 h-2 rounded-full bg-indigo-400"
          ></span>
          <span
            v-if="isCollapsed && item.id === 'ops' && store.metrics.dlqCount > 0"
            class="absolute top-2 right-2 w-2 h-2 rounded-full bg-rose-500"
          ></span>
        </button>
      </nav>
    </div>

    <!-- Bottom Realtime SignalR Status Card -->
    <div class="p-3 border-t border-slate-800 dark:border-zinc-800">
      <div
        v-if="!isCollapsed"
        class="bg-slate-800/80 dark:bg-zinc-900 rounded-xl p-3 border border-slate-700/60 dark:border-zinc-800 text-left space-y-2"
      >
        <div class="flex items-center justify-between">
          <div class="flex items-center space-x-1.5">
            <span
              class="w-2 h-2 rounded-full"
              :class="store.signalRConnected ? 'bg-emerald-400 animate-pulse' : 'bg-rose-400'"
            ></span>
            <span class="text-[11px] font-bold text-slate-300">SignalR Hub</span>
          </div>
          <button
            @click="store.initSignalR"
            class="text-[10px] text-indigo-400 hover:text-indigo-300 flex items-center space-x-1"
            title="Kết nối lại SignalR Hub"
          >
            <RefreshCw class="w-3 h-3" />
            <span>Kết nối lại</span>
          </button>
        </div>

        <p class="text-[10px] text-slate-400 font-mono truncate">
          {{ store.signalRConnected ? 'Group: user_' + store.userId : 'Mất kết nối' }}
        </p>
      </div>

      <!-- Icon-only status when collapsed -->
      <div v-else class="flex flex-col items-center py-2 space-y-2">
        <button
          @click="store.initSignalR"
          :title="store.signalRConnected ? 'SignalR: Connected' : 'SignalR: Disconnected (Click để kết nối lại)'"
          class="p-2 rounded-lg hover:bg-slate-800 dark:hover:bg-zinc-900 text-slate-400"
        >
          <span
            class="w-3 h-3 rounded-full block"
            :class="store.signalRConnected ? 'bg-emerald-400 animate-pulse' : 'bg-rose-400'"
          ></span>
        </button>
      </div>
    </div>
  </aside>
</template>

<script setup>
import { ref } from 'vue'
import { useNotificationStore } from '../stores/notificationStore'
import {
  LayoutDashboard,
  Inbox,
  FlaskConical,
  FileCode2,
  SlidersHorizontal,
  ChevronLeft,
  ChevronRight,
  Radio,
  RefreshCw
} from 'lucide-vue-next'

const store = useNotificationStore()
const isCollapsed = ref(false)

defineExpose({
  isCollapsed
})

const navItems = [
  { id: 'dashboard', label: 'Tổng quan & Thống kê', icon: LayoutDashboard },
  { id: 'inbox', label: 'Hộp thư & Truy vết', icon: Inbox },
  { id: 'simulator', label: 'Phòng Giả lập & Stress Test', icon: FlaskConical },
  { id: 'templates', label: 'Studio Mẫu Thông báo', icon: FileCode2 },
  { id: 'ops', label: 'Giám sát Outbox & DLQ', icon: SlidersHorizontal }
]
</script>
