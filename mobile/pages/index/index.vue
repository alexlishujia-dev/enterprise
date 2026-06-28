<template>
  <view class="page">
    <view class="hero" :style="{ paddingTop: statusBarHeight + 12 + 'px' }">
      <view class="hero__main flex-row">
        <u-avatar :src="avatarUrl" :text="avatarText" size="56" bg-color="#1677ff" />
        <view class="hero__info flex-1">
          <text class="hero__greet">你好，{{ userStore.displayName }}</text>
          <text class="hero__role">{{ roleText }}</text>
        </view>
      </view>
      <view class="hero__stats flex-row">
        <view class="stat-item flex-1">
          <text class="stat-item__value">{{ menuCount }}</text>
          <text class="stat-item__label">可用菜单</text>
        </view>
        <view class="stat-divider" />
        <view class="stat-item flex-1">
          <text class="stat-item__value">{{ permissionCount }}</text>
          <text class="stat-item__label">权限项</text>
        </view>
        <view class="stat-divider" />
        <view class="stat-item flex-1">
          <text class="stat-item__value">{{ roleCount }}</text>
          <text class="stat-item__label">角色</text>
        </view>
      </view>
    </view>

    <view class="page-container">
      <text class="section-title">快捷入口</text>
      <view class="quick-grid">
        <view
          v-for="item in quickMenus"
          :key="item.path"
          class="quick-grid__item"
          @click="goPage(item.path)">
          <view class="quick-grid__icon" :style="{ background: item.bg }">
            <u-icon :name="item.icon" color="#fff" size="22" />
          </view>
          <text class="quick-grid__name">{{ item.name }}</text>
        </view>
      </view>

      <text class="section-title">系统菜单</text>
      <view class="card card--flat">
        <view v-if="flatMenus.length">
          <view
            v-for="(menu, index) in flatMenus"
            :key="menu.id"
            class="menu-row flex-row"
            :class="{ 'menu-row--border': index < flatMenus.length - 1 }"
            @click="openMenu(menu)">
            <view class="menu-row__icon">
              <u-icon name="grid" color="#1677ff" size="18" />
            </view>
            <view class="menu-row__body flex-1">
              <text class="menu-row__title text-ellipsis">{{ menu.menuName }}</text>
              <text class="menu-row__path muted">{{ menu.path || menu.menuCode }}</text>
            </view>
            <u-icon name="arrow-right" color="#c0c4cc" size="14" />
          </view>
        </view>
        <empty-view v-else text="暂无可用菜单" />
      </view>
    </view>

    <app-tabbar active="index" />
  </view>
</template>

<script setup>
import { computed } from 'vue';
import { onShow } from '@dcloudio/uni-app';
import AppTabbar from '@/components/app-tabbar/app-tabbar.vue';
import EmptyView from '@/components/empty-view/empty-view.vue';
import { useUserStore } from '@/store/user';
import { resolveAssetUrl } from '@/utils/format';
import { getStatusBarHeight } from '@/utils/device';

const userStore = useUserStore();
const statusBarHeight = getStatusBarHeight();

const avatarUrl = computed(() => resolveAssetUrl(userStore.user?.avatarUrl));
const avatarText = computed(() => (userStore.user?.userName || 'U').slice(0, 1).toUpperCase());
const roleText = computed(() => (userStore.user?.roles || []).join('、') || '未分配角色');
const menuCount = computed(() => flatMenus.value.length);
const permissionCount = computed(() => userStore.user?.permissions?.length || 0);
const roleCount = computed(() => userStore.user?.roles?.length || 0);

const quickMenus = computed(() => {
  const items = [
    { name: '用户管理', path: '/pages/system/user-list/user-list', icon: 'account', bg: 'linear-gradient(135deg,#1677ff,#69b1ff)', permission: 'system.users:view' },
    { name: '角色管理', path: '/pages/system/role-list/role-list', icon: 'tags', bg: 'linear-gradient(135deg,#722ed1,#b37feb)', permission: 'system.roles:view' },
    { name: '操作日志', path: '/pages/system/log-list/log-list', icon: 'file-text', bg: 'linear-gradient(135deg,#13c2c2,#5cdbd3)', permission: 'system.logs:view' },
    { name: '个人中心', path: '/pages/mine/mine', icon: 'setting', bg: 'linear-gradient(135deg,#fa8c16,#ffc069)', permission: null }
  ];
  return items.filter(item => !item.permission || userStore.hasPermission(item.permission));
});

const flatMenus = computed(() => {
  const result = [];
  const walk = menus => {
    (menus || []).forEach(menu => {
      if (menu.path) result.push(menu);
      if (menu.children?.length) walk(menu.children);
    });
  };
  walk(userStore.menus);
  return result;
});

onShow(() => {
  if (!userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/login/login' });
  }
});

function goPage(path) {
  if (path.includes('/mine/mine')) {
    uni.reLaunch({ url: path });
    return;
  }
  uni.navigateTo({ url: path });
}

function openMenu(menu) {
  const path = menu.path || '';
  if (path.includes('/system/users')) {
    uni.navigateTo({ url: '/pages/system/user-list/user-list' });
    return;
  }
  if (path.includes('/system/roles')) {
    uni.navigateTo({ url: '/pages/system/role-list/role-list' });
    return;
  }
  if (path.includes('/system/logs')) {
    uni.navigateTo({ url: '/pages/system/log-list/log-list' });
    return;
  }
  if (path.includes('/system/role-permissions')) {
    uni.showToast({ title: '角色权限请在 PC 端配置', icon: 'none' });
    return;
  }
  uni.showToast({ title: '移动端页面开发中', icon: 'none' });
}
</script>

<style scoped lang="scss">
.hero {
  padding: 32rpx $page-padding 28rpx;
  padding-top: 32rpx;
  background: linear-gradient(135deg, #1677ff 0%, #0958d9 100%);
  color: #fff;
  border-radius: 0 0 32rpx 32rpx;

  &__main {
    margin-bottom: 28rpx;
  }

  &__info {
    margin-left: 24rpx;
  }

  &__greet {
    display: block;
    font-size: 36rpx;
    font-weight: 600;
    line-height: 1.4;
  }

  &__role {
    display: block;
    margin-top: 8rpx;
    font-size: 24rpx;
    opacity: 0.88;
    line-height: 1.4;
  }

  &__stats {
    padding: 24rpx;
    background: rgba(255, 255, 255, 0.14);
    border-radius: 20rpx;
  }
}

.stat-item {
  text-align: center;

  &__value {
    display: block;
    font-size: 36rpx;
    font-weight: 700;
    line-height: 1.2;
  }

  &__label {
    display: block;
    margin-top: 8rpx;
    font-size: 22rpx;
    opacity: 0.85;
  }
}

.stat-divider {
  width: 1rpx;
  height: 56rpx;
  background: rgba(255, 255, 255, 0.25);
}

.quick-grid {
  display: flex;
  flex-direction: row;
  flex-wrap: wrap;
  margin: 0 -10rpx 32rpx;

  &__item {
    width: 25%;
    padding: 0 10rpx 20rpx;
    box-sizing: border-box;
    text-align: center;
  }

  &__icon {
    width: 96rpx;
    height: 96rpx;
    margin: 0 auto 12rpx;
    border-radius: 24rpx;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  &__name {
    display: block;
    font-size: 24rpx;
    color: $uni-text-color;
  }
}

.menu-row {
  padding: 24rpx 0;

  &--border {
    border-bottom: 1rpx solid $uni-border-color;
  }

  &__icon {
    width: 64rpx;
    height: 64rpx;
    margin-right: 20rpx;
    border-radius: 16rpx;
    background: #f0f5ff;
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
  }

  &__title {
    display: block;
    font-size: 30rpx;
    color: $uni-text-color;
    font-weight: 500;
  }

  &__path {
    display: block;
    margin-top: 6rpx;
    font-size: 22rpx;
  }
}
</style>
