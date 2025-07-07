// src/components/RealtimeCaption.vue

<template>
  <div class="video-container">
    <video ref="videoPlayer" class="plyr-video" playsinline controls></video>
    <p class="caption">{{ captionText }}</p>
  </div>
</template>

<script setup lang="ts">
import 'plyr/dist/plyr.css';
import { ref, onMounted, onBeforeUnmount } from 'vue';
import Plyr from 'plyr';

const props = defineProps<{
  videoUrl: string;
  // 不再需要 Azure 相關的 props
  // azureKey: string;
  // azureRegion: string;
  language?: string;
}>();

const captionText = ref('');
const videoPlayer = ref<HTMLVideoElement | null>(null);
let plyrInstance: Plyr | null = null;
let socket: WebSocket | null = null;

// 設定與後端 WebSocket 的連線
const setupWebSocket = () => {
  // 確保地址和埠與您的後端伺服器匹配
  socket = new WebSocket('ws://localhost:3000');

  socket.onopen = () => {
    console.log('成功連線到後端 WebSocket 伺服器');
  };

  socket.onmessage = (event) => {
    try {
      const data = JSON.parse(event.data);
      // 接收後端傳來的字幕並更新
      if (data.type === 'recognizing' || data.type === 'recognized') {
        captionText.value = data.text;
      }
      if (data.type === 'error') {
        console.error('後端錯誤:', data.text);
        captionText.value = `錯誤: ${data.text}`;
      }
    } catch(e) {
      console.error('無法解析來自後端的訊息:', event.data);
    }
  };

  socket.onclose = () => {
    console.log('與後端的 WebSocket 連線已關閉');
  };

  socket.onerror = (error) => {
    console.error('WebSocket 發生錯誤:', error);
  };
};

onMounted(() => {
  // 1. 設定 WebSocket
  setupWebSocket();

  // 2. 初始化 Plyr 播放器
  if (videoPlayer.value) {
    plyrInstance = new Plyr(videoPlayer.value, {
      sources: [
        {
          src: props.videoUrl,
          provider: 'youtube',
        },
      ],
      // 關鍵：將播放器靜音，因為聲音是由後端處理的
      // 如果您只是想顯示字幕，不靜音也可以，但若有跟讀等功能則建議靜音
      muted: false, 
    });

    // 3. 監聽播放器事件，並通知後端
    plyrInstance.on('play', () => {
      if (socket && socket.readyState === WebSocket.OPEN) {
        console.log('通知後端開始處理...');
        // 發送訊息給後端，觸發處理流程
        socket.send(JSON.stringify({
          type: 'start',
          youtubeUrl: props.videoUrl
        }));
      }
    });

    plyrInstance.on('pause', () => {
       // 您可以選擇在暫停時也通知後端，以節省 Azure 資源
       if (socket && socket.readyState === WebSocket.OPEN) {
          socket.send(JSON.stringify({ type: 'stop' }));
       }
    });
  }
});

onBeforeUnmount(() => {
  // 元件銷毀時，關閉 WebSocket 連線並銷毀播放器
  if (socket) {
    socket.close();
  }
  if (plyrInstance) {
    plyrInstance.destroy();
  }
});
</script>

<style scoped>
.video-container {
  max-width: 800px;
  margin: auto;
}
.caption {
  margin-top: 10px;
  font-size: 1.2em;
  color: #333;
  user-select: text; /* 方便使用者複製字幕 */
}
</style>