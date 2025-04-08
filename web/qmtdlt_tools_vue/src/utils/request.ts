import axios from 'axios'
import type { AxiosResponse, InternalAxiosRequestConfig } from 'axios'
import { ElMessage } from 'element-plus'

// Define types for our request configurations
type RequestConfig = {
  headers?: Record<string, string>
  params?: any
  timeout?: number
  [key: string]: any
}

// Create axios instance with base URL from environment variables
const service = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '',
  timeout: 10000, // 10 seconds
  headers: {
    'Content-Type': 'application/json'
  }
})

// Request interceptor
service.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // You can add token handling here if needed
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    console.error('Request error:', error)
    return Promise.reject(error)
  }
)

// Response interceptor
service.interceptors.response.use(
  (response) => {
    return response
  },
  (error) => {
    console.error('Response error:', error)
    
    // Handle different error scenarios
    let message = 'Unknown error occurred'
    
    if (error.response) {
      const status = error.response.status
      
      switch (status) {
        case 400:
          message = 'Bad request'
          break
        case 401:
          message = 'Unauthorized, please login'
          // You might want to redirect to login page here
          break
        case 403:
          message = 'Access forbidden'
          break
        case 404:
          message = 'Resource not found'
          break
        case 500:
          message = 'Internal server error'
          break
        default:
          message = `Error: ${status}`
      }
    } else if (error.request) {
      message = 'Network error - no response received'
    } else {
      message = error.message
    }
    
    ElMessage.error(message)
    return Promise.reject(error)
  }
)

// Helper methods for common request types
const request = {
  get<T = any>(url: string, config?: RequestConfig): Promise<T> {
    return service.get(url, config).then(responseBody<T>)
  },
  
  post<T = any>(url: string, data?: any, config?: RequestConfig): Promise<T> {
    return service.post(url, data, config).then(responseBody<T>)
  },
  
  put<T = any>(url: string, data?: any, config?: RequestConfig): Promise<T> {
    return service.put(url, data, config).then(responseBody<T>)
  },
  
  delete<T = any>(url: string, config?: RequestConfig): Promise<T> {
    return service.delete(url, config).then(responseBody<T>)
  },
  
  // Helper for posting with query parameters (useful for your API)
  postWithParams<T = any>(url: string, params?: any): Promise<T> {
    return service.post(url, null, { params }).then(responseBody<T>)
  }
}

// Helper to extract response data
function responseBody<T>(response: AxiosResponse<T>): T {
  return response.data
}

export default request