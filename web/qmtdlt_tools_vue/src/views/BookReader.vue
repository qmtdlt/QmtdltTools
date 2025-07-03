<template>
  <div class="book-reader-container">
    <div class="book-reader-main">
      <!-- 左侧书籍内容,三种模式切换 -->
      <div class="div_left_content" :class="{ 'with-explanation': showExplanation }">
        <!-- 书籍内容 -->
        <HighlightedText v-if="useModelType === '1'" :full-text="readContent.full_pragraph_text"
          :highlight-text="readContent.speaking_text" />
        <!-- 听写组件 -->
        <div v-if="useModelType === '2'" class="lwdiv">
          <el-row>
            <ListenWrite ref="listenWriteRef" :target-text="listenwrite_text" @completed="handleListenWriteComplete" />
          </el-row>
          <el-row v-if="listenwrite_text_is_show" justify="center" style="margin: 18px 0;">
            <el-card class="listenwrite-text-card" shadow="hover">
              <span class="listenwrite-text">{{ listenwrite_text }}</span>
            </el-card>
          </el-row>
        </div>
        <!-- 跟读组件 -->
        <div v-if="useModelType === '3'" class="shadowDiv">
          <ShadowingView ref="shadowingRef" :target-text="listenwrite_text" @completed="handleShadowingComplete" />
        </div>
      </div>
      <!-- 右侧讲解面板 -->
      <div class="div_right_content" :class="{ 'show': showExplanation }">
        <div v-if="showExplanation" class="explanation-panel">
          <div class="explanation-title">
            段落讲解
            <el-icon @click="hideExplanation" style="cursor: pointer; margin-left: 20px;">
              <Right />
            </el-icon>
            <el-icon @click="playExplaintion" style="cursor: pointer; margin-left: 20px;">
              <Headset />
            </el-icon>
          </div>
          <div class="explanation-content">{{ explanationText }}</div>
        </div>
      </div>
    </div>
    <div class="book-reader-footer">
      <!-- 进度条 -->
      <div>
        <el-slider v-model="readContent.curPosition.progressValue" @change="progChange"></el-slider>
        <div>
          <el-button @click="startRead" type="primary" plain circle>
            <el-icon>
              <IconPlay />
            </el-icon>
          </el-button>

          <el-button @click="stopRead" type="danger" plain circle>
            <el-icon>
              <IconStop />
            </el-icon>
          </el-button>

          <el-radio-group v-model="useModelType" @change="switchMode">
            <el-radio-button label="1"><el-icon><Headset /></el-icon><p v-if="!isMobileRef">听书模式</p></el-radio-button>
            <el-radio-button v-if="!isMobileRef" label="2"><el-icon><Edit /></el-icon><p v-if="!isMobileRef">听写模式</p></el-radio-button>
            <el-radio-button label="3"><el-icon><Microphone /></el-icon><p v-if="!isMobileRef">跟读模式</p></el-radio-button>
          </el-radio-group>

          <el-button @click="listenWriteClick" type="success"><el-icon>
              <Headset />
            </el-icon><p v-if="!isMobileRef">Ctrl+1 重听</p></el-button>

          <el-button @click="listenWriteHint" type="success" v-if="!isMobileRef"><el-icon>
              <MagicStick />
            </el-icon><p >Ctrl+D 提示</p></el-button>

          <el-button @click="collectPharagraph" type="success"><el-icon>
              <Management />
            </el-icon><p v-if="!isMobileRef">收藏段落</p></el-button>

          <el-button @click="explainPhase" type="success" :loading="explanationLoading" >
            <el-icon><DataAnalysis/></el-icon><p v-if="!isMobileRef">段落讲解</p></el-button>
        </div>
      </div>
    </div>
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
import { Headset, MagicStick, CaretRight,Microphone,Edit, Close, ArrowLeft, ArrowRight, DataAnalysis, Management, Right } from '@element-plus/icons-vue'
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '../utils/audioplay';
import IconStop from '../components/icons/IconStop.vue';
import IconPlay from '../components/icons/IconPlay.vue';
import request from '@/utils/request'; // 导入请求工具
import { isMobbile } from '@/utils/myutil';

const isMobileRef = ref(isMobbile()); // 判断是否为移动端
// const subHeight = ref(isMobileRef.value ? '' : '60px'); // 用于存储子组件的高度
const explanationLoading = ref(false); // 控制讲解面板的显示
const listenwrite_text_is_show = ref(false); // 控制文本是否显示
const useModelType = ref("1"); // 1: 听书（听完一句自动read 下一句）2: 听写（听完一句，自动切换听写界面，听写成功回调后，下一句）3：跟读（听完一句，自动切换跟读界面，跟读成功回调后，下一句）
const route = useRoute() // 使用路由
const listenWriteRef = ref<InstanceType<typeof ListenWrite> | null>(null); // 引用 ListenWrite 组件实例
const shadowingRef = ref<InstanceType<typeof ShadowingView> | null>(null); // 引用 ShadowingView 组件实例
const listenwrite_buffer = ref('');   // 音频数据
const listenwrite_text = ref('');     // 音频数据

const showExplanation = ref(false)
const explanationText = ref('')

const switchMode = () => {
  if (useModelType.value === "1") {
    // ElMessage.success("切换到听书模式");
  } else if (useModelType.value === "2") {
    // ElMessage.success("切换到听写模式");
    listenWriteRef.value?.focusInput(); // 调用子组件的方法
    stopPlayBase64Audio();
  } else if (useModelType.value === "3") {
    // ElMessage.success("切换到跟读模式");
  } else {
    ElMessage.error("未知模式");
  }
}

const hideExplanation = () => {
  showExplanation.value = false; // 隐藏讲解
  stopPlayBase64Audio(); // 停止播放音频
}

const listenWriteClick = () => {
  stopPlayBase64Audio();
  startPlayBase64Audio(listenwrite_buffer.value, () => {
    console.log("播放完成");
  }); // 读取到的音频内容
}
const listenWriteHint = () => {
  listenwrite_text_is_show.value = !listenwrite_text_is_show.value; // 切换文本显示状态
}

const collectPharagraph = async () => {
  await excerptChapter();
}

const excerptChapter = async () => {
  const res = await ElMessageBox.confirm('是否要摘录这段话?', '提示', {
    confirmButtonText: '摘录',
    cancelButtonText: '取消',
  })
  if ("confirm" == res) {
    // Handle the action when the user confirms
    console.log('User confirmed:', res)
    await request.post('/api/EpubManage/ExcerptChapter/ExcerptChapter?content=' + readContent.value.full_pragraph_text);
    ElMessage.success('摘录成功')
  } else {
    // Handle the action when the user cancels
    console.log('User cancelled:', res)
  }
}

interface ExplainResultDto {
  explanation?: string
  voiceBuffer?: string
}

const explainResult = ref<ExplainResultDto>({}); // 用于存储讲解结果

const playExplaintion = () => {
  stopPlayBase64Audio();
  startPlayBase64Audio(explainResult.value.voiceBuffer ?? "", () => {
    console.log("播放完成");
  });
  explanationText.value = explainResult.value.explanation ?? '';
  showExplanation.value = true;
}

const explainPhase = async () => {
  const res = await ElMessageBox.confirm('是否要讲解这段话?', '提示', {
    confirmButtonText: '讲解',
    cancelButtonText: '取消',
  })
  if ("confirm" == res) {
    // 调用接口获取讲解内容
    explanationLoading.value = true; // 显示加载状态
    explainResult.value = await request.post<ExplainResultDto>('/api/ReadBook/GetExplainResult', {
      Phase: readContent.value.full_pragraph_text,
      bookId: readContent.value.bookId,
      PhaseIndex: readContent.value.curPosition.pragraphIndex,
    });
    explanationLoading.value = false; // 隐藏加载状态

    stopPlayBase64Audio();
    startPlayBase64Audio(explainResult.value.voiceBuffer ?? "", () => {
      console.log("播放完成");
    });
    explanationText.value = explainResult.value.explanation ?? '';
    showExplanation.value = true;
    ElMessage.success('讲解成功')
  } else {
    showExplanation.value = false;
  }
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
    // TODO 显示提示
    e.preventDefault();
    listenwrite_text_is_show.value = !listenwrite_text_is_show.value;
  }
}

const handleListenWriteComplete = async () => {
  ElMessage.success("听写完成!");
  readNext();
}
const readNext = () => {
  listenwrite_text_is_show.value = false; // 隐藏文本
  connection.invoke("Read", readContent.value.bookId)
    .catch((err) => {
      console.error("Error invoking Read from onended:", err);
      ElMessage.error(`请求下一段失败: ${err.message}`);
    });
}
const handleShadowingComplete = async (audio: Blob) => {
  console.log("跟读音频:", audio);
  readNext();
};

const handleReadContentChange = (data: string, text: string) => {
  listenwrite_buffer.value = data;
  // text — 替换为空格
  listenwrite_text.value = text.replace('—', ' ').replace('-', ' '); // 读取到的音频内容
}

const readContent = ref({
  full_pragraph_text: '', // 读取到的文本内容
  speaking_text: '', // 读取到的文本内容
  curPosition: { pragraphIndex: 0, sentenceIndex: 0, progressValue: 0 }, // 读取到的文本位置
  bookId: route.query.id as string, // 书籍 ID
  speaking_buffer: '', // 读取到的音频内容  
});

let connection = new signalR.HubConnectionBuilder()
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`, {
    accessTokenFactory: () => {
      const token = localStorage.getItem('token');
      return token ?? ''; // Return empty string if token is null
    }
  })
  .configureLogging(signalR.LogLevel.Information)
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: () => {
      return 2000; // 每5秒重连一次
    },
  }).build()

const progChange = () => {
  stopRead();
  setTimeout(() => {
    connection.invoke("ResetPosition", readContent.value.bookId, readContent.value.curPosition.progressValue).then(() => { // Added fallback for parseInt
      startRead(); // Start reading again
    }).catch((err) => {
      console.error("Error resetting position:", err);
      ElMessage.error(`重置位置失败: ${err.message}`);
    });
  }, 100); // Small delay
}

const startRead = async () => {
  console.log("startRead called. Invoking 'Read' on SignalR.");
  isStopRead.value = false;
  readNext();
}

const isStopRead = ref(false);
const stopRead = async () => {
  isStopRead.value = true;
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
    if (useModelType.value === "1" && isStopRead.value == false) {
      readNext(); // 继续读取下一段
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
.book-reader-container {
  height: 100%;
}

.book-reader-main {
  display: flex;
  flex: auto;
  overflow: auto;
  padding: 0;
  margin: 0;
  height: calc(100% - 130px); /* Adjust height to fit footer */
}

.book-reader-footer {
  flex-shrink: 0;
  height: 120px;
  position: sticky;
  z-index: 10;
  width: calc(100% - 20px);
  margin: 0px 10px 10px 10px !important;
  background-color: #f7f6f2;
  padding: 10px;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.06);
}

.div_left_content {
  flex: 1 1 0%;
  transition: width 0.3s;
  min-width: 0;
}

.div_left_content.with-explanation {
  flex: 0 0 calc(100% - 400px);
  max-width: calc(100% - 400px);
}

.div_right_content {
  width: 0;
  transition: width 0.3s;
  overflow: hidden;
}

.div_right_content.show {
  width: 400px;
  min-width: 400px;
  max-width: 400px;
  background: #f7fafd;
  border-left: 1px solid #e6e8eb;
  box-shadow: -2px 0 8px rgba(64, 158, 255, 0.06);
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 0;
}

.explanation-panel {
  padding: 18px 16px 16px 16px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.explanation-title {
  font-size: 1.1em;
  font-weight: bold;
  color: #409EFF;
  margin-bottom: 12px;
}
.el-radio-group{
  font-size: 14px;
  margin-left: 10px;
  margin-right: 10px;
}
.explanation-content {
  flex: 1 1 0;
  font-size: 1em;
  color: #333;
  white-space: pre-wrap;
  word-break: break-all;
  overflow-y: auto;
  line-height: 1.7;
  background: #fff;
  border-radius: 6px;
  padding: 12px;
  box-shadow: 0 1px 4px rgba(64, 158, 255, 0.04);
}


.listenwrite-text-card {
  background: #f7fafd;
  border-radius: 8px;
  padding: 12px 24px;
  width: 100%;
  text-align: left;
  border: 1px solid #e6e8eb;
  box-shadow: 0 2px 8px rgba(64, 158, 255, 0.06);
}

.listenwrite-text {
  font-size: 2em;
  color: #409EFF;
  font-weight: bold;
  letter-spacing: 1px;
  word-break: break-all;
}

.el-button+.el-button {
  margin-left: 10px;
}

.lwdiv {
  width: calc(100% - 20px);
  height: calc(100% - 20px);
  padding: 1rem;
  background-image: url('../assets/background1.png');
  border-radius: 5px;
  margin: 10px;
}

.shadowDiv {
  width: 99%;
  height: 100%;
  padding: 1rem;
  background-image: url('../assets/background1.png');
  border-radius: 5px;
  width: calc(100% - 20px);
  height: calc(100% - 20px);
  margin: 10px;
}
</style>