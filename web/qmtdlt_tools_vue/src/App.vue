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
        <h1>QmtdltTools</h1>
      </div>
      <nav class="main-nav">
        <RouterLink 
          to="/vocabulary" 
          @click="activeRoute = '/vocabulary'"
          :class="{ active: activeRoute === '/vocabulary' }" 
        >Vocabulary</RouterLink>
        <RouterLink 
          to="/listenwritelist" 
          @click="activeRoute = '/listenwritelist'"
          :class="{ active: activeRoute === '/listenwritelist' }" 
        >ListenWritelist</RouterLink>
        <!-- <RouterLink 
          to="/" 
          @click="activeRoute = '/'" 
          :class="{ active: activeRoute === '/' }"
        >DayToDo</RouterLink> -->
        <RouterLink 
          to="/library" 
          @click="activeRoute = '/library'" 
          :class="{ active: activeRoute === '/library' }"
        >Library</RouterLink>
        <RouterLink 
          to="/" 
          @click="activeRoute = '/'" 
          :class="{ active: activeRoute === '/' }"
        >Login</RouterLink>
      </nav>
    </header>
    <main class="content">
      <RouterView class="content-view"/>
    </main>
    <TranslateView ref="transRef" />
  </div>
</template>

<style scoped>

 
.content {
  padding: 0.5rem; /* 原为2rem */
  width: 100%;
  height: calc(100vh - 61px);
  background: red;
  box-sizing: border-box;
  background-color: white;
  border-radius: 0px;
  margin-bottom: 1rem;
}

.header {
  height: 60px;
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: linear-gradient(135deg, #3a7bd5, #3a6073);
  color: white;
  padding: 1rem; /* 原为0 2rem */
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
  
  .content {
    padding: 1rem;
    margin-top: 1rem;
  }
}
</style>