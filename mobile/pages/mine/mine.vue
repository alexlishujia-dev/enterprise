<template>
  <view class="page">
    <view class="profile-hero" :style="{ paddingTop: statusBarHeight + 24 + 'px' }">
      <u-avatar :src="avatarUrl" :text="avatarText" size="72" bg-color="#1677ff" />
      <text class="profile-hero__name">{{ userStore.displayName }}</text>
      <text class="profile-hero__meta">@{{ userStore.user?.userName }}</text>
      <view class="profile-hero__tags">
        <u-tag
          v-for="role in userStore.user?.roles || []"
          :key="role"
          :text="role"
          type="primary"
          plain
          size="mini" />
      </view>
    </view>

    <view class="page-container">
      <view class="card card--flat">
        <view class="cell flex-row" @click="goUsers">
          <u-icon name="account" color="#1677ff" size="20" />
          <text class="cell__label flex-1">用户管理</text>
          <u-icon name="arrow-right" color="#c0c4cc" size="14" />
        </view>
        <view class="cell flex-row cell--border" @click="goRoles">
          <u-icon name="tags" color="#722ed1" size="20" />
          <text class="cell__label flex-1">角色管理</text>
          <u-icon name="arrow-right" color="#c0c4cc" size="14" />
        </view>
        <view class="cell flex-row" @click="goLogs">
          <u-icon name="file-text" color="#13c2c2" size="20" />
          <text class="cell__label flex-1">操作日志</text>
          <u-icon name="arrow-right" color="#c0c4cc" size="14" />
        </view>
      </view>

      <view class="card card--flat">
        <view class="info-row flex-row">
          <text class="muted">邮箱</text>
          <text class="flex-1 text-ellipsis info-row__value">{{ userStore.user?.email || '-' }}</text>
        </view>
        <view class="info-row flex-row info-row--border">
          <text class="muted">权限数量</text>
          <text>{{ permissionCount }}</text>
        </view>
        <view class="info-row flex-row">
          <text class="muted">菜单数量</text>
          <text>{{ menuCount }}</text>
        </view>
      </view>

      <u-button type="error" plain text="退出登录" custom-style="margin-top: 16rpx;" @click="handleLogout" />
    </view>

    <app-tabbar active="mine" />
  </view>
</template>

<script setup>
import { computed } from 'vue';
import { onShow } from '@dcloudio/uni-app';
import AppTabbar from '@/components/app-tabbar/app-tabbar.vue';
import { useUserStore } from '@/store/user';
import { resolveAssetUrl } from '@/utils/format';
import { getStatusBarHeight } from '@/utils/device';

const userStore = useUserStore();
const statusBarHeight = getStatusBarHeight();

const avatarUrl = computed(() => resolveAssetUrl(userStore.user?.avatarUrl));
const avatarText = computed(() => (userStore.user?.userName || 'U').slice(0, 1).toUpperCase());
const permissionCount = computed(() => userStore.user?.permissions?.length || 0);
const menuCount = computed(() => {
  let count = 0;
  const walk = menus => {
    (menus || []).forEach(m => {
      count += 1;
      if (m.children?.length) walk(m.children);
    });
  };
  walk(userStore.menus);
  return count;
});

onShow(() => {
  if (!userStore.isLoggedIn) {
    uni.reLaunch({ url: '/pages/login/login' });
  }
});

function goUsers() {
  if (!userStore.hasPermission('system.users:view')) {
    uni.showToast({ title: '无权访问', icon: 'none' });
    return;
  }
  uni.navigateTo({ url: '/pages/system/user-list/user-list' });
}

function goRoles() {
  if (!userStore.hasPermission('system.roles:view')) {
    uni.showToast({ title: '无权访问', icon: 'none' });
    return;
  }
  uni.navigateTo({ url: '/pages/system/role-list/role-list' });
}

function goLogs() {
  if (!userStore.hasPermission('system.logs:view')) {
    uni.showToast({ title: '无权访问', icon: 'none' });
    return;
  }
  uni.navigateTo({ url: '/pages/system/log-list/log-list' });
}

function handleLogout() {
  uni.showModal({
    title: '提示',
    content: '确定退出登录吗？',
    success: res => {
      if (res.confirm) {
        userStore.logout();
        uni.reLaunch({ url: '/pages/login/login' });
      }
    }
  });
}
</script>

<style scoped lang="scss">
.profile-hero {
  padding: 48rpx $page-padding 40rpx;
  background: #fff;
  text-align: center;
  border-bottom: 1rpx solid $uni-border-color;

  &__name {
    display: block;
    margin-top: 20rpx;
    font-size: 40rpx;
    font-weight: 600;
    color: $uni-text-color;
  }

  &__meta {
    display: block;
    margin-top: 8rpx;
    font-size: 26rpx;
    color: $uni-text-color-secondary;
  }

  &__tags {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    justify-content: center;
    gap: 12rpx;
    margin-top: 20rpx;
  }
}

.cell {
  padding: 28rpx 0;

  &--border {
    border-top: 1rpx solid $uni-border-color;
    border-bottom: 1rpx solid $uni-border-color;
  }

  &__label {
    margin-left: 20rpx;
    font-size: 30rpx;
    color: $uni-text-color;
  }
}

.info-row {
  padding: 24rpx 0;
  font-size: 28rpx;

  &--border {
    border-top: 1rpx solid $uni-border-color;
    border-bottom: 1rpx solid $uni-border-color;
  }

  &__value {
    text-align: right;
    margin-left: 24rpx;
  }
}
</style>
