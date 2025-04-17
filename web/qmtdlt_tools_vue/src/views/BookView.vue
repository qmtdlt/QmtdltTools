<template>
  <el-row>
    <el-col :span="16">
      <div class="divLeft">
        <div>
          <el-button @click="callHub">test</el-button>
          <p style="color: black; overflow-y: scroll; height: 600px;">{{ full_pragraph_text }}</p>
          <span style="color: black;">当前阅读位置: {{ curPosition }}</span>
          <p style="color: black; overflow-y: scroll; height: 600px;">{{ speaking_text }}</p>
        </div>
       </div>
    </el-col>
    <el-col :span="8">
      <div class="divRIght" id="divRight" @dragover.prevent @drop="handleDrop">
        <!--这里准备放一些自定义功能，例如翻译，笔记等-->
        <el-row>
          <p style="height: 20vh;width: 90%;background-color: green;margin: auto;margin-top: 20px;">{{ droppedText }}
          </p>
        </el-row>
        <el-row>
          <!-- <el-button @click="autoSelection">自动选中段落</el-button>
          <el-button @click="speakText">朗读上方内容</el-button> -->
          <el-button>讲一讲上方内容</el-button>
        </el-row>
        <el-row>
          <div>
            <!-- <audio :src="audioSrc" controls></audio> -->
          </div>
        </el-row>
      </div>
    </el-col>
  </el-row>
</template>
<script setup lang="ts">
import { useStorage } from '@vueuse/core'
import { ref, onMounted,onBeforeUnmount } from 'vue'
import request from '@/utils/request' // Import your request utility
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数
import * as signalR from '@microsoft/signalr'
import { ElMessage } from 'element-plus';

const full_pragraph_text = ref("");
const speaking_text = ref("");
const curPosition = ref({});
const route = useRoute() // 使用路由
const bookId = ref(route.query.id as string) // 从查询参数中获取 id

var connection = new signalR.HubConnectionBuilder()
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`)
  .configureLogging(signalR.LogLevel.Information)
  .build()

const callHub = async () => {
  connection.start().then(() => connection.invoke("InitCache",bookId.value));        // 开始阅读任务 onShowReadingText 
}

connection.on("onShowErrMsg", (msg: string) => {
  console.error(msg);
  ElMessage.error(msg);
});

connection.on("UIReadInfo", (readContent: any) => {
  debugger
  full_pragraph_text.value = readContent.full_pragraph_text; // 读取到的文本内容
  speaking_text.value = readContent.speaking_text; // 读取到的文本内容
  curPosition.value = readContent.position; // 读取到的文本位置
  readBase64(readContent.speaking_buffer); // 读取到的音频内容
});

const readBase64 = (base64string:string)=>{
  var byteArray = new Uint8Array(atob(base64string).split('').map(char => char.charCodeAt(0)));  
  const audioContext = new AudioContext();
  const audioSource = audioContext.createBufferSource();
  audioContext.decodeAudioData(byteArray.buffer, (buffer) => {
    audioSource.buffer = buffer;
    audioSource.connect(audioContext.destination);
    audioSource.onended = () => {
      // callback when audio read finished
      connection.invoke("Read", bookId.value);   
    };
    audioSource.start();      // start playing
  })
}


connection.on("onsetbookposition", (pos: any) => {
  // 设置书籍位置
  curPosition.value = pos;
  connection.invoke("Read", bookId.value);        // 
});

// 使用 VueUse 的 useStorage 来存储书籍进度
const location = useStorage('book-progress', 0, undefined, {
  serializer: {
    read: (v: string) => JSON.parse(v),
    write: (v: unknown) => JSON.stringify(v),
  },
})


// 记录拖拽到右侧区域的文本
const droppedText = ref('')

// 接收拖拽到右侧区域的数据（使用原生拖拽事件，拖拽文本时默认类型为 text/plain）
const handleDrop = (event: DragEvent) => {
  event.preventDefault()
  droppedText.value = event.dataTransfer?.getData('text/plain') || ''
}

onMounted(() => {

})

onBeforeUnmount(() => {
  // 断开连接
  connection.stop().then(() => {
    console.log("Connection stopped.")
  }).catch((err) => {
    console.error(err)
  })
  // STOP audio play
  
})
</script>

<style scoped>
.divLeft {
  height: calc(90vh - 100px);
}

.divRIght {
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