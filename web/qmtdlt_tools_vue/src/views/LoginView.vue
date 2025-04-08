<template>
  <div class="login-container">
    <el-form :model="form" class="login-form" ref="formRef" label-width="80px">
      <el-form-item label="用户名" prop="username">
        <el-input v-model="form.username"></el-input>
      </el-form-item>
      <el-form-item label="密码" prop="password">
        <el-input type="password" v-model="form.password"></el-input>
      </el-form-item>
      <el-input v-model="token"></el-input>
      <el-button type="primary" @click="handleLogin">登录</el-button>
    </el-form>
    
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useStorage } from '@vueuse/core'
import request from '@/utils/request'

const form = ref({
  username: 'user',
  password: 'password'
})

const token = ref("");
const handleLogin = async () => {
  let param = {
    "username":  form.value.username,
    "password": form.value.password
  }
  if (!form.value.username || !form.value.password) {
    alert('请输入用户名和密码')
    return
  }
  const response = await request.post(
    '/api/Auth/Login/login', param
  );
  token.value = response.token;
  // 将 token 存储到 localStorage 中
  localStorage.setItem('token', token.value);
}
</script>
<style scoped>

</style>