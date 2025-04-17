<template>
  <el-row>
    <el-col :span="16">
      <div class="divLeft">
        <div>
          <el-button @click="startRead">start</el-button>
          <el-button @click="stopRead">stop</el-button>
          <!-- <p style="color: black; overflow-y: scroll; height: 600px;">{{ full_pragraph_text }}</p> -->
          <HighlightedText :full-text="full_pragraph_text" :highlight-text="speaking_text" />
          <span style="color: black;">当前阅读位置: {{ curPosition }}</span>
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
import { ref, onMounted, onBeforeUnmount } from 'vue'
import HighlightedText from './HighLightedText.vue' // Import your HighlightedText component; 
import request from '@/utils/request' // Import your request utility
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数
import * as signalR from '@microsoft/signalr'
import { ElMessage } from 'element-plus';

const full_pragraph_text = ref("");
const speaking_text = ref("");
const curPosition = ref({});
const route = useRoute() // 使用路由
const bookId = ref(route.query.id as string) // 从查询参数中获取 id
const isReading = ref(false) // 是否正在阅读
const currentAudioSource = ref<AudioBufferSourceNode | null>(null); // Store the current audio source

var connection = new signalR.HubConnectionBuilder()
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`)
  .configureLogging(signalR.LogLevel.Information)
  .build()

const startRead = async () => {
  // 开始阅读任务
  if (isReading.value) return; // Avoid starting multiple reads
  isReading.value = true
  connection.invoke("Read", bookId.value);
}
const stopRead = async() => {
  isReading.value = false;
  if (currentAudioSource.value) {
    currentAudioSource.value.stop(); // Stop the current audio playback
    currentAudioSource.value.disconnect(); // Disconnect to free up resources
    currentAudioSource.value = null; // Clear the reference
    console.log("Audio stopped by user.");
  }
}

connection.on("onShowErrMsg", (msg: string) => {
  console.error(msg);
  ElMessage.error(msg);
});

connection.on("UIReadInfo", (readContent: any) => {
  full_pragraph_text.value = readContent.full_pragraph_text; // 读取到的文本内容
  speaking_text.value = readContent.speaking_text; // 读取到的文本内容
  curPosition.value = readContent.position; // 读取到的文本位置
  readBase64(readContent.speaking_buffer); // 读取到的音频内容
});

const readBase64 = (base64string:string)=>{
  if (!isReading.value) {
    console.log("Reading stopped, skipping audio playback.");
    return; // Don't play if reading is stopped
  }

  // Stop and clean up any previous source
  if (currentAudioSource.value) {
    try {
      currentAudioSource.value.stop();
      currentAudioSource.value.disconnect();
    } catch (error) {
      console.warn("Error stopping previous audio source:", error);
    }
    currentAudioSource.value = null;
  }

  var byteArray = new Uint8Array(atob(base64string).split('').map(char => char.charCodeAt(0)));
  const audioContext = new AudioContext();
  const audioSource = audioContext.createBufferSource();
  currentAudioSource.value = audioSource; // Store the new source

  audioContext.decodeAudioData(byteArray.buffer, (buffer) => {
    if (!isReading.value || currentAudioSource.value !== audioSource) {
       // Check if reading stopped or a newer source was created before decoding finished
       console.log("Reading stopped or new audio started before decoding finished.");
       currentAudioSource.value = null; // Ensure it's cleared if it was this one
       audioContext.close(); // Close the context if not needed
       return;
    }
    audioSource.buffer = buffer;
    audioSource.connect(audioContext.destination);
    audioSource.onended = () => {
      console.log("Audio ended. isReading:", isReading.value);
      // Only proceed if this specific source finished naturally AND reading is still active
      if (isReading.value && currentAudioSource.value === audioSource) {
        currentAudioSource.value = null; // Clear ref after natural end
        console.log("Invoking next Read.");
        connection.invoke("Read", bookId.value);
      } else if (currentAudioSource.value === audioSource) {
         // If it ended but reading was stopped, just clear the ref
         currentAudioSource.value = null;
      }
      // Close the context after playback finishes or is stopped
      audioContext.close().catch(e => console.warn("Error closing AudioContext:", e));
    };
    try {
      audioSource.start();      // start playing
      console.log("Audio started.");
    } catch (error) {
      console.error("Error starting audio playback:", error);
      currentAudioSource.value = null; // Clear ref on error
      audioContext.close(); // Close context on error
    }
  }, (error) => {
    console.error("Error decoding audio data:", error);
    if (currentAudioSource.value === audioSource) {
       currentAudioSource.value = null; // Clear ref on decoding error
    }
    audioContext.close(); // Close context on error
  })
}

connection.on("onsetbookposition", (readContent: any) => {
  // 设置书籍位置
  full_pragraph_text.value = readContent.full_pragraph_text; // 读取到的文本内容
  speaking_text.value = readContent.speaking_text; // 读取到的文本内容
  curPosition.value = readContent.position; // 读取到的文本位置
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
  connection.start().then(() => connection.invoke("InitCache",bookId.value));        // 开始阅读任务 onShowReadingText s
})

onBeforeUnmount(() => {
  // Ensure reading stops and audio cleans up when component unmounts
  stopRead(); // Call stopRead to handle audio cleanup
  // Disconnect signalR
  connection.stop().then(() => {
    console.log("SignalR Connection stopped.")
  }).catch((err) => {
    console.error("Error stopping SignalR connection:", err)
  })
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