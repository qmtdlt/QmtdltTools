<template>
    <div class="shadowing-view">
        <el-row justify="center">
            <el-col :xs="20" :sm="16" :md="12">
                <h2>请跟读以下文本：</h2>
                <p>{{ targetText }}</p>
            </el-col>
        </el-row>
        <el-row :gutter="10" justify="center" style="margin-top: 20px;">
            <el-col :xs="8" :sm="6" :md="4">
                <el-button type="primary" @click="toggleRecording" :disabled="isProcessing" style="width: 100%;">
                    {{ isRecording ? '停止录音' : '开始录音' }}
                </el-button>
            </el-col>
            <el-col :xs="8" :sm="6" :md="4">
                <el-button type="success" @click="submitRecording"
                    :disabled="!recordedAudioUrl || isRecording || isProcessing" style="width: 100%;">
                    提交录音
                </el-button>
            </el-col>
        </el-row>

        <el-row justify="center" style="margin-top: 20px;" v-if="recordedAudioUrl">
            <el-col :xs="20" :sm="16" :md="12">
                <audio :src="recordedAudioUrl" controls style="width: 100%;"></audio>
            </el-col>
        </el-row>

        <el-row justify="center" style="margin-top: 10px;">
            <el-col :xs="20" :sm="16" :md="12">
                <p v-if="isRecording">正在录音...</p>
                <p v-if="statusMessage">{{ statusMessage }}</p>
            </el-col>
        </el-row>

        <!-- 结果展示区域 -->
        <el-row justify="center" style="margin-top: 30px;" v-if="shadowingResult">
            <el-col :xs="24" :sm="20" :md="16">
                <el-card class="box-card">
                    <template #header>
                        <div class="card-header">
                            <span>跟读分析结果</span>
                        </div>
                    </template>
                    <div>
                        <p><strong>总准确度:</strong> {{ shadowingResult.accuracyScore }}</p>
                        <p><strong>发音得分:</strong> {{ shadowingResult.pronunciationScore }}</p>
                        <p><strong>完整度得分:</strong> {{ shadowingResult.completenessScore }}</p>
                        <p><strong>流利度得分:</strong> {{ shadowingResult.fluencyScore }}</p>
                    </div>
                    <el-divider />
                    <h4>逐词分析:</h4>
                    <div v-for="(word, idx) in shadowingResult.words" :key="idx" class="word-analysis">
                        <p>
                            <strong>单词:</strong> {{ word.word }} -
                            <strong>准确度:</strong> {{ word.accuracyScore }} -
                            <strong>错误类型:</strong> {{ word.errorType }}
                        </p>
                    </div>
                </el-card>
            </el-col>
        </el-row>
    </div>
</template>

<script setup lang="ts">
import { ref, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import request from '@/utils/request'

const emit = defineEmits<{
    (e: 'completed', audioBlob: Blob): void
}>()

const isRecording = ref(false)
const isProcessing = ref(false)
const mediaRecorder = ref<MediaRecorder | null>(null)
const audioChunks = ref<Blob[]>([])
const recordedAudio = ref<Blob | null>(null)
const recordedAudioUrl = ref<string | null>(null)
const statusMessage = ref<string>('')
const props = defineProps<{
    targetText: string;
}>()

let stream: MediaStream | null = null;

// 新增：结果数据
const shadowingResult = ref<any>(null)

const toBase64 = (blob: Blob): Promise<string> => {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => {
            const base64 = (reader.result as string).split(',')[1]; // 去掉 data:audio/wav;base64,...
            resolve(base64);
        };
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
};


const startRecording = async () => {
    if (isRecording.value) { // If already recording, stop current and restart
        stopRecordingLogic();
        // It might be better to await stopRecordingLogic or handle the async nature
        // but for now, this will attempt to stop then proceed.
        // A more robust solution might involve a state machine or promises.
    }

    statusMessage.value = '正在请求麦克风权限...'
    isProcessing.value = true; // Indicate processing has started
    recordedAudio.value = null; // Clear previous recording
    if (recordedAudioUrl.value) {
        URL.revokeObjectURL(recordedAudioUrl.value); // Clean up old URL
        recordedAudioUrl.value = null;
    }
    audioChunks.value = [];

    try {
        stream = await navigator.mediaDevices.getUserMedia({ audio: true })
        statusMessage.value = '麦克风已连接，准备录音...'
        mediaRecorder.value = new MediaRecorder(stream)

        mediaRecorder.value.ondataavailable = (event) => {
            if (event.data.size > 0) {
                audioChunks.value.push(event.data)
            }
        }

        mediaRecorder.value.onstop = () => {
            if (audioChunks.value.length > 0) {
                recordedAudio.value = new Blob(audioChunks.value, { type: 'audio/wav' }) // Or 'audio/webm' etc.
                recordedAudioUrl.value = URL.createObjectURL(recordedAudio.value)
                statusMessage.value = '录音已停止。可以播放或提交。'
            } else {
                statusMessage.value = '没有录制到音频数据。';
            }
            isRecording.value = false
            isProcessing.value = false; // Processing finished (stop)
            // Stop all tracks in the stream to release the microphone
            if (stream) {
                stream.getTracks().forEach(track => track.stop());
                stream = null;
            }
        }

        mediaRecorder.value.onerror = (event) => {
            console.error('MediaRecorder error:', event);
            ElMessage.error('录音发生错误');
            statusMessage.value = `录音错误: ${(event as any).error?.name || 'Unknown error'}`;
            isRecording.value = false;
            isProcessing.value = false; // Processing finished (error)
            if (stream) {
                stream.getTracks().forEach(track => track.stop());
                stream = null;
            }
        };

        mediaRecorder.value.start()
        isRecording.value = true
        statusMessage.value = '录音中...'
        isProcessing.value = false; // <<< FIX: Recording started, no longer processing the start action
    } catch (err) {
        console.error('无法获取麦克风:', err)
        ElMessage.error('无法获取麦克风权限，请检查设置。')
        statusMessage.value = `无法获取麦克风: ${(err as Error).message}`;
        isProcessing.value = false; // Processing finished (error during start)
    }
    // The finally block for isProcessing is removed as it's handled in try/catch and onstop/onerror
}

const stopRecordingLogic = () => {
    if (mediaRecorder.value && mediaRecorder.value.state === 'recording') {
        statusMessage.value = '正在停止录音...'
        isProcessing.value = true; // Indicate processing has started for stopping
        mediaRecorder.value.stop() // This will trigger 'onstop' or 'onerror' which will set isProcessing to false
    } else {
        // If not recording or recorder not active, just ensure state is correct
        isRecording.value = false;
        isProcessing.value = false; // Not processing if not recording
        if (stream) { // Ensure stream is released if stop is called unexpectedly
            stream.getTracks().forEach(track => track.stop());
            stream = null;
        }
    }
}

const toggleRecording = () => {
    if (isRecording.value) {
        stopRecordingLogic()
    } else {
        startRecording()
    }
}

const submitRecording = async () => {
    if (recordedAudio.value) {
        emit('completed', recordedAudio.value);
        ElMessage.success('录音已提交')

        try {
            const base64Audio = await toBase64(recordedAudio.value);
            console.log("Base64 Audio:", typeof (base64Audio), base64Audio);
            debugger

            const formData = new FormData();
            formData.append('audioFile', recordedAudio.value, 'recording.wav');

            let res = await request.post<string>('/api/Shadowing/CheckShadowing?reftext=' + props.targetText, formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });

            console.log("Shadowing result:", res);

            // 新增：保存结果
            if (typeof res === 'string') {
                shadowingResult.value = JSON.parse(res);
            } else {
                shadowingResult.value = res;
            }
        } catch (err) {
            console.error("Error during shadowing:", err);
        }

    } else {
        ElMessage.warning('没有可提交的录音')
    }
}

onUnmounted(() => {
    if (mediaRecorder.value && mediaRecorder.value.state === 'recording') {
        mediaRecorder.value.stop()
    }
    if (stream) {
        stream.getTracks().forEach(track => track.stop());
    }
    if (recordedAudioUrl.value) {
        URL.revokeObjectURL(recordedAudioUrl.value) // Clean up
    }
})

</script>

<style scoped>
.shadowing-view {
    padding: 20px;
    text-align: center;
}
.box-card {
    margin-top: 20px;
    text-align: left;
}
.card-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}
.word-analysis {
    margin-bottom: 15px;
    padding: 10px;
    border: 1px solid #eee;
    border-radius: 4px;
}
</style>