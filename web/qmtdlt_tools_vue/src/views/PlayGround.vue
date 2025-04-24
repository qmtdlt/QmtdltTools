<template>
  <el-row>
    <el-col :span="12">
      <BookReader @readContentChange="handleReadContentChange"/>
    </el-col>
    <el-col :span="12">
      <!--右侧区域-->
      <el-card style="height: 50vh;">
        <div>
          <el-row>
            <el-button @click="listenWrite" type="success"><el-icon><Headset /></el-icon> Speak Highlight</el-button>
            <el-button @click="promptOneWord" type="warning" ><IconStop></IconStop>Prompt</el-button>
            <el-button @click="showOrHidReader" type="info">
              <el-icon v-if="showLeft"><View /></el-icon>
              <el-icon v-else><Hide /></el-icon>
              {{ showLeft ? '隐藏原文' : '显示原文' }}
            </el-button>
          </el-row>
          <div>
            <ListenWrite :target-text="listenwrite_text" @completed="handleListenWriteComplete" />
          </div>
        </div>
      </el-card>
    </el-col>
  </el-row>

</template>
<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount } from 'vue'
import BookReader from './BookReader.vue'
import { useRoute } from 'vue-router' // 导入 useRoute 获取路由参数
import { ElMessage } from 'element-plus';
import ListenWrite from './ListenWrite.vue'; // Keep this import
import { startPlayBase64Audio, stopPlayBase64Audio, cleanupAudio } from '@/utils/audioplay.ts' // Keep this import
// import icon
import { Headset,Lightning,View,Hide,VideoPause} from '@element-plus/icons-vue'
import IconStop from '@/components/icons/IconStop.vue';

const listenwrite_buffer = ref(''); // 音频数据
const listenwrite_text = ref(''); // 音频数据

const showLeft = ref(true); // Control visibility of .divLeft


const listenWrite = () => {
  stopPlayBase64Audio();
  startPlayBase64Audio(listenwrite_buffer.value, ()=>{
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

const handleReadContentChange = (data:string,text:string)=>{
  debugger
  listenwrite_buffer.value = data;
  listenwrite_text.value = text;
}
</script>

<style scoped>

</style>