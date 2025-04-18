<template>
    <div class="vocabulary-page">
      <el-card>
        <div style="margin-bottom: 16px;">
          <el-select
            v-model="selectedBookId"
            placeholder="请选择书籍"
            clearable
            style="width: 300px"
            @change="onBookChange"
          >
            <el-option
              v-for="book in books"
              :key="book.id"
              :label="book.title"
              :value="book.id"
            />
          </el-select>
        </div>
        <el-table
          :data="records"
          border
          style="width: 100%;"
          v-loading="loading"
        >
          <el-table-column prop="wordText" label="单词" width="120" />
          <el-table-column prop="aiExplanation" label="AI释义" />
          <el-table-column prop="aiTranslation" label="AI翻译" width="300"/>
          <!-- <el-table-column prop="firstSentenceYouMade" label="例句1" />
          <el-table-column prop="secondSentenceYouMade" label="例句2" />
          <el-table-column prop="thirdSentenceYouMade" label="例句3" /> -->
          <el-table-column prop="createTime" label="创建时间" width="180">
            <template #default="{ row }">
              {{ formatDate(row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column label="发音" width="80">
    <template #default="{ row }">
      <el-button
        v-if="row.pronunciation"
        size="small"
        circle
        @click="playPronunciation(row.pronunciation)"
        title="播放发音">
        <el-icon><Headset /></el-icon>
    </el-button>
    </template>
  </el-table-column>
        </el-table>
        <div style="margin-top: 16px; text-align: right;">
          <el-pagination
            background
            layout="prev, pager, next, jumper, ->, total"
            :total="total"
            :page-size="pageSize"
            :current-page="pageIndex"
            @current-change="onPageChange"
            @size-change="onSizeChange"
            :page-sizes="[10, 20, 50, 100]"
            :pager-count="5"
            :hide-on-single-page="false"
          />
        </div>
      </el-card>
    </div>
  </template>
  
  <script setup lang="ts">
  import { ref, onMounted } from 'vue'
  import request from '@/utils/request'
  import { ElMessage } from 'element-plus'
// import icon
import { Headset,Lightning,View,Hide,VideoPause} from '@element-plus/icons-vue'
  
  interface EBookMain {
    id: string
    title?: string
  }
  
  interface VocabularyRecord {
    id: string
    wordText?: string
    aiExplanation?: string
    aiTranslation?: string
    firstSentenceYouMade?: string
    secondSentenceYouMade?: string
    thirdSentenceYouMade?: string
    createTime?: string,
    pronunciation?: string // 新增字段
  }
  
  interface PageResult<T> {
    total: number
    pageIndex: number
    pageSize: number
    items: T[]
  }
  
  interface ApiResponse<T> {
    data: T
    success: boolean
    message?: string
  }
  
  const books = ref<EBookMain[]>([])
  const selectedBookId = ref<string | null>(null)
  const records = ref<VocabularyRecord[]>([])
  const total = ref(0)
  const pageIndex = ref(1)
  const pageSize = ref(20)
  const loading = ref(false)
  
  function formatDate(dateStr?: string) {
    if (!dateStr) return ''
    return new Date(dateStr).toLocaleString()
  }
  
  async function fetchBooks() {
    try {
      const res = await request.get<EBookMain[]>('/api/EpubManage/GetBooks/GetBooks')
      books.value = res
    } catch (e) {
      ElMessage.error('获取书籍列表失败')
    }
  }
  const currentAudioSource = ref<AudioBufferSourceNode | null>(null)

function playPronunciation(base64string?: string) {
  if (!base64string) return
  // 停止前一个音频
  if (currentAudioSource.value) {
    try {
      currentAudioSource.value.stop()
      currentAudioSource.value.disconnect()
    } catch {}
    currentAudioSource.value = null
  }
  const byteArray = new Uint8Array(atob(base64string).split('').map(char => char.charCodeAt(0)))
  const audioContext = new AudioContext()
  const audioSource = audioContext.createBufferSource()
  currentAudioSource.value = audioSource
  audioContext.decodeAudioData(byteArray.buffer, (buffer) => {
    audioSource.buffer = buffer
    audioSource.connect(audioContext.destination)
    audioSource.onended = () => {
      currentAudioSource.value = null
      audioContext.close().catch(() => {})
    }
    try {
      audioSource.start()
    } catch {
      currentAudioSource.value = null
      audioContext.close().catch(() => {})
    }
  }, () => {
    currentAudioSource.value = null
    audioContext.close().catch(() => {})
  })
}
  async function fetchRecords() {
    loading.value = true
    try {
      let res: ApiResponse<PageResult<VocabularyRecord>>
      if (selectedBookId.value) {
        res = await request.get<ApiResponse<PageResult<VocabularyRecord>>>(
          '/api/Vocabulary/GetBookRecordsPage',
          {
            params: {
              bookId: selectedBookId.value,
              pageindex: pageIndex.value,
              pagesize: pageSize.value,
            },
          }
        )
      } else {
        res = await request.get<ApiResponse<PageResult<VocabularyRecord>>>(
          '/api/Vocabulary/GetUserRecordsPage',
          {
            params: {
              pageindex: pageIndex.value,
              pagesize: pageSize.value,
            },
          }
        )
      }
      debugger
      if (res && res.data) {
        records.value = res.data.pageList
        total.value = res.data.total
      } else {
        records.value = []
        total.value = 0
        ElMessage.error(res?.message || '获取单词记录失败')
      }
    } catch (e) {
      records.value = []
      total.value = 0
    } finally {
      loading.value = false
    }
  }
  
  function onBookChange() {
    pageIndex.value = 1
    fetchRecords()
  }
  
  function onPageChange(newPage: number) {
    pageIndex.value = newPage
    fetchRecords()
  }
  
  function onSizeChange(newSize: number) {
    pageSize.value = newSize
    pageIndex.value = 1
    fetchRecords()
  }
  
  onMounted(() => {
    fetchBooks()
    fetchRecords()
  })
  </script>
  
  <style scoped>
  .vocabulary-page {
    padding: 24px;
  }
  </style>