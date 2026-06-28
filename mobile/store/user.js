import { defineStore } from 'pinia';
import * as authService from '@/service/auth.service';
import { getStoredUser, isSessionValid } from '@/service/auth.service';
import { hasPermission, hasRole } from '@/utils/auth';

export const useUserStore = defineStore('user', {
  state: () => ({
    user: null,
    initialized: false
  }),
  getters: {
    isLoggedIn: state => !!state.user && isSessionValid(),
    displayName: state => state.user?.displayName || state.user?.userName || '用户',
    permissions: state => state.user?.permissions || [],
    menus: state => state.user?.menus || []
  },
  actions: {
    hydrateFromStorage() {
      if (isSessionValid()) {
        this.user = getStoredUser();
      } else {
        this.user = null;
      }
      this.initialized = true;
    },
    async login(payload) {
      const response = await authService.login(payload);
      this.user = response.user;
      return response;
    },
    async refreshProfile() {
      const user = await authService.refreshCurrentUser();
      this.user = user;
      return user;
    },
    logout() {
      authService.logout();
      this.user = null;
    },
    hasPermission(code) {
      return hasPermission(this.user, code);
    },
    hasRole(code) {
      return hasRole(this.user, code);
    }
  }
});
