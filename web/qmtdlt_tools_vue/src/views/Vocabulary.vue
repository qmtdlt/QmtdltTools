<template>
  <div v-if="isMobileRef">
    <el-card style="height: 85vh;">
      <el-row>
        <el-col :span="8">
          <el-span>
            <!--单词-->
            {{ curWordRef?.wordText }}
          </el-span>
        </el-col>
      </el-row>
      <el-row>
        <el-span>
          <!--AI 解释-->
          {{ curWordRef?.aiExplanation }}
        </el-span>
      </el-row>
      <el-row>
        <el-span>
          <!--翻译-->
          {{ curWordRef?.aiTranslation }}
        </el-span>
      </el-row>
      <el-row>
        <el-span>
          <!--你的造句-->
          {{ curWordRef?.sentenceYouMade }}
        </el-span>
      </el-row>
      <el-row>
        <el-span>
          <!--造句是否正确-->
          {{ curWordRef?.ifUsageCorrect }}
        </el-span>
      </el-row>
      <el-row>
        <el-span>
          <!--错误原因-->
          {{ curWordRef?.incorrectReason }}
        </el-span>
      </el-row>
      <el-row>
        <el-col :span="12">
          <el-button size="small" type="primary" title="ignore in three days" @click="ignoreInTimeRange">
            ignore in three days
          </el-button>
        </el-col>
        <el-col :span="8">
          <el-button size="small" type="primary" title="next" @click="getWord">
            next
          </el-button>
        </el-col>
        <el-col :span="2">
          <el-button v-if="curWordRef?.pronunciation" size="small" circle
            @click="playPronunciation(curWordRef?.pronunciation)" title="播放发音">
            <el-icon>
              <Headset />
            </el-icon>
          </el-button>
        </el-col>
      </el-row>
    </el-card>
  </div>
  <div v-else>
    <el-card>
      <div>
        <el-select v-model="selectedBookId" placeholder="请选择书籍" clearable style="width: 300px" @change="onBookChange">
          <el-option v-for="book in books" :key="book.id" :label="book.title" :value="book.id" />
        </el-select>
      </div>
      <el-table height="80vh" :data="records" border v-loading="loading">
        <el-table-column label="序号" type="index" width="55" :index="indexMethod" />
        <el-table-column prop="wordText" label="单词" width="100" />
        <el-table-column prop="aiExplanation" label="AI释义" />
        <el-table-column prop="aiTranslation" label="AI翻译" width="160" />
        <el-table-column prop="sentenceYouMade" label="你的造句" width="200" />
        <el-table-column prop="ifUsageCorrect" label="是否正确" width="55" />
        <el-table-column prop="incorrectReason" label="错误原因" width="300" />
        <el-table-column prop="correctSentence" label="正确造句" width="200" />
        <!-- <el-table-column prop="createTime" label="创建时间" width="100">
          <template #default="{ row }">
            {{ formatDate(row.createTime) }}
          </template>
</el-table-column> -->
        <el-table-column label="发音" width="60">
          <template #default="{ row }">
            <el-button v-if="row.pronunciation" size="small" circle @click="playPronunciation(row.pronunciation)"
              title="播放发音">
              <el-icon>
                <Headset />
              </el-icon>
            </el-button>
          </template>
        </el-table-column>
        <!-- 新增操作列 -->
        <el-table-column label="操作" width="80">
          <template #default="{ row }">
            <el-button size="small" type="primary" @click="openMakeSentenceDialog(row.id)" title="造句">
              造句
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <div style="margin-top: 16px; text-align: right;">
        <el-pagination background layout=" prev, pager, next, sizes,jumper, ->, total" :total="total"
          :page-size="pageSize" :current-page="pageIndex" @current-change="onPageChange" @size-change="onSizeChange"
          :page-sizes="[5, 10, 20]" :pager-count="5" :hide-on-single-page="false" />
      </div>
    </el-card>

    <!-- 新增造句对话框 -->
    <el-dialog v-model="sentenceDialogVisible" title="Make Some Sentence" width="900px" @closed="fetchRecords">
      <!-- ... dialog content ... -->
      <el-row :gutter="20" align="middle">
        <el-col :span="16">
          <el-input v-model="currentSentence" placeholder="请输入您的句子"></el-input>
        </el-col>
        <el-col :span="8">
          <el-button @click="submitSentence" type="primary" :loading="sentenceSubmitting">
            What about my sentence
          </el-button>
        </el-col>
      </el-row>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="sentenceDialogVisible = false">取消</el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import request from '@/utils/request'
import { ElMessage, ElDialog, ElInput, ElMessageBox } from 'element-plus' // 确保导入 ElDialog, ElInput
// import icon
import { Headset } from '@element-plus/icons-vue' // 移除未使用的图标
import { isMobbile } from '@/utils/myutil'


const isMobileRef = ref(isMobbile());
const curWordRef = ref<VocabularyRecord>();

interface EBookMain {
  id: string
  title?: string
}

interface VocabularyRecord {
  id: string
  wordText?: string
  aiExplanation?: string
  aiTranslation?: string
  sentenceYouMade?: string
  ifUsageCorrect?: boolean
  incorrectReason?: string

  createTime?: string,
  pronunciation?: string // 新增字段
}

interface PageResult<T> {
  total: number
  pageIndex: number
  pageSize: number
  pageList: T[]
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
const makeSentenceResult = ref();
const pageSize = ref(5)
const loading = ref(false)
const currentAudioSource = ref<AudioBufferSourceNode | null>(null)

// --- 新增造句对话框相关状态 ---
const sentenceDialogVisible = ref(false)
const currentSentence = ref('')
const currentRecordId = ref<string | null>(null)
const sentenceSubmitting = ref(false)
// --- 结束新增状态 ---

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

function playPronunciation(base64string?: string) {
  if (!base64string) return
  // 停止前一个音频
  stopPronunciation();
  
  const byteArray = new Uint8Array(atob(base64string).split('').map(char => char.charCodeAt(0)))
  const audioContext = new AudioContext()
  const audioSource = audioContext.createBufferSource()
  currentAudioSource.value = audioSource
  audioContext.decodeAudioData(byteArray.buffer, (buffer) => {
    audioSource.buffer = buffer
    audioSource.connect(audioContext.destination)
    audioSource.onended = () => {
      currentAudioSource.value = null
      audioContext.close().catch(() => { })
    }
    try {
      audioSource.start()
    } catch {
      currentAudioSource.value = null
      audioContext.close().catch(() => { })
    }
  }, () => {
    currentAudioSource.value = null
    audioContext.close().catch(() => { })
  })
}
const stopPronunciation = () => {
  if (currentAudioSource.value) {
    try {
      currentAudioSource.value.stop()
      currentAudioSource.value.disconnect()
    } catch { }
    currentAudioSource.value = null
  }
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

// --- 新增打开造句对话框方法 ---
function openMakeSentenceDialog(recordId: string) {
  currentRecordId.value = recordId
  currentSentence.value = '' // 清空上次输入
  sentenceDialogVisible.value = true
}
// --- 结束新增方法 ---

// --- 新增提交造句方法 ---
async function submitSentence() {
  if (!currentSentence.value) {
    ElMessage.warning('请输入句子')
    return
  }
  if (!currentRecordId.value) {
    ElMessage.error('未找到记录ID，请重试')
    return
  }

  sentenceSubmitting.value = true
  try {
    const payload = {
      id: currentRecordId.value,
      sentence: currentSentence.value,
    }
    // 假设 MakeSentenceInputDto 只需要 id 和 sentence
    let result = await request.post('/api/Vocabulary/MakeSentence', payload)
    makeSentenceResult.value = result;
    ElMessage.success('造句提交成功')
    sentenceDialogVisible.value = false
    // 可选：提交成功后刷新列表
    // fetchRecords();
  } catch (error) {
    console.error('提交造句失败:', error)
    ElMessage.error('提交造句失败，请稍后重试')
  } finally {
    sentenceSubmitting.value = false
  }
}
// --- 结束新增方法 ---
onMounted(() => {
  fetchBooks()
  fetchRecords()
  if (isMobileRef.value) {
    getWord()
  }
})

const getWord = async () => {
  stopPronunciation();
  const res = await request.get<VocabularyRecord>('/api/Vocabulary/GetOneWord')
  curWordRef.value = res
  console.log(curWordRef.value)
}
const ignoreInTimeRange = async () => {
  if (!curWordRef.value) return
  let wordId = curWordRef.value.id
  // 弹出确认？
  ElMessageBox.confirm('Are you sure to ignore this word in three days?', 'Warning', {
    confirmButtonText: 'OK',
    cancelButtonText: 'Cancel',
    type: 'warning',
  }).then(async () => {
    // 确认后执行忽略操作
    await request.post('/api/Vocabulary/IgnoreInTimeRange', { id: wordId })
    getWord()
  }).catch(() => {
    // 取消操作
  })

}

// 序号列方法，支持分页
function indexMethod(index: number) {
  return (pageIndex.value - 1) * pageSize.value + index + 1
}
</script>

<style scoped>
/* 可选：为对话框按钮添加一些间距 */
.dialog-footer {
  text-align: right;
}
</style>