<template>
  <transition
    enter-active-class="transform ease-out duration-300 transition"
    enter-from-class="translate-y-2 opacity-0 sm:translate-y-0 sm:translate-x-2"
    enter-to-class="translate-y-0 opacity-100 sm:translate-x-0"
    leave-active-class="transition ease-in duration-100"
    leave-from-class="opacity-100"
    leave-to-class="opacity-0"
  >
    <div
      v-if="store.toast.show"
      class="fixed bottom-6 right-6 z-50 max-w-md w-full bg-white shadow-2xl rounded-xl border border-blue-100 p-4 flex items-start space-x-3 pointer-events-auto"
    >
      <div class="flex-shrink-0 w-10 h-10 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-bold text-lg">
        🔔
      </div>
      <div class="flex-1 min-w-0">
        <p class="text-sm font-bold text-gray-900 truncate">
          {{ store.toast.title }}
        </p>
        <p class="mt-1 text-xs text-gray-600 line-clamp-2">
          {{ store.toast.content }}
        </p>
        <span class="mt-2 inline-flex items-center px-2 py-0.5 rounded text-[10px] font-medium bg-blue-50 text-blue-700">
          ⚡ SignalR Realtime In-App
        </span>
      </div>
      <button
        @click="store.toast.show = false"
        class="text-gray-400 hover:text-gray-600 text-sm font-semibold"
      >
        ✕
      </button>
    </div>
  </transition>
</template>

<script setup>
import { useNotificationStore } from '../stores/notificationStore'

const store = useNotificationStore()
</script>
