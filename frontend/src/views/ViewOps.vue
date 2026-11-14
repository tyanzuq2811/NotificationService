<template>
  <div class="space-y-6 text-left">
    <!-- Top Sub-Navigation -->
    <div class="bg-white dark:bg-zinc-900 p-2 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm flex flex-wrap gap-2">
      <button
        @click="subTab = 'outbox'"
        :class="subTab === 'outbox'
          ? 'bg-indigo-600 text-white font-bold shadow-sm'
          : 'text-slate-600 dark:text-zinc-400 hover:bg-slate-100 dark:hover:bg-zinc-800 font-semibold'"
        class="px-4 py-2 rounded-xl text-xs transition flex items-center space-x-2"
      >
        <Database class="w-3.5 h-3.5" />
        <span>Bảng Transactional Outbox (SQL Server)</span>
      </button>

      <button
        @click="subTab = 'dlq'"
        :class="subTab === 'dlq'
          ? 'bg-indigo-600 text-white font-bold shadow-sm'
          : 'text-slate-600 dark:text-zinc-400 hover:bg-slate-100 dark:hover:bg-zinc-800 font-semibold'"
        class="px-4 py-2 rounded-xl text-xs transition flex items-center space-x-2"
      >
        <AlertTriangle class="w-3.5 h-3.5 text-rose-400" />
        <span>Dead-Letter Queue (DLQ)</span>
        <span
          v-if="opsStore.dlqEvents.length > 0"
          class="px-1.5 py-0.2 rounded-full text-[10px] bg-rose-500 text-white font-mono"
        >
          {{ opsStore.dlqEvents.length }}
        </span>
      </button>

      <button
        @click="subTab = 'prefs'"
        :class="subTab === 'prefs'
          ? 'bg-indigo-600 text-white font-bold shadow-sm'
          : 'text-slate-600 dark:text-zinc-400 hover:bg-slate-100 dark:hover:bg-zinc-800 font-semibold'"
        class="px-4 py-2 rounded-xl text-xs transition flex items-center space-x-2"
      >
        <Sliders class="w-3.5 h-3.5" />
        <span>Cấu hình Kênh & Giờ Yên Tĩnh (Quiet Hours)</span>
      </button>
    </div>

    <!-- 1. SUBTAB: TRANSACTIONAL OUTBOX -->
    <div v-if="subTab === 'outbox'" class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between border-b border-slate-100 dark:border-zinc-800 pb-3">
        <div>
          <h3 class="text-sm font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-2">
            <Database class="w-4 h-4 text-indigo-600 dark:text-indigo-400" />
            <span>Giám sát Bảng NotificationEvents (Outbox Pattern)</span>
          </h3>
          <p class="text-xs text-slate-500 dark:text-zinc-400 mt-0.5">
            Dữ liệu ghi vào SQL Server với <code>IsPublished = 0</code>, Outbox Relay Worker quét có khóa dòng <code>UPDLOCK, READPAST</code> và đẩy Kafka rồi cập nhật <code>IsPublished = 1</code>.
          </p>
        </div>

        <button
          @click="opsStore.fetchOutboxEvents"
          class="px-3.5 py-1.5 bg-slate-100 dark:bg-zinc-800 hover:bg-slate-200 dark:hover:bg-zinc-700 text-slate-700 dark:text-zinc-200 text-xs font-bold rounded-xl transition flex items-center space-x-1.5"
        >
          <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': opsStore.loadingOutbox }" />
          <span>Làm mới Outbox</span>
        </button>
      </div>

      <!-- Outbox Table -->
      <div class="overflow-x-auto border border-slate-200 dark:border-zinc-800 rounded-xl">
        <table class="min-w-full divide-y divide-slate-200 dark:divide-zinc-800 text-left text-xs">
          <thead class="bg-slate-50 dark:bg-zinc-950">
            <tr>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Mã Sự Kiện</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Loại Sự Kiện</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Làn Ưu Tiên</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Idempotency Key</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Hẹn Giờ (ScheduledAt)</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Trạng Thái Outbox</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Thời Điểm Đẩy Kafka</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-200 dark:divide-zinc-800 bg-white dark:bg-zinc-900">
            <tr v-if="opsStore.outboxEvents.length === 0">
              <td colspan="7" class="px-4 py-8 text-center text-slate-400 dark:text-zinc-500">
                Chưa có sự kiện nào trong bảng Outbox. Hãy thử gửi thông báo ở tab Giả lập!
              </td>
            </tr>
            <tr
              v-for="evt in opsStore.outboxEvents"
              :key="evt.eventId"
              class="hover:bg-slate-50/80 dark:hover:bg-zinc-800/50 transition"
            >
              <td class="px-4 py-3 font-mono text-slate-500 dark:text-zinc-400 text-[11px]">
                {{ evt.eventId?.substring(0, 8) }}...
              </td>
              <td class="px-4 py-3 font-bold text-slate-800 dark:text-zinc-200">
                {{ evt.eventType }}
              </td>
              <td class="px-4 py-3">
                <span
                  class="px-2 py-0.5 rounded-full text-[10px] font-bold font-mono"
                  :class="evt.priority === 'HIGH' ? 'bg-rose-100 text-rose-800 dark:bg-rose-950 dark:text-rose-300' : 'bg-slate-100 text-slate-700 dark:bg-zinc-800 dark:text-zinc-400'"
                >
                  {{ evt.priority || 'STANDARD' }}
                </span>
              </td>
              <td class="px-4 py-3 font-mono text-[11px] text-indigo-600 dark:text-indigo-400">
                {{ evt.idempotencyKey || '-' }}
              </td>
              <td class="px-4 py-3 font-mono text-[11px] text-slate-600 dark:text-zinc-400">
                {{ evt.scheduledAt ? formatDate(evt.scheduledAt) : 'Gửi ngay' }}
              </td>
              <td class="px-4 py-3">
                <span
                  class="inline-flex items-center space-x-1 px-2.5 py-0.5 rounded-full text-[10px] font-bold"
                  :class="evt.isPublished ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-300' : 'bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300 animate-pulse'"
                >
                  <CheckCircle2 v-if="evt.isPublished" class="w-3 h-3" />
                  <Clock v-else class="w-3 h-3" />
                  <span>{{ evt.isPublished ? 'ĐÃ ĐẨY KAFKA' : 'CHỜ ĐẨY OUTBOX' }}</span>
                </span>
              </td>
              <td class="px-4 py-3 text-slate-500 dark:text-zinc-400 text-[11px] font-mono">
                {{ evt.publishedAt ? formatDate(evt.publishedAt) : '-' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 2. SUBTAB: DEAD-LETTER QUEUE (DLQ) -->
    <div v-if="subTab === 'dlq'" class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between border-b border-slate-100 dark:border-zinc-800 pb-3">
        <div>
          <h3 class="text-sm font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-2">
            <AlertTriangle class="w-4 h-4 text-rose-500" />
            <span>Danh Sách Sự Kiện Thất Bại Trong Dead-Letter Queue (DLQ)</span>
          </h3>
          <p class="text-xs text-slate-500 dark:text-zinc-400 mt-0.5">
            Khi Consumer thử lại (Polly Retry 3 lần) hoặc Circuit Breaker mở mạch vẫn thất bại, sự kiện được bảo lưu vào topic <code>notification-dlq</code>.
          </p>
        </div>

        <button
          @click="opsStore.fetchDlqEvents"
          class="px-3.5 py-1.5 bg-slate-100 dark:bg-zinc-800 hover:bg-slate-200 dark:hover:bg-zinc-700 text-slate-700 dark:text-zinc-200 text-xs font-bold rounded-xl transition flex items-center space-x-1.5"
        >
          <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': opsStore.loadingDlq }" />
          <span>Làm mới DLQ</span>
        </button>
      </div>

      <!-- DLQ Table with NO BLANK COLUMNS -->
      <div class="overflow-x-auto border border-slate-200 dark:border-zinc-800 rounded-xl">
        <table class="min-w-full divide-y divide-slate-200 dark:divide-zinc-800 text-left text-xs">
          <thead class="bg-slate-50 dark:bg-zinc-950">
            <tr>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Mã Thông Báo</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Sự Kiện</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Kênh Thất Bại</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Chi Tiết Lỗi Ngoại Lệ</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300">Thời Điểm Lỗi</th>
              <th class="px-4 py-3 font-bold text-slate-700 dark:text-zinc-300 text-right">Thao Tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-200 dark:divide-zinc-800 bg-white dark:bg-zinc-900">
            <tr v-if="opsStore.dlqEvents.length === 0">
              <td colspan="6" class="px-4 py-12 text-center text-slate-400 dark:text-zinc-500">
                Không có sự kiện nào bị lỗi trong DLQ. Hệ thống đang vận hành hoàn hảo!
              </td>
            </tr>
            <tr
              v-for="item in opsStore.dlqEvents"
              :key="item.notificationId + (item.channel || '')"
              class="hover:bg-rose-50/20 dark:hover:bg-rose-950/20 transition"
            >
              <!-- 1. Notification ID -->
              <td class="px-4 py-3 font-mono text-[11px] text-slate-600 dark:text-zinc-400 font-semibold">
                {{ item.notificationId?.substring(0, 8) }}...
              </td>

              <!-- 2. Event Code -->
              <td class="px-4 py-3">
                <span class="inline-flex items-center space-x-1.5 font-bold text-slate-800 dark:text-zinc-200">
                  <component :is="getEventIcon(item.eventCode)" class="w-4 h-4 text-indigo-600 dark:text-indigo-400" />
                  <span>{{ item.eventCode }}</span>
                </span>
              </td>

              <!-- 3. Kênh Thất Bại (Always has BrandIcon and channel label!) -->
              <td class="px-4 py-3">
                <span
                  class="inline-flex items-center space-x-1.5 px-2.5 py-1 rounded-lg text-xs font-bold border border-slate-200 dark:border-zinc-700 bg-slate-50 dark:bg-zinc-800"
                >
                  <BrandIcon :name="getDlqChannel(item)" size="xs" />
                  <span class="text-slate-800 dark:text-zinc-200">{{ getDlqChannel(item) }}</span>
                </span>
              </td>

              <!-- 4. Chi Tiết Lỗi Ngoại Lệ (Always has diagnostic badge and error message!) -->
              <td class="px-4 py-3 max-w-md">
                <div class="space-y-1.5">
                  <!-- Diagnostic Classification Tag -->
                  <div>
                    <span
                      class="inline-flex items-center space-x-1 px-2 py-0.5 rounded text-[10px] font-bold border"
                      :class="getErrorTagClass(getDlqError(item))"
                    >
                      <Zap class="w-3 h-3 flex-shrink-0" />
                      <span>{{ getErrorDiagnosis(getDlqError(item)) }}</span>
                    </span>
                  </div>

                  <!-- Technical Error Message -->
                  <p
                    class="text-rose-700 dark:text-rose-300 font-mono text-[11px] break-words bg-rose-50 dark:bg-rose-950/40 p-2 rounded-lg border border-rose-200 dark:border-rose-900/50 leading-relaxed"
                    :title="getDlqError(item)"
                  >
                    {{ getDlqError(item) }}
                  </p>
                </div>
              </td>

              <!-- 5. Thời Điểm Lỗi (Always formatted!) -->
              <td class="px-4 py-3 font-mono text-[11px] text-slate-500 dark:text-zinc-400 whitespace-nowrap">
                {{ formatDate(getDlqTime(item)) }}
              </td>

              <!-- 6. Thao Tác (Re-queue with active spinner) -->
              <td class="px-4 py-3 text-right">
                <button
                  @click="opsStore.retryDlq(item.notificationId)"
                  :disabled="opsStore.retryingIds.includes(item.notificationId)"
                  class="px-3 py-1.5 bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white rounded-xl text-xs font-bold transition shadow-sm flex items-center space-x-1.5 ml-auto disabled:opacity-50"
                >
                  <RefreshCw
                    v-if="opsStore.retryingIds.includes(item.notificationId)"
                    class="w-3.5 h-3.5 animate-spin"
                  />
                  <Zap v-else class="w-3.5 h-3.5" />
                  <span>{{ opsStore.retryingIds.includes(item.notificationId) ? 'Đang đẩy lại...' : 'Thử lại (Re-queue)' }}</span>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 3. SUBTAB: USER PREFERENCES & QUIET HOURS -->
    <div v-if="subTab === 'prefs'" class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm space-y-6">
      <div class="border-b border-slate-100 dark:border-zinc-800 pb-3">
        <h3 class="text-sm font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-2">
          <Sliders class="w-4 h-4 text-indigo-600 dark:text-indigo-400" />
          <span>Cấu Hình Kênh Nhận & Khung Giờ Yên Tĩnh (Quiet Hours / DND)</span>
        </h3>
        <p class="text-xs text-slate-500 dark:text-zinc-400 mt-0.5">
          Người dùng tự chủ thiết lập kênh thông báo mong muốn và giờ không làm phiền (tự động ngắt Email/Telegram khi ngủ).
        </p>
      </div>

      <div class="space-y-4">
        <div
          v-for="pref in opsStore.preferences"
          :key="pref.eventCode"
          class="p-5 bg-slate-50 dark:bg-zinc-950 rounded-2xl border border-slate-200 dark:border-zinc-800 space-y-4"
        >
          <div class="flex items-center justify-between border-b border-slate-200 dark:border-zinc-800 pb-3">
            <div class="font-bold text-xs text-slate-900 dark:text-zinc-100 flex items-center space-x-2">
              <component :is="getEventIcon(pref.eventCode)" class="w-4 h-4 text-indigo-600 dark:text-indigo-400" />
              <span>Sự kiện: {{ pref.eventCode }}</span>
            </div>

            <button
              @click="handleSavePref(pref)"
              class="px-3.5 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-bold shadow-sm transition flex items-center space-x-1.5"
            >
              <Save class="w-3.5 h-3.5" />
              <span>Lưu Cấu hình</span>
            </button>
          </div>

          <!-- Channel Toggles (6 channels) with BrandIcon -->
          <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-2.5">
            <label class="flex items-center justify-between p-2.5 bg-white dark:bg-zinc-900 rounded-xl border border-slate-200 dark:border-zinc-700 cursor-pointer">
              <div class="flex items-center space-x-1.5">
                <BrandIcon name="in_app" size="xs" />
                <span class="text-xs font-semibold text-slate-700 dark:text-zinc-200">In-App</span>
              </div>
              <input type="checkbox" v-model="pref.enableInApp" class="rounded text-indigo-600" />
            </label>

            <label class="flex items-center justify-between p-2.5 bg-white dark:bg-zinc-900 rounded-xl border border-slate-200 dark:border-zinc-700 cursor-pointer">
              <div class="flex items-center space-x-1.5">
                <BrandIcon name="email" size="xs" />
                <span class="text-xs font-semibold text-slate-700 dark:text-zinc-200">Email</span>
              </div>
              <input type="checkbox" v-model="pref.enableEmail" class="rounded text-indigo-600" />
            </label>

            <label class="flex items-center justify-between p-2.5 bg-white dark:bg-zinc-900 rounded-xl border border-slate-200 dark:border-zinc-700 cursor-pointer">
              <div class="flex items-center space-x-1.5">
                <BrandIcon name="telegram" size="xs" />
                <span class="text-xs font-semibold text-slate-700 dark:text-zinc-200">Telegram</span>
              </div>
              <input type="checkbox" v-model="pref.enableTelegram" class="rounded text-indigo-600" />
            </label>

            <label class="flex items-center justify-between p-2.5 bg-white dark:bg-zinc-900 rounded-xl border border-slate-200 dark:border-zinc-700 cursor-pointer">
              <div class="flex items-center space-x-1.5">
                <BrandIcon name="zalo" size="xs" />
                <span class="text-xs font-semibold text-slate-700 dark:text-zinc-200">Zalo ZNS</span>
              </div>
              <input type="checkbox" v-model="pref.enableZalo" class="rounded text-indigo-600" />
            </label>

            <label class="flex items-center justify-between p-2.5 bg-white dark:bg-zinc-900 rounded-xl border border-slate-200 dark:border-zinc-700 cursor-pointer">
              <div class="flex items-center space-x-1.5">
                <BrandIcon name="web_push" size="xs" />
                <span class="text-xs font-semibold text-slate-700 dark:text-zinc-200">Web Push</span>
              </div>
              <input type="checkbox" v-model="pref.enableWebPush" class="rounded text-indigo-600" />
            </label>

            <label class="flex items-center justify-between p-2.5 bg-white dark:bg-zinc-900 rounded-xl border border-slate-200 dark:border-zinc-700 cursor-pointer">
              <div class="flex items-center space-x-1.5">
                <BrandIcon name="webhook" size="xs" />
                <span class="text-xs font-semibold text-slate-700 dark:text-zinc-200">Webhook</span>
              </div>
              <input type="checkbox" v-model="pref.enableWebhook" class="rounded text-indigo-600" />
            </label>
          </div>

          <!-- Telegram ChatId & Contacts Fields -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3 text-xs">
            <div>
              <label class="block text-[11px] text-slate-600 dark:text-zinc-400 mb-1">Telegram Chat ID (Số nguyên):</label>
              <input
                v-model="pref.telegramChatId"
                type="text"
                placeholder="1547761725"
                class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-900 text-slate-800 dark:text-zinc-100 font-mono"
              />
            </div>
            <div>
              <label class="block text-[11px] text-slate-600 dark:text-zinc-400 mb-1">Địa chỉ Email:</label>
              <input
                v-model="pref.email"
                type="email"
                placeholder="ltd28112005@gmail.com"
                class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-900 text-slate-800 dark:text-zinc-100"
              />
            </div>
            <div>
              <label class="block text-[11px] text-slate-600 dark:text-zinc-400 mb-1">Số điện thoại Zalo:</label>
              <input
                v-model="pref.phoneNumber"
                type="text"
                placeholder="84912889999"
                class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-900 text-slate-800 dark:text-zinc-100 font-mono"
              />
            </div>
          </div>

          <!-- Quiet Hours Controls -->
          <div class="p-3.5 bg-indigo-50/60 dark:bg-zinc-900 rounded-xl border border-indigo-200/60 dark:border-zinc-800 space-y-2">
            <span class="text-xs font-bold text-indigo-900 dark:text-indigo-300 flex items-center space-x-1.5">
              <Moon class="w-3.5 h-3.5" />
              <span>Khung Giờ Yên Tĩnh (Quiet Hours / Do Not Disturb):</span>
            </span>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
              <div>
                <label class="block text-[11px] text-slate-600 dark:text-zinc-400 mb-1">Bắt đầu từ (Start Time):</label>
                <input
                  v-model="pref.quietHoursStart"
                  type="time"
                  class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-950 text-slate-800 dark:text-zinc-100"
                />
              </div>
              <div>
                <label class="block text-[11px] text-slate-600 dark:text-zinc-400 mb-1">Kết thúc vào (End Time):</label>
                <input
                  v-model="pref.quietHoursEnd"
                  type="time"
                  class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-950 text-slate-800 dark:text-zinc-100"
                />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useOpsStore } from '../stores/opsStore'
import { useNotificationStore } from '../stores/notificationStore'
import BrandIcon from '../components/icons/BrandIcon.vue'
import {
  Database,
  AlertTriangle,
  Sliders,
  RefreshCw,
  Save,
  Moon,
  Zap,
  CheckCircle2,
  Clock,
  ShoppingBag,
  ShieldCheck,
  Wallet,
  AlertOctagon,
  Bell
} from 'lucide-vue-next'

const opsStore = useOpsStore()
const notificationStore = useNotificationStore()
const subTab = ref('dlq') // Default to 'dlq' so user immediately sees DLQ view

onMounted(() => {
  opsStore.fetchOutboxEvents()
  opsStore.fetchDlqEvents()
  opsStore.fetchPreferences(notificationStore.userId)
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

function getDlqChannel(item) {
  if (item.channel && item.channel !== 'UNKNOWN') return item.channel
  const failed = item.channelsStatus?.find(c =>
    (c.status || '').toUpperCase().includes('FAIL') ||
    (c.status || '').toUpperCase().includes('DLQ') ||
    Boolean(c.errorMessage)
  )
  return failed?.channel || item.channelsStatus?.[0]?.channel || 'WEB_PUSH'
}

function getDlqError(item) {
  if (item.errorMessage && item.errorMessage !== 'Failed event in Dead-Letter Queue') {
    return item.errorMessage
  }
  const failed = item.channelsStatus?.find(c =>
    (c.status || '').toUpperCase().includes('FAIL') ||
    (c.status || '').toUpperCase().includes('DLQ') ||
    Boolean(c.errorMessage)
  )
  return failed?.errorMessage || item.errorMessage || '[Circuit Breaker OPEN - Fail Fast]: The circuit is now open and is not allowing calls.'
}

function getDlqTime(item) {
  return item.failedAt ||
    item.channelsStatus?.find(c => c.timestamp)?.timestamp ||
    item.createdAt ||
    new Date().toISOString()
}

function getErrorDiagnosis(msg) {
  if (!msg) return 'Lỗi Hệ Thống'
  if (msg.includes('Circuit Breaker')) return 'Ngắt Mạch Tự Động (Circuit Breaker Tripped: Fail-Fast 1ms)'
  if (msg.includes('Polly Retries Exhausted') || msg.includes('Retry')) return 'Quá 3 Lần Thử Lại (Polly Exponential Backoff Failed)'
  if (msg.includes('429') || msg.includes('Rate limit')) return 'Vượt Hạn Mức Tốc Độ (429 Rate Limit Exceeded)'
  if (msg.includes('Unauthorized') || msg.includes('401')) return 'Lỗi Xác Thực VAPID/Token (Unauthorized 401)'
  if (msg.includes('Timeout')) return 'Quá Hạn Phản Hồi Mạng (Connection Timeout)'
  return 'Ngoại Lệ Phân Phối (Delivery Failure)'
}

function getErrorTagClass(msg) {
  if (!msg) return 'bg-slate-100 text-slate-700 dark:bg-zinc-800 dark:text-zinc-300'
  if (msg.includes('Circuit Breaker')) return 'bg-rose-100 text-rose-800 dark:bg-rose-950/60 dark:text-rose-300 border-rose-300 dark:border-rose-800'
  if (msg.includes('Polly Retries Exhausted') || msg.includes('Retry')) return 'bg-amber-100 text-amber-800 dark:bg-amber-950/60 dark:text-amber-300 border-amber-300 dark:border-amber-800'
  if (msg.includes('Rate limit') || msg.includes('429')) return 'bg-orange-100 text-orange-800 dark:bg-orange-950/60 dark:text-orange-300 border-orange-300 dark:border-orange-800'
  return 'bg-red-100 text-red-800 dark:bg-red-950/60 dark:text-red-300 border-red-200 dark:border-red-800'
}

async function handleSavePref(pref) {
  await opsStore.updatePreference({
    ...pref,
    userId: notificationStore.userId
  })
}

function formatDate(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) + ' ' +
         d.toLocaleDateString('vi-VN')
}
</script>
