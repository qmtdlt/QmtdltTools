import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
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
    // {
    //   path: '/',
    //   name: 'home',
    //   component: HomeView,
    //   meta: { requiresAuth: true }
    // },
    {
      path: '/library',
      name: 'library',
      // route level code-splitting
      // this generates a separate chunk (About.[hash].js) for this route
      // which is lazy-loaded when the route is visited.
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
      // route level code-splitting
      // this generates a separate chunk (About.[hash].js) for this route
      // which is lazy-loaded when the route is visited.
      component: () => import('../views/BookReader.vue'),
    },
    {
      path: '/mobilebookview',
      name: 'mobilebookview',
      // route level code-splitting
      // this generates a separate chunk (About.[hash].js) for this route
      // which is lazy-loaded when the route is visited.
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
