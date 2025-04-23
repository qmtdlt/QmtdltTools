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
// Import cleanupAudio as well
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '@/utils/audioplay';

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

// Use let instead of var for better scoping
let connection = new signalR.HubConnectionBuilder()
  .withUrl(`${import.meta.env.VITE_API_URL}/signalr-hubs/bookcontent`)
  .configureLogging(signalR.LogLevel.Information)
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

const playTransVoice = () => {
  translating.value = false;
  if (transResult.value.voiceBuffer) {
    console.log("Playing translation voice again.");
    startPlayBase64Audio(transResult.value.voiceBuffer, () => {
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
  console.log("Received UIReadInfo:", input);
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

const isFirstTime = ref(true); // This logic might need refinement based on desired UX
const handlePhaseSelect = async (phaseText: string) => {
  // Consider if you want the confirm dialog to appear *every* time they select a phase,
  // or only the first time they use the feature. The current logic with isFirstTime
  // seems to reset it immediately after one attempt (confirm or cancel).
  // Maybe the confirm dialog should only show the *very* first time this component
  // is used, or per session? If per session, use sessionStorage. If per user, use localStorage.
  // As it is, it will confirm every time but only *process* the first selection until confirmation/cancel.

  if (translating.value) {
      console.log("Translation already in progress. Ignoring phase select.");
      return; // Prevent multiple translation requests
  }

  console.log(`Phase selected: "${phaseText}"`);

  try {
      // Only show the confirmation on the first interaction attempt
      // The current logic of resetting isFirstTime = true will make it ask every time.
      // If you truly only want it once EVER, use localStorage/VueUse useStorage.
      // If you want it once *per selection attempt*, keep the isFirstTime logic.
      // Let's keep the current logic but clarify its behavior.

      // If you want it ONLY the very first time ANY phase is selected in this component session:
      // Use a ref outside this function and check/set it globally for the component.
      // If you want it every time for THIS specific phase selection attempt: keep it as is.

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

       showTransDialog.value = true;

       // If you want it to ask *again* next time they select something, keep isFirstTime = true here
       // isFirstTime.value = true; // Reset for the next selection attempt

     } catch(e: any) {
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

    // Re-evaluating the isFirstTime logic: If you want the confirmation for *each* selection attempt,
    // remove the isFirstTime check and just run the try/catch block directly.
    // If you want it only *once per component load*, initialize isFirstTime = true and remove the resets.
    // Let's assume you want it per selection attempt and simplify:
     /*
     translating.value = true; // Set loading immediately
     try {
         await ElMessageBox.confirm(...) // your confirm logic
         console.log("User confirmed translation.");
         connection.invoke("Trans", readContent.value.bookId, phaseText)
             .catch(...) // error handling
         showTransDialog.value = true;
     } catch(e: any) {
         console.log("User cancelled translation or confirm failed:", e);
         ElMessage.info('已取消');
         showTransDialog.value = false;
     } finally {
          translating.value = false; // Hide loading in all cases
     }
     */
     // Let's revert to the original isFirstTime structure but understand its behavior.
     // The original structure with `isFirstTime.value = false` at the start and `true` in catch/try
     // means it will *process* the logic only if `isFirstTime` is true, then set it to false,
     // then set it back to true in the catch/try. This is a convoluted way to effectively
     // process *one* selection attempt at a time, and potentially block rapid clicks.
     // A simpler guard is just checking the `translating.value` flag.
     // Let's remove the complex isFirstTime logic and just use `translating.value` as a guard.
}

// Removed the complex isFirstTime logic from handlePhaseSelect
const handlePhaseSelectCleaned = async (phaseText: string) => {
  if (translating.value) {
    console.log("Translation already in progress. Ignoring phase select.");
    return; // Prevent multiple translation requests
  }

  console.log(`Phase selected: "${phaseText}"`);
  translating.value = true; // Set loading immediately

  try {
    await ElMessageBox.confirm(
      `是否处理新单词: "${phaseText}"?`,
      '提示',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'info',
        closeOnClickModal: false,
        closeOnPressEscape: false
      }
    );

    // User clicked Confirm
    console.log("User confirmed translation.");
    connection.invoke("Trans", readContent.value.bookId, phaseText)
      .catch((err) => {
        console.error("Error invoking Trans:", err);
        ElMessage.error(`翻译请求失败: ${err.message}`);
      });

    showTransDialog.value = true;

  } catch(e: any) {
    // User clicked Cancel or an error occurred in the confirm box
    console.log("User cancelled translation or confirm failed:", e);
    ElMessage.info('已取消');
    showTransDialog.value = false; // Ensure dialog is closed
  } finally {
    translating.value = false; // Hide loading in all cases
  }
};
// Replace handlePhaseSelect with handlePhaseSelectCleaned in the template if you prefer the simpler logic
// For now, sticking to the original template structure, so keep the original handlePhaseSelect implementation
// but understand its behavior. The original implementation's isFirstTime logic is a bit strange.
// Let's refine the original isFirstTime logic slightly to make more sense if the goal is
// to only allow one confirmation process at a time.

const isProcessingPhaseSelect = ref(false); // Use a flag to prevent multiple confirmations

const handlePhaseSelectRefined = async (phaseText: string) => {
  if (isProcessingPhaseSelect.value) {
    console.log("Phase selection processing already in progress. Ignoring.");
    return;
  }

  isProcessingPhaseSelect.value = true; // Set flag at the start
  console.log(`Phase selected: "${phaseText}". Starting confirmation flow.`);

  // Combine translating and confirmation flow into one process flag
  translating.value = true; // Show loading during confirmation and translation request

  try {
    await ElMessageBox.confirm(
      `是否处理新单词: "${phaseText}"?`,
      '提示',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'info',
        closeOnClickModal: false,
        closeOnPressEscape: false
      }
    );

    // User clicked Confirm
    console.log("User confirmed translation. Invoking 'Trans'.");
    connection.invoke("Trans", readContent.value.bookId, phaseText)
      .catch((err) => {
        console.error("Error invoking Trans:", err);
        ElMessage.error(`翻译请求失败: ${err.message}`);
      });

    showTransDialog.value = true;

  } catch(e: any) {
    // User clicked Cancel or an error occurred in the confirm box
    console.log("User cancelled translation or confirm failed:", e);
    ElMessage.info('已取消');
    showTransDialog.value = false; // Ensure dialog is closed
  } finally {
    translating.value = false; // Hide loading
    isProcessingPhaseSelect.value = false; // Reset flag
  }
};
// Replace handlePhaseSelect with handlePhaseSelectRefined in the template.

</script>

<style scoped>
/* Your existing styles */
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