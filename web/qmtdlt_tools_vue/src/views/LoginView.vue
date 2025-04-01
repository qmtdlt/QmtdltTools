<template>
  <div>
    <input v-model="text" placeholder="请输入要转换的文本" />
    <button :disabled="isLoading" @click="speakText">
      {{ isLoading ? '生成中...' : '生成语音' }}
    </button>
    <audio v-if="audioSrc" :src="audioSrc" controls></audio>
    <p v-if="error" style="color: red">{{ error }}</p>
  </div>
</template>

<script lang="ts" setup>
import { ref } from 'vue';
import request from '@/utils/request'; // 导入你的封装请求

const text = ref<string>('');
const audioSrc = ref<string>('');
const isLoading = ref<boolean>(false);
const error = ref<string | null>(null);

const speakText = async (): Promise<void> => {
  isLoading.value = true;
  error.value = null;
  audioSrc.value = '';

  // 构造 SSML
  const ssml: string = `<speak version="1.0" xmlns="http://www.w3.org/2001/10/synthesis" xml:lang="zh-CN">
                      <voice name="zh-CN-YunxiNeural">
                        ${text.value}
                      </voice>
                    </speak>`;

  try {
    // 调用微软 TTS API
    const response = await request.post(
      'https://eastus.tts.speech.microsoft.com/cognitiveservices/v1',
      ssml,
      {
        headers: {
          'Ocp-Apim-Subscription-Key': 'my speech key', // 替换为你的密钥
          'Content-Type': 'application/ssml+xml',
          'X-Microsoft-OutputFormat': 'riff-24khz-16bit-mono-pcm',
        },
        responseType: 'blob', // 接收二进制音频流
      }
    );

    // 将音频数据转换为 Blob 并生成 URL
    const audioBlob = new Blob([response], { type: 'audio/wav' });
    audioSrc.value = URL.createObjectURL(audioBlob);
  } catch (err) {
    error.value = '生成语音失败，请重试！';
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};
</script>