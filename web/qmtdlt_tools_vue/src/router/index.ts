import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    //MobileVocabulary
    {
      path:'/vidoes',
      name: 'vidoes',
      component: () => import('../views/MVideo.vue'),
    },
    {
      path:'/mreplay',
      name: 'mreplay',
      component: () => import('../views/MobileReplayExplaination.vue'),
    },
    {
      path:'/mvocabulary',
      name: 'mocabulary',
      component: () => import('../views/MobileVocabulary.vue'),
    },
    {
      path:'/vocabulary',
      name: 'vocabulary',
      component: () => import('../views/Vocabulary.vue'),
    },
    {
      path:'/listenwritelist',
      name: 'listenwrite',
      component: () => import('../views/ListenWriteList.vue'),
    },
    {
      path: '/library',
      name: 'library',
      component: () => import('../views/Library.vue'),
    },
    {
      path: '/',
      name: 'login',
      component: LoginView
    },
    {
      path: '/register',
      name: 'register',
      component: RegisterView
    },
    {
      path: '/bookview',
      name: 'bookview',
      component: () => import('../views/BookReader.vue'),
    },
    {
      path: '/mobilebookview',
      name: 'mobilebookview',
      component: () => import('../views/BookReader.vue'),
    },
  ]
})

// 路由守卫
router.beforeEach((to, from, next) => {
  const isAuthenticated = !!localStorage.getItem('token')
  
  if (to.matched.some(record => record.meta.requiresAuth)) {
    if (!isAuthenticated) {
      next({ path: '/' })
    } else {
      next()
    }
  } else {
    next()
  }
})

export default router
