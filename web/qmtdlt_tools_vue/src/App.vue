<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { ref, onMounted } from 'vue'
import { isMobbile } from './utils/myutil'
import TranslateView from './views/TranslateView.vue'

const transRef = ref<InstanceType<typeof TranslateView> | null>(null); // 引用 ListenWrite 组件实例

const isMobileRef = ref(isMobbile())
const activeRoute = ref('')

onMounted(() => {
  activeRoute.value = window.location.pathname
})

const handleSelection = () => {
  const selection = window.getSelection()
  if (selection && selection.rangeCount > 0) {
    const selectedText = selection.toString().trim()
    if (selectedText) {
      // 取消选中状态
      selection.removeAllRanges()

      // Emit the selected text to the parent component
      transRef.value?.handlePhaseSelect(selectedText); // 调用子组件的方法
    }
  }
}
</script>

<template>
  <div class="container" @mouseup="handleSelection">
    <header class="header" >
      <div class="logo" v-if="!isMobileRef">
        <h1>YoungForYou</h1>
      </div>
      <nav class="main-nav">
        
        <RouterLink v-if="isMobileRef"
          to="/mvocabulary" 
          @click="activeRoute = '/mvocabulary'"
          :class="{ active: activeRoute === '/mvocabulary' }" 
        >Vocabulary</RouterLink>

        
        <RouterLink v-if="isMobileRef"
          to="/mreplay" 
          @click="activeRoute = '/mreplay'"
          :class="{ active: activeRoute === '/mreplay' }" 
        >Replay</RouterLink>

        <RouterLink v-if="!isMobileRef"
          to="/vocabulary" 
          @click="activeRoute = '/vocabulary'"
          :class="{ active: activeRoute === '/vocabulary' }" 
        >Vocabulary</RouterLink>
        
        <RouterLink v-if="!isMobileRef"
          to="/library" 
          @click="activeRoute = '/library'" 
          :class="{ active: activeRoute === '/library' }"
        >Library</RouterLink>

        
        <RouterLink v-if="false"
          to="/listenwritelist" 
          @click="activeRoute = '/listenwritelist'"
          :class="{ active: activeRoute === '/listenwritelist' }" 
        >Records</RouterLink>
      </nav>
    </header>
    <div class="content">
      <RouterView class="content-view"/>
    </div>
    <TranslateView ref="transRef" />
  </div>
</template>

<style scoped>
.container{
  height: calc(100vh - 60px); /* 原为100vh */
  width: 100%;
}
 
.content {
  width: 100%;
  height: 100%;
  box-sizing: border-box;
  border-radius: 0px;
  margin-bottom: 1rem;
}
.content-view{
  width: 100%;
  height: 100%;
  box-sizing: border-box;
  animation: fadeIn 0.5s ease-in-out; /* 添加淡入动画 */
}
.header {
  height: 60px;
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: linear-gradient(135deg, #3a7bd5, #3a6073);
  color: white;
  position: sticky;
  top: 0;
  z-index: 100;
  box-sizing: border-box;
}

.logo h1 {
  margin-left: 1rem;
  font-size: 1.6rem;
  font-weight: 600;
}

.main-nav {
  display: flex;
  gap: 2rem; /* Increased gap for better spacing */
}

.main-nav a {
  color: rgba(255, 255, 255, 0.8);
  text-decoration: none;
  font-size: 1.1rem;
  font-weight: 500;
  padding: 0.5rem 0.75rem;
  border-radius: 4px;
  transition: all 0.3s ease;
}

.main-nav a:hover, .main-nav a.active {
  color: white;
  background-color: rgba(255, 255, 255, 0.15);
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

@media (max-width: 768px) {
  .header {
    flex-direction: column;
    height: auto;
    padding: 1rem;
    gap: 1rem;
  }
}
</style>