<template>
  <span
    :class="[
      'inline-flex items-center space-x-1.5 font-mono text-[11px] font-bold px-2.5 py-0.5 rounded-full border transition-colors',
      badgeConfig.styleClass
    ]"
    :title="badgeConfig.tooltip"
  >
    <component :is="badgeConfig.icon" class="w-3.5 h-3.5 flex-shrink-0" :stroke-width="2" />
    <span>{{ badgeConfig.label }}</span>
  </span>
</template>

<script setup>
import { computed } from 'vue'
import {
  CheckCircle2,
  Send,
  Clock,
  AlertTriangle,
  TimerOff,
  ShieldCheck,
  HelpCircle
} from 'lucide-vue-next'

const props = defineProps({
  status: {
    type: String,
    required: true
  }
})

const badgeConfig = computed(() => {
  const s = (props.status || '').toUpperCase()

  switch (s) {
    case 'DELIVERED':
      return {
        label: 'DELIVERED',
        icon: CheckCircle2,
        styleClass: 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800',
        tooltip: 'Đã chuyển giao thành công đến thiết bị người nhận'
      }
    case 'SENT':
      return {
        label: 'SENT',
        icon: Send,
        styleClass: 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300 dark:border-blue-800',
        tooltip: 'Đã gửi qua cổng SMTP / Telegram / Zalo'
      }
    case 'PENDING':
      return {
        label: 'PENDING',
        icon: Clock,
        styleClass: 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800 animate-pulse',
        tooltip: 'Đang xếp hàng chờ xử lý'
      }
    case 'FAILED_DLQ':
    case 'FAILED':
      return {
        label: 'FAILED_DLQ',
        icon: AlertTriangle,
        styleClass: 'bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-950/40 dark:text-rose-300 dark:border-rose-800',
        tooltip: 'Thất bại sau 3 lần thử / Ngắt mạch Circuit Breaker'
      }
    case 'EXPIRED':
      return {
        label: 'EXPIRED',
        icon: TimerOff,
        styleClass: 'bg-zinc-100 text-zinc-600 border-zinc-200 dark:bg-zinc-800 dark:text-zinc-400 dark:border-zinc-700',
        tooltip: 'Hết hạn hiệu lực (TTL Expired)'
      }
    case 'FALLBACK_SUCCESS':
      return {
        label: 'FALLBACK_SUCCESS',
        icon: ShieldCheck,
        styleClass: 'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-950/40 dark:text-indigo-300 dark:border-indigo-800',
        tooltip: 'Kênh chính lỗi, chuyển phát dự phòng thành công'
      }
    default:
      return {
        label: s || 'UNKNOWN',
        icon: HelpCircle,
        styleClass: 'bg-slate-100 text-slate-600 border-slate-200 dark:bg-zinc-800 dark:text-zinc-400 dark:border-zinc-700',
        tooltip: s
      }
  }
})
</script>
