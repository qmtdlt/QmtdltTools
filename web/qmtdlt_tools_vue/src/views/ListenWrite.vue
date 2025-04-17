<template>
  <div class="listenwrite-wrapper">
    <el-row>
      <el-input
        v-model="userInput"
        placeholder="输入听到的内容"
        style="width: 100%;"
        clearable
        :disabled="!targetText"
        size="large"
        class="listenwrite-input"
      ></el-input>
    </el-row>
    <el-row v-if="targetText">
      <div class="feedback-display">
        <span v-for="(item, index) in feedbackWords" :key="index" :class="item.class">{{ item.word }}</span>
      </div>
    </el-row>
    <el-row v-if="isComplete">
      <span class="listenwrite-success">听写完成!</span>
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
.listenwrite-wrapper {
  background: #f9fafc;
  border-radius: 12px;
  box-shadow: 0 2px 12px 0 rgba(0,0,0,0.06);
  padding: 28px 18px 18px 18px;
  margin: 0 auto;
  max-width: 600px;
  width: 100%;
  min-height: 120px;
  display: flex;
  flex-direction: column;
  align-items: stretch;
  justify-content: flex-start;
}

.listenwrite-input {
  margin-bottom: 16px;
}

.feedback-display {
  width: 100%;
  min-height: 28px;
  padding: 8px 10px;
  border: 1px solid #e3e6eb;
  margin-top: 5px;
  font-size: 1.08em;
  line-height: 1.8;
  word-wrap: break-word;
  white-space: pre-wrap;
  background-color: #f5f7fa;
  border-radius: 8px;
  box-sizing: border-box;
  margin-bottom: 8px;
  letter-spacing: 1px;
}

.correct-word {
  color: #21b97a;
  font-weight: bold;
  background: #eafaf1;
  border-radius: 4px;
  padding: 0 2px;
  margin-right: 2px;
  transition: background 0.2s;
}

.incorrect-word {
  color: #e74c3c;
  font-weight: bold;
  background: #fff0f0;
  border-radius: 4px;
  padding: 0 2px;
  margin-right: 2px;
  transition: background 0.2s;
}

.feedback-display span {
  white-space: pre-wrap;
}

.listenwrite-success {
  color: #21b97a;
  font-weight: bold;
  font-size: 1.1em;
  padding: 6px 0 0 0;
  display: inline-block;
  letter-spacing: 2px;
}

.el-row {
  margin-bottom: 0;
}

@media (max-width: 600px) {
  .listenwrite-wrapper {
    padding: 12px 4px 10px 4px;
    max-width: 100vw;
  }
  .feedback-display {
    font-size: 1em;
    padding: 6px 4px;
  }
}
</style>