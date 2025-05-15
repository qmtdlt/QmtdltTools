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
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus';
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '../utils/audioplay';
import request from '@/utils/request';
import { Headset, VideoPause, CaretRight, Close, ArrowLeft, ArrowRight } from '@element-plus/icons-vue'

const transSource = ref(''); // Store the selected text for translation
const showTransDialog = ref(false) // 控制翻译弹窗显示
const translating = ref(false); // 拖拽到右侧区域的文本
const transResult = ref<VocabularyRecord>({} as VocabularyRecord); // Store translation result pronunciation is base64 string


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
    if (translating.value) {
        console.log("Translation already in progress. Ignoring phase select.");
        return; // Prevent multiple translation requests
    }

    try {

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
        translating.value = true; // Set loading immediately
        showTransDialog.value = true; // Hide dialog if it was open
        let res = await request.get<VocabularyRecord>(
            '/api/Vocabulary/Trans',
            {
                params: {
                    word: phaseText
                },
            }
        );
        // User clicked Confirm
        console.log("trans result.", res);

        transSource.value = phaseText; // Store the selected text for translation

        translating.value = false;
        transResult.value = res; // Store the translation result
        console.log("Received translation. Playing audio.");
        // Play translation voice - onEnded is not needed here
        startPlayBase64Audio(transResult.value.pronunciation, () => {
            console.log("Translation audio playback finished.");
        });

    } catch (e: any) {
        // User clicked Cancel or an error occurred in the confirm box
        console.log("User cancelled translation or confirm failed:", e);
        ElMessage.info('已取消');
        translating.value = false; // Hide loading
        showTransDialog.value = false; // Ensure dialog is closed if it opened prematurely
        // isFirstTime.value = true; // Reset for the next selection attempt
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