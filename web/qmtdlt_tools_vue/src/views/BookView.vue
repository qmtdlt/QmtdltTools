<template>
  <el-row>
    <el-col :span="12" v-if="showLeft">
      <div class="divLeft">
        <div>
          <el-button @click="startRead">start</el-button>
          <el-button @click="stopRead">stop</el-button>
          <el-button @click="goPrevious">go previous</el-button>
          <el-input v-model="jumpOffset" placeholder="移动偏移量" style="width: 100px;"></el-input>
          <el-button @click="goNext">go next</el-button>
          <HighlightedText :full-text="readContent.full_pragraph_text" :highlight-text="readContent.speaking_text" />
          <span style="color: black;">当前阅读位置: {{ readContent.curPosition }}</span>
        </div>
       </div>
    </el-col>
    <el-col :span="12">
      <div class="divRIght" id="divRight" @dragover.prevent @drop="handleDrop">
        <!--这里准备放一些自定义功能，例如翻译，笔记等-->
        <el-row>
          <p style="height: 20vh;width: 90%;background-color: green;margin: auto;margin-top: 20px;">{{ droppedText }}
          </p>
        </el-row>
        <el-row>
          <el-button @click="onListenWriteClick">speak highlight content</el-button>
          <el-button @click="promptOneWord">prompt</el-button>
          <el-button @click="showOrHidReader">showOrHidReader</el-button>
        </el-row>
        <ListenWrite
          :target-text="readContent.speaking_text"
          @completed="handleListenWriteComplete"
          style="padding: 10px;"
         />
        <el-row>
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
import ListenWrite from './ListenWrite.vue'; // Keep this import

const route = useRoute() // 使用路由

const  readContent = ref({
  full_pragraph_text: '', // 读取到的文本内容
  speaking_text: '', // 读取到的文本内容
  curPosition: {}, // 读取到的文本位置
  bookId: route.query.id as string, // 书籍 ID
  speaking_buffer: '' // 读取到的音频内容
});

const isReading = ref(false) // 是否正在阅读
const jumpOffset = ref(1); // 跳转偏移量
const currentAudioSource = ref<AudioBufferSourceNode | null>(null); // Store the current audio source
const userInputListenedText = ref('') // 用户输入的听到的内容
const showLeft = ref(true); // Control visibility of .divLeft

var connection = new signalR.HubConnectionBuilder()
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`)
  .configureLogging(signalR.LogLevel.Information)
  .build()

const goPrevious = async () => {
  resetPosition(0-jumpOffset.value); // Reset position to the previous one
}
const goNext = async () => {
  resetPosition(jumpOffset.value); // Reset position to the next one
}

const resetPosition = (offset:number)=>{
  debugger
  stopRead(); // Stop any current reading before starting a new one

  connection.invoke("ResetPosition", readContent.value.bookId,offset).then(() => {
    console.log("Position reset successfully.");
    startRead(); // Start reading again
  }).catch((err) => {
    console.error("Error resetting position:", err);
  });
}

const startRead = async () => {
  // 开始阅读任务
  isReading.value = true
  connection.invoke("Read", readContent.value.bookId);
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
const listenWrite = ()=>{
  stopRead(); // Stop any current reading before starting a new one
  isReading.value = true
  readBase64(readContent.value.speaking_buffer,true); // 读取到的音频内容
}
const onListenWriteClick = () => {
  listenWrite();
};

const promptOneWord = ()=>{

}

const showOrHidReader = ()=>{
  showLeft.value = !showLeft.value;
}

connection.on("onShowErrMsg", (msg: string) => {
  console.error(msg);
  ElMessage.error(msg);
});

connection.on("UIReadInfo", (input: any) => {
  readContent.value.full_pragraph_text = input.full_pragraph_text; // 读取到的文本内容
  readContent.value.speaking_text = input.speaking_text; // 读取到的文本内容
  readContent.value.curPosition = input.position; // 读取到的文本位置
  readContent.value.speaking_buffer = input.speaking_buffer; // 读取到的文本位置
  readBase64(input.speaking_buffer,false); // 读取到的音频内容
});

const readBase64 = (base64string:string,isReadOnlyOneSentence:boolean)=>{
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
    debugger
    if(!isReadOnlyOneSentence)
    {
      // not read only one sentence, so add onended event,and go to next sentence
      audioSource.onended = () => {
        console.log("Audio ended. isReading:", isReading.value);
        // Only proceed if this specific source finished naturally AND reading is still active
        if (isReading.value && currentAudioSource.value === audioSource) {
          currentAudioSource.value = null; // Clear ref after natural end
          console.log("Invoking next Read.");
          connection.invoke("Read", readContent.value.bookId);
        } else if (currentAudioSource.value === audioSource) {
          // If it ended but reading was stopped, just clear the ref
          currentAudioSource.value = null;
        }
        // Close the context after playback finishes or is stopped
        audioContext.close().catch(e => console.warn("Error closing AudioContext:", e));
      };
    }
    
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

connection.on("onsetbookposition", (input: any) => {
  // 设置书籍位置
  readContent.value.full_pragraph_text = input.full_pragraph_text; // 读取到的文本内容
  readContent.value.speaking_text = input.speaking_text; // 读取到的文本内容
  readContent.value.curPosition = input.position; // 读取到的文本位置
});

// 记录拖拽到右侧区域的文本
const droppedText = ref('')

// 接收拖拽到右侧区域的数据（使用原生拖拽事件，拖拽文本时默认类型为 text/plain）
const handleDrop = (event: DragEvent) => {
  event.preventDefault()
  droppedText.value = event.dataTransfer?.getData('text/plain') || ''
}

onMounted(() => {
  connection.start().then(() => connection.invoke("InitCache",readContent.value.bookId));        // 开始阅读任务 onShowReadingText s
  // Add keyboard shortcut listener for ctrl+1
  window.addEventListener('keydown', handleKeyDown);
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
  window.removeEventListener('keydown', handleKeyDown);
})

// 快捷键监听 ctrl+1
function handleKeyDown(e: KeyboardEvent) {
  if (e.ctrlKey && e.key === '1') {
    listenWrite();
    e.preventDefault();
  }
}

const handleListenWriteComplete = () => {
  console.log("Listen and write completed!");
  ElMessage.success("听写完成!");
  // Optionally trigger next action, e.g., goNext()
  // goNext();
}
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

.el-row {
  margin-bottom: 10px; /* Add some spacing between rows */
}
</style>