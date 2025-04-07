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
});


const callHub = async () => {
  connection.start()
    .then(() => connection.invoke("StartReadTask", "08dd7289-c344-4f77-851f-50dcb08f1049"));
}
</script>
<style scoped>

</style>