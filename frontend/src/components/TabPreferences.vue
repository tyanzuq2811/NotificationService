<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <div class="bg-white p-6 rounded-xl border border-gray-200 shadow-sm">
      <div class="border-b border-gray-200 pb-4 mb-6 flex justify-between items-center">
        <div>
          <h2 class="text-lg font-bold text-gray-900 flex items-center space-x-2">
            <span>⚙️</span>
            <span>Cấu hình Kênh nhận Thông báo của Người dùng (User Preferences)</span>
          </h2>
          <p class="text-xs text-gray-500 mt-1">
            Minh chứng tính linh hoạt của kiến trúc: Người dùng có thể bật/tắt từng kênh cho từng loại sự kiện cụ thể. Dữ liệu được lưu tại SQL Server và đồng bộ qua Notification Service.
          </p>
        </div>

        <button
          @click="store.fetchPreferences"
          class="p-2 text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition"
          title="Tải lại cài đặt"
        >
          🔄
        </button>
      </div>

      <div v-if="store.preferences.length === 0" class="text-center py-8 text-gray-500 text-xs">
        Đang tải cấu hình người dùng...
      </div>

      <div v-else class="space-y-4">
        <div
          v-for="pref in store.preferences"
          :key="pref.id"
          class="border border-gray-200 rounded-xl p-5 hover:border-gray-300 transition bg-white space-y-4"
        >
          <div class="flex items-center justify-between border-b border-gray-100 pb-3">
            <div class="flex items-center space-x-3">
              <span class="text-2xl">{{ getEventIcon(pref.eventCode) }}</span>
              <div>
                <h3 class="text-sm font-bold text-gray-900">{{ pref.eventCode }}</h3>
                <p class="text-[11px] text-gray-500">{{ getEventDesc(pref.eventCode) }}</p>
              </div>
            </div>

            <button
              @click="savePref(pref)"
              class="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-xs font-bold shadow-sm transition"
            >
              💾 Lưu thay đổi
            </button>
          </div>

          <!-- Channel Toggles -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <!-- In-App -->
            <label class="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200 cursor-pointer">
              <span class="text-xs font-semibold text-gray-700">📱 In-App Web</span>
              <input
                type="checkbox"
                v-model="pref.enableInApp"
                class="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
              />
            </label>

            <!-- Email -->
            <label class="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200 cursor-pointer">
              <span class="text-xs font-semibold text-gray-700">📧 Email</span>
              <input
                type="checkbox"
                v-model="pref.enableEmail"
                class="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
              />
            </label>

            <!-- Telegram -->
            <label class="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200 cursor-pointer">
              <span class="text-xs font-semibold text-gray-700">✈️ Telegram</span>
              <input
                type="checkbox"
                v-model="pref.enableTelegram"
                class="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
              />
            </label>
          </div>

          <!-- Contact Override -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-2">
            <div>
              <label class="block text-[11px] font-medium text-gray-500 mb-1">Email nhận:</label>
              <input
                v-model="pref.email"
                type="email"
                class="w-full text-xs px-3 py-1.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label class="block text-[11px] font-medium text-gray-500 mb-1">Telegram Chat ID:</label>
              <input
                v-model.number="pref.telegramChatId"
                type="number"
                class="w-full text-xs px-3 py-1.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useNotificationStore } from '../stores/notificationStore'

const store = useNotificationStore()

function getEventIcon(code) {
  switch (code) {
    case 'ORDER_SUCCESS': return '🛒'
    case 'SECURITY_OTP': return '🔐'
    case 'BALANCE_CHANGE': return '💰'
    default: return '🔔'
  }
}

function getEventDesc(code) {
  switch (code) {
    case 'ORDER_SUCCESS': return 'Thông báo giao dịch mua sắm, trạng thái đơn hàng'
    case 'SECURITY_OTP': return 'Mã xác thực bảo mật đăng nhập và thay đổi mật khẩu'
    case 'BALANCE_CHANGE': return 'Biến động tiền trong tài khoản hoặc ví điện tử'
    default: return ''
  }
}

async function savePref(pref) {
  await store.updatePreference(pref)
}
</script>
