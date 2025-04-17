<!-- filepath: e:\work\QmtdltTools\web\qmtdlt_tools_vue\src\components\HighlightedText.vue -->
<template>
    <p>
      <span>{{ beforeText }}</span>
      <span class="highlight">{{ highlightedText }}</span>
      <span>{{ afterText }}</span>
    </p>
  </template>
  
  <script setup lang="ts">
  import { computed, defineProps } from 'vue';
  
  const props = defineProps<{
    fullText: string;
    highlightText: string;
  }>();
  
  const beforeText = computed(() => {
    const index = props.fullText.indexOf(props.highlightText);
    return index > -1 ? props.fullText.substring(0, index) : props.fullText;
  });
  
  const highlightedText = computed(() => {
    const index = props.fullText.indexOf(props.highlightText);
    return index > -1 ? props.highlightText : '';
  });
  
  const afterText = computed(() => {
    const index = props.fullText.indexOf(props.highlightText);
    if (index === -1) {
      return '';
    }
    const startIndex = index + props.highlightText.length;
    return props.fullText.substring(startIndex);
  });
  </script>
  
  <style scoped>
  .highlight {
    background-color: yellow;
    font-weight: bold;
  }
  </style>