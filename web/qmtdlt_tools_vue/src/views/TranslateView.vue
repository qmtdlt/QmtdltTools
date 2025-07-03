<template>
    <el-dialog v-model="showSelectDialog" width="90%" :show-close="false" center class="select-dialog">
        <div style="text-align:left;">
            <el-row justify="end" style="margin-bottom:10px;">
                <div style="display: flex; gap: 8px; margin-right: 20px;">
                    <el-button @click="confirmTrans" type="success" plain circle>
                        <el-icon size="large">
                            <IconTranslate />
                        </el-icon>
                    </el-button>
                    <el-button @click="copyPhaseText" type="primary" plain circle>
                        <el-icon size="large">
                            <DocumentCopy />
                        </el-icon>
                    </el-button>
                    <el-button @click="excerptChapter" type="primary" plain circle>
                        <el-icon size="large">
                            <IconExcerptChapter />
                        </el-icon>
                    </el-button>
                    <el-button @click="cancelSelect" type="danger" plain circle>
                        <el-icon size="large">
                            <Close />
                        </el-icon>
                    </el-button>
                </div>
            </el-row>
            <h1 class="select-dialog-title">{{ selectDialogText }}</h1>
        </div>
    </el-dialog>
    <template v-for="dialog in transDialogs" :key="dialog.id">
        <el-dialog
            v-model="dialog.visible"
            title="翻译结果"
            width="500px"
            class="trans-dialog"
            :modal="false"
            :close-on-click-modal="false" 
            :append-to-body="true"
            :top="'auto'"
            :style="{ position: 'fixed', right: '20px', bottom: `${20 + 170 * (transDialogs.indexOf(dialog))}px`, width: '500px', minHeight: '150px', zIndex: 2000 + dialog.id }"
            @close="closeDialog(dialog.id)"
        >
            <div class="trans-content">
                <div class="word-translation-row">
                    <span >{{ dialog.result.wordText }}</span>
                    <el-button @click="playTransVoice(dialog.result.wordPronunciation)" class="sound-btn" circle>
                        <el-icon>
                            <Headset />
                        </el-icon>
                    </el-button>
                    <span>{{ dialog.result.aiTranslation }}</span>
                </div>
                <div class="explanation-row">
                    <span class="explanation-text">{{ dialog.result.aiExplanation }}</span>
                    <el-button @click="playTransVoice(dialog.result.pronunciation)" class="sound-btn" circle>
                        <el-icon>
                            <Headset />
                        </el-icon>
                    </el-button>
                    <el-button @click="stopPlayBase64Audio" class="sound-btn" circle>
                        <el-icon>
                            <VideoPause />
                        </el-icon>
                    </el-button>
                </div>
            </div>
        </el-dialog>
    </template>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, reactive } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '../utils/audioplay';
import request from '@/utils/request';
import { Headset, VideoPause, DocumentCopy, Close, ArrowLeft, ArrowRight } from '@element-plus/icons-vue'
import IconTranslate from '@/components/icons/IconTranslate.vue'
import IconExcerptChapter from '@/components/icons/IconExcerptChapter.vue'
import { el } from 'element-plus/es/locales.mjs';
const transSource = ref(''); // Store the selected text for translation
const showTransDialog = ref(false) // 控制翻译弹窗显示
const translating = ref(false); // 拖拽到右侧区域的文本
const transResult = ref<VocabularyRecord>({} as VocabularyRecord); // Store translation result pronunciation is base64 string
const showSelectDialog = ref(false)
const selectDialogText = ref('')

interface TransDialogItem {
    id: number
    word: string
    result: VocabularyRecord
    visible: boolean
}

const transDialogs = ref<TransDialogItem[]>([])
let dialogId = 0

const copyPhaseText = async () => {
    await navigator.clipboard.writeText(selectDialogText.value)
    ElMessage.success('已复制到剪贴板')
    showSelectDialog.value = false
}

const excerptChapter = async () => {
    const res = await ElMessageBox.confirm('是否要摘录这段话?', '提示', {
        confirmButtonText: '摘录',
        cancelButtonText: '取消',
    })
    if ("confirm" == res) {
        // Handle the action when the user confirms
        console.log('User confirmed:', res)
        await request.post('/api/EpubManage/ExcerptChapter/ExcerptChapter?content=' + selectDialogText.value);
        ElMessage.success('摘录成功')
    } else {
        // Handle the action when the user cancels
        console.log('User cancelled:', res)
    }
}

const confirmTrans = async () => {
    showSelectDialog.value = false
    await realHandlePhaseSelect(selectDialogText.value)
}

const cancelSelect = () => {
    showSelectDialog.value = false
}

interface VocabularyRecord {
    id: string
    wordText?: string
    aiExplanation?: string
    aiTranslation?: string
    sentenceYouMade?: string
    ifUsageCorrect?: boolean
    incorrectReason?: string
    pronunciation: string // 语音buffer
    translation: string // 翻译结果
    explanation: string // 解释
    wordPronunciation: string // 单词语音buffer
    createTime?: string,
}

const handlePhaseSelect = async (phaseText: string) => {
    selectDialogText.value = phaseText
    showSelectDialog.value = true

}
const realHandlePhaseSelect = async (phaseText: string) => {
    try {
        await enTransQueue(phaseText);
    } catch (e: any) {
        ElMessage.info('已取消');
    }
}
const transQueue = ref<string[]>([]);
const enTransQueue = async (phaseText:string) => {
    ElMessage.info('正在翻译，翻译结果即将呈现...');

    transQueue.value.push(phaseText);               // 加入队列

    while (transQueue.value.length > 0) {
        let tmpTransText = transQueue.value.shift() ?? "";

        // 如果 tmpTransText 为空，则跳过
        if (tmpTransText == "") {
            continue;
        }

        let res = await request.get<VocabularyRecord>(
            '/api/Vocabulary/Trans',
            { params: { word: tmpTransText } }
        );
        // 新建一个 dialog 项
        transDialogs.value.push({
            id: ++dialogId,
            word: tmpTransText,
            result: res,
            visible: true
        })
        await sleep(500);
    }

    
}

function sleep(ms: number) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

const playTransVoice = (voiceBuffer: string) => {
    if (voiceBuffer) {
        startPlayBase64Audio(voiceBuffer, () => {
            console.log("playTransVoice playback finished.");
        });
    } else {
        ElMessage.error("没有翻译语音!");
    }
}

// 关闭 dialog 时调用
const closeDialog = (id: number) => {
    const idx = transDialogs.value.findIndex(d => d.id === id)
    if (idx !== -1) {
        transDialogs.value.splice(idx, 1)
        stopPlayBase64Audio();
    }
}

defineExpose({
    handlePhaseSelect
});
</script>

<style scoped>
.trans-dialog .el-dialog__header {
  background: linear-gradient(90deg, #3a7bd5 0%, #3a6073 100%);
  color: #fff;
  border-radius: 12px 12px 0 0;
  padding-bottom: 12px;
}
.trans-content {
  padding: 18px 8px 8px 8px;
  background: #f8fafd;
  border-radius: 0 0 12px 12px;
}
.trans-row {
  margin-bottom: 12px;
  display: flex;
  align-items: center;
  gap: 12px;
}
.word-row {
  justify-content: center;
  margin-bottom: 18px;
}
.word-title {
  font-size: 2em;
  font-weight: bold;
  color: #222;
  margin-right: 10px;
}
.section-title {
  font-size: 1.2em;
  font-weight: 600;
  color: #409EFF;
  margin-right: 10px;
}
.sound-btn {
  width: 48px;
  height: 48px;
  background: #eaf6ff;
  border: none;
  color: #409EFF;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 2px 8px rgba(64,158,255,0.10);
  transition: background 0.2s, transform 0.2s;
}
.sound-btn:hover {
  background: #b3d8fd;
  transform: scale(1.08);
}
.explanation-text, .translation-text {
  font-size: 1.15em;
  color: #444;
  background: #fff;
  border-radius: 8px;
  padding: 14px 18px;
  margin: 0;
  box-shadow: 0 2px 8px rgba(64,158,255,0.04);
  word-break: break-word;
  line-height: 1.7;
}
.translation-text {
  color: #67C23A;
  font-weight: bold;
  font-size: 1.25em;
}
.select-dialog .el-dialog__body {
  padding: 18px 8px 8px 8px;
}
.select-dialog-title {
  font-size: 1.3em;
  color: #222;
  font-weight: bold;
  margin: 0 0 10px 0;
  word-break: break-word;
}
.word-translation-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 10px;
  padding: 0 10px;
}
.word-title {
  font-size: 1.6em;
  font-weight: bold;
  color: #222;
  margin-right: 10px;
  flex: 1;
  text-align: left;
}
.translation-text {
  color: #67C23A;
  font-weight: bold;
  font-size: 1.2em;
  margin-left: 10px;
  flex: 1;
  text-align: right;
}
.explanation-row {
  display: flex;
  align-items: center;
  padding: 0 10px;
}
.explanation-label {
  font-weight: 600;
  color: #409EFF;
  margin-right: 8px;
}
.explanation-text {
  font-size: 1.1em;
  color: #444;
  background: #fff;
  border-radius: 8px;
  padding: 8px 12px;
  margin: 0 10px 0 0;
  box-shadow: 0 2px 8px rgba(64,158,255,0.04);
  word-break: break-word;
  line-height: 1.7;
  flex: 1;
}
</style>