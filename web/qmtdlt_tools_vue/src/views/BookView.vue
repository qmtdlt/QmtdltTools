<template>
  <el-row>
    <el-col :span="16">
      <div class="divLeft">
        <div v-if="loading">Loading book...</div>
        <div v-else-if="error">{{ error }}</div>
        <vue-reader v-else :location="location" 
        :url="bookData" 
        @update:location="locationChange" 
        :getRendition="getRendition"/>
      </div>
    </el-col>
    <el-col :span="8" >
      <div class="divRIght" id="divRight" @dragover.prevent @drop="handleDrop">
        <!--这里准备放一些自定义功能，例如翻译，笔记等-->
       <el-row>
        <p style="height: 20vh;width: 90%;background-color: green;margin: auto;margin-top: 20px;">{{ droppedText }}</p>
       </el-row>
       <el-row>
        <el-button @click="autoSelection">自动选中段落</el-button>
        <el-button @click="speakText">朗读上方内容</el-button>
        <el-button>讲一讲上方内容</el-button>
       </el-row>
       <el-row>
        <div>
          <audio :src="audioSrc" controls></audio>
        </div>
       </el-row>
      </div>
    </el-col>
  </el-row>
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

// 使用 VueUse 的 useStorage 来存储书籍进度
const location = useStorage('book-progress', 0, undefined, {
  serializer: {
    read: (v: string) => JSON.parse(v),
    write: (v: unknown) => JSON.stringify(v),
  },
})

const rendition = ref<any>(null) // 用于存储 VueReader 的实例
const getRendition = (val:any) => {
  rendition.value = val
  rendition.value.on('selected', setRenderSelection)
}

const selections = ref<Array<any>>([]) // 用于存储选中的文本和范围
  const setRenderSelection = (cfiRange:any, contents:any) => {
  selections.value.push({
    text: rendition.value.getRange(cfiRange).toString(),
    cfiRange,
  })
  rendition.value.annotations.add('highlight', cfiRange, {}, null, 'hl', {
    fill: 'red',
    'fill-opacity': '0.5',
    'mix-blend-mode': 'multiply',
  })
  contents.window.getSelection().removeAllRanges()
}
const autoSelection = () => {
  console.log("自动选中段落文本",rendition.value.currentLocation());
  console.log("自动选中段落文本",rendition.value.currentLocation().start);
  console.log("自动选中段落文本",rendition.value.currentLocation().start.cfi);
  const range = rendition.value.getRange(rendition.value.currentLocation().start.cfi)
  const endRange = rendition.value.getRange(rendition.value.currentLocation().end.cfi)
  range.setEnd(endRange.startContainer, endRange.startOffset)
  droppedText.value = range
    .toString()
    .replace(/\s\s/g, '')
    .replace(/\r/g, '')
    .replace(/\n/g, '')
    .replace(/\t/g, '')
    .replace(/\f/g, '')
    
  myHeightlight("epubcfi(/6/20!/4/786,/1:0,/1:11)", range);
  console.log("自动选中段落文本: ", range.toString());
}
const myHeightlight = (cfi:string,range:any)=>{
  selections.value.push({
    text: rendition.value.getRange(cfi).toString(),
    range,
  })
  rendition.value.annotations.add('highlight', cfi, {}, null, 'hl', {
    fill: 'red',
    'fill-opacity': '0.5',
    'mix-blend-mode': 'multiply',
  })
}
const locationChange = (epubcifi: unknown) => {
  location.value = epubcifi
  autoSelection();
}
const remove = (cfiRange:any, index:any) => {
  rendition.value.annotations.remove(cfiRange, 'highlight')
  selections.value = selections.value.filter((item, j) => j !== index)
}

const show = (cfiRange:any) => {
  rendition.value.display(cfiRange)
}


const ttsLoading = ref<boolean>(true)
const bookData = ref<ArrayBuffer | null>(null)
const loading = ref<boolean>(true)
const error = ref<string | null>(null)
// 记录拖拽到右侧区域的文本
const droppedText = ref('')

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

// 接收拖拽到右侧区域的数据（使用原生拖拽事件，拖拽文本时默认类型为 text/plain）
const handleDrop = (event: DragEvent) => {
  event.preventDefault()
  droppedText.value = event.dataTransfer?.getData('text/plain') || ''
}

const audioSrc = ref<string>('');

const speakText = async (): Promise<void> => {
  ttsLoading.value = true;
  error.value = null;
  audioSrc.value = '';

  // 构造 SSML
  const ssml: string = `<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="zh-CN">
                      <voice name="zh-CN-YunxiNeural">
                        ${droppedText.value}
                        </voice>
                    </speak>`;

  try {
    // 调用微软 TTS API
    const response = await request.post(
      'https://eastus.tts.speech.microsoft.com/cognitiveservices/v1',
      ssml,
      {
        headers: {
          'Ocp-Apim-Subscription-Key': 'my speech key', // 替换为你的密钥
          'Content-Type': 'application/ssml+xml',
          'X-Microsoft-OutputFormat': 'riff-24khz-16bit-mono-pcm',
        },
        responseType: 'blob', // 接收二进制音频流
      }
    );

    // 将音频数据转换为 Blob 并生成 URL
    const audioBlob = new Blob([response], { type: 'audio/wav' });
    audioSrc.value = URL.createObjectURL(audioBlob);
  } catch (err) {
    error.value = '生成语音失败，请重试！';
    console.error(err);
  } finally {
    ttsLoading.value = false;
  }
};
onMounted(() => {
  fetchEpubBook()
})
</script>

<style scoped>
.divLeft{
  height: calc(90vh - 100px);
}
.divRIght{
  background: red;
  height: calc(90vh - 100px);
  width: 100%;
}
.selection {
  z-index: 1;
  background-color: white;
  color: #000;
}
</style>