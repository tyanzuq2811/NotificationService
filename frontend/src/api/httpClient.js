import axios from 'axios'
import { toast } from 'vue-sonner'

export const COMMAND_BASE_URL = import.meta.env.VITE_COMMAND_BASE_URL || 'http://localhost:5001/api'
export const QUERY_BASE_URL = import.meta.env.VITE_QUERY_BASE_URL || 'http://localhost:5002/api'
export const SIGNALR_HUB_URL = import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5002/hubs/notifications'

export const commandApi = axios.create({
  baseURL: COMMAND_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
})

export const queryApi = axios.create({
  baseURL: QUERY_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
})

function setupInterceptors(instance, serviceName) {
  instance.interceptors.response.use(
    (response) => response,
    (error) => {
      console.warn(`[${serviceName} Warning]:`, error.message)
      // Only show toast on write operations (POST, PUT, DELETE) to prevent spamming on read queries
      if (error.config && error.config.method !== 'get') {
        const msg = error.response?.data?.message || error.message || 'Lỗi kết nối máy chủ'
        toast.error(`${serviceName}: ${msg}`)
      }
      return Promise.reject(error)
    }
  )
}

setupInterceptors(commandApi, 'CommandService (5001)')
setupInterceptors(queryApi, 'QueryService (5002)')
