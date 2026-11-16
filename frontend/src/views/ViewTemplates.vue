<template>
  <div class="space-y-6 text-left">
    <div class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm flex flex-col md:flex-row items-center justify-between gap-4">
      <div>
        <h2 class="text-base font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-2">
          <FileCode2 class="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
          <span>Studio Quản lý Mẫu Thông báo (Dynamic Template Studio)</span>
        </h2>
        <p class="text-xs text-slate-500 dark:text-zinc-400 mt-1">
          Chỉnh sửa mẫu thông báo trong SQL Server. Hệ thống tự động phát tán sự kiện <code>system-config-events</code> qua Kafka để xóa Cache trên Consumer Service mà không cần khởi động lại.
        </p>
      </div>

      <button
        @click="opsStore.fetchTemplates"
        class="px-4 py-2 bg-slate-100 dark:bg-zinc-800 hover:bg-slate-200 dark:hover:bg-zinc-700 text-slate-700 dark:text-zinc-200 text-xs font-bold rounded-xl transition flex items-center space-x-1.5"
      >
        <RefreshCw class="w-3.5 h-3.5" :class="{ 'animate-spin': opsStore.loadingTemplates }" />
        <span>Làm mới Templates</span>
      </button>
    </div>

    <!-- Split-Screen Editor -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-6">
      <!-- Left Column: Template List & Editor (6 cols) -->
      <div class="lg:col-span-6 space-y-4">
        <!-- Template Picker Card -->
        <div class="bg-white dark:bg-zinc-900 p-5 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm space-y-3">
          <label class="block text-xs font-bold text-slate-700 dark:text-zinc-300 uppercase tracking-wider">
            1. Chọn Mẫu Cần Chỉnh Sửa (18 Templates):
          </label>
          <div class="grid grid-cols-2 sm:grid-cols-3 gap-2 max-h-56 overflow-y-auto pr-1">
            <button
              v-for="t in opsStore.templates"
              :key="t.templateId"
              @click="selectTemplate(t)"
              :class="[
                currentTemplate?.templateId === t.templateId
                  ? 'border-indigo-600 bg-indigo-50 dark:bg-indigo-950/60 text-indigo-900 dark:text-indigo-200 font-bold ring-2 ring-indigo-500'
                  : 'border-slate-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 hover:bg-slate-50 dark:hover:bg-zinc-750 text-slate-700 dark:text-zinc-300',
                'p-2.5 rounded-xl border text-left text-xs transition space-y-1'
              ]"
            >
              <div class="font-bold flex items-center space-x-1.5">
                <BrandIcon :name="t.channel" size="xs" />
                <span>{{ t.channel }}</span>
              </div>
              <div class="text-[10px] text-slate-500 dark:text-zinc-400 font-mono truncate">{{ t.eventCode }}</div>
            </button>
          </div>
        </div>

        <!-- Editor Form Card -->
        <div v-if="currentTemplate" class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm space-y-4">
          <div class="flex items-center justify-between border-b border-slate-100 dark:border-zinc-800 pb-3">
            <h3 class="text-xs font-bold uppercase tracking-wider text-slate-700 dark:text-zinc-300 flex items-center space-x-1.5">
              <span>Chỉnh Sửa:</span>
              <span class="text-indigo-600 dark:text-indigo-400">{{ currentTemplate.eventCode }} ({{ currentTemplate.channel }})</span>
            </h3>

            <button
              @click="handleSave"
              :disabled="saving"
              class="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-bold shadow-md transition disabled:opacity-50 flex items-center space-x-1.5"
            >
              <Save class="w-3.5 h-3.5" />
              <span>{{ saving ? 'Đang lưu...' : 'Lưu & Xóa Cache Kafka' }}</span>
            </button>
          </div>

          <!-- Title Input -->
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-zinc-300 mb-1">Mẫu Tiêu Đề (Title Template):</label>
            <input
              v-model="editTitle"
              type="text"
              class="w-full text-xs px-3 py-2 border border-slate-300 dark:border-zinc-700 rounded-xl focus:ring-2 focus:ring-indigo-500 bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 font-mono"
            />
          </div>

          <!-- Body Textarea -->
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-zinc-300 mb-1">Mẫu Nội Dung (Body Template / HTML / Text):</label>
            <textarea
              v-model="editBody"
              rows="7"
              class="w-full text-xs p-3 border border-slate-300 dark:border-zinc-700 rounded-xl focus:ring-2 focus:ring-indigo-500 bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 font-mono leading-relaxed"
            ></textarea>
          </div>

          <!-- Quick Variable Chips -->
          <div class="p-3 bg-slate-50 dark:bg-zinc-950 rounded-xl border border-slate-200 dark:border-zinc-800 text-[11px] text-slate-600 dark:text-zinc-400 space-y-1.5">
            <span class="font-bold text-slate-800 dark:text-zinc-200">Bấm để chèn biến mẫu vào nội dung:</span>
            <div class="flex flex-wrap gap-1.5 font-mono text-[10px]">
              <button
                v-for="v in availableVariables"
                :key="v"
                type="button"
                @click="insertVariable(v)"
                class="px-2 py-0.5 rounded bg-indigo-50 dark:bg-indigo-950/60 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800 hover:bg-indigo-100 transition"
              >
                {{ v }}
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Right Column: Live Device Previews (6 channels) -->
      <div class="lg:col-span-6 space-y-4">
        <div class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm space-y-4">
          <div class="flex flex-col sm:flex-row sm:items-center justify-between border-b border-slate-100 dark:border-zinc-800 pb-3 gap-2">
            <h3 class="text-xs font-bold uppercase tracking-wider text-slate-700 dark:text-zinc-300 flex items-center space-x-1.5">
              <Eye class="w-4 h-4 text-indigo-600 dark:text-indigo-400" />
              <span>Xem Trước Trực Quan Thiết Bị (6 Kênh)</span>
            </h3>

            <div class="flex flex-wrap gap-1 bg-slate-100 dark:bg-zinc-800 p-1 rounded-xl text-[11px]">
              <button
                v-for="ch in ['EMAIL', 'TELEGRAM', 'ZALO_ZNS', 'IN_APP', 'WEB_PUSH', 'WEBHOOK']"
                :key="ch"
                @click="previewChannel = ch"
                :class="previewChannel === ch ? 'bg-white dark:bg-zinc-700 shadow-sm text-indigo-600 dark:text-indigo-300 font-bold' : 'text-slate-500 dark:text-zinc-400'"
                class="px-2 py-0.5 rounded-lg transition"
              >
                {{ ch.replace('_ZNS', '').replace('_', ' ') }}
              </button>
            </div>
          </div>

          <!-- 1. EMAIL PREVIEW -->
          <div v-if="previewChannel === 'EMAIL'" class="border border-slate-200 dark:border-zinc-800 rounded-xl overflow-hidden shadow-inner">
            <div class="bg-indigo-950 p-4 text-center text-white space-y-1">
              <div class="inline-block p-2 rounded-full bg-white/15">
                <Bell class="w-4 h-4 text-white" />
              </div>
              <h4 class="text-xs font-bold tracking-wide uppercase">NotifyHub &bull; Nền Tảng Thông Báo Đa Kênh</h4>
            </div>
            <div class="p-5 bg-white dark:bg-zinc-950 space-y-3">
              <span class="inline-block px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
                ĐƠN HÀNG ĐÃ XÁC NHẬN
              </span>
              <h4 class="font-bold text-sm text-slate-900 dark:text-zinc-100">{{ renderedTitle }}</h4>
              <p class="text-xs text-slate-600 dark:text-zinc-300 leading-relaxed">{{ renderedBody }}</p>
              <div class="bg-slate-50 dark:bg-zinc-900 p-4 rounded-xl text-center border border-slate-200 dark:border-zinc-800">
                <div class="text-[10px] text-slate-400 uppercase font-bold">Tổng thanh toán</div>
                <div class="text-xl font-bold text-slate-900 dark:text-zinc-100 mt-1">3.250.000 đ</div>
              </div>
              <div class="text-center pt-2">
                <a href="#" class="inline-block px-5 py-2 bg-indigo-600 text-white rounded-lg text-xs font-bold shadow-sm">
                  Theo dõi trạng thái đơn hàng &rarr;
                </a>
              </div>
            </div>
          </div>

          <!-- 2. TELEGRAM CHAT PREVIEW -->
          <div v-else-if="previewChannel === 'TELEGRAM'" class="bg-[#0e1621] p-4 rounded-xl space-y-3 shadow-inner">
            <div class="text-[10px] text-slate-400 font-mono text-center">Telegram Bot API &bull; ChatId: 1547761725</div>
            <div class="max-w-[90%] bg-[#182533] p-3 rounded-2xl rounded-bl-sm text-white space-y-2 text-xs shadow-md">
              <div class="font-bold text-indigo-300">{{ renderedTitle }}</div>
              <div class="text-[11px] text-slate-200 leading-relaxed">{{ renderedBody }}</div>
              <blockquote class="border-l-2 border-indigo-400 pl-2 text-[10px] text-slate-300 font-mono bg-[#1c2c3d] p-1.5 rounded">
                <div>&bull; Tổng thanh toán: <code>3.250.000 đ</code></div>
                <div>&bull; Thời gian: <code>16:25:33 &bull; 14/11/2026</code></div>
                <div>&bull; Trạng thái: Đã xác nhận</div>
              </blockquote>
              <div class="text-[9px] text-slate-400 text-right">16:25 &bull; Đã chuyển</div>
            </div>
          </div>

          <!-- 3. ZALO ZNS PREVIEW -->
          <div v-else-if="previewChannel === 'ZALO_ZNS'" class="bg-sky-50 dark:bg-zinc-950 p-4 rounded-xl space-y-3 shadow-inner border border-sky-100 dark:border-zinc-800">
            <div class="text-[10px] text-sky-800 dark:text-sky-400 font-bold flex items-center justify-between">
              <span>Zalo Official Account &bull; OTP & Đơn hàng</span>
              <span class="font-mono">84766408268</span>
            </div>
            <div class="bg-white dark:bg-zinc-900 p-4 rounded-2xl border border-sky-200 dark:border-zinc-800 shadow-sm space-y-2.5 text-xs">
              <div class="font-bold text-slate-900 dark:text-zinc-100 text-sm">{{ renderedTitle }}</div>
              <p class="text-[11px] text-slate-600 dark:text-zinc-300 leading-relaxed">{{ renderedBody }}</p>
              <div class="border-t border-b border-slate-100 dark:border-zinc-800 py-2 space-y-1 text-[11px]">
                <div class="flex justify-between text-slate-500"><span>Mã đơn hàng:</span><span class="font-bold text-slate-800 dark:text-zinc-200">#DH-VN-9999</span></div>
                <div class="flex justify-between text-slate-500"><span>Tổng tiền:</span><span class="font-bold text-slate-800 dark:text-zinc-200">3.250.000 đ</span></div>
              </div>
              <button class="w-full py-1.5 bg-[#0068FF] text-white text-xs font-bold rounded-lg shadow-sm">
                Xem chi tiết đơn hàng
              </button>
            </div>
          </div>

          <!-- 4. IN-APP WEB NOTIFICATION PREVIEW -->
          <div v-else-if="previewChannel === 'IN_APP'" class="p-4 bg-slate-100 dark:bg-zinc-950 rounded-xl space-y-2">
            <div class="text-[10px] text-slate-500 font-mono">SignalR Hub WebSocket Push</div>
            <div class="bg-white dark:bg-zinc-900 p-4 rounded-xl border border-indigo-200 dark:border-indigo-900/50 shadow-md flex items-start space-x-3">
              <div class="p-2 rounded-lg bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 flex-shrink-0">
                <CheckCircle2 class="w-4 h-4" />
              </div>
              <div class="space-y-1 flex-1">
                <div class="text-xs font-bold text-slate-900 dark:text-zinc-100">{{ renderedTitle }}</div>
                <div class="text-[11px] text-slate-600 dark:text-zinc-300">{{ renderedBody }}</div>
                <div class="text-[9px] text-slate-400 font-mono">Vừa xong &bull; Nhóm: user_user_demo</div>
              </div>
            </div>
          </div>

          <!-- 5. WEB PUSH WINDOWS PREVIEW -->
          <div v-else-if="previewChannel === 'WEB_PUSH'" class="p-4 bg-slate-800 rounded-xl text-white space-y-2">
            <div class="text-[10px] text-slate-400 font-mono">Windows 11 Action Center &bull; W3C Service Worker</div>
            <div class="bg-slate-900/90 p-3.5 rounded-xl border border-slate-700 shadow-xl space-y-2 max-w-sm ml-auto">
              <div class="flex items-center space-x-2 text-[10px] text-slate-400">
                <BrandIcon name="web_push" size="xs" />
                <span class="font-bold text-slate-300">Google Chrome &bull; Notification Hub</span>
              </div>
              <div class="font-bold text-xs text-white">{{ renderedTitle }}</div>
              <div class="text-[11px] text-slate-300 leading-snug">{{ renderedBody }}</div>
              <div class="pt-1 flex justify-end">
                <button class="px-3 py-1 bg-indigo-600 hover:bg-indigo-500 text-white rounded text-[10px] font-bold">
                  Xem chi tiết
                </button>
              </div>
            </div>
          </div>

          <!-- 6. DISCORD WEBHOOK PREVIEW -->
          <div v-else-if="previewChannel === 'WEBHOOK'" class="bg-[#313338] p-4 rounded-xl text-white space-y-2">
            <div class="flex items-center space-x-2">
              <div class="w-6 h-6 rounded-full bg-indigo-500 flex items-center justify-center text-[10px] font-bold">NH</div>
              <span class="text-xs font-bold text-white">NotifyHub Gateway</span>
              <span class="text-[9px] px-1 py-0.2 bg-[#5865F2] text-white rounded font-bold">BOT</span>
            </div>
            <div class="border-l-4 border-emerald-500 bg-[#2b2d31] p-3 rounded-r-lg space-y-2 max-w-md">
              <div class="text-[10px] uppercase font-bold text-slate-400">THÔNG BÁO GIAO DỊCH &bull; ĐƠN HÀNG</div>
              <div class="text-xs font-bold text-white">{{ renderedTitle }}</div>
              <div class="text-[11px] text-slate-300">{{ renderedBody }}</div>
              <div class="grid grid-cols-2 gap-2 text-[10px] pt-1 border-t border-slate-700/50">
                <div><span class="text-slate-400 block">Khách hàng</span><span class="font-bold">Lê Tuấn Dũng</span></div>
                <div><span class="text-slate-400 block">Giá trị đơn hàng</span><span class="font-bold text-emerald-400">3.250.000 đ</span></div>
                <div><span class="text-slate-400 block">Trạng thái</span><span class="font-bold">Đã xác nhận</span></div>
                <div><span class="text-slate-400 block">Mã tham chiếu</span><span class="font-mono text-indigo-400">b49aac63</span></div>
              </div>
              <div class="text-[9px] text-slate-400 pt-1 border-t border-slate-700/30">
                NotifyHub Microservices &bull; Khoa CNTT - Đại học Đại Nam
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useOpsStore } from '../stores/opsStore'
import BrandIcon from '../components/icons/BrandIcon.vue'
import {
  FileCode2,
  RefreshCw,
  Save,
  Eye,
  Bell,
  CheckCircle2
} from 'lucide-vue-next'

const opsStore = useOpsStore()

const currentTemplate = ref(null)
const editTitle = ref('')
const editBody = ref('')
const saving = ref(false)
const previewChannel = ref('EMAIL')

const availableVariables = [
  '{{orderId}}',
  '{{amount}}',
  '{{userName}}',
  '{{time}}',
  '{{otp}}',
  '{{balance}}',
  '{{reason}}'
]

onMounted(async () => {
  await opsStore.fetchTemplates()
  if (opsStore.templates.length > 0) {
    selectTemplate(opsStore.templates[0])
  }
})

function selectTemplate(t) {
  currentTemplate.value = t
  editTitle.value = t.titleTemplate || ''
  editBody.value = t.bodyTemplate || ''
  previewChannel.value = t.channel || 'EMAIL'
}

function insertVariable(v) {
  editBody.value += ' ' + v
}

async function handleSave() {
  if (!currentTemplate.value) return
  saving.value = true
  try {
    await opsStore.updateTemplate(currentTemplate.value.templateId, {
      titleTemplate: editTitle.value,
      bodyTemplate: editBody.value
    })
  } finally {
    saving.value = false
  }
}

// Sample dummy parameters for preview
const dummyParams = {
  orderId: 'DH-VN-9999',
  amount: '3.250.000',
  userName: 'Lê Tuấn Dũng',
  time: '16:25:33 • 14/11/2026',
  otp: '849201',
  balance: '45.000.000 đ',
  reason: 'Thanh toán đơn hàng DH-VN-9999'
}

const renderedTitle = computed(() => {
  let t = editTitle.value || 'Tiêu đề thông báo'
  for (const [k, v] of Object.entries(dummyParams)) {
    t = t.replaceAll(`{{${k}}}`, v)
  }
  return t
})

const renderedBody = computed(() => {
  let b = editBody.value || 'Nội dung thông báo chi tiết'
  for (const [k, v] of Object.entries(dummyParams)) {
    b = b.replaceAll(`{{${k}}}`, v)
  }
  return b
})
</script>
