<template>
  <div v-if="store.isDrawerOpen" class="fixed inset-0 z-50 overflow-hidden">
    <!-- Backdrop -->
    <div
      @click="store.closeDrawer"
      class="absolute inset-0 bg-slate-900/50 dark:bg-black/70 backdrop-blur-sm transition-opacity"
    ></div>

    <div class="fixed inset-y-0 right-0 max-w-full flex pl-10">
      <div class="w-screen max-w-xl bg-white dark:bg-zinc-900 shadow-2xl flex flex-col justify-between border-l border-slate-200 dark:border-zinc-800 transition-colors">
        <!-- Drawer Header -->
        <div class="px-6 py-5 border-b border-slate-100 dark:border-zinc-800 flex items-center justify-between bg-slate-50/60 dark:bg-zinc-950/40">
          <div>
            <div class="flex items-center space-x-2">
              <span class="text-xs font-bold font-mono px-2 py-0.5 rounded bg-indigo-50 text-indigo-700 dark:bg-indigo-950/60 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800">
                #{{ item?.notificationId?.substring(0, 8) }}
              </span>
              <span class="text-xs font-semibold px-2 py-0.5 rounded bg-slate-100 text-slate-700 dark:bg-zinc-800 dark:text-zinc-300">
                {{ item?.eventCode }}
              </span>
              <span
                class="text-[10px] font-mono font-bold px-2 py-0.5 rounded"
                :class="item?.priority === 'HIGH' ? 'bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-300' : 'bg-slate-100 text-slate-600 dark:bg-zinc-800 dark:text-zinc-400'"
              >
                {{ item?.priority || 'STANDARD' }}
              </span>
            </div>
            <h2 class="text-base font-bold text-slate-900 dark:text-zinc-100 mt-2 line-clamp-1">
              {{ item?.title }}
            </h2>
          </div>

          <button
            @click="store.closeDrawer"
            class="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-zinc-200 rounded-xl hover:bg-slate-100 dark:hover:bg-zinc-800 transition"
          >
            <X class="w-5 h-5" />
          </button>
        </div>

        <!-- Drawer Body -->
        <div class="flex-1 overflow-y-auto p-6 space-y-6 text-left">
          <!-- Tab Switcher: Overview vs Raw JSON -->
          <div class="flex space-x-2 border-b border-slate-200 dark:border-zinc-800 pb-2">
            <button
              @click="activeSubTab = 'overview'"
              :class="activeSubTab === 'overview' ? 'text-indigo-600 dark:text-indigo-400 font-bold border-b-2 border-indigo-600 dark:border-indigo-400 pb-1' : 'text-slate-500 dark:text-zinc-400 font-medium'"
              class="text-xs transition flex items-center space-x-1.5"
            >
              <Activity class="w-3.5 h-3.5" />
              <span>Tổng quan & Waterfall</span>
            </button>
            <button
              @click="activeSubTab = 'json'"
              :class="activeSubTab === 'json' ? 'text-indigo-600 dark:text-indigo-400 font-bold border-b-2 border-indigo-600 dark:border-indigo-400 pb-1' : 'text-slate-500 dark:text-zinc-400 font-medium'"
              class="text-xs transition flex items-center space-x-1.5"
            >
              <FileJson class="w-3.5 h-3.5" />
              <span>Dữ liệu JSON gốc (MongoDB)</span>
            </button>
          </div>

          <!-- SUBTAB 1: OVERVIEW & WATERFALL -->
          <div v-if="activeSubTab === 'overview'" class="space-y-6">
            <!-- 1. Waterfall Lifecycle Trace -->
            <div class="space-y-3">
              <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-zinc-400 flex items-center space-x-2">
                <Activity class="w-4 h-4 text-indigo-600 dark:text-indigo-400" />
                <span>Sơ đồ Waterfall Độ trễ Từng chặng (Latency Lifecycle)</span>
              </h3>

              <div class="bg-slate-900 dark:bg-zinc-950 rounded-2xl p-4 text-xs font-mono space-y-3 shadow-inner border border-slate-800 dark:border-zinc-800">
                <div
                  v-for="(trace, idx) in formattedTraceLogs"
                  :key="idx"
                  class="space-y-1"
                >
                  <div class="flex items-center justify-between text-slate-300">
                    <div class="flex items-center space-x-2">
                      <span class="w-2 h-2 rounded-full bg-indigo-400"></span>
                      <span class="font-bold text-white text-[11px]">{{ trace.step }}</span>
                    </div>
                    <span class="text-emerald-400 font-bold tabular-nums">+{{ trace.durationMs }}ms</span>
                  </div>

                  <!-- Waterfall Progress Bar -->
                  <div class="w-full bg-slate-800 dark:bg-zinc-800 rounded-full h-1.5 overflow-hidden">
                    <div
                      class="bg-gradient-to-r from-indigo-500 to-emerald-400 h-1.5 rounded-full transition-all duration-500"
                      :style="{ width: Math.min(100, Math.max(12, trace.pct)) + '%' }"
                    ></div>
                  </div>
                  <div class="text-[9px] text-slate-500 text-right">
                    {{ trace.timeStr }}
                  </div>
                </div>

                <!-- Total Latency Summary -->
                <div class="pt-2 border-t border-slate-800 flex justify-between text-xs text-slate-300 font-sans">
                  <span class="font-bold">Tổng thời gian xử lý:</span>
                  <span class="text-emerald-400 font-mono font-bold tabular-nums">{{ totalLatency }} ms</span>
                </div>
              </div>
            </div>

            <!-- 2. Multi-Channel Status Cards -->
            <div class="space-y-3">
              <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-zinc-400">
                Trạng thái Phân phối Đa kênh (Multi-Channel Delivery)
              </h3>

              <div class="grid grid-cols-1 gap-2.5">
                <div
                  v-for="ch in item?.channelsStatus"
                  :key="ch.channel"
                  class="p-3.5 rounded-xl border border-slate-200 dark:border-zinc-800 bg-white dark:bg-zinc-900/60 flex items-start justify-between space-x-3 shadow-sm"
                >
                  <div class="flex items-start space-x-3">
                    <div class="p-2 rounded-lg bg-slate-50 dark:bg-zinc-800 flex-shrink-0">
                      <BrandIcon :name="ch.channel" size="md" />
                    </div>
                    <div class="space-y-1">
                      <div class="flex items-center space-x-2">
                        <span class="text-xs font-bold text-slate-900 dark:text-zinc-100">{{ ch.channel }}</span>
                        <StatusBadge :status="ch.status" />
                      </div>
                      <p class="text-[11px] text-slate-500 dark:text-zinc-400">
                        Đích nhận: <span class="font-mono text-slate-700 dark:text-zinc-300 font-semibold">{{ ch.recipient || 'N/A' }}</span>
                      </p>
                      <p v-if="ch.errorMessage" class="text-[11px] text-rose-600 dark:text-rose-400 font-mono bg-rose-50 dark:bg-rose-950/40 p-2 rounded-lg border border-rose-200 dark:border-rose-900/50 break-words">
                        {{ ch.errorMessage }}
                      </p>
                    </div>
                  </div>

                  <div class="text-right text-[10px] text-slate-400 dark:text-zinc-500 font-mono whitespace-nowrap">
                    {{ formatTime(ch.timestamp) }}
                  </div>
                </div>
              </div>
            </div>

            <!-- 3. Message Content Preview -->
            <div class="space-y-2">
              <h3 class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-zinc-400">
                Nội dung Thông báo Chi tiết
              </h3>
              <div class="p-4 bg-slate-50 dark:bg-zinc-950 rounded-xl border border-slate-200 dark:border-zinc-800 text-xs text-slate-700 dark:text-zinc-300 leading-relaxed space-y-2">
                <p class="font-bold text-slate-900 dark:text-zinc-100 text-sm">{{ item?.title }}</p>
                <p class="whitespace-pre-line">{{ item?.content }}</p>
              </div>
            </div>
          </div>

          <!-- SUBTAB 2: RAW JSON PAYLOAD -->
          <div v-else-if="activeSubTab === 'json'" class="space-y-3">
            <div class="flex items-center justify-between">
              <span class="text-xs font-bold text-slate-600 dark:text-zinc-300">Bản ghi BSON / JSON:</span>
              <button
                @click="copyPayload"
                class="px-3 py-1 bg-slate-100 dark:bg-zinc-800 hover:bg-slate-200 dark:hover:bg-zinc-700 text-slate-700 dark:text-zinc-200 rounded-lg text-xs font-semibold transition flex items-center space-x-1"
              >
                <Check v-if="copied" class="w-3.5 h-3.5 text-emerald-500" />
                <Copy v-else class="w-3.5 h-3.5" />
                <span>{{ copied ? 'Đã sao chép!' : 'Sao chép JSON' }}</span>
              </button>
            </div>

            <pre class="p-4 bg-slate-900 dark:bg-zinc-950 text-slate-200 font-mono text-[11px] rounded-2xl overflow-x-auto max-h-96 border border-slate-800 dark:border-zinc-800">{{ JSON.stringify(item, null, 2) }}</pre>
          </div>
        </div>

        <!-- Drawer Footer -->
        <div class="p-4 border-t border-slate-100 dark:border-zinc-800 bg-slate-50/60 dark:bg-zinc-950/40 flex justify-between items-center">
          <span class="text-[11px] text-slate-400 dark:text-zinc-500 font-mono">
            Khởi tạo: {{ formatTime(item?.createdAt) }}
          </span>

          <div class="flex space-x-2">
            <button
              v-if="!item?.isRead"
              @click="markRead"
              class="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold rounded-xl shadow-sm transition flex items-center space-x-1.5"
            >
              <CheckCircle class="w-3.5 h-3.5" />
              <span>Đánh dấu đã đọc</span>
            </button>
            <button
              @click="store.closeDrawer"
              class="px-4 py-2 bg-white dark:bg-zinc-800 border border-slate-200 dark:border-zinc-700 hover:bg-slate-50 dark:hover:bg-zinc-700 text-slate-700 dark:text-zinc-200 text-xs font-bold rounded-xl transition"
            >
              Đóng
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useNotificationStore } from '../stores/notificationStore'
import { useCopy } from '../composables/useCopy'
import BrandIcon from './icons/BrandIcon.vue'
import StatusBadge from './ui/StatusBadge.vue'
import {
  X,
  Activity,
  FileJson,
  Copy,
  Check,
  CheckCircle
} from 'lucide-vue-next'

const store = useNotificationStore()
const { copy, copied } = useCopy()
const activeSubTab = ref('overview')

const item = computed(() => store.selectedNotification)

const formattedTraceLogs = computed(() => {
  const logs = item.value?.traceLogs || []
  if (logs.length === 0) {
    return [
      { step: '1. Command API Accepted', durationMs: 5, pct: 10, timeStr: formatTime(item.value?.createdAt) },
      { step: '2. Outbox Relay -> Kafka', durationMs: 380, pct: 50, timeStr: formatTime(item.value?.createdAt) },
      { step: '3. Consumer Strategy Dispatch', durationMs: 2, pct: 15, timeStr: formatTime(item.value?.createdAt) },
      { step: '4. All Channels Delivered', durationMs: 420, pct: 100, timeStr: formatTime(item.value?.createdAt) }
    ]
  }

  const maxDuration = Math.max(...logs.map(l => l.durationMs || 10), 100)
  return logs.map(l => ({
    step: l.step,
    durationMs: l.durationMs,
    pct: Math.round((l.durationMs / maxDuration) * 100),
    timeStr: formatTime(l.timestamp)
  }))
})

const totalLatency = computed(() => {
  const logs = formattedTraceLogs.value
  const step4 = logs.find(l => l.step.includes('4.'))
  if (step4) return step4.durationMs
  return logs.reduce((sum, l) => sum + l.durationMs, 0)
})

function markRead() {
  if (item.value) {
    store.markAsRead(item.value.notificationId)
  }
}

function copyPayload() {
  copy(JSON.stringify(item.value, null, 2))
}

function formatTime(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
}
</script>
