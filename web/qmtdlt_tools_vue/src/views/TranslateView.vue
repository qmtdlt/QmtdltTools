<template>
    <el-dialog v-model="showTransDialog" title="翻译结果" width="85%">
        <div v-loading="translating">
            <el-row>
                <h1>{{ transResult.wordText }}</h1>
                <el-button @click="playTransVoice(transResult.wordPronunciation)" type="primary" plain circle>
                    <el-icon>
                        <Headset />
                    </el-icon>
                </el-button>
            </el-row>
            <el-row>
                <h2>Explanation:</h2>
                <el-button @click="playTransVoice(transResult.pronunciation)" type="primary" plain circle>
                    <el-icon>
                        <Headset />
                    </el-icon>
                </el-button>
                <el-button @click="stopPlayBase64Audio" type="primary" plain circle>
                    <el-icon>
                        <VideoPause />
                    </el-icon>
                </el-button>
            </el-row>
            <el-row>
                <h3>{{ transResult.aiExplanation }}</h3>
            </el-row>
            <el-row>
                <h2>Translation:</h2>
            </el-row>
            <el-row>
                <h3>{{ transResult.aiTranslation }}</h3>
            </el-row>
        </div>
    </el-dialog>
    <el-dialog v-model="showSelectDialog" width="480px" :show-close="false" center
        style="padding: 0px; padding-bottom: 20px;padding-left: 20px;padding-right: 20px;">
        <div style="text-align:left;">
            <el-row justify="end" style="margin-bottom:10px;">
                <div style="display: flex; gap: 0px; margin-right: 20px;">
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
                    <el-button @click="cancelSelect" type="danger" plain circle>
                        <el-icon size="large">
                            <Close />
                        </el-icon>
                    </el-button>
                </div>
            </el-row>
            <h1 style="margin: 0px 0px 10px 0px;">{{ selectDialogText }}</h1>
        </div>
    </el-dialog>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '../utils/audioplay';
import request from '@/utils/request';
import { Headset, VideoPause, DocumentCopy, Close, ArrowLeft, ArrowRight } from '@element-plus/icons-vue'
import IconTranslate from '@/components/icons/IconTranslate.vue'
const transSource = ref(''); // Store the selected text for translation
const showTransDialog = ref(false) // 控制翻译弹窗显示
const translating = ref(false); // 拖拽到右侧区域的文本
const transResult = ref<VocabularyRecord>({} as VocabularyRecord); // Store translation result pronunciation is base64 string
const showSelectDialog = ref(false)
const selectDialogText = ref('')

const copyPhaseText = async () => {
    await navigator.clipboard.writeText(selectDialogText.value)
    ElMessage.success('已复制到剪贴板')
    showSelectDialog.value = false
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
    if (translating.value) {
        console.log("Translation already in progress. Ignoring phase select.");
        return; // Prevent multiple translation requests
    }
    try {
        translating.value = true; // Set loading immediately
        showTransDialog.value = true; // Show dialog if it was open
        let res = await request.get<VocabularyRecord>(
            '/api/Vocabulary/Trans',
            {
                params: { word: phaseText },
            }
        );
        transSource.value = phaseText;
        translating.value = false;
        transResult.value = res;
        startPlayBase64Audio(transResult.value.pronunciation, () => {
            console.log("Translation audio playback finished.");
        });
    } catch (e: any) {
        console.log("User cancelled translation or confirm failed:", e);
        ElMessage.info('已取消');
        translating.value = false;
        showTransDialog.value = false;
    }
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
defineExpose({
    handlePhaseSelect
});
</script>