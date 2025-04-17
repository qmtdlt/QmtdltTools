<template>
  <div>
    <el-row>
      <el-input v-model="userInput" placeholder="输入听到的内容" style="width: 100%;" clearable :disabled="!targetText"></el-input>
    </el-row>
    <el-row v-if="targetText">
      <div class="feedback-display">
        <span v-for="(item, index) in feedbackWords" :key="index" :class="item.class">{{ item.word }}</span>
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

// 清除输入内容当目标文本变化时
watch(() => props.targetText, () => {
  userInput.value = '';
});

// 归一化并分词（忽略标点和空格，转小写）
function normalizeAndTokenize(text: string): string[] {
  if (!text) return [];
  const normalized = text.toLowerCase().replace(/[.,!?;:()"'\[\]{}，。！？；：“”‘’、]/g, '');
  return normalized.split(/\s+/).filter(word => word.length > 0);
}

// 反馈每个单词的正确与否
const feedbackWords = computed(() => {
  const inputWordsRaw = userInput.value.split(/\s+/);
  const inputWordsNormalized = normalizeAndTokenize(userInput.value);
  const targetWordsNormalized = normalizeAndTokenize(props.targetText);

  return inputWordsRaw.map((rawWord, index) => {
    if (index < inputWordsNormalized.length) {
      const normalizedWord = inputWordsNormalized[index];
      if (index < targetWordsNormalized.length && normalizedWord === targetWordsNormalized[index]) {
        return { word: rawWord + ' ', class: 'correct-word' };
      } else {
        return { word: rawWord + ' ', class: 'incorrect-word' };
      }
    } else {
      return { word: rawWord + ' ', class: 'incorrect-word' };
    }
  }).filter(item => item.word.trim().length > 0);
});

// 判断是否全部单词拼写正确
const isComplete = computed(() => {
  const inputWords = normalizeAndTokenize(userInput.value);
  const targetWords = normalizeAndTokenize(props.targetText);

  if (inputWords.length === 0 || targetWords.length === 0) return false;
  if (inputWords.length !== targetWords.length) return false;

  const complete = inputWords.every((word, idx) => word === targetWords[idx]);
  if (complete) emit('completed');
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

.correct-word {
  color: green;
  font-weight: bold;
}

.incorrect-word {
  color: red;
  font-weight: bold;
}

.feedback-display span {
  white-space: pre-wrap;
}

.el-row {
  margin-bottom: 10px;
}
</style>