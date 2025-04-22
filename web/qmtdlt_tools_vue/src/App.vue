<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { ref, onMounted } from 'vue'

const activeRoute = ref('')

onMounted(() => {
  activeRoute.value = window.location.pathname
})
</script>

<template>
  <div class="container">
    <header class="header">
      <div class="logo">
        <h1>QmtdltTools</h1>
      </div>
      <nav class="main-nav">
        <RouterLink 
          to="/vocabulary" 
          @click="activeRoute = '/vocabulary'"
          :class="{ active: activeRoute === '/vocabulary' }" 
        >Vocabulary</RouterLink>
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
          to="/login" 
          @click="activeRoute = '/login'" 
          :class="{ active: activeRoute === '/login' }"
        >Login</RouterLink>
      </nav>
    </header>
    <main class="content">
      <RouterView class="content-view"/>
    </main>
  </div>
</template>

<style scoped>

 
.content {
  padding: 0rem; /* 原为2rem */
  width: 100%;
  height: 100%;
  box-sizing: border-box;
  background-color: white;
  border-radius: 8px;
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

.content-view {
  animation: fadeIn 0.6s ease-in-out;
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