<template>
  <el-row>
    <el-col>
      <el-card style="height: 80vh;">
        <el-row justify="start" align="middle">
          <el-button-group>
            <el-button @click="startRead" type="primary" icon="el-icon-caret-right">start</el-button>
            <el-button @click="stopRead" type="danger" icon="el-icon-close">stop</el-button>
            <el-button @click="goPrevious" icon="el-icon-arrow-left">previous</el-button>
            <el-input v-model="jumpOffset" placeholder="偏移量" style="width: 90px; margin: 0 8px;"
              size="small"></el-input>
            <el-button @click="goNext" icon="el-icon-arrow-right">next</el-button>
          </el-button-group>
        </el-row>
        <el-row class="paragraph-row" justify="center">
          <div class="paragraph-area">
            <HighlightedTextMobile :full-text="readContent.full_pragraph_text"
              :highlight-text="readContent.speaking_text" @phaseSelect="handlePhaseSelect" />
          </div>
        </el-row>
        <el-row style="margin-top: 10px;" justify="right">
          <el-col :span="4" justify="end">
            <el-tag type="info" effect="plain" size="small">
              当前段落： {{ readContent.curPosition.pragraphIndex }} 第: {{ readContent.curPosition.sentenceIndex }} 句
              [{{ formatTime }}]
            </el-tag>
          </el-col>
        </el-row>       
      </el-card>
    </el-col>
  </el-row>
  <!--弹出翻译结果显示-->
  <el-dialog v-model="showTransDialog" title="翻译结果" width="85%">
    <div v-loading="translating">
      <el-row>
        <h2>Explanation:</h2>
        <el-button @click="playTransVoice" type="primary" plain circle>
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
import HighlightedTextMobile from './HighLightedTextMobile.vue' // Import your HighlightedText component; 
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数
import * as signalR from '@microsoft/signalr'
import { ElMessage, ElMessageBox } from 'element-plus';
import { Headset, VideoPause } from '@element-plus/icons-vue'
import { startPlayBase64Audio,stopPlayBase64Audio } from '@/utils/audioplay';
const route = useRoute() // 使用路由
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
    startRead(); // Start reading again
  }).catch((err) => {
    console.error("Error resetting position:", err);
  });
}

const startRead = async () => {
  connection.invoke("Read", readContent.value.bookId);
}
const stopRead = async () => {
  stopPlayBase64Audio(); // Stop any current audio playback
}

connection.on("onShowTrans", (result: any) => {
  translating.value = false; 
  transResult.value = result; // Store the translation result
  startPlayBase64Audio(transResult.value.voiceBuffer,()=>{
    console.log("onShowTrans playback finished.");
  });
});
const playTransVoice = () => {
  translating.value = false;
  if (transResult.value.voiceBuffer) {
    startPlayBase64Audio(transResult.value.voiceBuffer,()=>{
      console.log("playTransVoice playback finished.");
    });
  } else {
    ElMessage.error("没有翻译语音!");
  }
}
connection.on("onShowErrMsg", (msg: string) => {
  translating.value = false;
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
  let conn = connection; // 这里是为了避免在回调函数中使用 this
  startPlayBase64Audio(input.speaking_buffer,()=>{
    console.log("Audio playback finished.");
    conn.invoke("Read", readContent.value.bookId);
  });
});

connection.on("onsetbookposition", (input: any) => {
  // 设置书籍位置
  readContent.value.full_pragraph_text = input.full_pragraph_text; // 读取到的文本内容
  readContent.value.speaking_text = input.speaking_text; // 读取到的文本内容
  readContent.value.curPosition = input.position; // 读取到的文本位置
  readContent.value.speaking_buffer = input.speaking_buffer; // 读取到的文本位置
});

onMounted(() => {
  connection.start().then(() => connection.invoke("InitCache", readContent.value.bookId));        // 开始阅读任务 onShowReadingText s
})

onBeforeUnmount(() => {
  stopRead(); // Call stopRead to handle audio cleanup
  connection.stop().then(() => {
    console.log("SignalR Connection stopped.")
  }).catch((err) => {
    console.error("Error stopping SignalR connection:", err)
  })
})

const isFirstTime = ref(true); // 使用 VueUse 的 useStorage 来存储是否第一次使用
const handlePhaseSelect = async (phaseText: string) => {
  if (isFirstTime.value) {
    isFirstTime.value = false; // 设置为 false，表示已经处理过一次
    try {

      await ElMessageBox.confirm(
        `是否处理新单词: "${phaseText}"?`,
        '提示',
        {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'info',
        }
      )
      // 用户点击确定，调用翻译接口
      translating.value = true
      connection.invoke("Trans", readContent.value.bookId, phaseText)
      showTransDialog.value = true
      isFirstTime.value = true; // 重置为 true，表示可以再次处理
    } catch {
      // 用户点击取消
      ElMessage.info('已取消')
      isFirstTime.value = true; // 重置为 true，表示可以再次处理
    }
  }

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