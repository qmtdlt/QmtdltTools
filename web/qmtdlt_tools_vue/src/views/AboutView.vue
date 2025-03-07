<template>
  <div style="height: 100vh">
    <div v-if="loading">Loading book...</div>
    <div v-else-if="error">{{ error }}</div>
    <vue-reader
      v-else
      :location="location"
      :url="bookData"
      @update:location="locationChange"
    />
  </div>
</template>
<script setup>
import { VueReader } from 'vue-reader'
import { useStorage } from '@vueuse/core'
import { ref, onMounted } from 'vue'
import request from '@/utils/request' // Import your request utility

const location = useStorage('book-progress', 0, undefined, {
  serializer: {
    read: (v) => JSON.parse(v),
    write: (v) => JSON.stringify(v),
  },
})

const bookData = ref(null)
const loading = ref(true)
const error = ref(null)

const fetchEpubBook = async () => {
  try {
    loading.value = true
    error.value = null
    
    // Fetch the book data with responseType set to 'arraybuffer'
    const response = await request.get(
      '/api/EpubManage/DownloadEpub/DownloadEpub', 
      { responseType: 'arraybuffer' }
    )
    
    // 打印数据类型和大小以验证
    console.log('Response type:', Object.prototype.toString.call(response))
    console.log('Response size:', response.byteLength, 'bytes')
    
    // 检查前几个字节是否符合EPUB格式 (EPUB文件应以 "PK" 开头)
    const firstBytes = new Uint8Array(response).slice(0, 10)
    console.log('First bytes:', Array.from(firstBytes))
    
    // The response should already be an ArrayBuffer which vue-reader can accept
    bookData.value = response
    loading.value = false
  } catch (err) {
    error.value = 'Failed to load ebook: ' + (err.message || 'Unknown error')
    loading.value = false
    console.error('Error loading ebook:', err)
  }
}

onMounted(() => {
  fetchEpubBook()
})

const locationChange = (epubcifi) => {
  location.value = epubcifi
}
</script>