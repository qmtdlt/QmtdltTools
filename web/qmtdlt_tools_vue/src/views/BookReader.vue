<template>
  <div style="height: 70vh;">
    <el-row justify="center" style="height: 75vh;">
      <HighlightedText v-if="!showListenWrite" :full-text="readContent.full_pragraph_text"
        :highlight-text="readContent.speaking_text" @phaseSelect="handlePhaseSelect" />
      <div v-show="showListenWrite" class="lwdiv">
        <el-row>
          <el-button @click="listenWriteClick" type="success"><el-icon>
              <Headset />
            </el-icon>&nbsp; Ctrl+1</el-button>
        </el-row>
        <div>
          <ListenWrite ref="listenWriteRef" :target-text="listenwrite_text" @completed="handleListenWriteComplete" />
        </div>
      </div>
    </el-row>
    <el-row style="margin-top: 10px;margin-bottom: 10px;" justify="right">
      <el-col :span="4" justify="end">
        <el-tag type="info" effect="plain" size="small">
          当前段落： {{ readContent.curPosition.pragraphIndex }} 第: {{ readContent.curPosition.sentenceIndex }} 句
          [{{ formatTime }}]
        </el-tag>
      </el-col>
    </el-row>
    <el-row justify="start" align="middle">
      <el-button @click="startRead" type="primary" plain circle><el-icon>
          <IconPlay />
        </el-icon></el-button>
      <el-button @click="stopRead" type="danger" plain circle><el-icon>
          <IconStop />
        </el-icon></el-button>
      <el-button @click="goPrevious"><el-icon>
          <ArrowLeft />
        </el-icon></el-button>
      <el-input v-model="jumpOffset" placeholder="偏移量" style="width: 90px; height: 30px; margin: 0 8px;"
        size="small"></el-input>
      <el-button @click="goNext"><el-icon>
          <ArrowRight />
        </el-icon></el-button>
      <el-button @click="showHideListenWrite" style="min-width: 100px;"> {{ lwbtnText }}</el-button>
    </el-row>
  </div>
  <el-dialog v-model="showTransDialog" title="翻译结果" width="85%">
    <div v-loading="translating">
      <el-row>
        <h1>{{ transSource }}</h1>
        <el-button @click="playTransVoice(transResult.wordVoiceBuffer)" type="primary" plain circle>
          <el-icon>
            <Headset />
          </el-icon>
        </el-button>
      </el-row>
      <el-row>
        <h2>Explanation:</h2>
        <el-button @click="playTransVoice(transResult.voiceBuffer)" type="primary" plain circle>
          <el-icon>
            <Headset />
          </el-icon>
        </el-button>
        <el-button @click="stopRead" type="primary" plain circle>
          <el-icon>
            <VideoPause />
          </el-icon>
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
    </div>
  </el-dialog>

</template>
<script setup lang="ts">

import { ref, onMounted, onBeforeUnmount } from 'vue'
import HighlightedText from './HighLightedText.vue' // Import your HighlightedText component;
import ListenWrite from './ListenWrite.vue'; // Keep this import
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数
import * as signalR from '@microsoft/signalr'
import { ElMessage, ElMessageBox } from 'element-plus';
import { Headset, VideoPause, CaretRight, Close, ArrowLeft, ArrowRight } from '@element-plus/icons-vue'
// Import cleanupAudio as well
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '../utils/audioplay';
import IconStop from '../components/icons/IconStop.vue';
import IconPlay from '../components/icons/IconPlay.vue';


const route = useRoute() // 使用路由
const listenWriteRef = ref<InstanceType<typeof ListenWrite> | null>(null); // 引用 ListenWrite 组件实例
const showListenWrite = ref(false); // 控制听写弹窗显示

const listenwrite_buffer = ref(''); // 音频数据
const listenwrite_text = ref(''); // 音频数据

const lwbtnText = ref('听写'); // 按钮文本
const listenWriteClick = () => {
  stopPlayBase64Audio();
  startPlayBase64Audio(listenwrite_buffer.value, () => {
    console.log("播放完成");
  }); // 读取到的音频内容
}

const showHideListenWrite = () => {
  showListenWrite.value = !showListenWrite.value;
  if (showListenWrite.value) {
    listenWriteRef.value?.focusInput(); // 调用子组件的方法
    lwbtnText.value = '关闭听写';
  } else {
    lwbtnText.value = '听写';
  }
  stopPlayBase64Audio();
}

onMounted(() => {
  // Add keyboard shortcut listener for ctrl+1
  window.addEventListener('keydown', handleKeyDown);
})

onBeforeUnmount(() => {
  cleanupAudio();
  window.removeEventListener('keydown', handleKeyDown);
})

// 快捷键监听 ctrl+1
function handleKeyDown(e: KeyboardEvent) {
  if (e.ctrlKey && e.key === '1') {
    stopPlayBase64Audio();
    startPlayBase64Audio(listenwrite_buffer.value, () => {
      console.log("播放完成");
    }); // 读取到的音频内容
    e.preventDefault();
  }
  // 如果是空格，停止播放
  if (e.ctrlKey && e.key === ' ') {
    stopPlayBase64Audio();
    e.preventDefault();
  }
  // 如果是ctrl + d ，调用 showHideListenWrite
  if (e.ctrlKey && e.key === 'd') {
    showHideListenWrite();
    e.preventDefault();
  }
}

const handleListenWriteComplete = async () => {
  ElMessage.success("听写完成!");
}

const handleReadContentChange = (data: string, text: string) => {
  debugger
  listenwrite_buffer.value = data;
  listenwrite_text.value = text;
}


const readContent = ref({
  full_pragraph_text: '', // 读取到的文本内容
  speaking_text: '', // 读取到的文本内容
  curPosition: { pragraphIndex: 0, sentenceIndex: 0 }, // 读取到的文本位置
  bookId: route.query.id as string, // 书籍 ID
  speaking_buffer: '' // 读取到的音频内容
});

const showTransDialog = ref(false) // 控制翻译弹窗显示
const translating = ref(false); // 拖拽到右侧区域的文本
const jumpOffset = ref("1"); // 跳转偏移量
const transResult = ref({ explanation: "", translation: "", voiceBuffer: "", wordVoiceBuffer: "" }); // Store translation result pronunciation is base64 string

// Use let instead of var for better scoping
let connection = new signalR.HubConnectionBuilder()
  //.withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent?access_token=${localStorage.getItem('token')}`)
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`, {
    accessTokenFactory: () => localStorage.getItem('token')
  })
  .configureLogging(signalR.LogLevel.Information)
  .withAutomaticReconnect({
		nextRetryDelayInMilliseconds: () => {
			return 2000; // 每5秒重连一次
		},
	})
  .build()

const goPrevious = async () => {
  stopRead(); // Stop any current reading before starting a new one
  // Delay invoking to ensure audio stops before requesting new content
  setTimeout(() => {
    connection.invoke("ResetPosition", readContent.value.bookId, 0 - parseInt(jumpOffset.value || '0')).then(() => { // Added fallback for parseInt
      startRead(); // Start reading again
    }).catch((err) => {
      console.error("Error resetting position:", err);
      ElMessage.error(`重置位置失败: ${err.message}`);
    });
  }, 100); // Small delay
}
const goNext = async () => {
  stopRead(); // Stop any current reading before starting a new one
  // Delay invoking to ensure audio stops before requesting new content
  setTimeout(() => {
    connection.invoke("ResetPosition", readContent.value.bookId, parseInt(jumpOffset.value || '0')).then(() => { // Added fallback for parseInt
      startRead(); // Start reading again
    }).catch((err) => {
      console.error("Error resetting position:", err);
      ElMessage.error(`重置位置失败: ${err.message}`);
    });
  }, 100); // Small delay
}

const startRead = async () => {
  console.log("startRead called. Invoking 'Read' on SignalR.");
  // This call needs to be triggered by a user gesture (the button click)
  // The audio playback will happen in the UIReadInfo handler, which
  // now uses the persistent AudioContext and attempts to resume it.
  connection.invoke("Read", readContent.value.bookId)
    .catch((err) => {
      console.error("Error invoking Read:", err);
      ElMessage.error(`开始阅读失败: ${err.message}`);
    });
}

const stopRead = async () => {
  console.log("stopRead called.");
  stopPlayBase64Audio(); // Now suspends the context
}

connection.on("onShowTrans", (result: any) => {
  translating.value = false;
  transResult.value = result; // Store the translation result
  console.log("Received translation. Playing audio.");
  // Play translation voice - onEnded is not needed here
  startPlayBase64Audio(transResult.value.voiceBuffer, () => {
    console.log("Translation audio playback finished.");
    // No need to invoke Read here, as it's just translation
  });
});

const playTransVoice = (voiceBuffer: string) => {
  translating.value = false;
  if (voiceBuffer) {
    startPlayBase64Audio(voiceBuffer, () => {
      console.log("playTransVoice playback finished.");
    });
  } else {
    ElMessage.error("没有翻译语音!");
  }
}

connection.on("onShowErrMsg", (msg: string) => {
  translating.value = false;
  console.error("Received error message:", msg);
  ElMessage.error(msg);
});

const formatTime = ref(""); // 格式化时间
connection.on("onUpdateWatch", (formatTimeStr: string) => {
  formatTime.value = formatTimeStr; // 更新时间
});

connection.on("UIReadInfo", (input: any) => {
  handleReadContentChange(input.speaking_buffer, input.speaking_text); // Emit the read content change event
  readContent.value.full_pragraph_text = input.full_pragraph_text; // 读取到的文本内容
  readContent.value.speaking_text = input.speaking_text; // 读取到的文本内容
  readContent.value.curPosition = input.position; // 读取到的文本位置
  readContent.value.speaking_buffer = input.speaking_buffer; // 读取到的音频内容

  // Start playing the received audio buffer
  startPlayBase64Audio(input.speaking_buffer, () => {
    console.log("UIReadInfo audio playback finished. Requesting next.");
    // Once the audio finishes, request the next chunk
    // This is the continuous reading loop
    connection.invoke("Read", readContent.value.bookId)
      .catch((err) => {
        console.error("Error invoking Read from onended:", err);
        ElMessage.error(`请求下一段失败: ${err.message}`);
      });
  });
});

connection.on("onsetbookposition", (input: any) => {
  console.log("Received onsetbookposition:", input);
  // 设置书籍位置
  readContent.value.full_pragraph_text = input.full_pragraph_text; // 读取到的文本内容
  readContent.value.speaking_text = input.speaking_text; // 读取到的文本内容
  readContent.value.curPosition = input.position; // 读取到的文本位置
  readContent.value.speaking_buffer = input.speaking_buffer; // 读取到的文本位置
  debugger
  handleReadContentChange(input.speaking_buffer, input.speaking_text);
  // Note: onsetbookposition likely doesn't trigger immediate playback
  // It just updates the displayed text/position. Playback starts on startRead.
});

onMounted(() => {
  console.log("Component mounted. Starting SignalR connection.");
  connection.start()
    .then(() => {
      console.log("SignalR Connection started.");
      // Initialize the cache on the server side
      connection.invoke("InitCache", readContent.value.bookId)
        .then(() => console.log("InitCache invoked."))
        .catch((err) => console.error("Error invoking InitCache:", err));
    })
    .catch((err) => {
      console.error("Error starting SignalR connection:", err);
      ElMessage.error(`SignalR 连接失败: ${err.message}`);
    });
})

onBeforeUnmount(() => {
  console.log("Component unmounting.");
  cleanupAudio(); // Call cleanupAudio to stop playback and close the context
  connection.stop().then(() => {
    console.log("SignalR Connection stopped.")
  }).catch((err) => {
    console.error("Error stopping SignalR connection:", err)
  })
})
const transSource = ref(''); // Store the selected text for translation
const handlePhaseSelect = async (phaseText: string) => {

  if (translating.value) {
    console.log("Translation already in progress. Ignoring phase select.");
    return; // Prevent multiple translation requests
  }

  try {
    translating.value = true; // Set loading immediately

    await ElMessageBox.confirm(
      `是否处理新单词: "${phaseText}"?`,
      '提示',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'info',
        // Disable closing on clicks outside or ESC key
        closeOnClickModal: false,
        closeOnPressEscape: false
      }
    );

    // User clicked Confirm
    console.log("User confirmed translation.");
    // No need for isFirstTime = false here if you want it to confirm every time

    connection.invoke("Trans", readContent.value.bookId, phaseText)
      .catch((err) => {
        console.error("Error invoking Trans:", err);
        ElMessage.error(`翻译请求失败: ${err.message}`);
        translating.value = false; // Hide loading on error
      });

    transSource.value = phaseText; // Store the selected text for translation
    showTransDialog.value = true;

    // If you want it to ask *again* next time they select something, keep isFirstTime = true here
    // isFirstTime.value = true; // Reset for the next selection attempt

  } catch (e: any) {
    // User clicked Cancel or an error occurred in the confirm box
    console.log("User cancelled translation or confirm failed:", e);
    ElMessage.info('已取消');
    translating.value = false; // Hide loading
    showTransDialog.value = false; // Ensure dialog is closed if it opened prematurely
    // isFirstTime.value = true; // Reset for the next selection attempt
  } finally {
    // If you kept isFirstTime = true in both catch and try (after success),
    // this finally block could simplify the reset logic.
    // isFirstTime.value = true; // Reset regardless of outcome
  }

}
</script>
<style scoped>
.el-button+.el-button {
  margin-left: 10px;
}

.lwdiv {
  width: 100vw;
  height: 100%;
  padding: 1rem;
  background-image: url('../assets/background1.png');
  border-radius: 5px;
}
</style>