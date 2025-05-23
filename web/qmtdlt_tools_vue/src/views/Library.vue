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
          + 添加电子书
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
          <div class="actions">
            <el-button size="mini" type="danger" @click="deleteBook(book.id)">删除</el-button>
          </div>
          <!-- <div class="title" @click="readBook(book)" style="cursor:pointer;">{{ book.title }}</div> -->
        </div>
      </div>
    </div>

    <!-- TXT区域 -->
    <div class="txt-section">
      <div class="txt-header">
        <h3>TXT文档</h3>
        <el-button type="primary" @click="txtDialogVisible = true" size="small">上传TXT</el-button>
        <el-button type="primary" @click="fetchTxtBooks" size="small">刷新TXT列表</el-button>
      </div>
      <div class="txt-grid" v-loading="txtLoading" element-loading-text="加载中...">
        <el-empty v-if="txtBooks.length === 0 && !txtLoading" description="暂无TXT文档" />
        <div v-else class="txt-grid-container">
          <div class="txt-item" v-for="book in txtBooks" :key="book.id">
            <div class="txt-title" @click="readBook(book)" style="cursor:pointer;">{{ book.title }}</div>
            <div class="actions">
              <el-button size="mini" type="danger" @click="deleteBook(book.id)">删除</el-button>
            </div>
          </div>
        </div>
      </div>
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

    <!-- 上传TXT的弹窗 -->
    <el-dialog v-model="txtDialogVisible" title="上传TXT文档" width="30%" :close-on-click-modal="false">
      <div class="upload-section">
        <el-upload class="txt-uploader" :multiple="false" :show-file-list="false" :before-upload="beforeTxtUpload"
          :http-request="customTxtUploadRequest">
          <el-button type="primary">
            <el-icon>
              <Upload />
            </el-icon>
            选择TXT文件
          </el-button>
        </el-upload>
        <el-progress v-if="txtUploading" :percentage="txtUploadProgress" :stroke-width="8" status="success"
          class="upload-progress" />
      </div>
      <template #footer>
        <div class="dialog-footer">
          <el-button @click="txtDialogVisible = false">关闭</el-button>
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
import request from '@/utils/request';
import { isMobbile } from '@/utils/myutil';
import { BookTypes } from '@/data/BookTypes';
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
    const txtDialogVisible = ref(false);
    const router = useRouter();
    const books = ref<Book[]>([]);
    const txtBooks = ref<Book[]>([]);
    const loading = ref(false);
    const txtLoading = ref(false);
    const uploading = ref(false);
    const txtUploading = ref(false);
    const uploadProgress = ref(0);
    const txtUploadProgress = ref(0);
    const currentBook = ref<Book | null>(null);

    // 获取epub书籍列表
    const fetchBooks = async () => {
      loading.value = true;
      try {
        const response = await request.get('/api/EpubManage/GetBooks/GetBooks?booktype=' + BookTypes.Epub);
        books.value = response.data || response;
      } catch (error) {
        console.error('获取书籍失败:', error);
        ElMessage.error('获取书籍列表失败');
      } finally {
        loading.value = false;
      }
    };

    // 获取txt书籍列表
    const fetchTxtBooks = async () => {
      txtLoading.value = true;
      try {
        const response = await request.get('/api/EpubManage/GetBooks/GetBooks?booktype=' + BookTypes.Txt);
        txtBooks.value = response.data || response;
      } catch (error) {
        console.error('获取TXT失败:', error);
        ElMessage.error('获取TXT列表失败');
      } finally {
        txtLoading.value = false;
      }
    };

    // 上传前验证epub
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

    // 上传前验证txt
    const beforeTxtUpload = (file: File): boolean => {
      const isTxt = file.name.toLowerCase().endsWith('.txt');
      if (!isTxt) {
        ElMessage.error('请上传TXT格式的文件');
        return false;
      }
      const isLt100M = file.size / 1024 / 1024 < 100;
      if (!isLt100M) {
        ElMessage.error('文件大小不能超过100MB');
        return false;
      }
      return true;
    };

    // 自定义上传epub
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
        fetchBooks();
      } catch (error) {
        console.error('上传失败:', error);
        ElMessage.error('电子书上传失败');
      } finally {
        uploading.value = false;
      }
    };

    // 自定义上传txt
    const customTxtUploadRequest = async (options: UploadOption): Promise<void> => {
      const { file, onProgress } = options;
      txtUploading.value = true;
      txtUploadProgress.value = 0;

      const formData = new FormData();
      formData.append('file', file);

      try {
        await request.post('/api/EpubManage/UploadTxt/UploadTxt', formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
          onUploadProgress: (progressEvent: ProgressEvent) => {
            const percentCompleted = Math.round(
              (progressEvent.loaded * 100) / progressEvent.total
            );
            txtUploadProgress.value = percentCompleted;
            onProgress?.({ percent: percentCompleted });
          },
        });

        ElMessage.success('TXT上传成功');
        fetchTxtBooks();
      } catch (error) {
        console.error('TXT上传失败:', error);
        ElMessage.error('TXT上传失败');
      } finally {
        txtUploading.value = false;
      }
    };

    // 阅读电子书或TXT
    const readBook = (book: Book): void => {
      currentBook.value = book;
      if (isMobileRef.value) {
        router.push({ path: '/mobilebookview', query: { id: book.id, title: book.title } });
      } else {
        router.push({ path: '/bookview', query: { id: book.id, title: book.title } });
      }
      console.log('阅读:', book);
    };

    // 删除电子书或TXT
    const deleteBook = (bookId: string): void => {
      ElMessageBox.confirm('确定要删除这本书吗？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning',
      })
        .then(async () => {
          try {
            await request.delete(`/api/EpubManage/DeleteBook/DeleteBook?id=${bookId}`);
            ElMessage.success('删除成功');
            fetchBooks();
            fetchTxtBooks();
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
      fetchTxtBooks();
    });

    return {
      books,
      txtBooks,
      loading,
      txtLoading,
      uploading,
      txtUploading,
      uploadProgress,
      txtUploadProgress,
      dialogVisible,
      txtDialogVisible,
      currentBook,
      fetchBooks,
      fetchTxtBooks,
      beforeUpload,
      beforeTxtUpload,
      customUploadRequest,
      customTxtUploadRequest,
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

.cover {
  width: 100%;
  height: 180px;
  object-fit: cover;
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.title {
  margin-top: 8px;
  font-size: 14px;
  color: #333;
  font-weight: 500;
  cursor: pointer;
}

/* TXT区域样式 */
.txt-section {
  margin-top: 32px;
  background: #f9fafc;
  border-radius: 6px;
  padding: 18px 12px 12px 12px;
}
.txt-header {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 16px;
}
.txt-grid {
  min-height: 120px;
}
.txt-grid-container {
  display: flex;
  flex-wrap: wrap;
  gap: 16px;
}
.txt-item {
  width: 180px;
  background: #fff;
  border-radius: 4px;
  box-shadow: 0 1px 4px rgba(0,0,0,0.04);
  padding: 16px 8px 8px 8px;
  margin-bottom: 8px;
  display: flex;
  flex-direction: column;
  align-items: center;
}
.txt-title {
  font-size: 15px;
  color: #409EFF;
  font-weight: bold;
  margin-bottom: 10px;
  cursor: pointer;
  text-align: center;
  word-break: break-all;
}
.actions {
  margin-top: 8px;
  display: flex;
  justify-content: center;
  gap: 8px;
}

/* 标签区域 */
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