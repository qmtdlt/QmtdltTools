<template>
  <div class="login-container">
    <h2 class="title">登录</h2>
    <el-form :model="form" class="login-form" ref="formRef" label-width="80px" :rules="rules">
      <el-form-item label="用户名" prop="username">
        <el-input v-model="form.username" placeholder="请输入用户名"></el-input>
      </el-form-item>
      <el-form-item label="密码" prop="password">
        <el-input type="password" v-model="form.password" placeholder="请输入密码" @keyup.enter="handleLogin"></el-input>
      </el-form-item>
      <div class="button-group">
        <el-button type="primary" @click="handleLogin" :loading="loading">登录</el-button>
        <el-button @click="goToRegister">注册新账号</el-button>
      </div>
    </el-form>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import request from '@/utils/request'

const router = useRouter()
const loading = ref(false)

const form = ref({
  username: '',
  password: ''
})

const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

const formRef = ref()

const handleLogin = async () => {
  if (!form.value.username || !form.value.password) {
    ElMessage.warning('请输入用户名和密码')
    return
  }

  try {
    loading.value = true
    const param = {
      username: form.value.username,
      password: form.value.password
    }
    
    const response = await request.post('/api/Auth/Login/login', param)
    const token = response.token
    
    // 将 token 存储到 localStorage 中
    localStorage.setItem('token', token)
    
    ElMessage.success('登录成功')
    // 登录成功后跳转到首页
    router.push('/vocabulary')
  } catch (error) {
    console.error(error)
    ElMessage.error('登录失败')
  } finally {
    loading.value = false
  }
}

const goToRegister = () => {
  router.push('/register')
}
</script>

<style scoped>
.login-container {
  width: 380px;
  margin: 100px auto;
  padding: 20px;
  border-radius: 5px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.title {
  text-align: center;
  margin-bottom: 20px;
}

.button-group {
  display: flex;
  justify-content: space-between;
  margin-top: 20px;
}
</style>