// src/views/MVideo.vue

<template>
  <h1>MVideo</h1>
    <RealtimeCaption
      video-url="https://www.youtube.com/watch?v=xmeVhetSzxg" 
      language="en-US"
    />
    </template>
  
  <script setup lang="ts">
  import RealtimeCaption from '@/components/RealtimeCaption.vue';
  </script>