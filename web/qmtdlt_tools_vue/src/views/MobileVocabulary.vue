<template>
  <div>
    <el-card style="height: 85vh;">
      <el-row>
        <el-col :span="8">
          <el-span>
            <!--单词-->
            {{ curWordRef?.wordText }}
          </el-span>
          <el-button v-if="curWordRef?.pronunciation" size="small" circle
            @click="startPlayBase64Audio(curWordRef?.wordPronunciation,()=>{})" title="播放发音">
            <el-icon>
              <Headset />
            </el-icon>
          </el-button>
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
            @click="startPlayBase64Audio(curWordRef?.pronunciation,()=>{})" title="播放发音">
            <el-icon>
              <Headset />
            </el-icon>
          </el-button>
        </el-col>
      </el-row>
    </el-card>
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
/* 可选：为对话框按钮添加一些间距 */
.dialog-footer {
  text-align: right;
}
</style>