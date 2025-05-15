<template>
  <div v-if="isMobileRef">
    <el-card style="height: 85vh;">
      <el-row>
        <el-col :span="8">
          <el-span>
            {{ curWordRef?.sentenceText }}
          </el-span>
        </el-col>
      </el-row>
      <!-- <el-row>
        <el-span>
          {{ curWordRef?.pronunciation }}
        </el-span>
      </el-row> -->
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
            @click="startPlayBase64Audio(curWordRef?.pronunciation, () => { })" title="播放发音">
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
      <el-table height="80vh" :data="records" border v-loading="loading">
        <el-table-column label="序号" type="index" width="100" :index="indexMethod" />

        <el-table-column label="造句">
          <template #default="{ row }">
            {{ row.sentenceText }}
            <el-button v-if="row.pronunciation" size="small" circle
              @click="startPlayBase64Audio(row.pronunciation, () => { })" title="播放发音">
              <el-icon>
                <Headset />
              </el-icon>
            </el-button>
          </template>
        </el-table-column>
      </el-table>
      <div style="margin-top: 16px; text-align: right;">
        <el-pagination background layout=" prev, pager, next, sizes,jumper, ->, total" :total="total"
          :page-size="pageSize" :current-page="pageIndex" @current-change="onPageChange" @size-change="onSizeChange"
          :page-sizes="[10, 20, 50]" :pager-count="5" :hide-on-single-page="false" />
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import request from '../utils/request'
import { ElMessage, ElDialog, ElInput, ElMessageBox } from 'element-plus' // 确保导入 ElDialog, ElInput
// import icon
import { Headset } from '@element-plus/icons-vue' // 移除未使用的图标
import { isMobbile } from '../utils/myutil'
import { stopPlayBase64Audio, startPlayBase64Audio, cleanupAudio } from '../utils/audioplay'

const isMobileRef = ref(isMobbile());
const curWordRef = ref<ListenWriteRecord>();

interface ListenWriteRecord {
  id: string
  sentenceText?: string
  pronunciation?: string
  createTime?: string,
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

const records = ref<ListenWriteRecord[]>([])
const total = ref(0)
const pageIndex = ref(1)
const pageSize = ref(10)
const loading = ref(false)
const stopPronunciation = () => {
  stopPlayBase64Audio();
}
async function fetchRecords() {
  loading.value = true
  try {
    let res: ApiResponse<PageResult<ListenWriteRecord>>
    res = await request.get<ApiResponse<PageResult<ListenWriteRecord>>>(
      'api/ListenWrite/GetUserRecords',
      {
        params: {
          pageindex: pageIndex.value,
          pagesize: pageSize.value,
        },
      }
    )
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


function onPageChange(newPage: number) {
  pageIndex.value = newPage
  fetchRecords()
}

function onSizeChange(newSize: number) {
  pageSize.value = newSize
  pageIndex.value = 1
  fetchRecords()
}


// --- 结束新增方法 ---
onMounted(() => {
  fetchRecords()
  if (isMobileRef.value) {
    getWord()
  }
})

const getWord = async () => {
  stopPronunciation();
  const res = await request.get<ListenWriteRecord>('/api/ListenWrite/GetOneSentence')
  debugger
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
onBeforeUnmount(() => {
  cleanupAudio();
})
</script>

<style scoped>
/* 可选：为对话框按钮添加一些间距 */
.dialog-footer {
  text-align: right;
}
</style>