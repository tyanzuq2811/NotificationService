<template>
  <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 text-left">
    <!-- Left Column: Simulator Controls (7 cols) -->
    <div class="lg:col-span-7 space-y-6">
      <div class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm space-y-6">
        <div>
          <h2 class="text-base font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-2">
            <FlaskConical class="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
            <span>Phòng Giả lập Phát tán Sự kiện Đa Kênh (Enterprise Simulator)</span>
          </h2>
          <p class="text-xs text-slate-500 dark:text-zinc-400 mt-1">
            Mô phỏng các dịch vụ nghiệp vụ kích hoạt phát tán thông báo qua 6 kênh song song cùng cơ chế chống chịu lỗi phân tán.
          </p>
        </div>

        <!-- Quick Preset Buttons for Lê Tuấn Dũng -->
        <div class="space-y-2">
          <label class="block text-xs font-bold text-slate-700 dark:text-zinc-300 uppercase tracking-wider">
            Mẫu Thử Nghiệm Nhanh (Lê Tuấn Dũng):
          </label>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
            <button
              type="button"
              @click="applyPreset('ORDER')"
              class="p-2.5 rounded-xl border border-slate-200 dark:border-zinc-700 bg-slate-50 dark:bg-zinc-800 hover:border-indigo-400 text-left text-xs transition space-y-1"
            >
              <div class="font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-1.5">
                <ShoppingBag class="w-3.5 h-3.5 text-indigo-600 dark:text-indigo-400" />
                <span>Đơn hàng thành công</span>
              </div>
              <div class="text-[10px] text-slate-500 dark:text-zinc-400 font-mono">#DH-VN-9999 &bull; 3.250.000 đ</div>
            </button>

            <button
              type="button"
              @click="applyPreset('OTP')"
              class="p-2.5 rounded-xl border border-slate-200 dark:border-zinc-700 bg-slate-50 dark:bg-zinc-800 hover:border-indigo-400 text-left text-xs transition space-y-1"
            >
              <div class="font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-1.5">
                <ShieldCheck class="w-3.5 h-3.5 text-rose-600 dark:text-rose-400" />
                <span>Mã OTP Khẩn Cấp</span>
              </div>
              <div class="text-[10px] text-slate-500 dark:text-zinc-400 font-mono">OTP: 889214 &bull; TTL: 120s</div>
            </button>

            <button
              type="button"
              @click="applyPreset('BALANCE')"
              class="p-2.5 rounded-xl border border-slate-200 dark:border-zinc-700 bg-slate-50 dark:bg-zinc-800 hover:border-indigo-400 text-left text-xs transition space-y-1"
            >
              <div class="font-bold text-slate-900 dark:text-zinc-100 flex items-center space-x-1.5">
                <Wallet class="w-3.5 h-3.5 text-emerald-600 dark:text-emerald-400" />
                <span>Biến động số dư</span>
              </div>
              <div class="text-[10px] text-slate-500 dark:text-zinc-400 font-mono">+15.000.000 đ</div>
            </button>
          </div>
        </div>

        <form @submit.prevent="handleSendSingle" class="space-y-5">
          <!-- 1. Chọn loại sự kiện -->
          <div>
            <label class="block text-xs font-bold text-slate-700 dark:text-zinc-300 uppercase tracking-wider mb-2">
              1. Loại Sự kiện Nghiệp vụ:
            </label>
            <div class="grid grid-cols-2 sm:grid-cols-4 gap-2.5">
              <div
                v-for="ev in eventTypes"
                :key="ev.code"
                @click="selectEvent(ev)"
                :class="[
                  form.eventCode === ev.code
                    ? 'border-indigo-600 bg-indigo-50/50 dark:bg-indigo-950/40 ring-2 ring-indigo-500'
                    : 'border-slate-200 dark:border-zinc-700 hover:border-slate-300 dark:hover:border-zinc-600 bg-white dark:bg-zinc-800',
                  'border rounded-xl p-3 cursor-pointer transition text-left space-y-1'
                ]"
              >
                <component :is="ev.icon" class="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
                <div class="text-[11px] font-bold text-slate-900 dark:text-zinc-100 truncate">{{ ev.name }}</div>
                <div class="text-[9px] text-slate-500 dark:text-zinc-400 leading-tight">{{ ev.desc }}</div>
              </div>
            </div>
          </div>

          <!-- 2. Tham số dữ liệu nghiệp vụ -->
          <div class="bg-slate-50 dark:bg-zinc-950 p-4 rounded-xl border border-slate-200 dark:border-zinc-800 space-y-3">
            <label class="block text-xs font-bold text-slate-700 dark:text-zinc-300 uppercase tracking-wider">
              2. Tham số dữ liệu nghiệp vụ:
            </label>

            <!-- ORDER_SUCCESS parameters -->
            <div v-if="form.eventCode === 'ORDER_SUCCESS'" class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Mã đơn hàng:</label>
                <input
                  v-model="form.parameters.orderId"
                  type="text"
                  class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100"
                  required
                />
              </div>
              <div>
                <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Số tiền (VNĐ):</label>
                <input
                  v-model="form.parameters.amount"
                  type="text"
                  class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100"
                  required
                />
              </div>
            </div>

            <!-- SECURITY_OTP parameters -->
            <div v-if="form.eventCode === 'SECURITY_OTP'" class="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Mã xác thực OTP:</label>
                <input
                  v-model="form.parameters.otp"
                  type="text"
                  class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 font-mono font-bold"
                  required
                />
              </div>
              <div>
                <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Hiệu lực bảo mật:</label>
                <input
                  value="120 giây (Tự hủy nếu quá hạn TTL)"
                  disabled
                  class="w-full text-xs px-3 py-1.5 border border-slate-200 dark:border-zinc-700 rounded-lg bg-slate-100 dark:bg-zinc-800/50 text-slate-500 dark:text-zinc-400 font-mono"
                />
              </div>
            </div>

            <!-- BALANCE_CHANGE parameters -->
            <div v-if="form.eventCode === 'BALANCE_CHANGE'" class="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div>
                <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Loại biến động:</label>
                <select
                  v-model="form.parameters.type"
                  class="w-full text-xs px-2.5 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100"
                >
                  <option value="+ (Cộng)">+ (Cộng tiền)</option>
                  <option value="- (Trừ)">- (Trừ tiền)</option>
                </select>
              </div>
              <div>
                <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Số tiền:</label>
                <input
                  v-model="form.parameters.amount"
                  type="text"
                  class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100"
                  required
                />
              </div>
              <div>
                <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Số dư mới:</label>
                <input
                  v-model="form.parameters.balance"
                  type="text"
                  class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100"
                  required
                />
              </div>
            </div>

            <!-- SYSTEM_ALERT parameters -->
            <div v-if="form.eventCode === 'SYSTEM_ALERT'" class="space-y-2">
              <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400">Nội dung cảnh báo:</label>
              <input
                v-model="form.parameters.reason"
                type="text"
                class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100"
                required
              />
            </div>
          </div>

          <!-- 3. Thông tin Khách hàng / Người nhận tin -->
          <div class="bg-indigo-50/50 dark:bg-zinc-950 p-4 rounded-xl border border-indigo-100 dark:border-zinc-800 space-y-3.5">
            <div class="flex items-center justify-between">
              <label class="block text-xs font-bold text-indigo-950 dark:text-zinc-200 uppercase tracking-wider flex items-center space-x-1.5">
                <Users class="w-4 h-4 text-indigo-600 dark:text-indigo-400" />
                <span>3. Thông tin Khách hàng / Người nhận tin:</span>
              </label>
              <span class="text-[10px] text-slate-500 dark:text-zinc-400">Tự động lưu CSDL & Định tuyến đa kênh</span>
            </div>

            <!-- Dropdown chọn người nhận từ CSDL -->
            <div class="space-y-1">
              <div class="flex items-center justify-between">
                <label class="block text-[11px] font-bold text-slate-700 dark:text-zinc-300 flex items-center space-x-1">
                  <UserCheck class="w-3.5 h-3.5 text-indigo-600 dark:text-indigo-400" />
                  <span>Phát tán cho ai (Chọn từ CSDL):</span>
                </label>
                <button
                  type="button"
                  @click="selectNewRecipient"
                  class="text-[10px] text-indigo-600 dark:text-indigo-400 font-semibold hover:underline flex items-center space-x-0.5"
                >
                  <span>+ Thêm khách hàng mới</span>
                </button>
              </div>
              <select
                v-model="selectedRecipientKey"
                @change="onRecipientChange"
                class="w-full text-xs px-3 py-2 border border-slate-300 dark:border-zinc-700 rounded-xl bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 font-medium focus:ring-2 focus:ring-indigo-500 shadow-sm"
              >
                <option v-for="rec in recipientList" :key="rec.userId" :value="rec.userId">
                  {{ rec.name || rec.userId }} &bull; {{ rec.phoneNumber || rec.email || 'Khách hàng' }}
                </option>
                <option value="__NEW__">+ Thêm khách hàng mới (Nhập tự do)...</option>
              </select>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
              <!-- Ô 1: UserId -->
              <div>
                <label class="block text-[11px] font-medium text-slate-700 dark:text-zinc-300 mb-1">
                  Mã người dùng (UserId) <span class="text-rose-500">*</span>:
                </label>
                <input
                  v-model="form.userId"
                  @input="errors.userId = ''"
                  type="text"
                  placeholder="user_demo hoặc mã khách hàng"
                  :class="[
                    'w-full text-xs px-3 py-1.5 border rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 font-mono transition',
                    errors.userId
                      ? 'border-rose-500 ring-1 ring-rose-500 bg-rose-50/20'
                      : 'border-slate-300 dark:border-zinc-700 focus:ring-1 focus:ring-indigo-500'
                  ]"
                  required
                />
                <span v-if="errors.userId" class="text-[10px] text-rose-500 font-medium block mt-0.5">{{ errors.userId }}</span>
                <span v-else class="text-[9px] text-indigo-600 dark:text-indigo-400 block mt-0.5">&rarr; Kênh In-App Web, Web Push & Hộp thư</span>
              </div>

              <!-- Ô 2: Email -->
              <div>
                <label class="block text-[11px] font-medium text-slate-700 dark:text-zinc-300 mb-1">
                  Email người nhận:
                </label>
                <input
                  v-model="form.email"
                  @input="errors.email = ''"
                  type="email"
                  placeholder="vd: ltd28112005@gmail.com"
                  :class="[
                    'w-full text-xs px-3 py-1.5 border rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 transition',
                    errors.email
                      ? 'border-rose-500 ring-1 ring-rose-500 bg-rose-50/20'
                      : 'border-slate-300 dark:border-zinc-700 focus:ring-1 focus:ring-indigo-500'
                  ]"
                />
                <span v-if="errors.email" class="text-[10px] text-rose-500 font-medium block mt-0.5">{{ errors.email }}</span>
                <span v-else class="text-[9px] text-indigo-600 dark:text-indigo-400 block mt-0.5">&rarr; Kênh Google SMTP (phải có ký tự @)</span>
              </div>

              <!-- Ô 3: Số điện thoại -->
              <div>
                <label class="block text-[11px] font-medium text-slate-700 dark:text-zinc-300 mb-1">
                  Số điện thoại:
                </label>
                <input
                  v-model="form.phoneNumber"
                  @input="errors.phoneNumber = ''"
                  type="text"
                  placeholder="vd: 0766408268"
                  maxlength="11"
                  :class="[
                    'w-full text-xs px-3 py-1.5 border rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 font-mono transition',
                    errors.phoneNumber
                      ? 'border-rose-500 ring-1 ring-rose-500 bg-rose-50/20'
                      : 'border-slate-300 dark:border-zinc-700 focus:ring-1 focus:ring-indigo-500'
                  ]"
                />
                <span v-if="errors.phoneNumber" class="text-[10px] text-rose-500 font-medium block mt-0.5">{{ errors.phoneNumber }}</span>
                <span v-else class="text-[9px] text-indigo-600 dark:text-indigo-400 block mt-0.5">&rarr; Kênh Zalo ZNS (đủ 10 số, bắt đầu bằng 0)</span>
              </div>

              <!-- Ô 4: Telegram Chat ID -->
              <div>
                <label class="block text-[11px] font-medium text-slate-700 dark:text-zinc-300 mb-1">
                  Telegram Chat ID:
                </label>
                <input
                  v-model="form.telegramChatId"
                  @input="errors.telegramChatId = ''"
                  type="text"
                  placeholder="vd: 1547761725"
                  :class="[
                    'w-full text-xs px-3 py-1.5 border rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 font-mono transition',
                    errors.telegramChatId
                      ? 'border-rose-500 ring-1 ring-rose-500 bg-rose-50/20'
                      : 'border-slate-300 dark:border-zinc-700 focus:ring-1 focus:ring-indigo-500'
                  ]"
                />
                <span v-if="errors.telegramChatId" class="text-[10px] text-rose-500 font-medium block mt-0.5">{{ errors.telegramChatId }}</span>
                <span v-else class="text-[9px] text-indigo-600 dark:text-indigo-400 block mt-0.5">&rarr; Kênh Telegram Bot API (chỉ gồm các chữ số)</span>
              </div>
            </div>


          </div>

          <!-- 4. Kênh phân phối phát tán (6 channels) -->
          <div>
            <label class="block text-xs font-bold text-slate-700 dark:text-zinc-300 uppercase tracking-wider mb-2">
              4. Chọn Kênh Phát Tán Song Song (Multi-Channel Dispatch):
            </label>
            <div class="grid grid-cols-2 sm:grid-cols-3 gap-2.5">
              <label
                v-for="ch in channelOptions"
                :key="ch.id"
                class="flex items-center space-x-2.5 p-2.5 rounded-xl border border-slate-200 dark:border-zinc-700 bg-white dark:bg-zinc-800 cursor-pointer hover:border-indigo-300 transition"
              >
                <input
                  type="checkbox"
                  v-model="selectedChannels"
                  :value="ch.id"
                  class="rounded text-indigo-600 focus:ring-indigo-500"
                />
                <BrandIcon :name="ch.id" size="sm" />
                <span class="text-xs font-semibold text-slate-800 dark:text-zinc-200">{{ ch.name }}</span>
              </label>
            </div>
          </div>

          <!-- 5. Làn ưu tiên & Idempotency Key & ScheduledAt -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Làn Ưu Tiên (Priority):</label>
              <select
                v-model="form.priority"
                class="w-full text-xs px-3 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100"
              >
                <option value="STANDARD">STANDARD (Tiêu chuẩn)</option>
                <option value="HIGH">HIGH (Ưu tiên khẩn cấp)</option>
              </select>
            </div>

            <div>
              <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Hẹn giờ (ScheduledAt):</label>
              <input
                v-model="form.scheduledAt"
                type="datetime-local"
                class="w-full text-xs px-2.5 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100"
              />
            </div>

            <div>
              <label class="block text-[11px] font-medium text-slate-600 dark:text-zinc-400 mb-1">Idempotency Key:</label>
              <div class="flex space-x-1">
                <input
                  v-model="form.idempotencyKey"
                  type="text"
                  placeholder="Để trống = sinh tự động"
                  class="w-full text-xs px-2.5 py-1.5 border border-slate-300 dark:border-zinc-700 rounded-lg bg-white dark:bg-zinc-800 text-slate-800 dark:text-zinc-100 font-mono text-[10px]"
                />
                <button
                  type="button"
                  @click="generateKey"
                  class="px-2 py-1.5 bg-slate-100 dark:bg-zinc-800 hover:bg-slate-200 dark:hover:bg-zinc-700 text-slate-700 dark:text-zinc-300 text-xs rounded-lg transition"
                  title="Tạo khóa ngẫu nhiên"
                >
                  <RefreshCw class="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="pt-2 flex flex-col sm:flex-row gap-3">
            <button
              type="submit"
              :disabled="sending"
              class="flex-1 py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold rounded-xl shadow-md transition flex items-center justify-center space-x-2 disabled:opacity-50"
            >
              <Send class="w-4 h-4" />
              <span>{{ sending ? 'Đang gửi qua Outbox...' : 'Phát Tán Sự Kiện (Transactional Outbox)' }}</span>
            </button>

            <button
              type="button"
              @click="handleStressTest"
              :disabled="stressTesting"
              class="py-2.5 px-4 bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold rounded-xl shadow-md transition flex items-center justify-center space-x-2 disabled:opacity-50"
            >
              <Zap class="w-4 h-4" />
              <span>{{ stressTesting ? 'Đang stress test...' : 'Stress Test (200 Thông Báo)' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Right Column: Live Stream Console & Chaos Lab (5 cols) -->
    <div class="lg:col-span-5 space-y-6">
      <!-- 1. Chaos Engineering Fault Injection Panel -->
      <div class="bg-white dark:bg-zinc-900 p-6 rounded-2xl border border-slate-200/80 dark:border-zinc-800 shadow-sm space-y-4">
        <div class="flex items-center justify-between border-b border-slate-100 dark:border-zinc-800 pb-3">
          <div class="flex items-center space-x-2">
            <div class="p-1.5 rounded-lg bg-rose-50 dark:bg-rose-950/60 text-rose-600 dark:text-rose-400">
              <Flame class="w-4 h-4" />
            </div>
            <div>
              <h3 class="text-xs font-bold text-slate-900 dark:text-zinc-100">Chaos Engineering & Fault Injection</h3>
              <p class="text-[10px] text-slate-500 dark:text-zinc-400">Kích hoạt sự cố kiểm thử Circuit Breaker & Rate Limiter</p>
            </div>
          </div>
        </div>

        <div class="space-y-3">
          <!-- Switch 1: SMTP / Telegram Timeout -> Circuit Breaker -->
          <div class="p-3 rounded-xl bg-slate-50 dark:bg-zinc-950 border border-slate-200 dark:border-zinc-800 flex items-start justify-between space-x-3">
            <div class="space-y-0.5">
              <span class="text-xs font-bold text-slate-800 dark:text-zinc-200">1. Ngắt Mạch Circuit Breaker</span>
              <p class="text-[10px] text-slate-500 dark:text-zinc-400">
                Gửi vào địa chỉ SMTP/Push lỗi liên tục 3 lần để cầu dao tự nhảy sang OPEN trong 1 phút (Fail-fast 1ms, đẩy DLQ).
              </p>
            </div>
            <button
              @click="injectCircuitBreakerFault"
              class="px-2.5 py-1 text-[10px] font-bold bg-rose-100 text-rose-700 dark:bg-rose-950/60 dark:text-rose-300 rounded-lg hover:bg-rose-200 transition whitespace-nowrap"
            >
              Kích hoạt
            </button>
          </div>

          <!-- Switch 2: Rate Limit 429 Token Bucket -->
          <div class="p-3 rounded-xl bg-slate-50 dark:bg-zinc-950 border border-slate-200 dark:border-zinc-800 flex items-start justify-between space-x-3">
            <div class="space-y-0.5">
              <span class="text-xs font-bold text-slate-800 dark:text-zinc-200">2. Quá Tải Rate Limiter (429)</span>
              <p class="text-[10px] text-slate-500 dark:text-zinc-400">
                Bắn liên tiếp 35 yêu cầu trong 1 giây để vượt ngưỡng TokenBucket 25 req/s.
              </p>
            </div>
            <button
              @click="injectRateLimitFault"
              class="px-2.5 py-1 text-[10px] font-bold bg-amber-100 text-amber-700 dark:bg-amber-950/60 dark:text-amber-300 rounded-lg hover:bg-amber-200 transition whitespace-nowrap"
            >
              Kích hoạt
            </button>
          </div>

          <!-- Switch 3: TTL Expired -->
          <div class="p-3 rounded-xl bg-slate-50 dark:bg-zinc-950 border border-slate-200 dark:border-zinc-800 flex items-start justify-between space-x-3">
            <div class="space-y-0.5">
              <span class="text-xs font-bold text-slate-800 dark:text-zinc-200">3. Sự Kiện Hết Hạn TTL (EXPIRED)</span>
              <p class="text-[10px] text-slate-500 dark:text-zinc-400">
                Đặt ExpiresAt ở quá khứ: Consumer tự hủy mã OTP và đánh dấu trạng thái EXPIRED thay vì gửi đi.
              </p>
            </div>
            <button
              @click="injectExpiredFault"
              class="px-2.5 py-1 text-[10px] font-bold bg-slate-200 text-slate-700 dark:bg-zinc-800 dark:text-zinc-300 rounded-lg hover:bg-slate-300 transition whitespace-nowrap"
            >
              Kích hoạt
            </button>
          </div>
        </div>
      </div>

      <!-- 2. Live Stream Terminal Console -->
      <div class="bg-slate-900 dark:bg-zinc-950 p-5 rounded-2xl border border-slate-800 dark:border-zinc-800 shadow-xl flex flex-col h-96">
        <div class="flex items-center justify-between pb-3 border-b border-slate-800">
          <div class="flex items-center space-x-2">
            <Terminal class="w-4 h-4 text-emerald-400" />
            <span class="text-xs font-bold text-white font-mono">Live Stream CQRS Console</span>
          </div>
          <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse"></span>
        </div>

        <div class="flex-1 overflow-y-auto mt-3 space-y-2 font-mono text-[11px] pr-1">
          <div
            v-for="log in store.liveLogs"
            :key="log.id"
            class="flex items-start space-x-2 leading-relaxed"
          >
            <span class="text-slate-500 text-[10px] tabular-nums whitespace-nowrap">{{ log.time }}</span>
            <span
              class="px-1.5 py-0.2 rounded text-[9px] font-bold whitespace-nowrap"
              :class="getLogBadgeClass(log.type)"
            >
              {{ log.type }}
            </span>
            <span class="text-slate-300 break-all min-w-0 flex-1">{{ log.message }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useNotificationStore } from '../stores/notificationStore'
import { queryApi } from '../api/httpClient'
import BrandIcon from '../components/icons/BrandIcon.vue'
import {
  FlaskConical,
  ShoppingBag,
  ShieldCheck,
  Wallet,
  AlertOctagon,
  RefreshCw,
  Send,
  Zap,
  Flame,
  Terminal,
  Users,
  CheckCircle2,
  UserCheck
} from 'lucide-vue-next'

const store = useNotificationStore()

const sending = ref(false)
const stressTesting = ref(false)

const selectedChannels = ref(['IN_APP', 'EMAIL', 'TELEGRAM', 'ZALO_ZNS', 'WEB_PUSH', 'WEBHOOK'])

const eventTypes = [
  { code: 'ORDER_SUCCESS', name: 'Đơn hàng', icon: ShoppingBag, desc: 'Gửi biên nhận & vận chuyển' },
  { code: 'SECURITY_OTP', name: 'Mã OTP', icon: ShieldCheck, desc: 'Ưu tiên cao, TTL 120s' },
  { code: 'BALANCE_CHANGE', name: 'Số dư', icon: Wallet, desc: 'Biến động tài khoản ngân hàng' },
  { code: 'SYSTEM_ALERT', name: 'Cảnh báo', icon: AlertOctagon, desc: 'Cảnh báo hệ thống khẩn cấp' }
]

const channelOptions = [
  { id: 'IN_APP', name: 'In-App Web' },
  { id: 'EMAIL', name: 'Google SMTP' },
  { id: 'TELEGRAM', name: 'Telegram Bot' },
  { id: 'ZALO_ZNS', name: 'Zalo ZNS' },
  { id: 'WEB_PUSH', name: 'Windows Action Center' },
  { id: 'WEBHOOK', name: 'Discord Webhook' }
]

// Quản lý thông tin khách hàng / người nhận
const selectedRecipientKey = ref('user_demo')
const recipientList = ref([
  {
    userId: 'user_demo',
    name: 'Khách hàng Demo',
    email: 'ltd28112005@gmail.com',
    phoneNumber: '0766408268',
    telegramChatId: '1547761725'
  },
  {
    userId: 'user_quynh',
    name: 'Người dùng Quỳnh',
    email: 'quynh@example.com',
    phoneNumber: '0766408268',
    telegramChatId: '1547761725'
  }
])

const errors = ref({
  userId: '',
  email: '',
  phoneNumber: '',
  telegramChatId: ''
})

const form = ref({
  userId: 'user_demo',
  email: 'ltd28112005@gmail.com',
  phoneNumber: '0766408268',
  telegramChatId: '1547761725',
  eventCode: 'ORDER_SUCCESS',
  priority: 'STANDARD',
  scheduledAt: '',
  idempotencyKey: '',
  parameters: {
    orderId: 'DH-VN-' + Math.floor(1000 + Math.random() * 9000),
    amount: '3.250.000',
    otp: '889214',
    type: '+ (Cộng)',
    balance: '45.000.000 đ',
    reason: 'Phát hiện đăng nhập từ IP lạ 14.161.35.12'
  }
})

async function loadRecipientsFromDb() {
  try {
    const res = await queryApi.get('/preferences/recipients')
    if (res.data && Array.isArray(res.data) && res.data.length > 0) {
      recipientList.value = res.data
    }
  } catch (err) {
    console.warn('Could not load recipients from DB, using initial list:', err)
  }
}

function onRecipientChange() {
  if (selectedRecipientKey.value === '__NEW__') {
    selectNewRecipient()
    return
  }
  const found = recipientList.value.find(r => r.userId === selectedRecipientKey.value)
  if (found) {
    form.value.userId = found.userId || ''
    form.value.email = found.email || ''
    form.value.phoneNumber = found.phoneNumber || '0766408268'
    form.value.telegramChatId = found.telegramChatId ? String(found.telegramChatId) : ''
    errors.value.userId = ''
    errors.value.email = ''
    errors.value.phoneNumber = ''
    errors.value.telegramChatId = ''
  }
}

function selectNewRecipient() {
  selectedRecipientKey.value = '__NEW__'
  form.value.userId = ''
  form.value.email = ''
  form.value.phoneNumber = '0766408268'
  form.value.telegramChatId = ''
  errors.value.userId = ''
  errors.value.email = ''
  errors.value.phoneNumber = ''
  errors.value.telegramChatId = ''
}

function upsertLocalRecipient(profile) {
  const idx = recipientList.value.findIndex(r => r.userId === profile.userId)
  if (idx >= 0) {
    recipientList.value[idx] = { ...recipientList.value[idx], ...profile }
  } else {
    recipientList.value.unshift(profile)
  }
  selectedRecipientKey.value = profile.userId
}

function validateForm() {
  let isValid = true
  errors.value = {
    userId: '',
    email: '',
    phoneNumber: '',
    telegramChatId: ''
  }

  // 1. Kiểm tra UserId
  const uId = form.value.userId?.trim()
  if (!uId) {
    errors.value.userId = 'Vui lòng nhập Mã người dùng (UserId).'
    isValid = false
  } else if (uId.length < 2) {
    errors.value.userId = 'Mã người dùng phải có ít nhất 2 ký tự.'
    isValid = false
  }

  // 2. Kiểm tra Email
  const emailVal = form.value.email?.trim() || ''
  const isEmailChannel = selectedChannels.value.includes('EMAIL')
  if (isEmailChannel && !emailVal) {
    errors.value.email = 'Vui lòng nhập Email người nhận khi gửi qua kênh Google SMTP.'
    isValid = false
  } else if (emailVal) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!emailRegex.test(emailVal)) {
      errors.value.email = 'Email không hợp lệ (phải có ký tự @ và tên miền, vd: user@domain.com).'
      isValid = false
    }
  }

  // 3. Kiểm tra Số điện thoại (bắt đầu bằng 0, đúng 10 số)
  const phoneVal = (form.value.phoneNumber || '').trim().replace(/[\s-]/g, '')
  const isZaloChannel = selectedChannels.value.includes('ZALO_ZNS')
  if (isZaloChannel && !phoneVal) {
    errors.value.phoneNumber = 'Vui lòng nhập Số điện thoại khi gửi qua kênh Zalo ZNS.'
    isValid = false
  } else if (phoneVal) {
    const phoneRegex = /^0\d{9}$/
    if (!phoneRegex.test(phoneVal)) {
      errors.value.phoneNumber = 'Số điện thoại không hợp lệ (phải bắt đầu bằng số 0 và đủ đúng 10 chữ số).'
      isValid = false
    }
  }

  // 4. Kiểm tra Telegram Chat ID
  const tgVal = (form.value.telegramChatId || '').toString().trim()
  const isTgChannel = selectedChannels.value.includes('TELEGRAM')
  if (isTgChannel && !tgVal) {
    errors.value.telegramChatId = 'Vui lòng nhập Telegram Chat ID khi gửi qua kênh Telegram.'
    isValid = false
  } else if (tgVal) {
    const tgRegex = /^-?\d+$/
    if (!tgRegex.test(tgVal)) {
      errors.value.telegramChatId = 'Telegram Chat ID phải là chuỗi số nguyên (ví dụ: 1547761725).'
      isValid = false
    }
  }

  return isValid
}

function selectEvent(ev) {
  form.value.eventCode = ev.code
  if (ev.code === 'SECURITY_OTP') {
    form.value.priority = 'HIGH'
  } else {
    form.value.priority = 'STANDARD'
  }
}

function applyPreset(type) {
  form.value.userId = 'user_demo'
  form.value.email = 'ltd28112005@gmail.com'
  form.value.phoneNumber = '0766408268'
  form.value.telegramChatId = '1547761725'
  selectedRecipientKey.value = 'user_demo'
  errors.value = { userId: '', email: '', phoneNumber: '', telegramChatId: '' }

  if (type === 'ORDER') {
    form.value.eventCode = 'ORDER_SUCCESS'
    form.value.priority = 'STANDARD'
    form.value.parameters.orderId = 'DH-VN-9999'
    form.value.parameters.amount = '3.250.000'
  } else if (type === 'OTP') {
    form.value.eventCode = 'SECURITY_OTP'
    form.value.priority = 'HIGH'
    form.value.parameters.otp = String(Math.floor(100000 + Math.random() * 900000))
  } else if (type === 'BALANCE') {
    form.value.eventCode = 'BALANCE_CHANGE'
    form.value.priority = 'HIGH'
    form.value.parameters.type = '+ (Cộng)'
    form.value.parameters.amount = '15.000.000'
    form.value.parameters.balance = '60.000.000 đ'
  }
}

function generateKey() {
  form.value.idempotencyKey = 'IDEM-' + Math.random().toString(36).substring(2, 9).toUpperCase()
}

async function handleSendSingle() {
  if (!validateForm()) {
    store.addLog('ERR', 'Xác thực thất bại: Vui lòng kiểm tra lại các thông tin người nhận chưa đúng định dạng.')
    return
  }

  sending.value = true
  try {
    const targetUserId = form.value.userId?.trim() || store.userId
    const cleanPhone = (form.value.phoneNumber || '').trim().replace(/[\s-]/g, '')
    const cleanEmail = form.value.email?.trim() || null
    const cleanTgId = form.value.telegramChatId ? Number(form.value.telegramChatId) : null

    const payload = {
      userId: targetUserId,
      email: cleanEmail,
      phoneNumber: cleanPhone || null,
      telegramChatId: cleanTgId,
      eventCode: form.value.eventCode,
      parameters: {
        ...form.value.parameters,
        userName: targetUserId
      },
      priority: form.value.priority,
      channels: selectedChannels.value,
      scheduledAt: form.value.scheduledAt ? new Date(form.value.scheduledAt).toISOString() : null,
      idempotencyKey: form.value.idempotencyKey || null
    }

    await store.sendNotification(payload)

    // Cập nhật recipient list cục bộ & đồng bộ
    upsertLocalRecipient({
      userId: targetUserId,
      name: `Khách hàng ${targetUserId}`,
      email: cleanEmail || '',
      phoneNumber: cleanPhone || '0766408268',
      telegramChatId: cleanTgId ? String(cleanTgId) : ''
    })

    // Refetch notifications, metrics & updated recipients from DB
    setTimeout(() => {
      store.fetchNotifications()
      store.fetchMetrics()
      loadRecipientsFromDb()
    }, 1000)
  } catch (err) {
    console.error('Send failed:', err)
  } finally {
    sending.value = false
  }
}

onMounted(() => {
  loadRecipientsFromDb()
})

async function handleStressTest() {
  stressTesting.value = true
  try {
    const userIds = []
    for (let i = 1; i <= 200; i++) {
      userIds.push(`user_${i}`)
    }

    await store.sendBulkNotification({
      userIds,
      eventCode: 'ORDER_SUCCESS',
      parameters: {
        orderId: 'STRESS-TEST-200',
        amount: '1.000.000',
        userName: 'Stress Test User'
      },
      priority: 'STANDARD'
    })

    setTimeout(() => {
      store.fetchMetrics()
      store.fetchOutboxEvents()
    }, 1500)
  } catch (err) {
    console.error('Stress test failed:', err)
  } finally {
    stressTesting.value = false
  }
}

async function injectCircuitBreakerFault() {
  store.addLog('CHAOS', 'Kích hoạt thử nghiệm ngắt mạch Circuit Breaker: Cố ý gửi 3 tin lỗi tới endpoint không tồn tại')
  form.value.eventCode = 'SYSTEM_ALERT'
  form.value.parameters.reason = '[CHAOS TEST]: Cố ý ép mở Circuit Breaker (Fail-Fast 1ms)'
  await handleSendSingle()
}

async function injectRateLimitFault() {
  store.addLog('CHAOS', 'Kích hoạt thử nghiệm Rate Limiting: Bắn 35 thông báo liên tục trong 1 giây')
  for (let i = 0; i < 5; i++) {
    handleSendSingle()
  }
}

async function injectExpiredFault() {
  store.addLog('CHAOS', 'Kích hoạt thử nghiệm TTL Expired: Đặt ScheduledAt và ExpiresAt vào quá khứ')
  form.value.eventCode = 'SECURITY_OTP'
  form.value.priority = 'HIGH'
  form.value.parameters.otp = '999999'
  await handleSendSingle()
}

function getLogBadgeClass(type) {
  switch (type) {
    case 'CMD': return 'bg-blue-900/60 text-blue-300 border border-blue-700/50'
    case 'OUTBOX': return 'bg-amber-900/60 text-amber-300 border border-amber-700/50'
    case 'SIGNALR': return 'bg-indigo-900/60 text-indigo-300 border border-indigo-700/50'
    case 'IN_APP': return 'bg-emerald-900/60 text-emerald-300 border border-emerald-700/50'
    case 'ERR': return 'bg-rose-900/60 text-rose-300 border border-rose-700/50'
    case 'CHAOS': return 'bg-purple-900/60 text-purple-300 border border-purple-700/50'
    default: return 'bg-slate-800 text-slate-300'
  }
}
</script>
