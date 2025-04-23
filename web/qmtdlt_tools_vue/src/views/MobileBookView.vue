<template>
  <el-row>
    <el-col>
      <el-card style="height: 85vh;" >
        <el-row justify="start" align="middle" v-if="showLeft">
          <el-button-group>
            <el-button @click="startRead" type="primary" icon="el-icon-caret-right">start</el-button>
            <el-button @click="stopRead" type="danger" icon="el-icon-close">stop</el-button>
            <el-button @click="goPrevious" icon="el-icon-arrow-left">previous</el-button>
            <el-input v-model="jumpOffset" placeholder="偏移量" style="width: 90px; margin: 0 8px;"
              size="small"></el-input>
            <el-button @click="goNext" icon="el-icon-arrow-right">next</el-button>
          </el-button-group>
        </el-row>
        <el-row class="paragraph-row" justify="center" v-if="showLeft">
          <div class="paragraph-area">
            <HighlightedTextMobile :full-text="readContent.full_pragraph_text" :highlight-text="readContent.speaking_text"  @phaseSelect="handlePhaseSelect"/>
          </div>
        </el-row>
        <el-row style="margin-top: 10px;" justify="right" v-if="showLeft">
          <el-col :span="4" justify="end">
            <el-tag type="info" effect="plain" size="small">
              当前段落： {{ readContent.curPosition.pragraphIndex }} 第: {{ readContent.curPosition.sentenceIndex }} 句 [{{formatTime}}]
            </el-tag>
          </el-col>
        </el-row>
      </el-card>
    </el-col>
  </el-row>
  <!--弹出翻译结果显示-->
  <el-dialog>
    <el-card v-loading="dropTextDealing" style="width: 100%;height: 40vh;">
      <el-row>
        <h2>Explanation:</h2>
        <el-button @click="playTransVoice" type="primary" plain circle>
          <el-icon><Headset /></el-icon>
        </el-button>
        <el-button @click="stopRead" type="primary" plain circle>
          <el-icon><VideoPause /></el-icon>
        </el-button>
      </el-row>
      <el-row>
        <h3>{{ transResult.explanation }}</h3>
      </el-row>
      <el-row>
        <h2>Translation:</h2>
      </el-row>
      <el-row>
        <h3>{{ transResult.translation }}</h3>
      </el-row>
    </el-card>
  </el-dialog>

</template>
<script setup lang="ts">
import { useStorage } from '@vueuse/core'
import { ref, onMounted, onBeforeUnmount } from 'vue'
import HighlightedTextMobile from './HighLightedTextMobile.vue' // Import your HighlightedText component; 
import request from '@/utils/request' // Import your request utility
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数
import * as signalR from '@microsoft/signalr'
import { ElMessage } from 'element-plus';
import ListenWrite from './ListenWrite.vue'; // Keep this import
// import icon
import { Headset,Lightning,View,Hide,VideoPause} from '@element-plus/icons-vue'

const route = useRoute() // 使用路由

const readContent = ref({
  full_pragraph_text: '', // 读取到的文本内容
  speaking_text: '', // 读取到的文本内容
  curPosition: { pragraphIndex: 0, sentenceIndex: 0 }, // 读取到的文本位置
  bookId: route.query.id as string, // 书籍 ID
  speaking_buffer: '' // 读取到的音频内容
});

const dropTextDealing = ref(false); // 拖拽到右侧区域的文本
const sentence1 = ref(''); // 句子1
const sentence2 = ref(''); // 句子1
const sentence3 = ref(''); // 句子1
const isReading = ref(false) // 是否正在阅读
const jumpOffset = ref("1"); // 跳转偏移量
const currentAudioSource = ref<AudioBufferSourceNode | null>(null); // Store the current audio source
const showLeft = ref(true); // Control visibility of .divLeft
const transResult = ref({ explanation: "", translation: "", voiceBuffer: "" }); // Store translation result pronunciation is base64 string

var connection = new signalR.HubConnectionBuilder()
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`)
  .configureLogging(signalR.LogLevel.Information)
  .build()

const goPrevious = async () => {
  resetPosition(0 - parseInt(jumpOffset.value)); // Reset position to the previous one
}
const goNext = async () => {
  resetPosition(parseInt(jumpOffset.value)); // Reset position to the next one
}

const resetPosition = (offset: number) => {
  
  stopRead(); // Stop any current reading before starting a new one

  connection.invoke("ResetPosition", readContent.value.bookId, offset).then(() => {
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
const stopRead = async () => {
  isReading.value = false;
  if (currentAudioSource.value) {
    currentAudioSource.value.stop(); // Stop the current audio playback
    currentAudioSource.value.disconnect(); // Disconnect to free up resources
    currentAudioSource.value = null; // Clear the reference
    console.log("Audio stopped by user.");
  }
}
const listenWrite = () => {
  stopRead(); // Stop any current reading before starting a new one
  isReading.value = true
  readBase64(readContent.value.speaking_buffer, true); // 读取到的音频内容
}

connection.on("onShowTrans", (result: any) => {
  
  dropTextDealing.value = false; // 停止处理拖拽文本
  console.log(result);
  transResult.value = result; // Store the translation result
  isReading.value = true;
  readBase64(transResult.value.voiceBuffer, true); // 读取到的音频内容
});
const playTransVoice = () => {
  
  dropTextDealing.value = false; // 停止处理拖拽文本
  if (transResult.value.voiceBuffer) {
    isReading.value = true;
    readBase64(transResult.value.voiceBuffer, true); // 读取到的音频内容
  } else {
    ElMessage.error("没有翻译语音!");
  }
}
connection.on("onShowErrMsg", (msg: string) => {
  
  dropTextDealing.value = false; // 停止处理拖拽文本
  console.error(msg);
  ElMessage.error(msg);
});

const formatTime = ref(""); // 格式化时间
connection.on("onUpdateWatch", (formatTimeStr: string) => {
  formatTime.value = formatTimeStr; // 更新时间
});

connection.on("UIReadInfo", (input: any) => {
  
  readContent.value.full_pragraph_text = input.full_pragraph_text; // 读取到的文本内容
  readContent.value.speaking_text = input.speaking_text; // 读取到的文本内容
  readContent.value.curPosition = input.position; // 读取到的文本位置
  readContent.value.speaking_buffer = input.speaking_buffer; // 读取到的文本位置
  readBase64(input.speaking_buffer, false); // 读取到的音频内容
});

const readBase64 = (base64string: string, isReadOnlyOneSentence: boolean) => {
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
    
    if (!isReadOnlyOneSentence) {
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
  readContent.value.speaking_buffer = input.speaking_buffer; // 读取到的文本位置
});

// 修正拖拽事件，确保事件能被触发
const onDragOver = (event: DragEvent) => {
  event.preventDefault();
};

onMounted(() => {
  connection.start().then(() => connection.invoke("InitCache", readContent.value.bookId));        // 开始阅读任务 onShowReadingText s
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

const handlePhaseSelect = async (phaseText:string) => {
  ElMessage.success("选中内容: " + phaseText);  
  // connection.invoke("Trans", readContent.value.bookId, droppedText.value); // 发送拖拽文本到服务器进行翻译
  // 显示dialog
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
  box-shadow: 0 4px 24px 0 rgba(0, 0, 0, 0.07), 0 1.5px 6px 0 rgba(0, 0, 0, 0.03);
  padding: 32px 28px 24px 28px;
  min-height: 80vh;
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
  box-sizing: border-box;
}

.divLeft,
.divRight {
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
  min-width: 400px;
  background: linear-gradient(90deg, #e0f7fa 0%, #f1f8e9 100%);
  border-radius: 5px;
  padding: 5px 5px;
  color: #333;
  font-size: 1.08em;
  box-shadow: 0 1px 4px 0 rgba(0, 0, 0, 0.04);
  display: flex;
  align-items: left;
  margin-left: 10px;
  /* 虚线边框 */
  border: 1px dashed #ccc;
  transition: background 0.2s;
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
  box-shadow: 0 1px 6px 0 rgba(0, 0, 0, 0.03);
  padding: 18px 14px;
  margin-top: 10px;
  display: flex;
  flex-direction: column;
  align-items: stretch;
}

.el-row {
  margin-bottom: 0;
}

.el-button+.el-button {
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