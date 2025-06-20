<template>
  <div class="word-detail-mobile">
    <div class="word-header">
      <span class="word-title">
        {{ curWordRef?.wordText }}
        <el-button type="default" size="large" circle class="sound-btn" v-if="curWordRef?.wordPronunciation"
        @click="startPlayBase64Audio(curWordRef?.wordPronunciation,()=>{})">
        <el-icon><Headset /></el-icon>
      </el-button>
      </span>
    </div>
    <div class="word-explanation">
      {{ curWordRef?.aiExplanation }}
    </div>
    <div class="word-translation">
      {{ curWordRef?.aiTranslation }}
    </div>
    <div class="word-actions">
      <el-button type="primary" size="large" class="action-btn" @click="ignoreInTimeRange">三天内忽略</el-button>
      <el-button type="primary" size="large" class="action-btn" @click="getWord">下一个</el-button>
      <el-button type="default" size="large" circle class="sound-btn" v-if="curWordRef?.pronunciation"
        @click="startPlayBase64Audio(curWordRef?.pronunciation,()=>{})">
        <el-icon><Headset /></el-icon>
      </el-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import request from '@/utils/request'
import { ElMessage, ElMessageBox } from 'element-plus' // 确保导入 ElDialog, ElInput
// import icon
import { Headset } from '@element-plus/icons-vue' // 移除未使用的图标
import { isMobbile } from '@/utils/myutil'
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '../utils/audioplay';

const isMobileRef = ref(isMobbile());
const curWordRef = ref<VocabularyRecord>();


interface VocabularyRecord {
  id: string
  wordText?: string
  aiExplanation?: string
  aiTranslation?: string
  sentenceYouMade?: string
  ifUsageCorrect?: boolean
  incorrectReason?: string
  createTime?: string,
  wordPronunciation?: string // 新增字段
  pronunciation?: string // 新增字段
}


interface ApiResponse<T> {
  data: T
  success: boolean
  message?: string
}

const currentAudioSource = ref<AudioBufferSourceNode | null>(null)

// --- 结束新增方法 ---
onMounted(() => {
  if (isMobileRef.value) {
    getWord()
  }
})

const getWord = async () => {
  stopPlayBase64Audio();
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
</script>

<style scoped>
.word-detail-mobile {
  max-width: 480px;
  margin: 0 auto;
  padding: 18px 8px 32px 8px;
  background: #fff;
  min-height: 100vh;
  box-sizing: border-box;
}
.word-header {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-bottom: 18px;
}
.word-title {
  font-size: 1.6em;
  font-weight: bold;
  color: #222;
  display: flex;
  align-items: center;
  gap: 8px;
}
.sound-icon {
  font-size: 1.2em;
  color: #409EFF;
}
.word-explanation {
  font-size: 1.1em;
  color: #444;
  margin-bottom: 16px;
  line-height: 1.7;
  text-align: left;
  word-break: break-word;
}
.word-translation {
  font-size: 1.3em;
  color: #67C23A;
  font-weight: bold;
  margin-bottom: 28px;
  text-align: left;
}
.word-actions {
  display: flex;
  gap: 12px;
  justify-content: center;
  flex-wrap: wrap;
}
.action-btn {
  min-width: 110px;
  font-size: 1em;
}
.sound-btn {
  background: #f4f8fb;
  color: #409EFF;
  border: none;
}
</style>