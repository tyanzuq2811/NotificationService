<template>
  <div class="space-y-6">
    <div class="bg-white p-6 rounded-xl border border-gray-200 shadow-sm space-y-4">
      <div class="flex justify-between items-center border-b border-gray-200 pb-4">
        <div>
          <h2 class="text-lg font-bold text-gray-900 flex items-center space-x-2">
            <span>📊</span>
            <span>Giám sát Bảng Transactional Outbox (SQL Server)</span>
          </h2>
          <p class="text-xs text-gray-500 mt-1">
            Bằng chứng thực nghiệm mô hình Transactional Outbox Pattern: Dữ liệu được ghi nhận an toàn vào SQL Server với cờ <code>IsPublished = 0</code>, sau đó Outbox Relay Worker chạy ngầm quét và đẩy vào Apache Kafka rồi cập nhật <code>IsPublished = 1</code>.
          </p>
        </div>

        <button
          @click="store.fetchOutboxEvents"
          class="flex items-center space-x-1.5 px-3 py-1.5 bg-blue-50 text-blue-600 hover:bg-blue-100 rounded-lg text-xs font-bold transition"
        >
          <span>🔄</span>
          <span>Làm mới Outbox Logs</span>
        </button>
      </div>

      <!-- Explanation Alert Box for Committee -->
      <div class="bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-200 rounded-xl p-4 flex items-start space-x-3">
        <div class="text-xl">💡</div>
        <div class="text-xs text-blue-900 leading-relaxed">
          <strong>Điểm cộng kiến trúc phân tán:</strong> Nhờ Outbox Pattern, ngay cả khi Apache Kafka bị rớt mạng hoặc khởi động lại, Command API vẫn tiếp nhận yêu cầu gửi thông báo từ người dùng và các Microservice khác bình thường (không bị treo nút đặt hàng). Ngay khi Kafka online trở lại, Worker sẽ tự động bù sự kiện mà không hề mất mát thông điệp!
        </div>
      </div>

      <!-- Outbox Table -->
      <div class="overflow-x-auto border border-gray-200 rounded-xl">
        <table class="min-w-full divide-y divide-gray-200 text-left text-xs">
          <thead class="bg-gray-50">
            <tr>
              <th scope="col" class="px-4 py-3 font-bold text-gray-700">Mã Sự kiện (EventId)</th>
              <th scope="col" class="px-4 py-3 font-bold text-gray-700">Loại Sự kiện</th>
              <th scope="col" class="px-4 py-3 font-bold text-gray-700">Aggregate ID</th>
              <th scope="col" class="px-4 py-3 font-bold text-gray-700">Thời gian tạo (SQL)</th>
              <th scope="col" class="px-4 py-3 font-bold text-gray-700">Trạng thái Outbox</th>
              <th scope="col" class="px-4 py-3 font-bold text-gray-700">Thời gian đẩy Kafka</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr v-if="store.outboxEvents.length === 0">
              <td colspan="6" class="px-4 py-8 text-center text-gray-500">
                Chưa có sự kiện nào trong bảng Outbox. Hãy thử phát tán thông báo ở tab Giả lập!
              </td>
            </tr>
            <tr
              v-for="evt in store.outboxEvents"
              :key="evt.eventId"
              class="hover:bg-gray-50 transition"
            >
              <td class="px-4 py-3 font-mono text-gray-600 text-[11px]">
                {{ evt.eventId.substring(0, 8) }}...
              </td>
              <td class="px-4 py-3 font-bold text-gray-800">
                {{ evt.eventType }}
              </td>
              <td class="px-4 py-3 font-mono text-gray-500 text-[11px]">
                {{ evt.aggregateId.substring(0, 8) }}...
              </td>
              <td class="px-4 py-3 text-gray-600 text-[11px]">
                {{ formatDate(evt.createdAt) }}
              </td>
              <td class="px-4 py-3">
                <span
                  class="inline-flex items-center px-2.5 py-0.5 rounded-full text-[10px] font-bold"
                  :class="evt.isPublished ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'"
                >
                  {{ evt.isPublished ? '✓ ĐÃ ĐẨY KAFKA' : '⏳ CHỜ ĐẨY' }}
                </span>
              </td>
              <td class="px-4 py-3 text-gray-600 text-[11px]">
                {{ evt.publishedAt ? formatDate(evt.publishedAt) : '-' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useNotificationStore } from '../stores/notificationStore'

const store = useNotificationStore()

function formatDate(isoStr) {
  if (!isoStr) return '-'
  const d = new Date(isoStr)
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) + ' ' +
         d.toLocaleDateString('vi-VN')
}
</script>
