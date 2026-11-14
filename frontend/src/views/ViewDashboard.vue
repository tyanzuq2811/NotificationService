<template>
  <div class="space-y-6 text-left max-w-7xl mx-auto">
    <!-- 1. Top 4 KPI Metric Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
      <!-- Total Notifications -->
      <div class="bg-white dark:bg-zinc-900 p-5 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm hover:shadow-md transition">
        <div class="flex items-center justify-between text-slate-500 dark:text-zinc-400 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">Tổng Thông Báo</span>
          <div class="p-2 rounded-xl bg-indigo-50 dark:bg-indigo-950/60 text-indigo-600 dark:text-indigo-400">
            <Bell class="w-4 h-4" />
          </div>
        </div>
        <div class="text-3xl font-black text-slate-900 dark:text-zinc-100 tracking-tight font-mono tabular-nums">
          {{ store.metrics.totalNotifications }}
        </div>
        <p class="text-[11px] text-slate-400 dark:text-zinc-500 mt-2 flex items-center space-x-1">
          <span class="text-emerald-500 font-bold">100% CQRS Sync</span>
          <span>&bull; MongoDB Read Model</span>
        </p>
      </div>

      <!-- Success Rate -->
      <div class="bg-white dark:bg-zinc-900 p-5 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm hover:shadow-md transition">
        <div class="flex items-center justify-between text-slate-500 dark:text-zinc-400 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">Tỷ Lệ Thành Công</span>
          <div class="p-2 rounded-xl bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400">
            <CheckCircle2 class="w-4 h-4" />
          </div>
        </div>
        <div class="text-3xl font-black text-emerald-600 dark:text-emerald-400 tracking-tight font-mono tabular-nums">
          {{ store.metrics.successRate }}%
        </div>
        <p class="text-[11px] text-slate-400 dark:text-zinc-500 mt-2 flex items-center space-x-1">
          <span>Polly Retry Exponential Backoff</span>
        </p>
      </div>

      <!-- Outbox Latency -->
      <div class="bg-white dark:bg-zinc-900 p-5 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm hover:shadow-md transition">
        <div class="flex items-center justify-between text-slate-500 dark:text-zinc-400 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">Độ Trễ Outbox TB</span>
          <div class="p-2 rounded-xl bg-amber-50 dark:bg-amber-950/60 text-amber-600 dark:text-amber-400">
            <Zap class="w-4 h-4" />
          </div>
        </div>
        <div class="text-3xl font-black text-slate-900 dark:text-zinc-100 tracking-tight font-mono tabular-nums">
          {{ store.metrics.avgOutboxLatencyMs }} <span class="text-lg font-normal text-slate-400 dark:text-zinc-500">ms</span>
        </div>
        <p class="text-[11px] text-slate-400 dark:text-zinc-500 mt-2 flex items-center space-x-1">
          <span class="text-indigo-600 dark:text-indigo-400 font-bold">Outbox Relay Worker</span>
          <span>&bull; Polling 500ms</span>
        </p>
      </div>

      <!-- DLQ Count -->
      <div class="bg-white dark:bg-zinc-900 p-5 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm hover:shadow-md transition">
        <div class="flex items-center justify-between text-slate-500 dark:text-zinc-400 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">Sự Kiện Lỗi DLQ</span>
          <div class="p-2 rounded-xl bg-rose-50 dark:bg-rose-950/60 text-rose-600 dark:text-rose-400">
            <AlertTriangle class="w-4 h-4" />
          </div>
        </div>
        <div class="text-3xl font-black text-rose-600 dark:text-rose-400 tracking-tight font-mono tabular-nums">
          {{ store.metrics.dlqCount }}
        </div>
        <p class="text-[11px] text-slate-400 dark:text-zinc-500 mt-2 flex items-center space-x-1">
          <span>Topic: <code>notification-dlq</code></span>
        </p>
      </div>
    </div>

    <!-- 2. Dual Analytics Charts -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Bar Chart: Volume by Channel (2 cols) -->
      <div class="lg:col-span-2 bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm flex flex-col justify-between space-y-4">
        <div class="flex items-center justify-between border-b border-slate-100 dark:border-zinc-800 pb-3">
          <div>
            <h3 class="text-sm font-bold text-slate-900 dark:text-zinc-100">Lưu Lượng Phân Phối Theo Kênh</h3>
            <p class="text-xs text-slate-500 dark:text-zinc-400">Số lượng thông điệp chuyển giao qua 6 kênh độc lập</p>
          </div>
          <span class="px-2.5 py-1 rounded-full text-[10px] font-bold bg-indigo-50 text-indigo-700 dark:bg-indigo-950/50 dark:text-indigo-300 font-mono">
            Realtime CQRS
          </span>
        </div>

        <div class="h-80 relative w-full">
          <Bar v-if="barChartData" :data="barChartData" :options="barChartOptions" />
        </div>

        <!-- Channel Breakdown Quick Chips -->
        <div class="pt-3 border-t border-slate-100 dark:border-zinc-800 grid grid-cols-3 sm:grid-cols-6 gap-2">
          <div
            v-for="ch in channelStats"
            :key="ch.id"
            class="p-2 rounded-xl bg-slate-50 dark:bg-zinc-800/60 border border-slate-200/60 dark:border-zinc-800 flex items-center space-x-2"
          >
            <BrandIcon :name="ch.id" size="xs" />
            <div class="text-left overflow-hidden">
              <div class="text-[10px] text-slate-400 dark:text-zinc-500 truncate">{{ ch.name }}</div>
              <div class="text-xs font-bold text-slate-800 dark:text-zinc-200 font-mono tabular-nums">{{ ch.count }}</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Donut Chart: Status Breakdown (1 col) -->
      <div class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm flex flex-col justify-between space-y-4">
        <div class="flex items-center justify-between border-b border-slate-100 dark:border-zinc-800 pb-3">
          <div>
            <h3 class="text-sm font-bold text-slate-900 dark:text-zinc-100">Tỷ Lệ Trạng Thái Giao Dịch</h3>
            <p class="text-xs text-slate-500 dark:text-zinc-400">Vòng đời thông báo hệ thống</p>
          </div>
          <span class="px-2.5 py-1 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300 font-mono">
            Sync Live
          </span>
        </div>

        <div class="h-80 relative w-full flex items-center justify-center">
          <Doughnut v-if="donutChartData" :data="donutChartData" :options="donutChartOptions" />
        </div>

        <!-- Status Breakdown Quick Chips -->
        <div class="pt-3 border-t border-slate-100 dark:border-zinc-800 grid grid-cols-2 gap-2">
          <div
            v-for="st in statusStats"
            :key="st.name"
            class="p-2 rounded-xl bg-slate-50 dark:bg-zinc-800/60 border border-slate-200/60 dark:border-zinc-800 flex items-center space-x-2"
          >
            <span class="w-2.5 h-2.5 rounded-full flex-shrink-0" :style="{ backgroundColor: st.color }"></span>
            <div class="text-left overflow-hidden">
              <div class="text-[10px] text-slate-400 dark:text-zinc-500 truncate">{{ st.name }}</div>
              <div class="text-xs font-bold text-slate-800 dark:text-zinc-200 font-mono tabular-nums">{{ st.count }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useNotificationStore } from '../stores/notificationStore'
import { useThemeStore } from '../stores/themeStore'
import BrandIcon from '../components/icons/BrandIcon.vue'
import {
  Bell,
  CheckCircle2,
  Zap,
  AlertTriangle
} from 'lucide-vue-next'
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  BarElement,
  CategoryScale,
  LinearScale,
  ArcElement
} from 'chart.js'
import { Bar, Doughnut } from 'vue-chartjs'

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale, ArcElement)

const store = useNotificationStore()
const themeStore = useThemeStore()

onMounted(() => {
  store.fetchMetrics()
})

// Channel Breakdown Quick Chips
const channelStats = computed(() => {
  const c = store.metrics.channelBreakdown || {}
  return [
    { id: 'in_app', name: 'In-App', count: c.inApp || 0 },
    { id: 'email', name: 'Email', count: c.email || 0 },
    { id: 'telegram', name: 'Telegram', count: c.telegram || 0 },
    { id: 'zalo', name: 'Zalo ZNS', count: c.zalo || 0 },
    { id: 'web_push', name: 'Web Push', count: c.webPush || 0 },
    { id: 'webhook', name: 'Webhook', count: c.webhook || 0 }
  ]
})

// Status Breakdown Quick Chips
const statusStats = computed(() => {
  const s = store.metrics.statusBreakdown || {}
  return [
    { name: 'DELIVERED', color: '#10B981', count: s.delivered || 0 },
    { name: 'SENT', color: '#3B82F6', count: s.sent || 0 },
    { name: 'PENDING', color: '#F59E0B', count: s.pending || 0 },
    { name: 'FAILED_DLQ', color: '#EF4444', count: s.failedDlq || 0 }
  ]
})

// Bar Chart Data
const barChartData = computed(() => {
  const c = store.metrics.channelBreakdown || {}
  return {
    labels: ['In-App', 'Email', 'Telegram', 'Zalo ZNS', 'Web Push', 'Webhook'],
    datasets: [
      {
        label: 'Số thông điệp đã phát tán',
        backgroundColor: ['#4F46E5', '#EA4335', '#229ED9', '#0068FF', '#6366F1', '#5865F2'],
        borderRadius: 8,
        data: [
          c.inApp || 0,
          c.email || 0,
          c.telegram || 0,
          c.zalo || 0,
          c.webPush || 0,
          c.webhook || 0
        ]
      }
    ]
  }
})

const barChartOptions = computed(() => {
  const c = themeStore.chartColors || {
    textColor: '#64748B',
    gridColor: '#F1F5F9',
    tooltipBg: '#FFFFFF',
    tooltipBorder: '#E2E8F0'
  }
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        backgroundColor: c.tooltipBg,
        titleColor: themeStore.isDark ? '#F4F4F5' : '#0F172A',
        bodyColor: themeStore.isDark ? '#A1A1AA' : '#475569',
        borderColor: c.tooltipBorder,
        borderWidth: 1
      }
    },
    scales: {
      x: {
        grid: { display: false },
        ticks: { color: c.textColor, font: { family: 'Inter', size: 11, weight: '600' } }
      },
      y: {
        grid: { color: c.gridColor },
        ticks: { color: c.textColor, font: { family: 'JetBrains Mono', size: 10 } }
      }
    }
  }
})

// Donut Chart Data
const donutChartData = computed(() => {
  const s = store.metrics.statusBreakdown || {}
  return {
    labels: ['DELIVERED', 'SENT', 'PENDING', 'FAILED_DLQ'],
    datasets: [
      {
        backgroundColor: ['#10B981', '#3B82F6', '#F59E0B', '#EF4444'],
        borderWidth: 0,
        data: [
          s.delivered || 0,
          s.sent || 0,
          s.pending || 0,
          s.failedDlq || 0
        ]
      }
    ]
  }
})

const donutChartOptions = computed(() => {
  const c = themeStore.chartColors || {
    textColor: '#64748B',
    gridColor: '#F1F5F9',
    tooltipBg: '#FFFFFF',
    tooltipBorder: '#E2E8F0'
  }
  return {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          color: c.textColor,
          font: { family: 'Inter', size: 11, weight: 'bold' },
          boxWidth: 12,
          padding: 15
        }
      },
      tooltip: {
        backgroundColor: c.tooltipBg,
        titleColor: themeStore.isDark ? '#F4F4F5' : '#0F172A',
        bodyColor: themeStore.isDark ? '#A1A1AA' : '#475569',
        borderColor: c.tooltipBorder,
        borderWidth: 1
      }
    },
    cutout: '70%'
  }
})
</script>