<template>
  <div>
    <el-button @click="callHub">test</el-button>
    <p>{{ curText }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useStorage } from '@vueuse/core'
import * as signalR from '@microsoft/signalr'
import { el } from 'element-plus/es/locales.mjs';
import { ElMessage } from 'element-plus';
import { blob } from 'stream/consumers';

var connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5183/signalr-hubs/bookcontent")
  .configureLogging(signalR.LogLevel.Information)
  .build()

const curText = ref("");


connection.on("onShowErrMsg", (msg: string) => {
  console.error(msg);
  ElMessage.error(msg);
});
connection.on("onShowReadingText", (text: string) => {
  curText.value = text;
  // call ms tts read context
  
});


connection.on("onPlayVoiceBuffer", async (arraybuffer: any) => {
  
  // play buffer as audio
  console.log("onPlayVoiceBuffer", arraybuffer);

  var byteArray = new Uint8Array(atob(arraybuffer).split('').map(char => char.charCodeAt(0)));
  
  const audioContext = new AudioContext();
  const audioSource = audioContext.createBufferSource()

  debugger

  audioContext.decodeAudioData(byteArray.buffer, (buffer) => {
    audioSource.buffer = buffer;
    audioSource.connect(audioContext.destination);
    audioSource.start();
  })
});

const callHub = async () => {
  connection.start().then(() => connection.invoke("StartReadTask", "08dd7289-c344-4f77-851f-50dcb08f1049"));        // 开始阅读任务 onShowReadingText 
}
</script>
<style scoped>

</style>