<template>
    <div class="shadowing-view">
        <el-row :gutter="32" justify="center" align="top">
            <!-- 左侧：录音操作区 -->
            <el-col :xs="24" :sm="6" :md="6" :lg="6">
                <el-card shadow="hover" class="operate-card">
                    <template #header>
                        <span>跟读操作</span>
                    </template>
                    <div>
                        <h3 style="margin-bottom: 10px;">请跟读以下文本：</h3>
                        <p class="target-text">{{ targetText }}</p>
                        <el-row :gutter="10" justify="center" style="margin-top: 20px;">
                            <el-col :xs="12" :sm="24" :md="24">
                                <el-button type="primary" @click="toggleRecording" :disabled="isProcessing" style="width: 100%;">
                                    {{ isRecording ? '停止录音' : '开始录音' }}
                                </el-button>
                            </el-col>
                            <el-col :xs="12" :sm="24" :md="24" style="margin-top: 10px;">
                                <el-button type="success" @click="submitRecording"
                                    :disabled="!recordedAudioUrl || isRecording || isProcessing" style="width: 100%;">
                                    提交录音
                                </el-button>
                            </el-col>
                        </el-row>
                        <div style="margin-top: 24px;">
                            <audio v-if="recordedAudioUrl" :src="recordedAudioUrl" controls style="width: 100%;"></audio>
                        </div>
                        <div style="margin-top: 16px;">
                            <el-alert v-if="isRecording" title="正在录音..." type="info" show-icon />
                            <el-alert v-if="statusMessage" :title="statusMessage" type="warning" show-icon />
                        </div>
                    </div>
                </el-card>
            </el-col>

            <!-- 右侧：评估结果区 -->
            <el-col :xs="24" :sm="18" :md="18" :lg="18">
                <el-card v-if="isProcessing" shadow="hover" class="result-card">
                    <template #header>
                        <div class="card-header">
                            <span>跟读分析结果</span>
                        </div>
                    </template>
                    <div style="text-align:center;padding:60px 0;">
                        <el-icon style="font-size:32px;color:#409EFF;"><i class="el-icon-loading"></i></el-icon>
                        <div style="margin-top:16px;color:#888;">分析中，请稍候...</div>
                    </div>
                </el-card>
                <el-card v-else-if="shadowingResult" shadow="hover" class="result-card">
                    <template #header>
                        <div class="card-header">
                            <span>跟读分析结果</span>
                        </div>
                    </template>
                    <div>
                        <el-row :gutter="16" class="score-row">
                            <el-col :span="12"><span class="score-label">总准确度：</span><span class="score-value">{{ shadowingResult.accuracyScore }}</span></el-col>
                            <el-col :span="12"><span class="score-label">发音得分：</span><span class="score-value">{{ shadowingResult.pronunciationScore }}</span></el-col>
                            <el-col :span="12"><span class="score-label">完整度得分：</span><span class="score-value">{{ shadowingResult.completenessScore }}</span></el-col>
                            <el-col :span="12"><span class="score-label">流利度得分：</span><span class="score-value">{{ shadowingResult.fluencyScore }}</span></el-col>
                        </el-row>
                        <el-divider />
                        <h4 style="margin-bottom: 12px;">逐词分析</h4>
                        <div class="word-card-flow">
                            <el-card
                                v-for="(word, idx) in shadowingResult.words"
                                :key="idx"
                                class="word-mini-card"
                                :body-style="{ padding: '12px' }"
                                shadow="never"
                            >
                                <div style="display: flex; align-items: center; justify-content: space-between;">
                                    <el-tag :type="getWordTagType(word.errorType)" effect="plain" size="small">
                                        {{ word.errorType }}
                                    </el-tag>
                                    <span class="word-score">分数: {{ word.accuracyScore }}</span>
                                </div>
                                <div class="word-main">
                                    <strong>{{ word.word }}</strong>
                                </div>
                                <el-collapse>
                                    <el-collapse-item title="音节详情" v-if="word.syllables && word.syllables.length">
                                        <ul>
                                            <li v-for="(syll, sidx) in word.syllables" :key="sidx">
                                                {{ syll.syllable }} <span v-if="syll.grapheme">({{ syll.grapheme }})</span> - 分数: {{ syll.accuracyScore }}
                                            </li>
                                        </ul>
                                    </el-collapse-item>
                                    <el-collapse-item title="音素详情" v-if="word.phonemes && word.phonemes.length">
                                        <ul>
                                            <li v-for="(ph, pidx) in word.phonemes" :key="pidx">
                                                {{ ph.phoneme }} - 分数: {{ ph.accuracyScore }}
                                            </li>
                                        </ul>
                                    </el-collapse-item>
                                </el-collapse>
                            </el-card>
                        </div>
                    </div>
                </el-card>
                <el-empty v-else description="暂无评估结果" style="margin-top: 40px;" />
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

// 结果数据
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
        // 清空分析结果并显示加载中
        shadowingResult.value = null;
        isProcessing.value = true;
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
            
            emit('completed', recordedAudio.value);
        } catch (err) {
            console.error("Error during shadowing:", err);
        } finally {
            isProcessing.value = false;
        }
    } else {
        ElMessage.warning('没有可提交的录音')
    }
}

// 新增：根据错误类型返回不同tag颜色
const getWordTagType = (type: string) => {
    switch (type?.toLowerCase()) {
        case 'none': return 'success'
        case 'mispronunciation': return 'warning'
        case 'omission': return 'danger'
        case 'insertion': return 'info'
        default: return ''
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
    padding: 32px 0;
    min-height: 100%;
}
.operate-card {
    min-height: 420px;
    margin-bottom: 24px;
}
.result-card {
    min-height: 420px;
    margin-bottom: 24px;
}
.target-text {
    font-size: 1.15em;
    color: #333;
    background: #f7fafd;
    border-radius: 8px;
    padding: 12px 14px;
    margin-bottom: 0;
    margin-top: 8px;
    display: block;
}
.score-row {
    margin-bottom: 10px;
}
.score-label {
    color: #888;
    font-weight: bold;
}
.score-value {
    color: #409EFF;
    font-weight: bold;
    margin-left: 6px;
}
.word-card-flow {
    display: flex;
    flex-wrap: wrap;
    gap: 18px;
    margin-top: 10px;
}
.word-mini-card {
    max-width: 300px;
    min-width: 220px;
    flex: 1 1 220px;
    margin-bottom: 0;
    border: 1px solid #e5e6eb;
    border-radius: 8px;
    background: #fff;
    box-shadow: 0 1px 4px 0 rgba(0,0,0,0.03);
    transition: box-shadow 0.2s;
}
.word-mini-card:hover {
    box-shadow: 0 4px 16px 0 rgba(64,158,255,0.10);
}
.word-main {
    font-size: 1.12em;
    margin: 8px 0 4px 0;
    color: #222;
    text-align: left;
}
.word-score {
    color: #67c23a;
    font-weight: bold;
    margin-left: 8px;
}
</style>