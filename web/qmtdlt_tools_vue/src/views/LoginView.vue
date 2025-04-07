<template>
  <div>
    <el-button @click="callHub">test</el-button>
    <p style="color: black;">{{ curText }}</p>
    <span style="color: black;">当前阅读位置: {{ curPosition }}</span>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useStorage } from '@vueuse/core'
import * as signalR from '@microsoft/signalr'
import { el } from 'element-plus/es/locales.mjs';
import { ElMessage } from 'element-plus';
import { blob } from 'stream/consumers';
import { connect } from 'http2';

const bookId = ref("08dd75df-7d91-42b1-8c6b-0e8bec20e66a") // 从查询参数中获取 id

var connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5183/signalr-hubs/bookcontent")
  .configureLogging(signalR.LogLevel.Information)
  .build()

const callHub = async () => {
  connection.start().then(() => connection.invoke("InitCache",bookId.value));        // 开始阅读任务 onShowReadingText 
}

const curText = ref("");
const curPosition = ref(0);
const bookPosition = ref(0);


connection.on("onShowErrMsg", (msg: string) => {
  console.error(msg);
  ElMessage.error(msg);
});

connection.on("UIReadInfo", (readContent: any) => {
  debugger
  curText.value = readContent.text; // 读取到的文本内容
  curPosition.value = readContent.position; // 读取到的文本位置
  readBase64(readContent.buffer); // 读取到的音频内容
  // call ms tts read context
});


const readBase64 = (base64string:string)=>{
  var byteArray = new Uint8Array(atob(base64string).split('').map(char => char.charCodeAt(0)));  
  const audioContext = new AudioContext();
  const audioSource = audioContext.createBufferSource();
  audioContext.decodeAudioData(byteArray.buffer, (buffer) => {
    audioSource.buffer = buffer;
    audioSource.connect(audioContext.destination);
    audioSource.onended = () => {
      bookPosition.value += 1; // 读取下一个位置
      connection.invoke("Read", bookId.value,bookPosition.value);        // 
    };
    audioSource.start();
  })
}

connection.on("onSetBookPosition", (pos: number) => {
  // 设置书籍位置
  bookPosition.value = pos;
  connection.invoke("Read", bookId.value,bookPosition.value);        // 
});


const onChapterReadFinished = ()=>{
  // 章节阅读完成，调用 SignalR Hub 的 onChapterReadFinished 方法
  connection.invoke("bookGoNext", bookId.value);        // 
  console.log("章节阅读完成");
};



</script>
<style scoped>

</style>