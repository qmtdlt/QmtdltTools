<template>
  <div class="library-container">
    <!-- 顶部操作栏 -->
    <div class="header">
      <div class="left-actions">
        <h2>图书库</h2>
        <el-button type="primary" @click="fetchBooks">刷新列表</el-button>
      </div>
      <div class="right-actions">
        <el-button type="primary" @click="dialogVisible = true">
          + 添加图书
        </el-button>
      </div>
    </div>

    <!-- 书籍网格展示区域 -->
    <div class="books-grid" v-loading="loading" element-loading-text="加载中...">
      <el-empty v-if="books.length === 0 && !loading" description="暂无电子书" />
      <div v-else class="grid-container">
        <div class="book-item" v-for="book in books" :key="book.id">
          <img @click="readBook(book)" :src="book.coverImage
            ? 'data:image/jpeg;base64,' + book.coverImage
            : 'default-cover.jpg'" alt="book cover" class="cover" />
          <!-- <div class="title">{{ book.title }}</div> -->
          <div class="actions">
            <el-button size="mini" type="danger" @click="deleteBook(book.id)">删除</el-button>
          </div>
        </div>
      </div>
    </div>

    <!-- 标签区域(可自行调整或删除) -->
    <div class="tag-section">
      <h3>标签</h3>
      <p>这里可以放一些标签筛选、分类等功能</p>
    </div>

    <!-- 上传电子书的弹窗 -->
    <el-dialog v-model="dialogVisible" title="上传电子书" width="30%" :close-on-click-modal="false">
      <div class="upload-section">
        <el-upload class="epub-uploader" :multiple="false" :show-file-list="false" :before-upload="beforeUpload"
          :http-request="customUploadRequest">
          <el-button type="primary">
            <el-icon>
              <Upload />
            </el-icon>
            选择EPUB文件
          </el-button>
        </el-upload>

        <el-progress v-if="uploading" :percentage="uploadProgress" :stroke-width="8" status="success"
          class="upload-progress" />
      </div>

      <template #footer>
        <div class="dialog-footer">
          <el-button @click="dialogVisible = false">关闭</el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Upload } from '@element-plus/icons-vue';
import request from '@/utils/request'; // 自行替换为你的请求工具
import { isMobbile } from '@/utils/myutil'; // 自行替换为你的工具函数
import {BookTypes} from '@/data/BookTypes'; // 自行替换为你的数据文件
const isMobileRef = ref(isMobbile())
interface Book {
  id: string;
  title: string;
  author?: string;
  bookType?: string;
  coverImage?: string;
  createTime?: string;
}

interface ProgressEvent {
  loaded: number;
  total: number;
}

interface UploadOption {
  file: File;
  onProgress?: (progressData: { percent: number }) => void;
}

export default defineComponent({
  name: 'LibraryView',
  components: { Upload },
  setup() {
    const dialogVisible = ref(false);
    const router = useRouter();
    const books = ref<Book[]>([]);
    const loading = ref(false);
    const uploading = ref(false);
    const uploadProgress = ref(0);
    const currentBook = ref<Book | null>(null);

    // 获取书籍列表
    const fetchBooks = async () => {
      loading.value = true;
      try {
        const response = await request.get('/api/EpubManage/GetBooks/GetBooks?booktype=' + BookTypes.Epub);
        // 如果后端返回的数据在 response.data 中，请根据实际返回结构调整
        books.value = response.data || response;
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
            'Content-Type': 'multipart/form-data',
          },
          onUploadProgress: (progressEvent: ProgressEvent) => {
            const percentCompleted = Math.round(
              (progressEvent.loaded * 100) / progressEvent.total
            );
            uploadProgress.value = percentCompleted;
            onProgress?.({ percent: percentCompleted });
          },
        });

        ElMessage.success('电子书上传成功');
        // 刷新书籍列表
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
      if (isMobileRef.value) {
        // 在移动端使用 replace 跳转到 /bookview 并携带查询参数
        router.push({ path: '/mobilebookview', query: { id: book.id, title: book.title } });
      } else {
        // 在桌面端使用 push 跳转到 /bookview 并携带查询参数
        // 使用 path 跳转到 /bookview 并携带查询参数
        router.push({ path: '/bookview', query: { id: book.id, title: book.title } });
      }
      console.log('阅读电子书:', book);
    };

    // 删除电子书
    const deleteBook = (bookId: string): void => {
      ElMessageBox.confirm('确定要删除这本电子书吗？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning',
      })
        .then(async () => {
          try {
            await request.delete(`/api/EpubManage/DeleteBook/DeleteBook?id=${bookId}`);
            ElMessage.success('删除成功');
            fetchBooks();
          } catch (error) {
            console.error('删除失败:', error);
            ElMessage.error('删除失败');
          }
        })
        .catch(() => {
          // 取消删除
        });
    };

    onMounted(() => {
      fetchBooks();
    });

    return {
      books,
      loading,
      uploading,
      uploadProgress,
      dialogVisible,
      currentBook,
      fetchBooks,
      formatDate,
      beforeUpload,
      customUploadRequest,
      readBook,
      deleteBook,
    };
  },
});
</script>

<style scoped>
.library-container {
  padding: 24px;
  background-color: #fff;
}

/* 顶部操作区域 */
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.left-actions,
.right-actions {
  display: flex;
  align-items: center;
  gap: 16px;
}

.left-actions h2 {
  margin: 0;
}

/* 书籍展示区域 */
.books-grid {
  min-height: 200px;
  margin-bottom: 24px;
}

.grid-container {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
}

.book-item {
  width: 120px;
  text-align: center;
  cursor: pointer;
  margin: 20px;
}

/* 封面图片 */
.cover {
  width: 100%;
  height: 180px;
  object-fit: cover;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

/* 书名 */
.title {
  margin-top: 8px;
  font-size: 14px;
  color: #333;
  font-weight: 500;
}

/* 操作按钮 */
.actions {
  margin-top: 8px;
  display: flex;
  justify-content: center;
  gap: 8px;
}

/* 标签区域，可根据需求修改或去掉 */
.tag-section {
  margin-top: 16px;
  padding: 12px;
  background-color: #f9fafc;
  border-radius: 4px;
}

.tag-section h3 {
  margin: 0 0 8px;
}

/* 上传区域对话框内部 */
.upload-section {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 12px;
}

/* 进度条 */
.upload-progress {
  width: 100%;
}

/* 弹窗底部 */
.dialog-footer {
  text-align: right;
}
</style>