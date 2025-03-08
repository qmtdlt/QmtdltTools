<template>
  <div class="library-container">
    <div class="upload-section">
      <h2>上传电子书</h2>
      <el-upload
        class="epub-uploader"
        :multiple="false"
        :show-file-list="false"
        :before-upload="beforeUpload"
        :http-request="customUploadRequest"
      >
        <el-button type="primary">
          <el-icon><Upload /></el-icon>
          选择EPUB文件
        </el-button>
      </el-upload>
      <el-progress 
        v-if="uploading" 
        :percentage="uploadProgress" 
        :stroke-width="8"
        status="success"
        class="upload-progress"
      />
    </div>
    <el-divider />

    <div class="books-section">
      <h2>电子书库</h2>
      <el-button type="primary" @click="fetchBooks" style="margin-bottom: 16px">
        刷新列表
      </el-button>
      
      <div v-loading="loading" element-loading-text="加载中...">
        <el-empty v-if="books.length === 0 && !loading" description="暂无电子书" />
        <el-row :gutter="16" v-else>
          <el-col :xs="24" :sm="12" :md="8" :lg="6" v-for="book in books" :key="book.id" class="book-item">
            <el-card shadow="hover">
              <div class="book-cover">
                <img
                  alt="book cover"
                  :src="book.coverImage ? `data:image/jpeg;base64,${book.coverImage}` : 'default-cover.jpg'"
                  style="height: 200px; object-fit: cover; width: 100%"
                />
              </div>
              <div class="book-info">
                <h3>{{ book.title }}</h3>
                <p>作者: {{ book.author || '未知' }}</p>
                <p>上传时间: {{ formatDate(book.createTime) }}</p>
              </div>
              <div class="book-actions">
                <el-button type="primary" @click="readBook(book)">阅读</el-button>
                <el-button type="danger" @click="deleteBook(book.id)">删除</el-button>
              </div>
            </el-card>
          </el-col>
        </el-row>
      </div>
    </div>
    <el-dialog
    v-model="dialogVisible"
    fullscreen
    top="40vh"
    width="70%"
    draggable
  >
    <span>It's a fullscreen Dialog</span>
    <template #footer>
      <div class="dialog-footer">
        <el-button @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" @click="dialogVisible = false">
          Confirm
        </el-button>
      </div>
    </template>
  </el-dialog>
  </div>
  
</template>

<script lang="ts">
import { Upload } from '@element-plus/icons-vue';
import { ElMessage, ElMessageBox } from 'element-plus';
import request from '@/utils/request'; // 使用request工具
import { defineComponent, ref, onMounted } from 'vue';
import { useRouter } from 'vue-router'; // 导入 router

// 定义书籍数据的接口
interface Book {
  id: string;
  title: string;
  author?: string;
  coverImage?: string;
  createTime?: string;
  // 添加其他可能的属性
}

// 定义上传进度事件的接口
interface ProgressEvent {
  loaded: number;
  total: number;
}

// 定义上传选项的接口
interface UploadOption {
  file: File;
  onProgress?: (progressData: { percent: number }) => void;
}

export default defineComponent({
  name: 'LibraryView',
  components: {
    Upload
  },
  setup() {
    const dialogVisible = ref(false);
    const router = useRouter(); // 初始化 router
    const books = ref<Book[]>([]); // 指定类型为 Book 数组
    const loading = ref<boolean>(false);
    const uploading = ref<boolean>(false);
    const uploadProgress = ref<number>(0);
    const currentBook = ref<Book | null>(null);

    // 获取所有书籍
    const fetchBooks = async () => {
      loading.value = true;
      try {
        const response = await request.get('/api/EpubManage/GetBooks/GetBooks');
        books.value = response.data || response; // 兼容不同格式的返回
      } catch (error) {
        console.error('获取书籍失败:', error);
        ElMessage.error('获取书籍列表失败');
      } finally {
        loading.value = false;
      }
    };

    // 格式化日期
    const formatDate = (dateString: string | undefined): string => {
      if (!dateString) return '未知';
      const date = new Date(dateString);
      return date.toLocaleString();
    };

    // 上传前验证
    const beforeUpload = (file: File): boolean => {
      const isEpub = file.name.toLowerCase().endsWith('.epub');
      if (!isEpub) {
        ElMessage.error('请上传EPUB格式的文件');
        return false;
      }
      const isLt100M = file.size / 1024 / 1024 < 100;
      if (!isLt100M) {
        ElMessage.error('文件大小不能超过100MB');
        return false;
      }
      return true;
    };

    // 自定义上传请求
    const customUploadRequest = async (options: UploadOption): Promise<void> => {
      const { file, onProgress } = options;
      uploading.value = true;
      uploadProgress.value = 0;
      
      const formData = new FormData();
      formData.append('file', file);
      
      try {
        await request.post('/api/EpubManage/UploadEpub/UploadEpub', formData, {
          headers: {
            'Content-Type': 'multipart/form-data'
          },
          onUploadProgress: (progressEvent: ProgressEvent) => {
            const percentCompleted = Math.round(
              (progressEvent.loaded * 100) / progressEvent.total
            );
            uploadProgress.value = percentCompleted;
            if (onProgress) onProgress({ percent: percentCompleted });
          }
        });
        
        ElMessage.success('电子书上传成功');
        // 上传成功后刷新书籍列表
        fetchBooks();
      } catch (error) {
        console.error('上传失败:', error);
        ElMessage.error('电子书上传失败');
      } finally {
        uploading.value = false;
      }
    };

    // 阅读电子书
    const readBook = (book: Book): void => {
      currentBook.value = book;
      dialogVisible.value = true;
      console.log('阅读电子书:', book);
      console.log('对话框状态:', dialogVisible.value);
    };

    // 删除电子书
    const deleteBook = (bookId: string): void => {
      ElMessageBox.confirm('确定要删除这本电子书吗？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(async () => {
        try {
          // 假设有删除API，需要根据实际情况实现
         await request.delete(`/api/EpubManage/DeleteBook/${bookId}`);
          ElMessage.success('删除成功');
          fetchBooks(); // 刷新列表
        } catch (error) {
          console.error('删除失败:', error);
          ElMessage.error('删除失败');
        }
      }).catch(() => {
        // 取消删除操作
      });
    };

    // 组件挂载时获取书籍列表
    onMounted(() => {
      fetchBooks();
    });

    return {
      books,
      loading,
      uploading,
      uploadProgress,
      dialogVisible, // 返回dialogVisible变量
      currentBook,   // 返回当前选中的书籍
      fetchBooks,
      formatDate,
      beforeUpload,
      customUploadRequest,
      readBook,
      deleteBook
    };
  }
});
</script>