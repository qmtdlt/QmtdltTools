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
<script setup lang="ts">
import { VueReader } from 'vue-reader'
import { useStorage } from '@vueuse/core'
import { ref, onMounted } from 'vue'
import request from '@/utils/request' // Import your request utility
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数

const route = useRoute() // 使用路由
const bookId = ref(route.query.id as string) // 从查询参数中获取 id
const bookTitle = ref(route.query.title as string) // 从查询参数中获取 title

const location = useStorage('book-progress', 0, undefined, {
  serializer: {
    read: (v: string) => JSON.parse(v),
    write: (v: unknown) => JSON.stringify(v),
  },
})

const bookData = ref<ArrayBuffer | null>(null)
const loading = ref<boolean>(true)
const error = ref<string | null>(null)

const fetchEpubBook = async () => {
  try {
    loading.value = true
    error.value = null
    
    if (!bookId.value) {
      throw new Error('未提供电子书 ID')
    }
    
    // 使用路由参数中的 id
    const response = await request.get(
      `/api/EpubManage/DownloadEpub/DownloadEpub?id=${bookId.value}`, 
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

    // 如果有标题，可以更新页面标题
    if (bookTitle.value) {
      document.title = `阅读: ${bookTitle.value}`
    }
  } catch (err: unknown) {
    const errorMessage = err instanceof Error ? err.message : 'Unknown error';
    error.value = 'Failed to load ebook: ' + errorMessage;
    loading.value = false;
    console.error('Error loading ebook:', err);
  }
}

onMounted(() => {
  fetchEpubBook()
})

const locationChange = (epubcifi: unknown) => {
  location.value = epubcifi
}
</script>