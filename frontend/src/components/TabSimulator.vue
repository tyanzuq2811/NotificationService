<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <div class="bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
      <div class="border-b border-gray-200 pb-4 mb-6">
        <h2 class="text-lg font-bold text-gray-900 flex items-center space-x-2">
          <span>🚀</span>
          <span>Bộ Giả lập Phát tán Sự kiện (Event Simulator)</span>
        </h2>
        <p class="text-xs text-gray-500 mt-1">
          Dành riêng cho kịch bản bảo vệ đồ án: Mô phỏng các Microservice nghiệp vụ (Bán hàng, Bảo mật, Thanh toán) bắn sự kiện tới Notification Service.
        </p>
      </div>

      <form @submit.prevent="handleSend" class="space-y-6">
        <!-- 1. Chọn loại sự kiện -->
        <div>
          <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-2">
            1. Chọn Loại Sự kiện Nghiệp vụ:
          </label>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div
              v-for="ev in eventTypes"
              :key="ev.code"
              @click="selectEvent(ev)"
              :class="[
                form.eventCode === ev.code
                  ? 'border-blue-600 bg-blue-50/50 ring-2 ring-blue-500'
                  : 'border-gray-200 hover:border-gray-300 bg-white',
                'border rounded-xl p-4 cursor-pointer transition text-left'
              ]"
            >
              <div class="text-2xl mb-1">{{ ev.icon }}</div>
              <div class="text-sm font-bold text-gray-900">{{ ev.name }}</div>
              <div class="text-xs text-gray-500 mt-0.5">{{ ev.desc }}</div>
            </div>
          </div>
        </div>

        <!-- 2. Tham số động theo sự kiện -->
        <div class="bg-gray-50 p-4 rounded-xl border border-gray-200 space-y-4">
          <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider">
            2. Tham số dữ liệu sự kiện:
          </label>

          <!-- Tham số ORDER_SUCCESS -->
          <div v-if="form.eventCode === 'ORDER_SUCCESS'" class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">Mã đơn hàng (orderId):</label>
              <input
                v-model="form.parameters.orderId"
                type="text"
                class="w-full text-xs px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 bg-white"
                required
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">Tổng tiền (amount):</label>
              <input
                v-model="form.parameters.amount"
                type="text"
                class="w-full text-xs px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 bg-white"
                required
              />
            </div>
          </div>

          <!-- Tham số SECURITY_OTP -->
          <div v-if="form.eventCode === 'SECURITY_OTP'" class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">Mã OTP (otp):</label>
              <input
                v-model="form.parameters.otp"
                type="text"
                class="w-full text-xs px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 bg-white"
                required
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">Thời hạn hiệu lực:</label>
              <input
                value="5 phút"
                disabled
                class="w-full text-xs px-3 py-2 border border-gray-200 rounded-lg bg-gray-100 text-gray-500"
              />
            </div>
          </div>

          <!-- Tham số BALANCE_CHANGE -->
          <div v-if="form.eventCode === 'BALANCE_CHANGE'" class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">Loại biến động:</label>
              <select
                v-model="form.parameters.type"
                class="w-full text-xs px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 bg-white"
              >
                <option value="+ (Cộng)">+ (Cộng tiền)</option>
                <option value="- (Trừ)">- (Trừ tiền)</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">Số tiền (amount):</label>
              <input
                v-model="form.parameters.amount"
                type="text"
                class="w-full text-xs px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 bg-white"
                required
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">Lý do giao dịch (reason):</label>
              <input
                v-model="form.parameters.reason"
                type="text"
                class="w-full text-xs px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 bg-white"
                required
              />
            </div>
          </div>
        </div>

        <!-- 3. Địa chỉ người nhận Đa kênh -->
        <div>
          <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-2">
            3. Thông tin người nhận đa kênh:
          </label>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">Email nhận thông báo:</label>
              <input
                v-model="form.email"
                type="email"
                placeholder="vd: khachhang.demo@gmail.com"
                class="w-full text-xs px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 bg-white"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-600 mb-1">
                Telegram Chat ID (Số nguyên numeric):
              </label>
              <input
                v-model.number="form.telegramChatId"
                type="number"
                placeholder="vd: 618920145"
                class="w-full text-xs px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 bg-white"
              />
              <span class="block mt-1 text-[11px] text-gray-500">
                💡 Lấy Chat ID bằng cách nhắn bot <strong class="text-blue-600">@userinfobot</strong> trên Telegram và bấm <strong>/start</strong>.
              </span>
            </div>
          </div>
        </div>

        <!-- 4. Lựa chọn kênh phân phối -->
        <div>
          <label class="block text-xs font-bold text-gray-700 uppercase tracking-wider mb-2">
            4. Chọn các kênh gửi song song:
          </label>
          <div class="flex flex-wrap gap-4">
            <label class="flex items-center space-x-2 text-xs font-medium text-gray-700 cursor-pointer">
              <input
                type="checkbox"
                v-model="form.channels"
                value="IN_APP"
                class="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-blue-500"
              />
              <span>📱 In-App Web (SignalR Realtime)</span>
            </label>

            <label class="flex items-center space-x-2 text-xs font-medium text-gray-700 cursor-pointer">
              <input
                type="checkbox"
                v-model="form.channels"
                value="EMAIL"
                class="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-blue-500"
              />
              <span>📧 Email (MailKit / SMTP)</span>
            </label>

            <label class="flex items-center space-x-2 text-xs font-medium text-gray-700 cursor-pointer">
              <input
                type="checkbox"
                v-model="form.channels"
                value="TELEGRAM"
                class="w-4 h-4 text-blue-600 rounded border-gray-300 focus:ring-blue-500"
              />
              <span>✈️ Telegram Bot API</span>
            </label>
          </div>
        </div>

        <!-- Submit Button -->
        <div class="pt-4 border-t border-gray-200 flex justify-end">
          <button
            type="submit"
            :disabled="sending"
            class="px-6 py-3 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white text-sm font-bold rounded-xl shadow-lg hover:shadow-xl transition flex items-center space-x-2 disabled:opacity-50"
          >
            <span v-if="sending">Đang phát tán sự kiện...</span>
            <span v-else>🚀 Phát tán Sự kiện Thông báo Đa kênh</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue'
import { useNotificationStore } from '../stores/notificationStore'

const store = useNotificationStore()
const sending = ref(false)

const eventTypes = [
  {
    code: 'ORDER_SUCCESS',
    name: 'Đặt hàng thành công',
    icon: '🛒',
    desc: 'Bắn tin đơn hàng, số tiền, trạng thái vận chuyển.'
  },
  {
    code: 'SECURITY_OTP',
    name: 'Xác thực mã OTP',
    icon: '🔐',
    desc: 'Bắn mã xác minh tài khoản, hết hạn 5 phút.'
  },
  {
    code: 'BALANCE_CHANGE',
    name: 'Biến động số dư',
    icon: '💰',
    desc: 'Thông báo thay đổi số dư ngân hàng/ví điện tử.'
  }
]

const form = reactive({
  eventCode: 'ORDER_SUCCESS',
  email: 'ltd28112005@gmail.com',
  telegramChatId: 1547761725,
  channels: ['IN_APP', 'EMAIL', 'TELEGRAM'],
  parameters: {
    orderId: 'DH-8899',
    amount: '1.250.000',
    userName: 'Tuấn Dũng',
    time: new Date().toLocaleTimeString('vi-VN') + ' ' + new Date().toLocaleDateString('vi-VN'),
    otp: '892104',
    type: '+ (Cộng)',
    reason: 'Nạp tiền ví điện tử',
    balance: '5.450.000'
  }
})

function selectEvent(ev) {
  form.eventCode = ev.code
  if (ev.code === 'SECURITY_OTP') {
    form.parameters.otp = Math.floor(100000 + Math.random() * 900000).toString()
  } else if (ev.code === 'ORDER_SUCCESS') {
    form.parameters.orderId = 'DH-' + Math.floor(1000 + Math.random() * 9000)
  }
}

async function handleSend() {
  sending.value = true
  try {
    const payload = {
      userId: store.userId,
      eventCode: form.eventCode,
      parameters: { ...form.parameters },
      email: form.email,
      telegramChatId: form.telegramChatId,
      channels: form.channels
    }
    await store.sendNotification(payload)
  } catch (err) {
    alert('Lỗi gửi thông báo: ' + (err.response?.data?.message || err.message))
  } finally {
    sending.value = false
  }
}
</script>
