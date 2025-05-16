<template>
  <div style="height: 70vh;">
    <el-row justify="center" style="height: 75vh;">
      <HighlightedText v-if="!showListenWrite && !showShadowDiv" :full-text="readContent.full_pragraph_text"
        :highlight-text="readContent.speaking_text" />
      <div v-show="showListenWrite" class="lwdiv">
        <el-row justify="left">
          <el-col :span="2">
            <el-button @click="listenWriteClick" type="success"><el-icon>
                <Headset />
              </el-icon>&nbsp; Ctrl+1</el-button>
          </el-col>
          <el-col :span="2">
            <el-button @click="showHideListenWrite" type="success"><el-icon>
                <Sort />
              </el-icon>&nbsp; Ctrl+D</el-button>
          </el-col>
        </el-row>
        <el-row>
          <ListenWrite ref="listenWriteRef" :target-text="listenwrite_text" @completed="handleListenWriteComplete" />
        </el-row>
      </div>
      <div v-show="showShadowDiv" class="shadowDiv">
        <ShadowingView ref="shadowingRef" :target-text="listenwrite_text" @completed="handleShadowingComplete" />
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
      <!-- <el-button @click="switchMode" type="primary" > -->

      <!-- <span v-if="useModelType === 1">听书模式</span>
          <span v-else-if="useModelType === 2">听写模式</span>
          <span v-else-if="useModelType === 3">跟读</span> -->
      <!-- </el-button> -->

      <el-radio-group v-model="useModelType" @change="switchMode">
        <el-radio-button label="1">听书模式</el-radio-button>
        <el-radio-button label="2">听写模式</el-radio-button>
        <el-radio-button label="3">跟读模式</el-radio-button>
      </el-radio-group>
      <el-button @click="showHideListenWrite" style="min-width: 100px;"> {{ lwbtnText }}</el-button>
    </el-row>
  </div>
</template>
<script setup lang="ts">

import { ref, onMounted, onBeforeUnmount } from 'vue'
import HighlightedText from './HighLightedText.vue' // Import your HighlightedText component;
import ListenWrite from './ListenWrite.vue'; // Keep this import
import ShadowingView from './ShadowingView.vue'; // Import your ShadowingView component
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数
import * as signalR from '@microsoft/signalr'
import { ElMessage, ElMessageBox } from 'element-plus';
import { Headset, VideoPause, CaretRight, Close, ArrowLeft, ArrowRight, Sort } from '@element-plus/icons-vue'
// Import cleanupAudio as well
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '../utils/audioplay';
import IconStop from '../components/icons/IconStop.vue';
import IconPlay from '../components/icons/IconPlay.vue';

const useModelType = ref("1"); // 1: 听书（听完一句自动read 下一句）2: 听写（听完一句，自动切换听写界面，听写成功回调后，下一句）3：跟读（听完一句，自动切换跟读界面，跟读成功回调后，下一句）

const switchMode = () => {
  
  if (useModelType.value === "1") {
    ElMessage.success("切换到听书模式");
  } else if (useModelType.value === "2") {
    ElMessage.success("切换到听写模式");
  } else if (useModelType.value === "3") {
    ElMessage.success("切换到跟读模式");
  } else {
    ElMessage.error("未知模式");
  }
}
const route = useRoute() // 使用路由
const listenWriteRef = ref<InstanceType<typeof ListenWrite> | null>(null); // 引用 ListenWrite 组件实例
const shadowingRef = ref<InstanceType<typeof ShadowingView> | null>(null); // 引用 ShadowingView 组件实例
const showListenWrite = ref(false); // 控制听写弹窗显示
const showShadowDiv = ref(false); // 跟读组件是否显示

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
  showListenWrite.value = false; // Show the ListenWrite component
  lwbtnText.value = '听写';
  connection.invoke("Read", readContent.value.bookId)
    .catch((err) => {
      console.error("Error invoking Read from onended:", err);
      ElMessage.error(`请求下一段失败: ${err.message}`);
    });
}

const handleShadowingComplete = async (audio: Blob) => {
  console.log("跟读音频:", audio);
  
  // 将 Blob 转为 Base64
  
  
};

const handleReadContentChange = (data: string, text: string) => {

  listenwrite_buffer.value = data;
  // text — 替换为空格
  listenwrite_text.value = text.replace('—', ' ').replace('-', ' '); // 读取到的音频内容
  
}

const readContent = ref({
  full_pragraph_text: '', // 读取到的文本内容
  speaking_text: '', // 读取到的文本内容
  curPosition: { pragraphIndex: 0, sentenceIndex: 0 }, // 读取到的文本位置
  bookId: route.query.id as string, // 书籍 ID
  speaking_buffer: '' // 读取到的音频内容
});

const jumpOffset = ref("1"); // 跳转偏移量

// Use let instead of var for better scoping
//.withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent?access_token=${localStorage.getItem('token')}`)
let connection = new signalR.HubConnectionBuilder()
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`, {
    accessTokenFactory: () => {
      // localStorage.getItem('token')
      const token = localStorage.getItem('token');
      return token ?? ''; // Return empty string if token is null
    }
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



connection.on("onShowErrMsg", (msg: string) => {
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
    if (useModelType.value === "1") {
      connection.invoke("Read", readContent.value.bookId)
        .catch((err) => {
          console.error("Error invoking Read from onended:", err);
          ElMessage.error(`请求下一段失败: ${err.message}`);
        });
    } else if (useModelType.value === "2") {
      showListenWrite.value = true; // Show the ListenWrite component
      listenWriteRef.value?.focusInput(); // 调用子组件的方法
      lwbtnText.value = '关闭听写';
    } else if (useModelType.value === "3") {
      showShadowDiv.value = true; // Show the Shadowing component
    }
  });
});

connection.on("onsetbookposition", (input: any) => {
  // 设置书籍位置
  readContent.value.full_pragraph_text = input.full_pragraph_text; // 读取到的文本内容
  readContent.value.speaking_text = input.speaking_text; // 读取到的文本内容
  readContent.value.curPosition = input.position; // 读取到的文本位置
  readContent.value.speaking_buffer = input.speaking_buffer; // 读取到的文本位置

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

.shadowDiv {
  width: 100vw;
  height: 100%;
  padding: 1rem;
  background-image: url('../assets/background1.png');
  border-radius: 5px;
}
</style>