<template>
  <div class="min-h-screen bg-slate-50 dark:bg-zinc-950 flex font-sans text-slate-800 dark:text-zinc-100 antialiased transition-colors duration-200 overflow-x-hidden w-full max-w-full">
    <!-- Left Sidebar (Collapsible: 256px -> 80px) -->
    <SidebarNav ref="sidebarRef" />

    <!-- Main Content Area -->
    <div
      :class="sidebarRef?.isCollapsed ? 'pl-20' : 'pl-64'"
      class="flex-1 min-w-0 flex flex-col min-h-screen transition-all duration-300 ease-in-out overflow-x-hidden w-full"
    >
      <!-- Top Header (64px Glassmorphism) -->
      <TopHeader />

      <!-- View Containers -->
      <main class="flex-1 p-6 sm:p-8 max-w-7xl xl:max-w-[1500px] w-full mx-auto min-w-0">
        <ViewDashboard v-if="store.activeTab === 'dashboard'" />
        <ViewInbox v-else-if="store.activeTab === 'inbox'" />
        <ViewSimulator v-else-if="store.activeTab === 'simulator'" />
        <ViewTemplates v-else-if="store.activeTab === 'templates'" />
        <ViewOps v-else-if="store.activeTab === 'ops'" />
      </main>

      <!-- Bottom Platform Footer -->
      <footer class="py-4 border-t border-slate-200/80 dark:border-zinc-800/80 text-center text-[11px] text-slate-400 dark:text-zinc-500 font-medium">
        Đề án: Xây dựng Service Gửi Thông Báo Đa Kênh trong Hệ Thống Microservices (CQRS + Event-Driven) &bull; Khoa CNTT - Đại học Đại Nam
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
