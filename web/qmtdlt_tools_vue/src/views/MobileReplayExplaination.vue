<template>
    <div class="replay_container">
        <div class="replay_content">
            {{ curRef?.explanation }}
            
        </div>
        <div class="buttons_gp">
            <el-button type="primary" size="large" class="action-btn" @click="autoPlay">自动播放</el-button>
            <el-button type="primary" size="large" class="action-btn" @click="stopAutoPlay">停止循环</el-button>
            <el-button type="default" size="large" circle class="sound-btn" v-if="curRef?.voiceBuffer"
                @click="startPlayBase64Audio(curRef?.voiceBuffer, () => { })">
                <el-icon>
                    <Headset />
                </el-icon>
            </el-button>
        </div>
    </div>
</template>


<script setup lang="ts">
import { ref, onMounted } from 'vue'
import request from '@/utils/request'
import { ElMessageBox } from 'element-plus' // 确保导入 ElDialog, ElInput
// import icon
import { Headset } from '@element-plus/icons-vue' // 移除未使用的图标
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '../utils/audioplay';

const curRef = ref<ExplainRecord>({} as ExplainRecord);


interface ExplainRecord {
    id: string
    bookId: string
    phaseIndex?: number
    phase?: string
    explanation?: string
    voiceBuffer?: string
    createTime?: string,
}

// --- 结束新增方法 ---
onMounted(() => {
    getNext();
    autoPlay();
})

const isAutoPlay = ref(false);
const stopAutoPlay = () => {
    isAutoPlay.value = false;
    stopPlayBase64Audio();
    cleanupAudio();
}
const autoPlay = () => {
    isAutoPlay.value = true;
    startPlayBase64Audio(curRef.value?.voiceBuffer ?? '', async () => {
        await getNext();
        autoPlay();
    });
}
const getNext = async () => {
    stopPlayBase64Audio();
    const res = await request.post<ExplainRecord>('/api/ReadBook/GetNext', curRef.value);
    curRef.value = res
}
</script>

<style scoped>
.replay_container {
    width: 100%;
    height: calc(100vh - 190px);
    display: flex;
    flex-direction: column;
    overflow: hidden;
    position: relative; /* 重要，方便buttons_gp绝对定位 */
}
.replay_content {
    flex: 1 1 auto;
    padding: 10px;
    overflow-y: scroll;
}
.buttons_gp {
    position: absolute;
    left: 0;
    bottom: 0;
    width: 100%;
    height: 60px;
    padding: 10px;
    background: #fff; /* 避免内容滑动时下方透明 */
    box-shadow: 0 -2px 8px rgba(0,0,0,0.04);
    z-index: 2;
    display: flex;
    gap: 10px;
    align-items: center;
}

</style>