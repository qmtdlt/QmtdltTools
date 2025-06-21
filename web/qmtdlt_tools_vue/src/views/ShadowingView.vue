<template>
    <div class="shadowing-view">
        <!-- 左侧：录音操作区 -->
        <div shadow="hover" class="left">
            <div class="card-header">
                <span>跟读录音</span>
            </div>
            <div>
                <h3 style="margin-bottom: 10px;">请跟读以下文本：</h3>
                <p class="target-text">{{ targetText }}</p>
                <div style="margin-top: 20px;">
                    <el-col :xs="12" :sm="24" :md="24">
                        <el-button type="primary" @click="toggleRecording" :disabled="isProcessing"
                            style="width: 100%;">
                            {{ isRecording ? '停止录音' : '开始录音' }}
                        </el-button>
                    </el-col>
                    <el-col :xs="12" :sm="24" :md="24" style="margin-top: 10px;">
                        <el-button type="success" @click="submitRecording"
                            :disabled="!recordedAudioUrl || isRecording || isProcessing" style="width: 100%;">
                            提交录音
                        </el-button>
                    </el-col>
                </div>
                <div style="margin-top: 24px;">
                    <audio v-if="recordedAudioUrl" :src="recordedAudioUrl" controls style="width: 100%;"></audio>
                </div>
                <div style="margin-top: 16px;">
                    <el-alert v-if="isRecording" title="正在录音..." type="info" show-icon />
                    <el-alert v-if="statusMessage" :title="statusMessage" type="warning" show-icon />
                </div>
            </div>
        </div>
        <!-- 右侧：评估结果区 -->
        <div class="right">
            <div v-if="isProcessing" shadow="hover" class="result-card">
                <div class="card-header">
                    <span>跟读分析结果</span>
                </div>
                <div style="text-align:center;padding:60px 0;">
                    <el-icon style="font-size:32px;color:#409EFF;"><i class="el-icon-loading"></i></el-icon>
                    <div style="margin-top:16px;color:#888;">分析中，请稍候...</div>
                </div>
            </div>
            <div v-else-if="shadowingResult" shadow="hover" class="result-card">
                <div class="card-header">
                    <span>跟读分析结果</span>
                </div>
                <div style="padding: 5px;">
                    <div style="display: flex; height: 180px;">
                        <div style="flex: 1;">
                            <div ref="mainChart" style="height: 160px; "></div>
                            <div style="text-align:center; margin-top: 8px; color:#888;flex: 2;">发音分数</div>
                        </div>
                        <div style="flex: 2;">
                            <div ref="detailChart" style="height: 170px;"></div>
                        </div>
                    </div>
                    <el-divider />
                    <h4 style="margin-bottom: 12px;">逐词分析</h4>
                    <div class="word-card-flow">
                        <el-card v-for="(word, idx) in shadowingResult.words" :key="idx" class="word-mini-card"
                            :body-style="{ padding: '12px' }" shadow="never">
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
                                            {{ syll.syllable }} <span v-if="syll.grapheme">({{ syll.grapheme }})</span>
                                            - 分数: {{
                                                syll.accuracyScore }}
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
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch, nextTick, onUnmounted } from 'vue'
import { ElMessage } from 'element-plus'
import request from '@/utils/request'
import Recorder from 'recorder-core' // 如果你用npm引入
import 'recorder-core/src/engine/wav.js'
import * as echarts from 'echarts';

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

interface PronunciationAssessmentResult {
    accuracyScore: number;
    pronunciationScore: number;
    completenessScore: number;
    fluencyScore: number;
    prosodyScore: number;
    words: Array<{
        word: string;
        accuracyScore: number;
        errorType: string;
        syllables?: Array<{
            syllable: string;
            accuracyScore: number;
            grapheme?: string;
        }>;
        phonemes?: Array<{
            phoneme: string;
            accuracyScore: number;
        }>;
    }>;
}

// const stream = ref<MediaStream| null>(null);

// 结果数据
const shadowingResult = ref<PronunciationAssessmentResult | null>(null);

const mainChart = ref<HTMLElement | null>(null);
const detailChart = ref<HTMLElement | null>(null);
let mainChartInstance: echarts.ECharts | null = null;
let detailChartInstance: echarts.ECharts | null = null;
// 渲染主环形仪表盘
function renderMainChart() {
    if (!mainChart.value) return;
    // 每次都销毁并重新初始化，避免多次渲染后图表失效
    if (mainChartInstance) {
        mainChartInstance.dispose();
        mainChartInstance = null;
    }
    mainChartInstance = echarts.init(mainChart.value);
    const option = {
        title: { show: false },
        series: [{
            type: 'gauge',
            startAngle: 90,
            endAngle: -269.9999,
            progress: { show: true, width: 18 },
            axisLine: { lineStyle: { width: 18 } },
            pointer: { show: false },
            axisTick: { show: false },
            splitLine: { show: false },
            axisLabel: { show: false },
            anchor: { show: false },
            detail: {
                valueAnimation: true,
                fontSize: 38,
                offsetCenter: [0, '35%'],
                color: '#409EFF',
                formatter: '{value}'
            },
            data: [{ value: shadowingResult.value?.pronunciationScore }]
        }]
    };
    mainChartInstance.setOption(option);
}

// 渲染条形图
function renderDetailChart() {
    if (!detailChart.value) return;
    // 每次都销毁并重新初始化，避免多次渲染后图表失效
    if (detailChartInstance) {
        detailChartInstance.dispose();
        detailChartInstance = null;
    }
    detailChartInstance = echarts.init(detailChart.value);

    const data = [
        { name: '发音准确度', value: shadowingResult.value?.accuracyScore },
        { name: '语音的流畅度', value: shadowingResult.value?.fluencyScore },
        { name: '完整性', value: shadowingResult.value?.completenessScore },
        { name: '韵律', value: shadowingResult.value?.prosodyScore }
    ];
    const option = {
        grid: { left: 90, right: 80, top: 14, bottom: 14 },
        xAxis: { type: 'value', min: 0, max: 100, show: false },
        yAxis: {
            type: 'category',
            data: data.map(d => d.name),
            axisLine: { show: false },
            axisTick: { show: false },
            axisLabel: { fontWeight: 'bold', color: '#888' }
        },
        series: [{
            type: 'bar',
            data: data.map(d => d.value),
            label: { show: true, position: 'right', formatter: '{c} / 100', color: '#222', fontWeight: 'bold' },
            barWidth: 18,
            itemStyle: {
                borderRadius: 8,
                color: (params: any) => {
                    if (params.value < 60) return '#E53E3E';
                    if (params.value < 80) return '#FFB100';
                    return '#67C23A';
                }
            }
        }]
    };
    detailChartInstance.setOption(option);
}

// 当评分数据变动时自动刷新图表
watch(shadowingResult, () => {
    nextTick(() => {
        renderMainChart();
        renderDetailChart();
    });
}, { deep: true });
onMounted(() => {
    renderMainChart();
    renderDetailChart();
});
let rec: any = null;
const startRecording = async () => {
    rec = Recorder({
        type: "wav",
        sampleRate: 16000,
        bitRate: 16,
        mono: true,
    });
    rec.open(function () {
        rec.start();
        isRecording.value = true;
        statusMessage.value = "录音中...";
    }, function (msg: string, isUserNotAllow: boolean) {
        ElMessage.error('无法获取麦克风权限，请检查设置。')
    });
};

const stopRecordingLogic = () => {
    if (rec && isRecording.value) {
        rec.stop(function (blob: Blob, duration: number) {
            recordedAudio.value = blob;
            recordedAudioUrl.value = URL.createObjectURL(blob);
            isRecording.value = false;
            statusMessage.value = '录音已停止。可以播放或提交。'
        }, function (msg: string) {
            ElMessage.error('录音停止失败: ' + msg);
        });
    }
};

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
        isProcessing.value = true;
        ElMessage.success('录音已提交')

        try {
            const formData = new FormData();
            formData.append('audioFile', recordedAudio.value, 'recording.wav');

            let res = await request.post<PronunciationAssessmentResult>('/api/Shadowing/CheckShadowing?reftext=' + props.targetText, formData, {
                headers: { 'Content-Type': 'multipart/form-data' }
            });


            shadowingResult.value = res;

            console.log("Shadowing result:", shadowingResult.value);
            // 关键：强制刷新图表
            nextTick(() => {
                renderMainChart();
                renderDetailChart();
            });
            // emit('completed', recordedAudio.value);
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
    // if (stream) {
    //     stream.value.getTracks().forEach((track: MediaStreamTrack) => track.stop());
    // }
    if (recordedAudioUrl.value) {
        URL.revokeObjectURL(recordedAudioUrl.value) // Clean up
    }
})
</script>

<style scoped>
.shadowing-view {
    padding: 5px;
    height: 100%;
    display: flex;
}

.left {
    flex: 1;
    width: 25%;
    height: 100%;
    background-color: #ffffff;
    border-radius: 8px;
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
    padding: 20px;
    overflow-y: auto;
}

.right {
    flex: 2;
    width: calc(75% - 20px);
    height: 100%;
    background-color: #ffffff;
    border-radius: 8px;
    box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
    padding: 20px;
    overflow-y: auto;
    margin: 0px 0 0 10px;
}


.result-card {
    width: 100%;
    height: 100%;
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
    overflow-y: scroll;
    height: calc(100% - 200px); /* Adjust height to fit within the card */
    padding: 10px 0;
}

.word-mini-card {
    max-width: 300px;
    min-width: 220px;
    flex: 1 1 220px;
    margin-bottom: 0;
    border: 1px solid #e5e6eb;
    border-radius: 8px;
    background: #fff;
    box-shadow: 0 1px 4px 0 rgba(0, 0, 0, 0.03);
    transition: box-shadow 0.2s;
}

.word-mini-card:hover {
    box-shadow: 0 4px 16px 0 rgba(64, 158, 255, 0.10);
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