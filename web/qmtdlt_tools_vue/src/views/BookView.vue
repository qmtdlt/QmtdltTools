<template>
  <el-row :gutter="32" class="main-row">
    <el-col :span="12" v-if="showLeft">
      <div class="divLeft card">
        <el-row class="left-header" justify="start" align="middle">
          <el-button-group>
            <el-button @click="startRead" type="primary" icon="el-icon-caret-right">start</el-button>
            <el-button @click="stopRead" type="danger" icon="el-icon-close">stop</el-button>
            <el-button @click="goPrevious" icon="el-icon-arrow-left">previous</el-button>
            <el-input v-model="jumpOffset" placeholder="偏移量" style="width: 90px; margin: 0 8px;" size="small"></el-input>
            <el-button @click="goNext" icon="el-icon-arrow-right">next</el-button>
          </el-button-group>
        </el-row>
        <el-row class="paragraph-row" justify="center">
          <div class="paragraph-area">
            <HighlightedText :full-text="readContent.full_pragraph_text" :highlight-text="readContent.speaking_text" />
          </div>
        </el-row>
        <el-row class="position-row" justify="end">
          <el-tag type="info" effect="plain" size="small">
            当前段落： {{ readContent.curPosition.pragraphIndex }} 第: {{ readContent.curPosition.sentenceIndex }} 句
          </el-tag>
        </el-row>
        <el-row class="dropped-row" justify="center">
          <div class="dropped-text-area" @dragover.prevent="onDragOver" @drop="handleDrop">
            <p style="min-height: 100px;">{{ droppedText }}</p>
          </div>
        </el-row>
      </div>
    </el-col>
    <el-col :span="12">
      <div class="divRight card">
        <el-row class="right-btn-group" justify="center" align="middle">
          <el-button @click="onListenWriteClick" type="success" icon="el-icon-headset">speak highlight</el-button>
          <el-button @click="promptOneWord" type="warning" icon="el-icon-lightning">prompt</el-button>
          <el-button @click="showOrHidReader" type="info" icon="el-icon-view">
            {{ showLeft ? '隐藏原文' : '显示原文' }}
          </el-button>
        </el-row>
        <div class="listenwrite-card">
          <ListenWrite
            :target-text="readContent.speaking_text"
            @completed="handleListenWriteComplete"
          />
        </div>
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
const jumpOffset = ref("1"); // 跳转偏移量
const currentAudioSource = ref<AudioBufferSourceNode | null>(null); // Store the current audio source
const userInputListenedText = ref('') // 用户输入的听到的内容
const showLeft = ref(true); // Control visibility of .divLeft

var connection = new signalR.HubConnectionBuilder()
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`)
  .configureLogging(signalR.LogLevel.Information)
  .build()

const goPrevious = async () => {
  resetPosition(0-parseInt(jumpOffset.value)); // Reset position to the previous one
}
const goNext = async () => {
  resetPosition(parseInt(jumpOffset.value)); // Reset position to the next one
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
  event.preventDefault();
  // 兼容性处理，确保 dataTransfer 存在
  if (event.dataTransfer) {
    droppedText.value = event.dataTransfer.getData('text/plain') || '';
  }
}

// 修正拖拽事件，确保事件能被触发
const onDragOver = (event: DragEvent) => {
  event.preventDefault();
};

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
.main-row {
  margin: 0;
  min-height: 100vh;
  background: #f6f8fa;
  padding: 32px 0 0 0;
  box-sizing: border-box;
}

.card {
  background: #fff;
  border-radius: 14px;
  box-shadow: 0 4px 24px 0 rgba(0,0,0,0.07), 0 1.5px 6px 0 rgba(0,0,0,0.03);
  padding: 32px 28px 24px 28px;
  min-height: 70vh;
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
  box-sizing: border-box;
}

.divLeft, .divRight {
  width: 100%;
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
}

.left-header {
  margin-bottom: 18px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.paragraph-row {
  margin-bottom: 12px;
  justify-content: center;
}

.paragraph-area {
  width: 100%;
  font-size: 1.18em;
  color: #222;
  line-height: 2;
  background: #f7fafd;
  border-radius: 8px;
  padding: 18px 16px;
  min-height: 120px;
  box-sizing: border-box;
  word-break: break-all;
  margin-bottom: 0;
  margin-top: 15px;
}

.position-row {
  margin-bottom: 12px;
  justify-content: flex-end;
}

.dropped-row {
  margin-top: 10px;
  justify-content: center;
}

.dropped-text-area {
  width: 100%;
  min-width: 220px;
  min-height: 40px;
  background: linear-gradient(90deg, #e0f7fa 0%, #f1f8e9 100%);
  border-radius: 8px;
  padding: 10px 16px;
  color: #333;
  font-size: 1.08em;
  box-shadow: 0 1px 4px 0 rgba(0,0,0,0.04);
  display: flex;
  align-items: center;
  margin: 0 auto;
}

.right-btn-group {
  margin-bottom: 18px;
  display: flex;
  gap: 16px;
  justify-content: center;
}

.listenwrite-card {
  width: 100%;
  background: #f9fafc;
  border-radius: 10px;
  box-shadow: 0 1px 6px 0 rgba(0,0,0,0.03);
  padding: 18px 14px;
  margin-top: 10px;
  display: flex;
  flex-direction: column;
  align-items: stretch;
}

.el-row {
  margin-bottom: 0;
}

.el-button + .el-button {
  margin-left: 10px;
}

@media (max-width: 900px) {
  .main-row {
    padding: 0;
  }
  .card {
    padding: 12px 2vw 10px 2vw;
    min-height: unset;
  }
  .paragraph-area {
    padding: 10px 6px;
    min-height: 60px;
  }
  .listenwrite-card {
    padding: 10px 4px;
  }
  .dropped-text-area {
    min-width: 120px;
    padding: 6px 4px;
    font-size: 0.98em;
  }
}
</style>