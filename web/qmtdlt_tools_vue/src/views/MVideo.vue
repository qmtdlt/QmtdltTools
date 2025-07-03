<template>
    <RealtimeCaption
      video-url="https://www.youtube.com/watch?v=your_video_id"
      azure-key="你的Azure订阅Key"
      azure-region="你的Azure订阅Region"
      language="en-US"
    />
  </template>
  
  <script setup lang="ts">
  import RealtimeCaption from '@/components/RealtimeCaption.vue';
  </script>
  