<template>
  <span :class="['inline-flex items-center justify-center flex-shrink-0', sizeClass]" :title="titleText">
    <!-- 1. TELEGRAM -->
    <svg v-if="normalizedName === 'telegram'" viewBox="0 0 24 24" fill="none" class="w-full h-full">
      <circle cx="12" cy="12" r="12" fill="#229ED9"/>
      <path d="M17.5 7L5.5 11.6L9.6 13.2L11.2 18.2L13.3 14.8L16.2 17L17.5 7Z" fill="white"/>
      <path d="M9.6 13.2L15.3 9.4L11.2 14.8V18.2L9.6 13.2Z" fill="#D2EAF7"/>
    </svg>

    <!-- 2. GMAIL / EMAIL -->
    <svg v-else-if="normalizedName === 'email' || normalizedName === 'gmail'" viewBox="0 0 24 24" fill="none" class="w-full h-full">
      <path d="M2 6C2 4.9 2.9 4 4 4H20C21.1 4 22 4.9 22 6V18C22 19.1 21.1 20 20 20H4C2.9 20 2 19.1 2 18V6Z" fill="#EA4335" fill-opacity="0.12" stroke="#EA4335" stroke-width="1.75"/>
      <path d="M22 6L12 13L2 6" stroke="#EA4335" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"/>
      <rect x="5" y="7" width="14" height="10" rx="1.5" fill="#4285F4" fill-opacity="0.1"/>
    </svg>

    <!-- 3. ZALO ZNS -->
    <svg v-else-if="normalizedName === 'zalo' || normalizedName === 'zalo_zns'" viewBox="0 0 24 24" fill="none" class="w-full h-full">
      <rect width="24" height="24" rx="6" fill="#0068FF"/>
      <text x="12" y="16.5" font-family="Inter, sans-serif" font-weight="900" font-size="11" fill="white" text-anchor="middle" letter-spacing="-0.5">Zalo</text>
    </svg>

    <!-- 4. WEB PUSH (Windows Action Center / Browser Push) -->
    <svg v-else-if="normalizedName === 'web_push' || normalizedName === 'push'" viewBox="0 0 24 24" fill="none" class="w-full h-full">
      <rect width="24" height="24" rx="6" fill="#6366F1"/>
      <path d="M12 4C9.24 4 7 6.24 7 9V14L5 16V17H19V16L17 14V9C17 6.24 14.76 4 12 4Z" fill="white"/>
      <path d="M10 18C10 19.1 10.9 20 12 20C13.1 20 14 19.1 14 18H10Z" fill="white" fill-opacity="0.8"/>
    </svg>

    <!-- 5. IN-APP WEB (SignalR Realtime) -->
    <svg v-else-if="normalizedName === 'in_app' || normalizedName === 'signalr'" viewBox="0 0 24 24" fill="none" class="w-full h-full">
      <rect width="24" height="24" rx="6" fill="#4F46E5"/>
      <path d="M6 16.5L10 12.5L13.5 16L18 10M18 10H14M18 10V14" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
    </svg>

    <!-- 6. DISCORD WEBHOOK -->
    <svg v-else-if="normalizedName === 'webhook' || normalizedName === 'discord'" viewBox="0 0 24 24" fill="none" class="w-full h-full">
      <rect width="24" height="24" rx="6" fill="#5865F2"/>
      <path d="M18.5 7.5C17.3 6.9 16.1 6.5 14.8 6.3C14.6 6.6 14.4 7.1 14.2 7.5C12.8 7.3 11.4 7.3 10 7.5C9.8 7.1 9.6 6.6 9.4 6.3C8.1 6.5 6.9 6.9 5.7 7.5C3.7 10.5 3.2 13.4 3.5 16.3C4.9 17.3 6.2 18 7.5 18.4C7.8 17.9 8.1 17.4 8.4 16.9C7.9 16.7 7.5 16.5 7.1 16.2C7.2 16.1 7.3 16 7.4 15.9C10.4 17.3 13.8 17.3 16.8 15.9C16.9 16 17 16.1 17.1 16.2C16.7 16.5 16.3 16.7 15.8 16.9C16.1 17.4 16.4 17.9 16.7 18.4C18 18 19.3 17.3 20.7 16.3C21.1 12.9 20.1 10.1 18.5 7.5ZM9 14.5C8.2 14.5 7.5 13.8 7.5 13C7.5 12.2 8.2 11.5 9 11.5C9.8 11.5 10.5 12.2 10.5 13C10.5 13.8 9.8 14.5 9 14.5ZM15.2 14.5C14.4 14.5 13.7 13.8 13.7 13C13.7 12.2 14.4 11.5 15.2 11.5C16 11.5 16.7 12.2 16.7 13C16.7 13.8 16 14.5 15.2 14.5Z" fill="white"/>
    </svg>

    <!-- DEFAULT -->
    <svg v-else viewBox="0 0 24 24" fill="none" class="w-full h-full text-indigo-500" stroke="currentColor" stroke-width="1.75">
      <circle cx="12" cy="12" r="10"/>
      <path d="M12 16V12M12 8H12.01"/>
    </svg>
  </span>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  name: {
    type: String,
    required: true
  },
  size: {
    type: String,
    default: 'md' // 'xs', 'sm', 'md', 'lg', 'xl'
  }
})

const normalizedName = computed(() => {
  return (props.name || '').toLowerCase().trim()
})

const sizeClass = computed(() => {
  switch (props.size) {
    case 'xs': return 'w-3.5 h-3.5'
    case 'sm': return 'w-4 h-4'
    case 'lg': return 'w-6 h-6'
    case 'xl': return 'w-8 h-8'
    case 'md':
    default: return 'w-5 h-5'
  }
})

const titleText = computed(() => {
  switch (normalizedName.value) {
    case 'telegram': return 'Telegram Bot API'
    case 'email':
    case 'gmail': return 'Google SMTP Email'
    case 'zalo':
    case 'zalo_zns': return 'Zalo Notification Service (ZNS)'
    case 'web_push': return 'Windows Action Center (Web Push)'
    case 'in_app': return 'In-App SignalR'
    case 'webhook': return 'Discord Webhook'
    default: return props.name
  }
})
</script>
