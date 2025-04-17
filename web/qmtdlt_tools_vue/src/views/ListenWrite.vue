<template>
    <div>
      <el-row>
        <el-input v-model="userInput" placeholder="输入听到的内容" style="width: 100%;" clearable :disabled="!targetText"></el-input>
      </el-row>
      <el-row v-if="targetText">
        <div class="feedback-display">
          <span v-for="(item, index) in feedbackCharacters" :key="index" :class="item.class">{{ item.char }}</span>
        </div>
      </el-row>
      <el-row v-if="isComplete">
        <span style="color: green; font-weight: bold;">听写完成!</span>
      </el-row>
    </div>
  </template>
  
  <script setup lang="ts">
  import { ref, computed, watch } from 'vue';
  
  const props = defineProps<{
    targetText: string;
  }>();
  
  const emit = defineEmits(['completed']);
  
  const userInput = ref('');
  
  // Clear input when target text changes
  watch(() => props.targetText, () => {
    userInput.value = '';
  });
  
  const feedbackCharacters = computed(() => {
    const input = userInput.value;
    const target = props.targetText || '';
    return input.split('').map((char, index) => {
      if (index < target.length && char === target[index]) {
        return { char, class: 'correct-char' };
      } else {
        return { char, class: 'incorrect-char' };
      }
    });
  });
  
  const isComplete = computed(() => {
    const complete = userInput.value.length > 0 && userInput.value === props.targetText;
    if (complete) {
      emit('completed');
    }
    return complete;
  });
  </script>
  
  <style scoped>
  .feedback-display {
    width: 100%;
    min-height: 20px;
    padding: 5px;
    border: 1px solid #dcdfe6;
    margin-top: 5px;
    font-size: 1em;
    line-height: 1.5;
    word-wrap: break-word;
    white-space: pre-wrap;
    background-color: #f5f7fa;
  }
  
  .correct-char {
    color: green;
    font-weight: bold;
  }
  
  .incorrect-char {
    color: red;
    font-weight: bold;
  }
  
  .el-row {
    margin-bottom: 10px;
  }
  </style>