<template>
    <div class="video-container">
      <div ref="videoPlayer"></div>
      <p class="caption" v-text="captionText"></p>
    </div>
  </template>
  
  <script setup lang="ts">
  import { ref, onMounted, onBeforeUnmount } from 'vue';
  import Plyr from 'plyr';
  import * as SpeechSDK from 'microsoft-cognitiveservices-speech-sdk';
  import { setupAudioCapture, stopAudioCapture } from '@/components/helpers/audioHelper';
  
  // 定义props
  const props = defineProps<{
    videoUrl: string;
    azureKey: string;
    azureRegion: string;
    language?: string;
  }>();
  
  const captionText = ref('');
  const videoPlayer = ref<HTMLElement | null>(null);
  let plyrInstance: Plyr | null = null;
  
  let recognizer: SpeechSDK.SpeechRecognizer | null = null;
  
  onMounted(async () => {
    // 初始化plyr视频播放器，支持YouTube和Bilibili嵌入式播放
    plyrInstance = new Plyr(videoPlayer.value!, {
      type: 'video',
      sources: [
        {
          src: props.videoUrl,
          provider: props.videoUrl.includes('youtube') ? 'youtube' : 'html5',
        },
      ],
    });
  
    // 等待视频播放器就绪
    plyrInstance.on('ready', async () => {
      const mediaElement = videoPlayer.value!.querySelector('video')!;
      
      // 初始化Azure语音识别
      const speechConfig = SpeechSDK.SpeechConfig.fromSubscription(props.azureKey, props.azureRegion);
      speechConfig.speechRecognitionLanguage = props.language || 'en-US';
  
      const audioFormat = SpeechSDK.AudioStreamFormat.getWaveFormatPCM(44100, 16, 1);
      const pushStream = SpeechSDK.AudioInputStream.createPushStream(audioFormat);
      const audioConfig = SpeechSDK.AudioConfig.fromStreamInput(pushStream);
      recognizer = new SpeechSDK.SpeechRecognizer(speechConfig, audioConfig);
  
      recognizer.recognizing = (_, e) => {
        captionText.value = e.result.text;
      };
      recognizer.recognized = (_, e) => {
        captionText.value = e.result.text;
      };
      recognizer.startContinuousRecognitionAsync();
  
      // 开始捕获音频数据
      await setupAudioCapture(mediaElement, pushStream);
    });
  });
  
  onBeforeUnmount(() => {
    if (recognizer) {
      recognizer.stopContinuousRecognitionAsync();
      recognizer.close();
    }
    stopAudioCapture();
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
    user-select: text; /* 字幕可选择 */
  }
  </style>
  