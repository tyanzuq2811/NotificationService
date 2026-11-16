<template>
  <div class="h-screen bg-slate-50 dark:bg-zinc-950 flex font-sans text-slate-800 dark:text-zinc-100 antialiased transition-colors duration-200 overflow-hidden w-full max-w-full">
    <!-- Left Sidebar (Collapsible: 256px -> 80px) -->
    <SidebarNav ref="sidebarRef" />

    <!-- Main Content Area -->
    <div
      :class="sidebarRef?.isCollapsed ? 'pl-20' : 'pl-64'"
      class="flex-1 min-w-0 flex flex-col h-screen transition-all duration-300 ease-in-out overflow-hidden w-full"
    >
      <!-- Top Header (Fixed at top, 64px Glassmorphism) -->
      <TopHeader class="shrink-0" />

      <!-- Scrollable Main View Area -->
      <main class="flex-1 overflow-y-auto p-6 sm:p-8 max-w-7xl xl:max-w-[1500px] w-full mx-auto min-w-0">
        <ViewDashboard v-if="store.activeTab === 'dashboard'" />
        <ViewInbox v-else-if="store.activeTab === 'inbox'" />
        <ViewSimulator v-else-if="store.activeTab === 'simulator'" />
        <ViewTemplates v-else-if="store.activeTab === 'templates'" />
        <ViewOps v-else-if="store.activeTab === 'ops'" />
      </main>

      <!-- Bottom Platform Footer (Fixed at bottom, Glassmorphism) -->
      <footer class="shrink-0 h-11 bg-white/90 dark:bg-zinc-900/90 backdrop-blur-md border-t border-slate-200/80 dark:border-zinc-800/80 px-6 sm:px-8 flex items-center justify-between text-[11px] text-slate-500 dark:text-zinc-400 transition-colors z-20 select-none">
        <div class="flex items-center space-x-2 min-w-0">
          <span class="w-2 h-2 rounded-full bg-emerald-500 animate-pulse shrink-0"></span>
          <span class="font-bold text-slate-700 dark:text-zinc-200 truncate">NovuCQRS Platform</span>
          <span class="text-slate-300 dark:text-zinc-700 hidden sm:inline">&bull;</span>
          <span class="truncate hidden sm:inline text-slate-600 dark:text-zinc-300 font-medium">Hệ thống Thông báo Đa kênh Microservices (CQRS + Event-Driven)</span>
        </div>

        <div class="hidden lg:flex items-center space-x-2.5 text-slate-600 dark:text-zinc-300 font-medium">
          <span>Khoa CNTT &bull; Trường Đại học Đại Nam</span>
          <span class="text-slate-300 dark:text-zinc-700">&bull;</span>
          <span>GVHD: <strong class="text-indigo-600 dark:text-indigo-400 font-semibold">ThS. Trần Vũ Đại</strong></span>
        </div>

        <div class="flex items-center space-x-2 font-mono text-[10px] text-slate-500 dark:text-zinc-400 shrink-0">
          <span>SVTH: <strong class="text-slate-700 dark:text-zinc-300 font-semibold">Lê Tuấn Dũng &bull; Đinh Trọng Quỳnh</strong></span>
        </div>
      </footer>
    </div>

    <!-- Slide-Over Waterfall Trace Drawer -->
    <TraceDrawer />

    <!-- Modern Stacked Toast Notifications (vue-sonner) -->
    <Toaster position="bottom-right" richColors />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useNotificationStore } from './stores/notificationStore'
import { useOpsStore } from './stores/opsStore'
import { useThemeStore } from './stores/themeStore'
import { Toaster } from 'vue-sonner'

import SidebarNav from './components/SidebarNav.vue'
import TopHeader from './components/TopHeader.vue'
import TraceDrawer from './components/TraceDrawer.vue'

import ViewDashboard from './views/ViewDashboard.vue'
import ViewInbox from './views/ViewInbox.vue'
import ViewSimulator from './views/ViewSimulator.vue'
import ViewTemplates from './views/ViewTemplates.vue'
import ViewOps from './views/ViewOps.vue'

const store = useNotificationStore()
const opsStore = useOpsStore()
const themeStore = useThemeStore()
const sidebarRef = ref(null)

onMounted(async () => {
  themeStore.applyTheme()

  // Non-blocking SignalR init
  store.initSignalR().catch(err => console.warn('SignalR init:', err))

  // Fetch all read data in parallel with resilient execution
  await Promise.allSettled([
    store.fetchMetrics(),
    store.fetchNotifications(),
    store.fetchUnreadCount(),
    opsStore.fetchTemplates(),
    opsStore.fetchPreferences(store.userId),
    opsStore.fetchOutboxEvents(),
    opsStore.fetchDlqEvents()
  ])
})
</script>
