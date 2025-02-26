<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
</script>

<template>
   <div class="container">
    <div class="topdiv">
      <nav>
        <RouterLink to="/">Home</RouterLink>
        <RouterLink to="/about">About</RouterLink>
        <RouterLink to="/login">Login</RouterLink>
      </nav>
    </div>
      <div class="bottomdiv">
        <RouterView class="contentview"/>
      </div>
   </div>
</template>

<style scoped>
  .container {
  display: flex;
  flex-direction: column;
  height: 100vh;
}

.topdiv {
  background-color: #333;
  color: white;
  background: red;
  height: 100px;
  width: 100%;
  /* 如需要内边距，可以使用 box-sizing: border-box 来保证高度不被额外内边距撑大 */
  box-sizing: border-box;
  padding: 10px;
}

.bottomdiv {
  flex: 1;
  padding: 10px;
  width: 100%;
  box-sizing: border-box;
}
</style>