import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export const useThemeStore = defineStore('theme', () => {
  const currentTheme = ref(localStorage.getItem('theme') || 'light')

  const isDark = computed(() => {
    if (currentTheme.value === 'system') {
      return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches
    }
    return currentTheme.value === 'dark'
  })

  function applyTheme() {
    const root = document.documentElement
    if (isDark.value) {
      root.classList.add('dark')
    } else {
      root.classList.remove('dark')
    }
  }

  function toggleTheme() {
    currentTheme.value = isDark.value ? 'light' : 'dark'
    localStorage.setItem('theme', currentTheme.value)
    applyTheme()
  }

  function setTheme(theme) {
    currentTheme.value = theme
    localStorage.setItem('theme', theme)
    applyTheme()
  }

  // Chart.js Theme helpers
  const chartColors = computed(() => {
    if (isDark.value) {
      return {
        textColor: '#A1A1AA',      // zinc-400
        gridColor: '#27272A',      // zinc-800
        tooltipBg: '#18181B',      // zinc-900
        tooltipBorder: '#3F3F46'   // zinc-700
      }
    } else {
      return {
        textColor: '#64748B',      // slate-500
        gridColor: '#F1F5F9',      // slate-100
        tooltipBg: '#FFFFFF',
        tooltipBorder: '#E2E8F0'
      }
    }
  })

  // Initialize theme on load
  applyTheme()

  return {
    currentTheme,
    isDark,
    toggleTheme,
    setTheme,
    applyTheme,
    chartColors
  }
})
