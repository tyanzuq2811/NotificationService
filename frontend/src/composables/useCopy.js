import { ref } from 'vue'
import { toast } from 'vue-sonner'

export function useCopy() {
  const copiedKey = ref(null)

  async function copyText(text, key = 'default', label = 'Nội dung') {
    if (!text) return false

    try {
      if (navigator.clipboard && window.isSecureContext) {
        await navigator.clipboard.writeText(text)
      } else {
        // Fallback for non-https or older browsers
        const textarea = document.createElement('textarea')
        textarea.value = text
        textarea.style.position = 'fixed'
        textarea.style.left = '-999999px'
        textarea.style.top = '-999999px'
        document.body.appendChild(textarea)
        textarea.focus()
        textarea.select()
        document.execCommand('copy')
        textarea.remove()
      }

      copiedKey.value = key
      toast.success(`Đã sao chép ${label}!`)

      setTimeout(() => {
        if (copiedKey.value === key) {
          copiedKey.value = null
        }
      }, 2000)

      return true
    } catch (err) {
      console.error('Copy failed:', err)
      toast.error('Không thể sao chép vào bộ nhớ tạm')
      return false
    }
  }

  function isCopied(key = 'default') {
    return copiedKey.value === key
  }

  return {
    copyText,
    isCopied,
    copiedKey
  }
}
