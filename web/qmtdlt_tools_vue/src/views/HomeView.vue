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
    
    <!-- Current Unfinished tasks list -->
    <div class="todo-list">
      <el-card class="todo-card" shadow="hover">
        <template #header>
          <div class="card-header">
            <span>Current Unfinished Tasks</span>
            <el-button type="primary" :loading="loading" @click="fetchTodos" circle>
              <el-icon><Refresh /></el-icon>
            </el-button>
          </div>
        </template>
        
        <el-table
          v-loading="loading"
          :data="todoList"
          style="width: 100%"
          empty-text="No current unfinished tasks. Everything done! 🎉"
        >
          <el-table-column prop="content" label="Task" min-width="200" />
          <el-table-column label="Created" width="180">
            <template #default="scope">
              {{ formatDate(scope.row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column label="Actions" width="180" align="center">
            <template #default="scope">
              <el-tooltip content="Move to non-current" placement="top">
                <el-button 
                  type="warning" 
                  circle 
                  size="small"
                  @click="moveToNonCurrent(scope.row)"
                >
                  <el-icon><ArrowRight /></el-icon>
                </el-button>
              </el-tooltip>
              <el-tooltip content="Mark as complete" placement="top">
                <el-button 
                  type="success" 
                  circle 
                  size="small"
                  @click="markAsComplete(scope.row)"
                >
                  <el-icon><Check /></el-icon>
                </el-button>
              </el-tooltip>
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
    
    <!-- Non-Current Unfinished tasks list -->
    <div class="todo-list">
      <el-card class="todo-card non-current-card" shadow="hover">
        <template #header>
          <div class="card-header">
            <span>Non-Current Unfinished Tasks</span>
            <el-button type="primary" :loading="nonCurrentLoading" @click="fetchNonCurrentTodos" circle>
              <el-icon><Refresh /></el-icon>
            </el-button>
          </div>
        </template>
        
        <el-table
          v-loading="nonCurrentLoading"
          :data="nonCurrentTodoList"
          style="width: 100%"
          empty-text="No non-current unfinished tasks."
        >
          <el-table-column prop="content" label="Task" min-width="200" />
          <el-table-column label="Created" width="180">
            <template #default="scope">
              {{ formatDate(scope.row.createTime) }}
            </template>
          </el-table-column>
          <el-table-column label="Actions" width="120" align="center">
            <template #default="scope">
              <el-tooltip content="Move to current" placement="top">
                <el-button 
                  type="primary" 
                  circle 
                  size="small"
                  @click="moveToCurrent(scope.row)"
                >
                  <el-icon><ArrowLeft /></el-icon>
                </el-button>
              </el-tooltip>
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
    
    <!-- Finished tasks list -->
    <div class="todo-list">
      <el-card class="todo-card finished-card" shadow="hover">
        <template #header>
          <div class="card-header">
            <span>Completed Tasks</span>
            <el-button type="primary" :loading="finishedLoading" @click="fetchFinishedTodos" circle>
              <el-icon><Refresh /></el-icon>
            </el-button>
          </div>
        </template>
        
        <el-table
          v-loading="finishedLoading"
          :data="finishedList"
          style="width: 100%"
          empty-text="No completed tasks yet. Start by completing a task above!"
        >
          <el-table-column prop="content" label="Task" min-width="200" />
          <el-table-column label="Completed" width="180">
            <template #default="scope">
              {{ formatDate(scope.row.finishTime) }}
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

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Check, Delete, Refresh, ArrowRight, ArrowLeft } from '@element-plus/icons-vue'
import request from '@/utils/request' // Import your request utility

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
const finishedList = ref<DayToDo[]>([]) // Add this for finished tasks
const nonCurrentTodoList = ref<DayToDo[]>([]) // Add this for non-current unfinished tasks
const newTodoContent = ref('')
const loading = ref(false)
const finishedLoading = ref(false) // Separate loading state for finished tasks
const nonCurrentLoading = ref(false) // Separate loading state for non-current tasks

// Fetch the list of unfinished todos
const fetchTodos = async () => {
  loading.value = true
  try {
    // Using your request utility instead of axios directly
    const response = await request.get('/api/ToDo/GetCurrentUnFinishedList/GetCurrentUnFinishedList')
    console.log('API Response:', response) // Add this to debug
    
    // Check the structure of the response
    if (response && response.data) {
      todoList.value = response.data
    } else if (Array.isArray(response)) {
      // If the response itself is the array (your utility might unwrap the data)
      todoList.value = response
    } else {
      console.error('Unexpected response format:', response)
      todoList.value = [] // Set empty array as fallback
    }
    
    console.log('Todo list after assignment:', todoList.value)
  } catch (error) {
    console.error('Failed to fetch todos:', error)
    ElMessage.error('Failed to load todo list')
    todoList.value = [] // Ensure we have an empty array on error
  } finally {
    loading.value = false
  }
}

// Fetch the list of non-current unfinished todos
const fetchNonCurrentTodos = async () => {
  nonCurrentLoading.value = true
  try {
    // Using your request utility instead of axios directly
    const response = await request.get('/api/ToDo/GetDayUnFinishedList/GetDayUnFinishedList')
    
    // Check the structure of the response
    if (response && response.data) {
      nonCurrentTodoList.value = response.data
    } else if (Array.isArray(response)) {
      // If the response itself is the array (your utility might unwrap the data)
      nonCurrentTodoList.value = response
    } else {
      console.error('Unexpected response format for non-current tasks:', response)
      nonCurrentTodoList.value = [] // Set empty array as fallback
    }
    
    console.log('Non-current list after assignment:', nonCurrentTodoList.value)
  } catch (error) {
    console.error('Failed to fetch non-current todos:', error)
    ElMessage.error('Failed to load non-current tasks')
    nonCurrentTodoList.value = [] // Ensure we have an empty array on error
  } finally {
    nonCurrentLoading.value = false
  }
}

// Fetch the list of finished todos
const fetchFinishedTodos = async () => {
  finishedLoading.value = true
  try {
    // Using your request utility instead of axios directly
    const response = await request.get('/api/ToDo/GetDayFinishedList/GetDayFinishedList')
    
    // Check the structure of the response
    if (response && response.data) {
      finishedList.value = response.data
    } else if (Array.isArray(response)) {
      // If the response itself is the array (your utility might unwrap the data)
      finishedList.value = response
    } else {
      console.error('Unexpected response format for finished tasks:', response)
      finishedList.value = [] // Set empty array as fallback
    }
    
    console.log('Finished list after assignment:', finishedList.value)
  } catch (error) {
    console.error('Failed to fetch finished todos:', error)
    ElMessage.error('Failed to load finished tasks')
    finishedList.value = [] // Ensure we have an empty array on error
  } finally {
    finishedLoading.value = false
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
    // Using your request utility instead of axios directly
    const response = await request.post('/api/ToDo/AddDayToDoItem/AddDayToDoItem', null, {
      params: {
        content: newTodoContent.value
      }
    })
    
    if (response.data) {
      ElMessage.success('Todo added successfully')
      newTodoContent.value = '' // Clear input
      fetchTodos() // Refresh the current unfinished list
      fetchNonCurrentTodos() // Refresh the non-current unfinished list
    } else {
      ElMessage.error(response.message || 'Failed to add todo')
    }
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
    // Using your request utility instead of axios directly
    await request.post('/api/ToDo/DeleteDayToDoItem/DeleteDayToDoItem', null, {
      params: {
        Id: todo.id
      }
    })
    
    ElMessage.success('Todo deleted successfully')
    fetchTodos() // Refresh the unfinished list
    fetchFinishedTodos() // Also refresh the finished list in case a completed task was deleted
    fetchNonCurrentTodos() // Also refresh the non-current list in case a non-current task was deleted
  } catch (error) {
    if (error !== 'cancel') {
      console.error('Failed to delete todo:', error)
      ElMessage.error('Failed to delete todo')
    }
  } finally {
    loading.value = false
  }
}

// Mark todo as complete
const markAsComplete = async (todo: DayToDo) => {
  loading.value = true
  try {
    // Using your request utility instead of axios directly
    await request.post('/api/ToDo/MarkAsComplete/MarkAsComplete', null, {
      params: {
        Id: todo.id,
        IsFinish: true,
        FinishTime: new Date().toISOString()
      }
    })
    
    ElMessage.success('Todo completed successfully')
    fetchTodos() // Refresh the unfinished list
    fetchFinishedTodos() // Also refresh the finished list to show the newly completed item
  } catch (error) {
    console.error('Failed to mark todo as complete:', error)
    ElMessage.error('Failed to mark todo as complete')
  } finally {
    loading.value = false
  }
}

// Move todo to non-current list
const moveToNonCurrent = async (todo: DayToDo) => {
  loading.value = true
  try {
    // Using your request utility instead of axios directly
    await request.post('/api/ToDo/SetItemOutCurrent/SetItemOutCurrent', null, {
      params: {
        Id: todo.id,
        InCurrent: false
      }
    })
    
    ElMessage.success('Todo moved to non-current list successfully')
    fetchTodos() // Refresh the current unfinished list
    fetchNonCurrentTodos() // Refresh the non-current unfinished list
  } catch (error) {
    console.error('Failed to move todo to non-current list:', error)
    ElMessage.error('Failed to move todo to non-current list')
  } finally {
    loading.value = false
  }
}

// Move todo to current list
const moveToCurrent = async (todo: DayToDo) => {
  nonCurrentLoading.value = true
  try {
    // Using your request utility instead of axios directly
    await request.post('/api/ToDo/SetItemInCurrent/SetItemInCurrent', null, {
      params: {
        Id: todo.id,
        InCurrent: true
      }
    })
    
    ElMessage.success('Todo moved to current list successfully')
    fetchTodos() // Refresh the current unfinished list
    fetchNonCurrentTodos() // Refresh the non-current unfinished list
  } catch (error) {
    console.error('Failed to move todo to current list:', error)
    ElMessage.error('Failed to move todo to current list')
  } finally {
    nonCurrentLoading.value = false
  }
}

// Format date for display
const formatDate = (dateString: string | null) => {
  if (!dateString) return '-'
  return new Date(dateString).toLocaleString()
}

// Load todos when the component is mounted
onMounted(() => {
  fetchTodos()
  fetchFinishedTodos()
  fetchNonCurrentTodos()
})
</script>


<style scoped>
.todo-container {
  width: 100%; /* 添加此行 */
  margin: 0 auto; /* 保留，实际对width: 100%无影响 */
  padding: 0.2rem 0.2rem;
}

.add-todo-form {
  display: flex;
  gap: 1rem;
  margin-bottom: 0.8rem;
}

.todo-input {
  flex-grow: 1;
}

.todo-card {
  margin-bottom: 0.5rem;
}

.finished-card {
  margin-top: 0.5rem;
  border-top: 1px solid #ebeef5;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

h1 {
  color: #409EFF;
  margin-bottom: 1rem;
  text-align: center;
  font-weight: 600;
}

.el-table {
  --el-table-header-bg-color: #f0f7ff;
}

.finished-card .el-table {
  --el-table-header-bg-color: #f5f7fa;
  color: #909399;
}

/* Style for completed tasks */
.finished-card .el-table .el-table__row {
  color: #909399;
}
</style>