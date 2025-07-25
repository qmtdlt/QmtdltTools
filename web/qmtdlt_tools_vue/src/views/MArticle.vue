// src/views/MVideo.vue

<template>
  <div class="m-article-container">
    <h2 class="title">中英文文章互译</h2>
    <textarea
      v-model="inputText"
      class="input-area"
      placeholder="请输入一段中文文章..."
      rows="8"
    ></textarea>
    <button class="translate-btn" :disabled="loading || !inputText.trim()" @click="translateArticle">
      {{ loading ? '翻译中...' : '翻译为英文' }}
    </button>
    <div v-if="outputText" class="output-area">
      <h3 class="output-title">英文版本：</h3>
      <div class="output-text">{{ outputText }}</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import request from '@/utils/request';

const inputText = ref('');
const outputText = ref('');
const loading = ref(false);

const translateArticle = async () => {
  if (!inputText.value.trim()) return;
  loading.value = true;
  outputText.value = '';
  try {
    // 直接用 request.post(url, data)，data 为字符串或对象
    const res = await request.post('/api/Article/GetEnglishArticle?chineseArticle=' + inputText.value);
    outputText.value = res;
  } catch (err) {
    outputText.value = '翻译失败，请稍后重试。';
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.m-article-container {
  width: 98%;
  margin: 32px auto;
  padding: 24px 16px;
  background: #fff;
  border-radius: 16px;
  box-shadow: 0 2px 16px rgba(64,158,255,0.08);
  display: flex;
  flex-direction: column;
  align-items: stretch;
}
.title {
  text-align: center;
  font-size: 1.5em;
  color: #409EFF;
  margin-bottom: 18px;
  font-weight: bold;
}
.input-area {
  width: 100%;
  border: 1px solid #dbeafe;
  border-radius: 8px;
  padding: 12px;
  font-size: 1.1em;
  margin-bottom: 18px;
  resize: vertical;
  box-sizing: border-box;
  background: #f7fafd;
  color: #222;
}
.translate-btn {
  background: #409EFF;
  color: #fff;
  border: none;
  border-radius: 8px;
  padding: 12px 0;
  font-size: 1.1em;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s;
  margin-bottom: 22px;
}
.translate-btn:disabled {
  background: #b3d8fd;
  cursor: not-allowed;
}
.output-area {
  background: #f7fafd;
  border-radius: 8px;
  padding: 18px 12px;
  box-shadow: 0 2px 8px rgba(64,158,255,0.04);
}
.output-title {
  font-size: 1.15em;
  color: #409EFF;
  margin-bottom: 10px;
  font-weight: bold;
}
.output-text {
  font-size: 1.1em;
  color: #222;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>