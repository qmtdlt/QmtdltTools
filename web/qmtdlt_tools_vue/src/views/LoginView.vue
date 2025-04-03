<template>
  <div>
    <el-button @click="callHub">test</el-button>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useStorage } from '@vueuse/core'
import * as signalR from '@microsoft/signalr'

var connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5183/signalr-hubs/bookcontent")
  .configureLogging(signalR.LogLevel.Information)
  .build()


connection.on("onReceiveFromSignalRHub", (data: string) => {
  console.log(data);
});


const callHub = async () => {
  console.log("callHub");
  debugger
  connection.start()
    .then(() => connection.invoke("SendMessage", "HelloFromVue"));
}
</script>
<style scoped>

</style>