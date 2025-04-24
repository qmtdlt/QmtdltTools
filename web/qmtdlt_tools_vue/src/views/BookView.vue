<template>
  <el-row>
    <el-col :span="12">
      <MobileBookView @readContentChange="handleReadContentChange"/>
    </el-col>
    <el-col :span="12">
      <!--右侧区域-->
      <el-card style="height: 50vh;">
        <div>
          <el-row>
            <el-button @click="listenWrite" type="success"><el-icon><Headset /></el-icon> Speak Highlight</el-button>
            <el-button @click="promptOneWord" type="warning" ><el-icon><Lightning/></el-icon> Prompt</el-button>
            <el-button @click="showOrHidReader" type="info">
              <el-icon v-if="showLeft"><View /></el-icon>
              <el-icon v-else><Hide /></el-icon>
              {{ showLeft ? '隐藏原文' : '显示原文' }}
            </el-button>
          </el-row>
          <div>
            <ListenWrite :target-text="speaking_buffer" @completed="handleListenWriteComplete" />
          </div>
        </div>
      </el-card>
    </el-col>
  </el-row>

</template>
<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import MobileBookView from './MobileBookView.vue'
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数
import { ElMessage } from 'element-plus';
import ListenWrite from './ListenWrite.vue'; // Keep this import
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '@/utils/audioplay.ts' // Keep this import
// import icon
import { Headset,Lightning,View,Hide,VideoPause} from '@element-plus/icons-vue'


const speaking_buffer = ref(''); // 音频数据

const showLeft = ref(true); // Control visibility of .divLeft


const listenWrite = () => {
  debugger
  stopPlayBase64Audio();
  startPlayBase64Audio(speaking_buffer.value, ()=>{
    console.log("播放完成");
  }); // 读取到的音频内容
}

const promptOneWord = () => {

}

const showOrHidReader = () => {
  showLeft.value = !showLeft.value;
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
    listenWrite();
    e.preventDefault();
  }
}

const handleListenWriteComplete = async () => {
  ElMessage.success("听写完成!");
}

const handleReadContentChange = (data:string)=>{
  debugger
  speaking_buffer.value = data;
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
.divLeft,
.divRight {
  width: 100%;
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
}



.position-row {
  margin-bottom: 12px;
  justify-content: flex-end;
}

.dropped-row {
  margin-top: 10px;
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

@media (max-width: 900px) {
  .main-row {
    padding: 0;
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