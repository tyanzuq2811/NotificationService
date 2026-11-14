<template>
  <header class="bg-white border-b border-gray-200 sticky top-0 z-30 shadow-sm">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex justify-between items-center h-16">
        <!-- Logo & Title -->
        <div class="flex items-center space-x-3">
          <div class="w-10 h-10 rounded-lg bg-gradient-to-tr from-blue-600 to-indigo-600 flex items-center justify-center text-white font-black text-xl shadow-md">
            N
          </div>
          <div>
            <h1 class="text-base font-bold text-gray-900 leading-tight">
              Service Gửi Thông Báo Đa Kênh
            </h1>
            <p class="text-xs text-gray-500 font-medium">
              Kiến trúc Microservices & CQRS (.NET 8 + Apache Kafka)
            </p>
          </div>
        </div>

        <!-- Realtime Status & User Info -->
        <div class="flex items-center space-x-4">
          <!-- WebSockets Indicator -->
          <div class="flex items-center space-x-2 px-3 py-1 bg-gray-50 rounded-full border border-gray-200 text-xs">
            <span
              class="w-2.5 h-2.5 rounded-full"
              :class="store.signalRConnected ? 'bg-emerald-500 animate-pulse' : 'bg-rose-500'"
            ></span>
            <span class="font-medium text-gray-700">
              {{ store.signalRConnected ? 'SignalR Online' : 'Connecting...' }}
            </span>
          </div>

          <!-- Notification Bell -->
          <button
            @click="store.activeTab = 'inbox'"
            class="relative p-2 text-gray-600 hover:text-blue-600 hover:bg-gray-100 rounded-full transition"
            title="Hộp thư thông báo"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
            </svg>
            <span
              v-if="store.unreadCount > 0"
              class="absolute top-1 right-1 inline-flex items-center justify-center px-1.5 py-0.5 text-xs font-bold leading-none text-white transform translate-x-1/4 -translate-y-1/4 bg-red-600 rounded-full shadow"
            >
              {{ store.unreadCount > 99 ? '99+' : store.unreadCount }}
            </span>
          </button>

          <!-- Current User Profile -->
          <div class="flex items-center space-x-2 pl-2 border-l border-gray-200">
            <div class="w-8 h-8 rounded-full bg-blue-100 text-blue-700 flex items-center justify-center font-bold text-xs">
              UD
            </div>
            <div class="hidden sm:block text-left">
              <span class="block text-xs font-bold text-gray-800">{{ store.userId }}</span>
              <span class="block text-[10px] text-gray-500">Khách hàng Demo</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Navigation Tabs -->
    <div class="bg-gray-50 border-t border-gray-200">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <nav class="flex space-x-8" aria-label="Tabs">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            @click="store.activeTab = tab.id"
            :class="[
              store.activeTab === tab.id
                ? 'border-blue-600 text-blue-600 font-bold'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 font-medium',
              'whitespace-nowrap py-3 px-1 border-b-2 text-sm flex items-center space-x-2 transition'
            ]"
          >
            <span>{{ tab.icon }}</span>
            <span>{{ tab.name }}</span>
            <span
              v-if="tab.id === 'inbox' && store.unreadCount > 0"
              class="ml-2 bg-red-100 text-red-600 px-2 py-0.5 rounded-full text-xs font-bold"
            >
              {{ store.unreadCount }}
            </span>
          </button>
        </nav>
      </div>
    </div>
  </header>
</template>

<script setup>
import { useNotificationStore } from '../stores/notificationStore'

const store = useNotificationStore()

const tabs = [
  { id: 'inbox', name: 'Hộp thư Thông báo', icon: '📬' },
  { id: 'simulator', name: 'Bộ Giả lập Sự kiện (Demo)', icon: '🚀' },
  { id: 'preferences', name: 'Cấu hình Kênh nhận tin', icon: '⚙️' },
  { id: 'admin', name: 'Giám sát Transactional Outbox', icon: '📊' }
]
</script>
