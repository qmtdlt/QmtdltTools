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
          to="/" 
          @click="activeRoute = '/'" 
          :class="{ active: activeRoute === '/' }"
        >Day ToDo</RouterLink>
        <RouterLink 
          to="/about" 
          @click="activeRoute = '/about'" 
          :class="{ active: activeRoute === '/about' }"
        >About</RouterLink>
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
.container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  background-color: #f8f9fa;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: linear-gradient(135deg, #3a7bd5, #3a6073);
  color: white;
  padding: 0 2rem;
  height: 80px;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 100;
}

.logo h1 {
  font-size: 1.6rem;
  font-weight: 600;
  margin: 0;
}

.main-nav {
  display: flex;
  gap: 1.5rem;
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

.content {
  flex: 1;
  padding: 2rem;
  max-width: 1200px;
  width: 100%;
  margin: 0 auto;
  box-sizing: border-box;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 2px 15px rgba(0, 0, 0, 0.05);
  margin-top: 1.5rem;
  margin-bottom: 1.5rem;
}

.content-view {
  animation: fadeIn 0.3s ease-in-out;
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