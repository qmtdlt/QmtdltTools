<script setup lang="ts">
import { ref, onMounted } from 'vue'
import axios from 'axios'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Check, Delete, Refresh } from '@element-plus/icons-vue'

// Type definition for Todo items based on your Swagger schema
interface DayToDo {
  id: string
  createTime: string | null
  createBy: string | null
  updateTime: string | null
  updateBy: string | null
  content: string | null
  isFinish: boolean | null
  finishTime: string | null
}

const todoList = ref<DayToDo[]>([])
const newTodoContent = ref('')
const loading = ref(false)
const apiBaseUrl = import.meta.env.VITE_API_URL || ''

// Fetch the list of unfinished todos
const fetchTodos = async () => {
  loading.value = true
  try {
    const response = await axios.get(`${apiBaseUrl}/api/ToDo/GetDayUnFinishedList/GetDayUnFinishedList`)
    todoList.value = response.data
  } catch (error) {
    console.error('Failed to fetch todos:', error)
    ElMessage.error('Failed to load todo list')
  } finally {
    loading.value = false
  }
}

// Add a new todo item
const addTodo = async () => {
  if (!newTodoContent.value.trim()) {
    ElMessage.warning('Please enter a todo item')
    return
  }
  
  loading.value = true
  try {
    await axios.post(`${apiBaseUrl}/api/ToDo/AddDayToDoItem/AddDayToDoItem`, null, {
      params: {
        content: newTodoContent.value
      }
    })
    
    ElMessage.success('Todo added successfully')
    newTodoContent.value = '' // Clear input
    fetchTodos() // Refresh the list
  } catch (error) {
    console.error('Failed to add todo:', error)
    ElMessage.error('Failed to add todo')
  } finally {
    loading.value = false
  }
}

// Delete a todo item
const deleteTodo = async (todo: DayToDo) => {
  try {
    await ElMessageBox.confirm(
      `Are you sure you want to delete "${todo.content}"?`,
      'Delete Todo',
      {
        confirmButtonText: 'Delete',
        cancelButtonText: 'Cancel',
        type: 'warning'
      }
    )
    
    loading.value = true
    await axios.post(`${apiBaseUrl}/api/ToDo/DeleteDayToDoItem/DeleteDayToDoItem`, null, {
      params: {
        Id: todo.id
      }
    })
    
    ElMessage.success('Todo deleted successfully')
    fetchTodos() // Refresh the list
  } catch (error) {
    if (error !== 'cancel') {
      console.error('Failed to delete todo:', error)
      ElMessage.error('Failed to delete todo')
    }
  } finally {
    loading.value = false
  }
}

// Format date for display
const formatDate = (dateString: string | null) => {
  if (!dateString) return '-'
  return new Date(dateString).toLocaleString()
}

// Load todos when the component is mounted
onMounted(fetchTodos)
</script>

<template>
  <div class="todo-container">
    <h1>Daily Tasks</h1>
    
    <!-- Add new todo form -->
    <div class="add-todo-form">
      <el-input
        v-model="newTodoContent"
        placeholder="What needs to be done?"
        class="todo-input"
        @keyup.enter="addTodo"
      />
      <el-button 
        type="primary" 
        :loading="loading" 
        @click="addTodo"
      >
        Add Todo
      </el-button>
    </div>
    
    <!-- Todo list -->
    <div class="todo-list">
      <el-card class="todo-card" shadow="hover">
        <template #header>
          <div class="card-header">
            <span>Unfinished Tasks</span>
            <el-button type="primary" :loading="loading" @click="fetchTodos" circle>
              <el-icon><Refresh /></el-icon>
            </el-button>
          </div>
        </template>
        
        <el-table
          v-loading="loading"
          :data="todoList"
          style="width: 100%"
          empty-text="No tasks yet. Add your first task above!"
        >
          <el-table-column prop="content" label="Task" min-width="200" />
          <el-table-column label="Created" width="180">
            <template #default="scope">
              {{ formatDate(scope.row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column label="Actions" width="120" align="center">
            <template #default="scope">
              <el-tooltip content="Delete" placement="top">
                <el-button 
                  type="danger" 
                  circle 
                  size="small"
                  @click="deleteTodo(scope.row)"
                >
                  <el-icon><Delete /></el-icon>
                </el-button>
              </el-tooltip>
            </template>
          </el-table-column>
        </el-table>
      </el-card>
    </div>
  </div>
</template>

<style scoped>
.todo-container {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem 1rem;
}

.add-todo-form {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
}

.todo-input {
  flex-grow: 1;
}

.todo-card {
  margin-bottom: 1rem;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

h1 {
  color: #409EFF;
  margin-bottom: 2rem;
  text-align: center;
  font-weight: 600;
}

.el-table {
  --el-table-header-bg-color: #f0f7ff;
}
</style>